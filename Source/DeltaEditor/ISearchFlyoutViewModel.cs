namespace DeltaEditor
{
    public interface ISearchFlyoutViewModel
    {
        public string GetName { get; }
    }

    internal readonly struct SearchFlyoutViewModel<T>:ISearchFlyoutViewModel
    {
        public readonly T Data;
        public readonly string Name;

        public SearchFlyoutViewModel(T data, string name)
        {
            Data = data;
            Name = name;
        }

        public readonly string GetName => Name;
    }
}
