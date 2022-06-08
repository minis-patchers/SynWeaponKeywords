using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis.Settings;

namespace WeaponKeywords.Types
{
    public struct WATModOverrides
    {
        public ModKey Mod;
        public WeaponAnimationType OneHandedAnimation;
        public WeaponAnimationType TwoHandedAnimation;
    }
    public struct WeaponDB
    {
        //Keywords to assgin
        public List<string> keyword;
        //Common names of item (partial match)
        public List<string> commonNames;
        //descriptor when patched
        public string outputDescription;
        //Default Weapon's Animation Type(s)
        public WeaponAnimationType OneHandedAnimation;
        public WeaponAnimationType TwoHandedAnimation;
        //Specific WAT-Type Overrides
        public List<WATModOverrides> WATModOverride;
        //Include editorID
        public List<string> include;
        //Exclude specific phrases
        public List<string> exclude;
        //Exclude editor id's
        public List<string> excludeEditID;
        //Exlcude specific sources
        public List<ModKey> excludeSource;
        //Exclude a mod from being patched
        public List<ModKey> excludeMod;
        //Ignore Weapon Animation Type Overrides for a mod
        public List<ModKey> IgnoreWATOverrides;
    }
    public struct ExcludesDB
    {
        //These are phrases to globally exclude
        public List<string> phrases;
        //These are editor ids to globally exclude
        public List<string> weapons;
        //exclude mods from patch
        public List<ModKey> excludeMod;
    }
    public class Database
    {
        public Dictionary<string, WeaponDB> DB = new();
        public ExcludesDB excludes = new();
        public List<ModKey> sources = new();
        [SynthesisSettingName("Do not Touch - Internal Use Only")]
        [SynthesisTooltip("Internal use only, do not touch")]
        public int CurrentSchemeVersion;
    }
}