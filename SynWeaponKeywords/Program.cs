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
            var db = JObject.Parse(File.ReadAllText(Path.Combine(state.ExtraSettingsDataPath, "types.json"))).ToObject<Dictionary<string, WeaponDB>>();
            var edb = JObject.Parse(File.ReadAllText(Path.Combine(state.ExtraSettingsDataPath, "excludes.json"))).ToObject<ExcludesDB>();
            var idb = JObject.Parse(File.ReadAllText(Path.Combine(state.ExtraSettingsDataPath, "includes.json"))).ToObject<Dictionary<string, string>>();
            if(db!=null) {
                Dictionary<string, FormKey> formkeys = new Dictionary<string, FormKey>();
                Dictionary<string, List<FormKey>> alternativekeys = new Dictionary<string, List<FormKey>>();
                foreach(var item in db) {
                    if(item.Value.keyword!=null) {
                        var keyword  = state.LoadOrder.PriorityOrder.Keyword().WinningOverrides().Where(kywd => ((kywd.FormKey.ModKey.FileName == item.Value.mod)&&((kywd.EditorID?.ToString()??"")==item.Value.keyword))).FirstOrDefault();
                        if(keyword!=null) {
                            formkeys[item.Key] = keyword.FormKey;
                        }
                    }
                }
                foreach(var weapon in state.LoadOrder.PriorityOrder.Weapon().WinningOverrides()) {
                    var edid = weapon.EditorID;
                    var nameToTest = weapon.Name?.String?.ToLower();
                    var kyds = db.Where(kv => kv.Value.commonNames.Any(cn => nameToTest?.Contains(cn)??false)).Select(kd => kd.Key).ToArray();
                    var exclude = edb?.weapons.Contains(edid)??false;
                    var orex = edb?.phrases.Any(ph => nameToTest?.Contains(ph)??false)??false;
                    if(idb?.ContainsKey(edid??"")??false) {
                        var nw = state.PatchMod.Weapons.GetOrAddAsOverride(weapon);
                        if(formkeys.ContainsKey(idb[edid??""])) {
                            nw.Keywords?.Add(formkeys[idb[edid??""]]);
                            Console.WriteLine($"{nameToTest} is {db[idb[edid??""]].outputDescription}");
                        } else {
                            Console.WriteLine($"{nameToTest} is {db[idb[edid??""]].outputDescription}, but not changing (missing esp?)");
                        }
                    }

                    if(kyds.Length > 0 && !((exclude) || (orex))) {
                        if(!kyds.All(kd => weapon.Keywords?.Contains(formkeys.GetValueOrDefault(kd))??false)) {
                            var nw = state.PatchMod.Weapons.GetOrAddAsOverride(weapon);
                            foreach(var kyd in kyds) {
                                if(formkeys.ContainsKey(kyd) && !(nw.Keywords?.Contains(formkeys[kyd])??false)) {
                                    nw.Keywords?.Add(formkeys[kyd]);
                                    Console.WriteLine($"{nameToTest} is {db[kyd].outputDescription}");
                                }
                            }
                        }
                        foreach(var kyd in kyds) {
                            if(!kyds.All(kyd => db[kyd].akeywords?.Length > 0)) {
                                if(!alternativekeys.ContainsKey(kyd)){
                                    alternativekeys[kyd] = new List<FormKey>();
                                    foreach(var keywd in db[kyd].akeywords??new string[0]) {
                                        var test = state.LoadOrder.PriorityOrder.Keyword().WinningOverrides().Where(kywd => ((kywd.EditorID??"") == keywd)).FirstOrDefault();
                                        if(test != null) {
                                            alternativekeys[kyd].Add(test.FormKey);
                                        } else {
                                            alternativekeys[kyd].Add(state.PatchMod.Keywords.AddNew(keywd).FormKey);
                                        }
                                    }
                                }
                                if(alternativekeys[kyd].Count > 0) {
                                    var nw = state.PatchMod.Weapons.GetOrAddAsOverride(weapon);
                                    foreach(var alt in alternativekeys[kyd]) {
                                        nw.Keywords?.Add(alt);
                                        Console.WriteLine($"{nameToTest} is {db[kyd].outputDescription}, adding extra keyword(s)");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}