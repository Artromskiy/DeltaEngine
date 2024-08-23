namespace Delta.ECS;

public interface ISystem
{
    public void Execute();
    //public ReadOnlySpan<Type> Ref { get; }
    //public ReadOnlySpan<Type> RefReadonly { get; }
    //public ReadOnlySpan<Type> In { get; }
    //public ReadOnlySpan<Type> None { get; }

    public enum ComponentAccess
    {
        Ref,
        RefReadonly,
        In,
        None
    }
}


