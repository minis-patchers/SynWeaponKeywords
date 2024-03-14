using System.Data;
using System.Diagnostics;
using System.Security.Policy;
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
    static Database Data => LazyDB.Value;
    static readonly JsonSerializerSettings Serializer = new();
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
        ProcessStartInfo PatchProc = new()
        {
            CreateNoWindow = true,
            Arguments = $"\"{state.DataFolderPath}\"",
            FileName = Path.Combine(state.ExtraSettingsDataPath!, "patchman.exe"),
            WorkingDirectory = state.ExtraSettingsDataPath!,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        using var HttpClient = new HttpClient();
        HttpClient.Timeout = TimeSpan.FromSeconds(20);
        string resp = string.Empty;
        if (!File.Exists(Path.Combine(state.ExtraSettingsDataPath!, "patchman.exe")))
        {
            Console.Out.WriteLine("Downloading the latest release of patchman for database patching");
            var task = HttpClient.GetByteArrayAsync("https://github.com/Minizbot2012/mzjd-rs/releases/latest/download/patchman.exe");
            task.Wait();
            File.WriteAllBytes(Path.Combine(state.ExtraSettingsDataPath!, "patchman.exe"), task.Result);
        }
        var proc = Process.Start(PatchProc);
        proc?.Start();
        Console.Out.WriteLine(proc?.StandardOutput.ReadToEnd());
        proc?.WaitForExit();
    }
    public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
    {
        Console.WriteLine($"Running with Database Version: V{Data.DBVer}");
        Dictionary<string, List<IKeywordGetter>> formkeys = new();
        var Keywords = Data.DB.SelectMany(x => x.Value.keyword).Distinct();
        foreach (var kyd in Data.DB.Select(x => x.Key))
        {
            formkeys[kyd] = new List<IKeywordGetter>();
        }
        foreach (var src in Data.sources)
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
                    var type = Data.DB.Where(x => x.Value.keyword.Contains(keyword.EditorID ?? "")).Select(x => x.Key);
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
            var matchingKeywords = Data.DB
                .Where(kv => kv.Value.commonNames.Any(cn => weapon.Name?.String?.Contains(cn, StringComparison.OrdinalIgnoreCase) ?? false))
                .Where(kv => kv.Value.validEquipType == DBConst.equipTable[weapon.EquipmentType.FormKey])
                .Where(kv => !kv.Value.excludeNames.Any(en => weapon.Name?.String?.Contains(en, StringComparison.OrdinalIgnoreCase) ?? false))
                .Where(kv => !kv.Value.exclude.Contains(weapon.FormKey))
                .Where(kv => !Data.excludes.phrases.Any(ph => weapon.Name?.String?.Contains(ph, StringComparison.OrdinalIgnoreCase) ?? false))
                .Where(kv => !Data.excludes.weapons.Contains(weapon.FormKey))
                .Select(kv => kv.Key)
                .Concat(Data.DB.Where(x => x.Value.include.Contains(weapon.FormKey)).Select(x => x.Key))
                .Distinct()
                .ToHashSet();

            IWeapon? nw = null;
            if (matchingKeywords.Count > 0)
            {
                Console.WriteLine($"{edid} - {weapon.FormKey.IDString()}:{weapon.FormKey.ModKey} matches: {string.Join(",", matchingKeywords)}");
                Console.WriteLine($"\t{weapon.Name}: {weapon.EditorID} is {string.Join(" & ", Data.DB.Where(x => matchingKeywords.Contains(x.Key)).Select(x => x.Value.outputDescription))}");
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
