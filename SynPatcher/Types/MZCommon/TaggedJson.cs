using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MZCommonClass.Attributes;

using System;
using System.Linq;
using System.Collections.Generic;

namespace MZCommonClass.TaggedJson;

public class UnionJson : JsonConverter
{
    static HashSet<(string Tag, string Name, string? altName, Type type)> UnionTypes;
    static UnionJson()
    {
        UnionTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes().Where(x => x.GetCustomAttribute<UnionAttribute>(true) != null)
        .Select(x => (x.GetCustomAttribute<UnionAttribute>(true)!.Tag, x.Name!, x.GetCustomAttribute<NameAttribute>()?.Name ?? null, x))).ToHashSet();
    }
    public override bool CanConvert(Type typ)
    {
        return UnionTypes.Any(x => x.Item4 == typ);
    }
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var jObj = JObject.Load(reader);
        var tag = UnionTypes.Where(x => jObj.ContainsKey(x.Tag)).First().Tag;
        var obj = Activator.CreateInstance(UnionTypes.Where(x => x.Name == jObj[tag]?.Value<string>() || x.altName == jObj[tag]?.Value<string>()).First().type);
        jObj.Remove(tag);
        var typ = obj!.GetType();
        foreach (var vobj in jObj)
        {
            var f = typ.GetField(vobj.Key);
            switch (vobj.Value!.Type)
            {
                case JTokenType.Array:
                    f!.SetValue(obj, vobj.Value.Values<object>().ToArray());
                    break;
                case JTokenType.Object:
                    if (f!.FieldType.IsClass && f.FieldType.Name != "Object")
                    {
                        var tmp = Activator.CreateInstance(f.FieldType);
                        serializer.Populate(vobj.Value!.CreateReader(), tmp!);
                        f.SetValue(obj, tmp);
                    }
                    else
                    {
                        var des = serializer.Deserialize(vobj.Value!.CreateReader());
                        f.SetValue(obj, des);
                    }
                    break;
                case JTokenType.String:
                    f!.SetValue(obj, vobj.Value.Value<string>());
                    break;
                case JTokenType.Integer:
                    f!.SetValue(obj, vobj.Value.Value<int>());
                    break;
                case JTokenType.Float:
                    f!.SetValue(obj, vobj.Value.Value<float>());
                    break;
            }
        }
        return obj;
    }
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value != null)
        {
            var jObj = new JObject();
            var typ = value.GetType();
            foreach (var field in typ.GetFields())
            {
                if (field.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                {
                    continue;
                }
                var tmp = new JTokenWriter();
                serializer.Serialize(tmp, field.GetValue(value), field.FieldType);
                jObj.Add(new JProperty(field.Name, tmp.Token));
            }
            string name = typ.Name;
            if (typ.GetCustomAttribute<NameAttribute>(false) != null)
            {
                name = typ.GetCustomAttribute<NameAttribute>(false)!.Name;
            }
            jObj.Add(new JProperty(typ.GetCustomAttribute<UnionAttribute>(true)!.Tag, name));
            jObj.WriteTo(writer);
        }
    }
}