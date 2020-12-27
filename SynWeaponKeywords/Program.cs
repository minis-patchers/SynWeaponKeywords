using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using WeaponKeywords.Types;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Newtonsoft.Json.Linq;

namespace WeaponKeywords
{
    public class Program
    {
        public static int Main(string[] args)
        {
            return SynthesisPipeline.Instance.Patch<ISkyrimMod, ISkyrimModGetter>(
                args: args,
                patcher: RunPatch,
                userPreferences: new UserPreferences()
                {
                    ActionsForEmptyArgs = new RunDefaultPatcher()
                    {
                        IdentifyingModKey = "WeapTypeKeywords.esp",
                        TargetRelease = GameRelease.SkyrimSE,
                        BlockAutomaticExit = false,
                    }
                });
        }

        public static void RunPatch(SynthesisState<ISkyrimMod, ISkyrimModGetter> state)
        {
            try {
                var database = JObject.Parse(File.ReadAllText(Path.Combine(state.ExtraSettingsDataPath, "database.json"))).ToObject<Database>();
                if(database.SchemeVer < DBVersioning.SchemeVer) {
                    Directory.Delete(state.ExtraSettingsDataPath, true);
                    Console.WriteLine("Deleting old database due to an outdated DB Scheme, please rerun the patcher to get the new Database");
                    throw new Exception("Database version error, rerun");
                }
                Dictionary<string, List<IKeywordGetter>> formkeys = new Dictionary<string, List<IKeywordGetter>>();
                foreach(var (key, value) in database.DB)
                {
                    foreach(var src in database.sources)
                    {
                        if(value.keyword==null) continue;
                        var keywords  = state.LoadOrder.PriorityOrder.Keyword().WinningOverrides()
                            .Where(x=>x.FormKey.ModKey == src)
                            .Where(x=>value.keyword.Contains(x.EditorID??""));
                        foreach(var keyword in keywords) {
                            if(keyword == null) continue;
                            if(!formkeys.ContainsKey(key)) formkeys[key] = new List<IKeywordGetter>();
                            formkeys[key].Add(keyword);
                        }
                    }
                }
                foreach(var weapon in state.LoadOrder.PriorityOrder.Weapon().WinningOverrides())
                {
                    var edid = weapon.EditorID;
                    var nameToTest = weapon.Name?.String?.ToLower();
                    var matchingKeywords = database.DB
                        .Where(kv => kv.Value.commonNames.Any(cn => nameToTest?.Contains(cn) ?? false))
                        .Select(kv => kv.Key)
                        .ToArray();
                    var globalExclude = database.excludes.phrases
                        .Any(ph => nameToTest?.Contains(ph) ?? false) ||
                        database.excludes.weapons.Contains(edid??"");
                    if(database.includes.ContainsKey(edid ?? ""))
                    {
                        var nw = state.PatchMod.Weapons.GetOrAddAsOverride(weapon);
                        if(formkeys.ContainsKey(database.includes[edid ?? ""]))
                        {
                            foreach(var keyform in formkeys[database.includes[edid ?? ""]]) {
                                if(!(nw.Keywords?.Contains(keyform)??false)) {
                                    nw.Keywords?.Add(keyform.FormKey);
                                    Console.WriteLine($"{nameToTest} is {database.DB[database.includes[edid ?? ""]].outputDescription}, adding {keyform.EditorID} from {keyform.FormKey.ModKey}");
                                }
                            }
                        } else {
                            Console.WriteLine($"{nameToTest} is {database.DB[database.includes[edid??""]].outputDescription}, but not changing (missing esp?)");
                        }
                    }
                    if(matchingKeywords.Length > 0 && !globalExclude)
                    {
                        Console.WriteLine($"{nameToTest}: \n\tMatching Keywords: {String.Join(",", matchingKeywords)}");
                        var nw = state.PatchMod.Weapons.GetOrAddAsOverride(weapon);
                        foreach(var kyd in matchingKeywords)
                        {
                            if(formkeys.ContainsKey(kyd) && !(database.DB[kyd].exclude.Any(cn => nameToTest?.Contains(cn) ?? false))) 
                            {
                                Console.WriteLine($"\t{nw.Name}: {nw.EditorID} from {nw.FormKey.ModKey} is {database.DB[kyd].outputDescription} adding: ");
                                foreach(var keyform in formkeys[kyd]) 
                                {
                                    if(database.DB[kyd].excludeSource.Contains(keyform.FormKey.ModKey.FileName)) continue;
                                    if(!(nw.Keywords?.Contains(keyform.FormKey)??false)) {
                                        nw.Keywords?.Add(keyform.FormKey);
                                        Console.WriteLine($"\t\tAdded keyword {keyform.EditorID} from {keyform.FormKey.ModKey}");
                                    }
                                }
                            }
                        }
                    }
                }
            } catch {
                throw new Exception($"Database Error, try deleting {state.ExtraSettingsDataPath}");
            }
        }
    }
}