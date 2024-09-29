using Newtonsoft.Json;

namespace MZCommon.Datapack;

public static class DatapackStatics {
    public static readonly HttpClient Cli = new();
    public static PKGInfo GetPKG(string URI) {
        var tsk = Cli.GetStringAsync(URI);
        tsk.Wait();
        return JsonConvert.DeserializeObject<PKGInfo>(tsk.Result);
    }
}

public struct PKGInfo {
    public string Root;
    public HashSet<string> Files;
    public readonly string GetFile(string file) {
        if(!Files.Contains(file)) return string.Empty;
        var tsk = DatapackStatics.Cli.GetStringAsync($"{Root}/{file}");
        tsk.Wait();
        return tsk.Result;
    }
}