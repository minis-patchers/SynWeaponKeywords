using System.Collections.Generic;

using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace WeaponKeywords.Types
{
    public struct WeaponDB
    {
        //Keywords to assgin
        public List<string> keyword;
        //Common names of item (partial match)
        public List<string> commonNames;
        //Exclude specific phrases
        public List<string> exclude;
        //Exclude editor id's
        public List<string> excludeEditID;
        //Exlcude specific sources
        public List<ModKey> excludeSource;
        //descriptor when patched
        public string outputDescription;
        //Ignore Weapon Animation Type Overrides
        public List<ModKey> IgnoreWATOverrides;
        //Weapon's Animation Type(s)
        public WeaponAnimationType OneHandedAnimation;
        public WeaponAnimationType TwoHandedAnimation;
    }
    public struct ExcludesDB
    {
        //These are phrases to globally exclude
        public List<string> phrases;
        //These are editor ids to globally exclude
        public List<string> weapons;
    }
    public class Database
    {
        public Dictionary<string, WeaponDB> DB = new();
        public Dictionary<string, string> includes = new();
        public ExcludesDB excludes = new();
        public List<ModKey> sources = new();
    }
}