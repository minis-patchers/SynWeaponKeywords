using System.Collections.Generic;
using Mutagen.Bethesda.Plugins;
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
        //Exlcude specific sources
        public List<ModKey> excludeSource;
        //descriptor when patched
        public string outputDescription;
        //Weapon's Animation Type(s)
        public string OneHandedAnimation;
        public string TwoHandedAnimation;
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