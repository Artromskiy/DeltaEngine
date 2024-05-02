using System;

namespace Delta.Scripting;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public class DirtyAttribute : Attribute { }