using Newtonsoft.Json;

namespace MZCommon.Datapack;

public static class DatapackStatics {
    public static HttpClient Cli = new();
    public static PKGInfo GetPKG(string URI) {
        var tsk = DatapackStatics.Cli.GetStringAsync(URI);
        tsk.Wait();
        return JsonConvert.DeserializeObject<PKGInfo>(tsk.Result);
    }
}

public struct PKGInfo {
    public string Root;
    public HashSet<string> Files;
    public string GetFile(string File) {
        if(!this.Files.Contains(File)) return string.Empty;
        var tsk = DatapackStatics.Cli.GetStringAsync($"{this.Root}/{File}");
        tsk.Wait();
        return tsk.Result;
    }
}