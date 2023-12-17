using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using MZCommonClass.Attributes;
using MZCommonClass.TaggedJson;

namespace MZCommonClass.JsonPatch;

//A naive json patch implementation
public static class Extensions
{
    public static JToken Pointer(this JToken token, IEnumerable<string> path)
    {
        if (token.Type == JTokenType.Object && path.Any())
        {
            return Pointer(((JObject)token)[path.First()]!, path.TakeLast(path.Count() - 1));
        }
        else if (token.Type == JTokenType.Array && path.Any())
        {
            return Pointer(((JArray)token)[int.Parse(path.First())]!, path.TakeLast(path.Count() - 1));
        }
        else
        {
            return token;
        }
    }
    public static bool ApplyTo(this IList<JsonOperation> oplist, JToken obj)
    {
        var success = true;
        foreach (var op in oplist)
        {
            var ss = op.ApplyTo(obj);
            if (ss == false)
            {
                Console.WriteLine($"Failed to apply: {op}");
            }
            success &= ss;
        }
        return success;
    }
    public static int IndexWhere<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
    {
        var enumerator = source.GetEnumerator();
        int index = 0;
        while (enumerator.MoveNext())
        {
            TSource obj = enumerator.Current;
            if (predicate(obj))
                return index;
            index++;
        }
        return -1;
    }

}

[Union("op")]
[JsonConverter(typeof(UnionJson))]
public class JsonOperation
{
    public virtual bool ApplyTo(JToken obj)
    {
        return false;
    }
}

[Name("add")]
public class Add : JsonOperation
{
    public string? path;
    public object? value;
    public override bool ApplyTo(JToken obj)
    {
        var c = path!.Split("/")![1..];
        var jt = obj.Pointer(c.Take(c.Length - 1))!;
        if (value != null)
        {
            if (jt.Type == JTokenType.Object)
            {
                if (!((JObject)jt).ContainsKey(c.Last()))
                {
                    ((JObject)jt).Add(c.Last(), JToken.FromObject(value!));
                    return true;
                }
            }
            else if (jt.Type == JTokenType.Array)
            {
                if (!((JArray)jt).Contains(JToken.FromObject(value!)))
                {
                    ((JArray)jt).Insert(int.Parse(c.Last()), JToken.FromObject(value!));
                    return true;
                }
            }
        }
        return false;
    }
}

[Name("replace")]
public class Replace : JsonOperation
{
    public string? path;
    public object? value;
    public override bool ApplyTo(JToken obj)
    {
        var c = path!.Split("/")![1..];
        var jt = obj.Pointer(c.Take(c.Length - 1))!;
        if (jt.Type == JTokenType.Object)
        {
            ((JObject)jt)[c.Last()] = JToken.FromObject(value!);
            return true;
        }
        else if (jt.Type == JTokenType.Array)
        {
            ((JArray)jt)[int.Parse(c.Last())] = JToken.FromObject(value!);
            return true;
        }
        return false;
    }
}

[Name("remove")]
public class Remove : JsonOperation
{
    public string? path;
    public object? value;
    public override bool ApplyTo(JToken obj)
    {
        var c = path!.Split("/")![1..];
        var jt = obj.Pointer(c.Take(c.Length - 1))!;
        if (jt.Type == JTokenType.Object)
        {
            if (((JObject)jt).ContainsKey(c.Last()))
            {
                ((JObject)jt).Remove(c.Last());
                return true;
            }
        }
        else if (jt.Type == JTokenType.Array)
        {
            if (value != null)
            {
                var idx = ((JArray)jt).IndexWhere(x => x.ToObject<object>() == value);
                if (idx != -1)
                {
                    ((JArray)jt).RemoveAt(idx);
                    return true;
                }
            }
            else
            {
                if (((JArray)jt).Count >= int.Parse(c.Last()))
                {
                    ((JArray)jt).RemoveAt(int.Parse(c.Last()));
                    return true;
                }
            }
        }
        return false;
    }
}

[Name("move")]
public class Move : JsonOperation
{
    public string? path;
    public string? from;
    public override bool ApplyTo(JToken obj)
    {
        var c = path!.Split("/")![1..];
        var c2 = from!.Split("/")![1..];
        var jt = obj.Pointer(c.Take(c.Length - 1))!;
        var target = obj.Pointer(c2.Take(c2.Length - 1))!;

        if (jt.Type == JTokenType.Object)
        {
            if (((JObject)jt).ContainsKey(c.Last()))
            {
                var tmp = ((JObject)jt)[c.Last()]!;
                ((JObject)jt).Remove(c.Last());
                if (target.Type == JTokenType.Object)
                {
                    ((JObject)target)[c2.Last()] = tmp;
                }
                else if (target.Type == JTokenType.Array)
                {
                    ((JArray)target).Insert(int.Parse(c2.Last()), tmp);
                }
                return true;
            }
        }
        else if (jt.Type == JTokenType.Array)
        {
            if (((JArray)jt).Count() >= int.Parse(c.Last()))
            {
                var tmp = ((JArray)jt)[int.Parse(c.Last())]!;
                ((JArray)jt).RemoveAt(int.Parse(c.Last()));
                if (target.Type == JTokenType.Object)
                {
                    ((JObject)target)[c2.Last()] = tmp;
                }
                else if (target.Type == JTokenType.Array)
                {
                    ((JArray)target).Insert(int.Parse(c2.Last()), tmp);
                }
                return true;
            }
        }
        return false;
    }
}