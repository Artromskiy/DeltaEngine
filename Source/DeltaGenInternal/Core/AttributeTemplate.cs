namespace DeltaGenInternal.Core;

internal abstract class AttributeTemplate : Template
{
    public string ShortName => Name.Substring(0, Name.Length - "Attribute".Length);
}