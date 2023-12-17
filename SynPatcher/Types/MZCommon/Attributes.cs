namespace MZCommonClass.Attributes;

//Attributes used for common union-like tasks
[AttributeUsage(AttributeTargets.Class)]
public class UnionAttribute : Attribute
{
    public string Tag;
    public UnionAttribute(string tag)
    {
        Tag = tag;
    }
}

//Attributes used for common union-like tasks
[AttributeUsage(AttributeTargets.Class)]
public class NameAttribute : Attribute
{
    public string Name;
    public NameAttribute(string name)
    {
        Name = name;
    }
}