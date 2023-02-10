using System;
using System.Collections.Generic;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Synthesis.Bethesda.DTO;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace WeaponKeywords.Types;

public enum EquipType
{
    NULL, EitherHand, RightHand, LeftHand, BothHands
}

public enum EXP
{
    OFF, SIMPLE, FULL
}

public static class DBConst
{
    public static Dictionary<FormKey, EquipType> EquipTypeTableR = new()
    {
        {Skyrim.EquipType.EitherHand.FormKey, EquipType.EitherHand},
        {Skyrim.EquipType.BothHands.FormKey, EquipType.BothHands},
        {Skyrim.EquipType.RightHand.FormKey,EquipType.RightHand},
        {Skyrim.EquipType.LeftHand.FormKey,EquipType.LeftHand}
    };
    public static Dictionary<EquipType, FormKey> EquipTypeTable = new() {
        {EquipType.EitherHand, Skyrim.EquipType.EitherHand.FormKey},
        {EquipType.BothHands, Skyrim.EquipType.BothHands.FormKey},
        {EquipType.RightHand, Skyrim.EquipType.RightHand.FormKey},
        {EquipType.LeftHand, Skyrim.EquipType.LeftHand.FormKey}
    };
}

public struct AnimOverrideEnum<T> where T : Enum
{
    public T Compare;
    public Dictionary<EquipType, WeaponAnimationType> Animation;
}

public struct AnimOverride<T> where T : IEquatable<T>
{
    public T Compare;
    public Dictionary<EquipType, WeaponAnimationType> Animation;
}

public struct Scripts
{
    public ModKey Requires;
    public string ScriptName;
    public List<ModKey> ExcludeMods;
    public List<FormKey> ExcludeItems;
    public Dictionary<string, FormKey> ObjectParam;
    public Dictionary<string, List<FormKey>> ObjectListParam;
    public Dictionary<string, float> FloatParam;
    public Dictionary<string, List<float>> FloatListParam;
}

public struct Weapons
{
    //Keywords to assgin
    public List<string> keyword;
    //Common names of item (partial match)
    public List<string> commonNames;
    //Exclude a specific phrase or name
    public List<string> excludeNames;
    //descriptor when patched
    public string outputDescription;
    //Weapon Animation Type handling
    public List<AnimOverrideEnum<EquipType>> AnimEQOverride;
    public List<AnimOverride<string>> AnimNameOverride;
    public List<AnimOverride<ModKey>> AnimModOverride;
    public List<AnimOverride<FormKey>> AnimItemOverride;
    //Include editorID
    public List<FormKey> include;
    //exclude specific item
    public List<FormKey> exclude;
    //Exclude a mod from being patched
    public List<ModKey> excludeMod;
    //Add a script to a weapon
    public List<Scripts> Script;
}
public struct Excludes
{
    //These are phrases to globally exclude
    public List<string> phrases;
    //These are formkeys to globally exclude
    public List<FormKey> weapons;
    //exclude mods from patch
    public List<ModKey> excludeMod;
}
public class Database
{
    public Dictionary<string, Weapons> DB = new();
    public Excludes excludes = new();
    public List<ModKey> sources = new();
    [SynthesisDescription("Generates keywords for use in the patcher")]
    [SynthesisSettingName("Generate Keywords")]
    public bool Gen = true;
    //Experimental mode, while structures are included with the main mod and DB updates, the features that use them are disabled by this switch
    [SynthesisSettingName("Experimental mode")]
    [SynthesisDescription("Sets the level of experimental mode (Recommended OFF, unless you know what you're doing or are told to change this)")]
    [JsonConverter(typeof(StringEnumConverter))]
    public EXP exp = EXP.OFF;
    public Dictionary<string, FormKey> InjectedKeywords = new();
    [SynthesisSettingName("Do not Touch - Internal Use Only")]
    [SynthesisTooltip("Internal use only, do not touch")]
    public int DBVer = -1;
}
