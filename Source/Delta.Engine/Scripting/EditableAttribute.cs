using System;

using Delta.Engine.Scripting;
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public class EditableAttribute() : Attribute;
