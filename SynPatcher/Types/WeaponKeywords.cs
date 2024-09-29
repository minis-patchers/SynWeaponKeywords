using System.Text.Json.Serialization;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;
using Newtonsoft.Json.Converters;

namespace WeaponKeywords.Types;

public enum EquippedType
{
    OneHand, TwoHand
}

public static class Consts
{
    public static Dictionary<FormKey, EquippedType> equipTable = new() {
        {Skyrim.EquipType.BothHands.FormKey, EquippedType.TwoHand},
        {Skyrim.EquipType.LeftHand.FormKey, EquippedType.OneHand},
        {Skyrim.EquipType.RightHand.FormKey, EquippedType.OneHand},
        {Skyrim.EquipType.EitherHand.FormKey, EquippedType.OneHand},
    };
}
public struct WeaponKeywordInfo
{
    public string name;
    //Keywords to assgin
    public HashSet<string> keyword;
    //Common names of item (partial match)
    public HashSet<string> commonNames;
    //Exclude a specific phrase or name (partial match)
    public HashSet<string> excludeNames;
    //descriptor when patched
    public string outputDescription;
    //Include editorID
    public HashSet<FormKey> include;
    //exclude specific item
    public HashSet<FormKey> exclude;
    //List of valid EquipTypes
    [JsonConverter(typeof(StringEnumConverter))]
    public EquippedType validEquipType;
}
public struct ExcludePackage
{
    //These are phrases to globally exclude
    public HashSet<string> phrases;
    //These are formkeys to globally exclude
    public HashSet<FormKey> weapons;
}

public struct WeaponKeywordPackage
{
    public string Name;
    public string Description;
    public ExcludePackage excludes;
    public HashSet<ModKey> sources;
}

public class Settings
{
    public bool UseRemote = true;
    public bool UseLocal = true;
    public HashSet<string> Remotes = ["https://raw.githubusercontent.com/minis-patchers/DataPacks/refs/heads/main/WeaponKeywords/MZWeapPKG.json"];
}