using System;
using System.Net.Http;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.FormKeys.SkyrimSE;

using Noggog;

using WeaponKeywords.Types;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;

namespace WeaponKeywords
{
    public class Program
    {
        static Lazy<Database> LazyDB = new();
        static Database DB => LazyDB.Value;
        static List<FormKey> OneHanded = new() { Skyrim.EquipType.EitherHand.FormKey, Skyrim.EquipType.LeftHand.FormKey, Skyrim.EquipType.RightHand.FormKey };
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .SetAutogeneratedSettings("Database", "database.json", out LazyDB)
                .AddRunnabilityCheck(ConvertJson)
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "SynWeaponKeywords.esp")
                .Run(args);
        }
        public static async void ConvertJson(IRunnabilityState state)
        {
            var DBConv = JObject.Parse(File.ReadAllText(Path.Combine("Data", "Skyrim Special Edition", "SynWeaponKeywords", "database.json")));
            if ((DBConv["DBPatchVer"]?.Value<int>() ?? 0) < 0)
            {
                DBConv = new JObject();
            }
            //New Age JSON Patcher // Shiny
            using (var HttpClient = new HttpClient())
            {
                HttpClient.Timeout = TimeSpan.FromSeconds(5);
                string resp = string.Empty;
                try
                {
                    resp = await HttpClient.GetStringAsync("https://raw.githubusercontent.com/minis-patchers/SynDelta/main/SynWeaponKeywords/index.json");
                }
                catch (Exception)
                {
                    Console.WriteLine("Failed to download patch index");
                    return;
                }
                var pi = JArray.Parse(resp).ToObject<List<string>>()!;
                for (int i = DBConv["DBPatchVer"]?.Value<int>() ?? 0; i < pi.Count; i++)
                {
                    try
                    {
                        resp = await HttpClient.GetStringAsync(pi[i]);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine($"Failed to download patch {i} - {pi[i]}");
                        return;
                    }
                    var pch = new JsonPatchDocument(JsonConvert.DeserializeObject<List<Operation>>(resp), new DefaultContractResolver());
                    pch.ApplyTo(DBConv);
                    DBConv["DBPatchVer"] = i + 1;
                    File.WriteAllText(Path.Combine("Data", "Skyrim Special Edition", "SynWeaponKeywords", "database.json"), JsonConvert.SerializeObject(DBConv, Formatting.Indented));
                }
            }
        }
        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            Dictionary<string, List<IKeywordGetter>> formkeys = new();
            var Keywords = DB.DB.SelectMany(x => x.Value.keyword).Distinct();
            foreach (var src in DB.sources)
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
                        var type = DB.DB.Where(x => x.Value.keyword.Contains(keyword.EditorID ?? "")).Select(x => x.Key);
                        Console.WriteLine($"Keyword : {keyword.FormKey.ModKey} : {keyword.EditorID}");
                        foreach (var tp in type)
                        {
                            if (!formkeys.ContainsKey(tp))
                            {
                                formkeys[tp] = new List<IKeywordGetter>();
                            }
                            formkeys[tp].Add(keyword);
                        }
                    }
                }
            }
            foreach (var weapon in state.LoadOrder.PriorityOrder.Weapon().WinningOverrides())
            {
                var edid = weapon.EditorID;
                var matchingKeywords = DB.DB
                    .Where(kv => kv.Value.commonNames.Any(cn => weapon.Name?.String?.Contains(cn, StringComparison.OrdinalIgnoreCase) ?? false))
                    .Where(kv => !kv.Value.exclude.Any(v => weapon.Name?.String?.Contains(v, StringComparison.OrdinalIgnoreCase) ?? false))
                    .Where(kv => !kv.Value.excludeEditID.Contains(edid ?? ""))
                    .Where(kv => !DB.excludes.excludeMod.Contains(weapon.FormKey.ModKey))
                    .Where(kv => !kv.Value.excludeMod.Contains(weapon.FormKey.ModKey))
                    .Select(kv => kv.Key)
                    .Concat(DB.DB.Where(x => x.Value.include.Contains(edid ?? "")).Select(x => x.Key))
                    .ToArray();
                var globalExclude = DB.excludes.phrases
                    .Any(ph => weapon.Name?.String?.Contains(ph, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    DB.excludes.weapons.Contains(edid ?? "");
                var isOneHanded = OneHanded.Any(x => x.Equals(weapon.EquipmentType.FormKey));
                IWeapon? nw = null;
                //Legacy code
                if (matchingKeywords.Length > 0 && !globalExclude)
                {
                    Console.WriteLine($"{edid} - {weapon.FormKey.ModKey} matches: {string.Join(",", matchingKeywords)}:");
                    foreach (var kyd in matchingKeywords)
                    {
                        if (formkeys.ContainsKey(kyd))
                        {
                            Console.WriteLine($"\t{weapon.Name}: {weapon.EditorID} from {weapon.FormKey.ModKey} is {DB.DB[kyd].outputDescription}");
                            foreach (var keyform in formkeys[kyd])
                            {
                                if (DB.DB[kyd].excludeSource.Contains(keyform.FormKey.ModKey)) continue;
                                if (!weapon.Keywords?.Select(x => x.FormKey.ModKey).Contains(keyform.FormKey.ModKey) ?? false)
                                {
                                    nw = nw == null ? state.PatchMod.Weapons.GetOrAddAsOverride(weapon)! : nw!;
                                    nw.Keywords?.Add(keyform);
                                    Console.WriteLine($"\t\tAdded keyword {keyform.EditorID} from {keyform.FormKey.ModKey}");
                                }
                            }
                        }
                    }
                    if (weapon.Data != null)
                    {
                        var fKeyword = matchingKeywords.First();
                        if (!DB.DB[fKeyword].IgnoreWATOverrides.Contains(weapon.FormKey.ModKey))
                        {
                            WeaponAnimationType OneHanded = DB.DB[fKeyword].OneHandedAnimation;
                            WeaponAnimationType TwoHanded = DB.DB[fKeyword].TwoHandedAnimation;
                            if (DB.DB[fKeyword].WATModOverride.Any(x => x.Mod.Equals(weapon.FormKey.ModKey)))
                            {
                                OneHanded = DB.DB[fKeyword].WATModOverride.Where(x => x.Mod.Equals(weapon.FormKey.ModKey)).First().OneHandedAnimation;
                                TwoHanded = DB.DB[fKeyword].WATModOverride.Where(x => x.Mod.Equals(weapon.FormKey.ModKey)).First().TwoHandedAnimation;
                            }
                            if (isOneHanded)
                            {
                                if (OneHanded != weapon.Data.AnimationType)
                                {
                                    nw = nw == null ? state.PatchMod.Weapons.GetOrAddAsOverride(weapon)! : nw!;
                                    nw.Data!.AnimationType = OneHanded;
                                    Console.WriteLine($"\t\tChanged Animation Type to {OneHanded}");
                                }
                            }
                            else
                            {
                                if (TwoHanded != weapon.Data.AnimationType)
                                {
                                    nw = nw == null ? state.PatchMod.Weapons.GetOrAddAsOverride(weapon)! : nw!;
                                    nw.Data!.AnimationType = TwoHanded;
                                    Console.WriteLine($"\t\tChanged Animation Type to {TwoHanded}");
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}