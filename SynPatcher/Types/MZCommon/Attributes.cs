using System;
namespace MZCommonClass.Attributes;

//Attributes used for common union-like tasks
[AttributeUsage(AttributeTargets.Class)]
public class UnionAttribute : Attribute
{
    public string Tag;
    public UnionAttribute(string tag)
    {
        this.Tag = tag;
    }
}

//Attributes used for common union-like tasks
[AttributeUsage(AttributeTargets.Class)]
public class NameAttribute : Attribute
{
    public string Name;
    public NameAttribute(string name)
    {
        this.Name = name;
    }
}

//Tagged binary attribute
[AttributeUsage(AttributeTargets.Class)]
public class MagicAttribute : Attribute
{
    public byte magicValue;
    public MagicAttribute(byte magic)
    {
        magicValue = magic;
    }
}