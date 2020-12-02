using System.Collections.Generic;
using Mutagen.Bethesda;
namespace WeaponKeywords.Types {
    public struct WeaponDB {
        //Mod / main keyword
        public string keyword;
        //akeywords don't require specific mods
        public string[] akeywords;
        //Common names of item (partial match)
        public string[] commonNames;
        //Exclude specifics
        public string[] exclude;
        //descriptor when patched
        public string outputDescription;
        //unused
        public string animation;
    }
    public struct ExcludesDB {
        public string[] phrases;
        public string[] weapons;
    }
    public struct Database {
        public Dictionary<string, WeaponDB> DB;
        public Dictionary<string, string> includes;
        public ExcludesDB excludes;
        public List<ModKey> sources;
    }
}