using System.Collections.Generic;
using Mutagen.Bethesda;
namespace WeaponKeywords.Types
{
    public struct WeaponDB
    {
        //Mod / main keyword
        public List<string> keyword;
        //Common names of item (partial match)
        public List<string> commonNames;
        //Exclude specific phrases
        public List<string> exclude;
        //Exlcude specific sources
        public List<ModKey> excludeSource;
        //descriptor when patched
        public string outputDescription;
        //unused
        public string animation;
    }
    public struct ExcludesDB
    {
        public List<string> phrases;
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