namespace DeltaGenCore;

public abstract class AttributeTemplate : Template
{
    public string ShortName => Name.Substring(0, Name.Length - "Attribute".Length);
}