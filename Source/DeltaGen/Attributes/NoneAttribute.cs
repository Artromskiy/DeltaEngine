using DeltaGen.Templates;

namespace DeltaGen.Attributes;
internal class NoneAttribute : GenericVariadicAttribute
{
    public override string Name => nameof(NoneAttribute);
}
