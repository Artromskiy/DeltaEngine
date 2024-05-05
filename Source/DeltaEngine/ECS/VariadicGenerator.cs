using System;

namespace Delta.ECS;
internal class VariadicGenerator
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class RefTuple : Attribute { }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class ReadonlyRefTuple : Attribute { }

    [AttributeUsage(AttributeTargets.GenericParameter)]
    public class Variadic : Attribute { }
}