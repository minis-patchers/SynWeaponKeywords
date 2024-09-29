using System.Data;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Json;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Newtonsoft.Json;
using Noggog;
using WeaponKeywords.Types;

namespace WeaponKeywords;

public class Program
{
    static readonly JsonSerializerSettings settings = new();
    public static async Task<int> Main(string[] args)
    {
        settings.AddMutagenConverters();
        return await SynthesisPipeline.Instance
            .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
            .SetTypicalOpen(GameRelease.SkyrimSE, "SynWeaponKeywords.esp")
            .Run(args);
    }
    public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
    {
        var packages = Directory.EnumerateDirectories($"{state.DataFolderPath}/SynWeaponKeywords");
        foreach (var package in packages)
        {
            List<WeaponKeywordInfo> weaponDB = new();
            if(!File.Exists($"{package}/index.json")) continue;
            var pck = File.ReadAllText($"{package}/index.json");
            var Settings = JsonConvert.DeserializeObject<WeaponKeywordPackage>(pck, settings);
            foreach (var kyd in Directory.EnumerateFiles(package))
            {
                if (kyd == "index.json") continue;
                var data = File.ReadAllText(kyd);
                var weap = JsonConvert.DeserializeObject<WeaponKeywordInfo>(data, settings)!;
                weaponDB.Add(weap);
            }
            Console.WriteLine($"Running weapon keyword package {Settings.Name} ({Settings.Description})");
            Dictionary<string, List<IKeywordGetter>> formkeys = new();
            var Keywords = weaponDB.SelectMany(x => x.keyword).Distinct();
            foreach (var kyd in weaponDB.Select(x => x.name))
            {
                formkeys[kyd] = new List<IKeywordGetter>();
            }
            foreach (var src in Settings.sources)
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
                        var type = weaponDB.Where(x => x.keyword.Contains(keyword.EditorID ?? "")).Select(x => x.name);
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
                var matchingKeywords = weaponDB
                    .Where(kv => kv.commonNames.Any(cn => weapon.Name?.String?.Contains(cn, StringComparison.OrdinalIgnoreCase) ?? false))
                    .Where(kv => kv.validEquipType == Consts.equipTable[weapon.EquipmentType.FormKey])
                    .Where(kv => !kv.excludeNames.Any(en => weapon.Name?.String?.Contains(en, StringComparison.OrdinalIgnoreCase) ?? false))
                    .Where(kv => !kv.exclude.Contains(weapon.FormKey))
                    .Where(kv => !Settings.excludes.phrases.Any(ph => weapon.Name?.String?.Contains(ph, StringComparison.OrdinalIgnoreCase) ?? false))
                    .Where(kv => !Settings.excludes.weapons.Contains(weapon.FormKey))
                    .Select(kv => kv.name)
                    .Concat(weaponDB.Where(x => x.include.Contains(weapon.FormKey)).Select(x => x.name))
                    .Distinct()
                    .ToHashSet();

                IWeapon? nw = null;
                if (matchingKeywords.Count > 0)
                {
                    Console.WriteLine($"{edid} - {weapon.FormKey.IDString()}:{weapon.FormKey.ModKey} matches: {string.Join(",", matchingKeywords)}");
                    Console.WriteLine($"\t{weapon.Name}: {weapon.EditorID} is {string.Join(" & ", weaponDB.Where(x => matchingKeywords.Contains(x.name)).Select(x => x.outputDescription))}");
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
}
