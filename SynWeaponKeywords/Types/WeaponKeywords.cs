namespace WeaponKeywords.Types {
    public class WeaponDB {
        //Mod / main keyword
        public string? keyword;
        //akeywords don't require specific mods
        public string[]? akeywords;
        //Common names of item (partial match)
        public string[]? commonNames;
        //descriptor when patched
        public string? outputDescription;
        //unused
        public string? animation;
    }
    public class ExcludesDB {
        public string[]? phrases;
        public string[]? weapons;
    }
}