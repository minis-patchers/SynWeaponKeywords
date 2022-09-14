using System.Collections.Generic;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis.Settings;

namespace WeaponKeywords.Types
{
    public struct WATModOverrides
    {
        public ModKey Mod;
        public WeaponAnimationType Animation;
    }
    public struct WeaponDB
    {
        //Keywords to assgin
        public List<string> keyword;
        //Common names of item (partial match)
        public List<string> commonNames;
        //excludeNames
        public List<string> excludeNames;
        //descriptor when patched
        public string outputDescription;
        public WeaponAnimationType Animation;
        //Specific WAT-Type Overrides
        public List<WATModOverrides> WATModOverride;
        //Ignore Weapon Animation Type Overrides for a mod
        public List<ModKey> IgnoreWATOverrides;
        //Include editorID
        public List<FormKey> include;
        //Exclude specific phrases
        public List<FormKey> exclude;
        //Exlcude specific sources
        public List<ModKey> excludeSource;
        //Exclude a mod from being patched
        public List<ModKey> excludeMod;
    }
    public struct ExcludesDB
    {
        //These are phrases to globally exclude
        public List<string> phrases;
        //These are editor ids to globally exclude
        public List<FormKey> weapons;
        //exclude mods from patch
        public List<ModKey> excludeMod;
    }
    public class Database
    {
        public Dictionary<string, WeaponDB> DB = new();
        public ExcludesDB excludes = new();
        public List<ModKey> sources = new();
        [SynthesisDescription("Generates keywords for use in the patcher")]
        [SynthesisSettingName("Generate Keywords")]
        public bool Gen = true;
        public Dictionary<string, string> InjectedKeywords = new();
        [SynthesisSettingName("Do not Touch - Internal Use Only")]
        [SynthesisTooltip("Internal use only, do not touch")]
        public int DBPatchVer = -1;
    }
}