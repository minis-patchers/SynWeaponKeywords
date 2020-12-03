using System.Collections.Generic;
using Mutagen.Bethesda;
namespace WeaponKeywords.Types {
    public struct WeaponDB {
        //Mod / main keyword
        public string keyword;
        //akeywords don't require specific mods
        public List<string> akeywords;
        //Common names of item (partial match)
        public List<string> commonNames;
        //Exclude specifics
        public List<string> exclude;
        //descriptor when patched
        public string outputDescription;
        //unused
        public string animation;
    }
    public struct ExcludesDB {
        public List<string> phrases;
        public List<string> weapons;
    }
    public struct Database {
        public Dictionary<string, WeaponDB> DB;
        public Dictionary<string, string> includes;
        public ExcludesDB excludes;
        public List<ModKey> sources;
    }
}