using System;

namespace Delta.Scripting;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class ComponentAttribute() : Attribute;