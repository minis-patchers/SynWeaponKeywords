using System.Text.Json.Serialization;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Synthesis.Settings;
using Newtonsoft.Json.Converters;

namespace WeaponKeywords.Types;

public enum EquippedType {
    OneHand,TwoHand
}

public static class DBConst
{
    public const string DEFAULT_UPDATE_LOCATION = "https://raw.githubusercontent.com/minis-patchers/SynDelta/main/SynWeaponKeywords/index.json";
    public static Dictionary<FormKey, EquippedType> equipTable = new() {
        {Skyrim.EquipType.BothHands.FormKey, EquippedType.TwoHand},
        {Skyrim.EquipType.LeftHand.FormKey, EquippedType.OneHand},
        {Skyrim.EquipType.RightHand.FormKey, EquippedType.OneHand},
        {Skyrim.EquipType.EitherHand.FormKey, EquippedType.OneHand},
    };
}
public struct Weapons
{
    //Keywords to assgin
    public List<string> keyword;
    //Common names of item (partial match)
    public List<string> commonNames;
    //Exclude a specific phrase or name (partial match)
    public List<string> excludeNames;
    //descriptor when patched
    public string outputDescription;
    //Include editorID
    public List<FormKey> include;
    //exclude specific item
    public List<FormKey> exclude;
    //List of valid EquipTypes
    [JsonConverter(typeof(StringEnumConverter))]
    public EquippedType validEquipType;
}
public struct Excludes
{
    //These are phrases to globally exclude
    public List<string> phrases;
    //These are formkeys to globally exclude
    public List<FormKey> weapons;
}
public class Database
{
    public Dictionary<string, Weapons> DB = new();
    public Excludes excludes = new();
    public List<ModKey> sources = new();
    [SynthesisSettingName("Use remote updates")]
    [SynthesisTooltip("This patcher by default uses a mechanism to grab remote updates for it's database, this setting can disabled if you use your own database.json from online... these updates only work for it's original database")]
    public bool DoUpdates = true;
    [SynthesisSettingName("Remote update location")]
    [SynthesisTooltip("Location to grab this patcher's remote updates, generally only useful if you have your own modpack / modlist, and know the format")]
    //Default location is the mini's patcher's github
    public string UpdateLocation = DBConst.DEFAULT_UPDATE_LOCATION;
    [SynthesisSettingName("Do not Touch - Internal Use Only")]
    [SynthesisTooltip("Internal use only, do not touch")]
    public int DBVer = -1;
}
