using Delta.Runtime;
using System.Collections.ObjectModel;

namespace DeltaEditor.Hierarchy
{
    internal class HierarchyEntityViewModel : BindableObject
    {
        public ObservableCollection<HierarchyEntity> Nodes { get; set; } = [];

        public HierarchyEntityViewModel()
        {

        }

        public void UpdateHierarchy(IRuntime runtime)
        {
            /*
            Nodes.Clear();
            var entities = runtime.GetEntities().ToArray();
            var preTree = SceneTree.GeneratePreTree(entities);
            Dictionary<EntityReference, HierarchyEntity> tree = [];
            foreach (var item in preTree)
                tree.Add(item.Key, new(item.Key));
            foreach (var node in tree)
                if (preTree.TryGetValue(node.Key, out var leafs))
                    foreach (var leaf in leafs)
                        node.Value.Children.Add(tree[leaf]);
            if(tree.TryGetValue(EntityReference.Null, out var root))
                Nodes.Add(root);
            */
        }
    }
}
