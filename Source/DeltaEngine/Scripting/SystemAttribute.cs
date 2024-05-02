using System;

namespace Delta.Scripting;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public class SystemAttribute() : Attribute;