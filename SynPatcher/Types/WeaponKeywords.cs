using System.Collections.Generic;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Synthesis.Settings;

namespace WeaponKeywords.Types;
public struct Scripts {
    public ModKey Requires;
    public string ScriptName;
    public Dictionary<string, FormKey> Objects;
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
    //Include editorID
    public List<FormKey> include;
    //Exclude specific phrases
    public List<FormKey> exclude;
    //Exlcude specific sources
    public List<ModKey> excludeSource;
    //Exclude a mod from being patched
    public List<ModKey> excludeMod;
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
    public Dictionary<string, FormKey> InjectedKeywords = new();
    [SynthesisSettingName("Do not Touch - Internal Use Only")]
    [SynthesisTooltip("Internal use only, do not touch")]
    public int DBVer = -1;
}
