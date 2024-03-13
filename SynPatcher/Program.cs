using System.Data;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Json;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Noggog;
using WeaponKeywords.Types;

namespace WeaponKeywords;

public class Program
{
    static Lazy<Database> LazyDB = new();
    static readonly JsonSerializerSettings Serializer = new();
    static readonly ProcessStartInfo PatchProc = new()
    {
        CreateNoWindow = true,
        RedirectStandardInput = true,
        RedirectStandardError = true,
        RedirectStandardOutput = true,
        FileName = "jd.exe",
        Arguments = "-o database.json -p patch.json database.json"
    };
    public static async Task<int> Main(string[] args)
    {
        Serializer.AddMutagenConverters();
        return await SynthesisPipeline.Instance
            .SetAutogeneratedSettings("Database", "database.json", out LazyDB)
            .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
            .AddRunnabilityCheck(ConvertJson)
            .SetTypicalOpen(GameRelease.SkyrimSE, "SynWeaponKeywords.esp")
            .Run(args);
    }
    public static void ConvertJson(IRunnabilityState state)
    {
        PatchProc.WorkingDirectory = state.ExtraSettingsDataPath;
        JObject? DBConv = new();
        if (File.Exists(Path.Combine(state.ExtraSettingsDataPath!, "database.json")))
        {
            DBConv = JObject.Parse(File.ReadAllText(Path.Combine(state.ExtraSettingsDataPath!, "database.json")));
        }
        if ((DBConv["DBVer"]?.Value<int>() ?? -1) <= 0)
        {
            DBConv = new()
            {
                ["DBVer"] = 0,
                ["DoUpdates"] = true,
                ["UpdateLocation"] = DBConv["UpdateLocation"]?.Value<string>() ?? DBConst.DEFAULT_UPDATE_LOCATION,
                ["Marker"] = DBConv["Marker"]?.Value<string>() ?? DBConst.DEFAULT_UPDATE_LOCATION,
            };
            File.WriteAllText(Path.Combine(state.ExtraSettingsDataPath!, "database.json"), DBConv.ToString(Formatting.Indented));
        }
        if (DBConv["DoUpdates"]?.Value<bool>() ?? true)
        {
            using var HttpClient = new HttpClient();
            HttpClient.Timeout = TimeSpan.FromSeconds(20);
            string resp = string.Empty;
            if (!File.Exists(Path.Combine(state.ExtraSettingsDataPath!, "jd.exe")))
            {
                Console.Out.WriteLine("Downloading the latest release of JD for DB patching");
                var task = HttpClient.GetByteArrayAsync("https://github.com/josephburnett/jd/releases/latest/download/jd-amd64-windows.exe");
                task.Wait();
                File.WriteAllBytes(Path.Combine(state.ExtraSettingsDataPath!, "jd.exe"), task.Result);
            }
            try
            {
                var http = HttpClient.GetStringAsync(DBConv["UpdateLocation"]?.Value<string>() ?? DBConst.DEFAULT_UPDATE_LOCATION);
                http.Wait();
                resp = http.Result;
            }
            catch (Exception)
            {
                Console.WriteLine("Failed to download patch index");
                return;
            }

            var pi = JObject.Parse(resp).ToObject<UpdateServer>()!;
            if ((DBConv["Marker"]?.Value<string>() ?? "none") != pi.marker)
            {
                Console.WriteLine("MARKER CHANGE - Clearing DB");
                DBConv = new()
                {
                    ["DBVer"] = 0,
                    ["DoUpdates"] = true,
                    ["UpdateLocation"] = DBConv["UpdateLocation"]?.Value<string>() ?? DBConst.DEFAULT_UPDATE_LOCATION,
                    ["Marker"] = pi.marker
                };
                File.WriteAllText(Path.Combine(state.ExtraSettingsDataPath!, "database.json"), DBConv.ToString(Formatting.Indented));
            }
            var cver = DBConv["DBVer"]?.Value<int>() ?? 0;
            var bver = JObject.Parse(File.ReadAllText(Path.Combine(state.ExtraSettingsDataPath!, "database.bak.json")))["DBVer"]?.Value<int>() ?? -1;
            if (File.Exists(Path.Combine(state.ExtraSettingsDataPath!, "database.bak.json")) && bver == cver)
            {
                File.Copy(Path.Combine(state.ExtraSettingsDataPath!, "database.bak.json"), Path.Combine(state.ExtraSettingsDataPath!, "database.json"), true);
            }
            for (var i = cver; i < pi.index.Count; i++)
            {
                try
                {
                    Console.WriteLine($"Downloading patch {pi.index[i]} from {pi.index[i]}");
                    var http = HttpClient.GetStringAsync(pi.index[i]);
                    http.Wait();
                    resp = http.Result;
                }
                catch (Exception)
                {
                    Console.WriteLine($"Failed to download patch {pi.index[i]}");
                    return;
                }
                File.WriteAllText(Path.Combine(state.ExtraSettingsDataPath!, "patch.json"), resp);
                Process.Start(PatchProc)?.WaitForExit();
                File.Delete(Path.Combine(state.ExtraSettingsDataPath!, "patch.json"));
                File.Copy(Path.Combine(state.ExtraSettingsDataPath!, "database.json"), Path.Combine(state.ExtraSettingsDataPath!, "database.bak.json"), true);
            }
        }
        File.Copy(Path.Combine(state.ExtraSettingsDataPath!, "database.bak.json"), Path.Combine(state.ExtraSettingsDataPath!, "database.json"), true);
    }
    public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
    {
        File.Copy(Path.Combine(state.ExtraSettingsDataPath!, "database.bak.json"), Path.Combine(state.ExtraSettingsDataPath!, "database.json"), true);
        PatchProc.WorkingDirectory = state.ExtraSettingsDataPath;
        var SWK_PATCHES = state.DataFolderPath.EnumerateFiles().Where(x => x.NameWithoutExtension.EndsWith("_SWK"));
        var Customizations = new List<string>();
        foreach (var patch in SWK_PATCHES)
        {
            Customizations.Add(patch.NameWithoutExtension);
            File.Copy(patch.Path, Path.Combine(state.ExtraSettingsDataPath!, "patch.json"));
            Process.Start(PatchProc)?.WaitForExit();
            File.Delete(Path.Combine(state.ExtraSettingsDataPath!, "patch.json"));
        }
        var DBase = JsonConvert.DeserializeObject<Database>(File.ReadAllText(Path.Combine(state.ExtraSettingsDataPath!, "database.json")), Serializer)!;
        Console.WriteLine($"Running with Database Version: V{DBase.DBVer}");
        Console.WriteLine($"Special Customizations: {string.Join(", ", Customizations)}");
        Dictionary<string, List<IKeywordGetter>> formkeys = new();
        var Keywords = DBase.DB.SelectMany(x => x.Value.keyword).Distinct();
        foreach (var kyd in DBase.DB.Select(x => x.Key))
        {
            formkeys[kyd] = new List<IKeywordGetter>();
        }
        foreach (var src in DBase.sources)
        {
            if (!state.LoadOrder.PriorityOrder.Select(x => x.ModKey).Contains(src)) continue;
            state.LoadOrder.TryGetValue(src, out var mod);
            if (mod != null && mod.Mod != null && mod.Mod.Keywords != null)
            {
                var keywords = mod.Mod.Keywords
                    .Where(x => Keywords.Contains(x.EditorID ?? ""))
                    .ToList() ?? new List<IKeywordGetter>();
                foreach (var keyword in keywords)
                {
                    if (keyword == null) continue;
                    var type = DBase.DB.Where(x => x.Value.keyword.Contains(keyword.EditorID ?? "")).Select(x => x.Key);
                    Console.WriteLine($"Keyword : {keyword.FormKey.IDString()}:{keyword.FormKey.ModKey}:{keyword.EditorID}");
                    foreach (var tp in type)
                    {
                        formkeys[tp].Add(keyword);
                    }
                }
            }
        }
        foreach (var weapon in state.LoadOrder.PriorityOrder.Weapon().WinningOverrides())
        {
            if (!weapon.Template.IsNull) continue;
            var edid = weapon.EditorID;
            var matchingKeywords = DBase.DB
                .Where(kv => kv.Value.commonNames.Any(cn => weapon.Name?.String?.Contains(cn, StringComparison.OrdinalIgnoreCase) ?? false))
                .Where(kv => kv.Value.validEquipType == DBConst.equipTable[weapon.EquipmentType.FormKey])
                .Where(kv => !kv.Value.excludeNames.Any(en => weapon.Name?.String?.Contains(en, StringComparison.OrdinalIgnoreCase) ?? false))
                .Where(kv => !kv.Value.exclude.Contains(weapon.FormKey))
                .Where(kv => !DBase.excludes.phrases.Any(ph => weapon.Name?.String?.Contains(ph, StringComparison.OrdinalIgnoreCase) ?? false))
                .Where(kv => !DBase.excludes.weapons.Contains(weapon.FormKey))
                .Select(kv => kv.Key)
                .Concat(DBase.DB.Where(x => x.Value.include.Contains(weapon.FormKey)).Select(x => x.Key))
                .Distinct()
                .ToHashSet();

            IWeapon? nw = null;
            if (matchingKeywords.Count > 0)
            {
                Console.WriteLine($"{edid} - {weapon.FormKey.IDString()}:{weapon.FormKey.ModKey} matches: {string.Join(",", matchingKeywords)}");
                Console.WriteLine($"\t{weapon.Name}: {weapon.EditorID} is {string.Join(" & ", DBase.DB.Where(x => matchingKeywords.Contains(x.Key)).Select(x => x.Value.outputDescription))}");
                var keywords = weapon.Keywords?
                    .Select(x => x.TryResolve(state.LinkCache, out var kyd) ? kyd : null)
                    .Where(x => x != null)
                    .Concat(matchingKeywords.SelectMany(x => formkeys[x]))
                    .Select(x => x!)
                    .DistinctBy(x => x.FormKey)
                    .ToHashSet() ?? new();

                if (keywords.Any(x => !(weapon.Keywords?.Contains(x) ?? false)))
                {
                    nw = nw == null ? state.PatchMod.Weapons.GetOrAddAsOverride(weapon)! : nw!;
                    nw.Keywords = keywords.Select(x => x.ToLinkGetter()).ToExtendedList();
                    Console.WriteLine($"\tSetting keywords to:\n\t\t{string.Join("\n\t\t", keywords.Select(x => $"{x.EditorID} from {x.FormKey.ModKey}"))}");
                }
                var fKeyword = matchingKeywords.First();
            }
        }
    }
}
