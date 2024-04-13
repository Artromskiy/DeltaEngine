using Arch.Core;
using Arch.Core.Extensions;
using Delta.ECS.Components;
using Delta.Runtime;
using Delta.Scripting;
using DeltaEditorLib.Scripting;
using System.Collections.ObjectModel;

namespace DeltaEditor
{
    public partial class MainPage : ContentPage
    {
        private readonly IProjectPath _projectPath;
        private readonly RuntimeLoader _runtimeLoader;
        private IRuntime Runtime => _runtimeLoader.Runtime;

        private readonly ObservableCollection<HierarchyEntityView> _entities = [];
        private readonly ObservableCollection<InspectorComponentView> _components = [];
        private readonly ObservableCollection<InspectorAvaliableComponent> _avaliableComponents = [];

        private List<ContentView> _inspectorComponentsEditors = [];

        public MainPage(RuntimeLoader runtimeLoader, IProjectPath projectData)
        {
            _runtimeLoader = runtimeLoader;
            _projectPath = projectData;
            InitializeComponent();
            HierarchyListView.ItemsSource = _entities;
            InspectorAvaliableComponents.ItemsSource = _avaliableComponents;
        }

        private void CreateScene(object sender, EventArgs e)
        {
            Runtime.CreateTestScene();
        }

        private void RunScene(object sender, ToggledEventArgs e)
        {
            Runtime.Running = e.Value;
        }

        private void SaveScene(object sender, EventArgs e)
        {
            Runtime.SaveScene("scene");
        }

        private void TryCompile(object sender, EventArgs e)
        {
            _runtimeLoader.ReloadRuntime();
            _avaliableComponents.Clear();

        }


        private static string EntityReferenceToString(EntityReference entityReference) => $"id: {entityReference.Entity.Id}, ver: {entityReference.Version}";

        private void OpenProjectFolder(object sender, EventArgs e) => _runtimeLoader.OpenProjectFolder();

        private void OnPickerComponentIndexChanged(object sender, EventArgs e)
        {

        }

        private void PauseButton_Clicked(object sender, EventArgs e)
        {

        }

        private void NextButton_Clicked(object sender, EventArgs e)
        {

        }

        private void UpdateHierarchyButton_Clicked(object sender, EventArgs e)
        {
            using var _ = _runtimeLoader.Runtime.Pause;
            _entities.Clear();
            var entities = _runtimeLoader.Runtime.GetEntities();
            foreach (var entityReference in entities)
            {
                if (!entityReference.Entity.IsAlive())
                    continue;
                string name;
                if (entityReference.Entity.TryGet<EntityName>(out var entityName))
                    name = entityName.text;
                else
                    name = EntityReferenceToString(entityReference);

                _entities.Add(new HierarchyEntityView(entityReference, name));
            }
        }

        private void HierarchyListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            using var _ = _runtimeLoader.Runtime.Pause;
            var entityReference = _entities[e.SelectedItemIndex].EntityReference;
            if (!entityReference.IsAlive())
                return;
            UpdateComponentsNew(entityReference);
        }


        private void UpdateComponentsNew(EntityReference entityReference)
        {
            var componentsTypes = entityReference.Entity.GetComponentTypes();
            var components = entityReference.Entity.GetAllComponents();

            foreach (var componentEditor in _inspectorComponentsEditors)
                NewInspectorListView.Remove(componentEditor);
            _inspectorComponentsEditors.Clear();
            foreach (var component in components)
            {
                var type = component.GetType();
                if (!_runtimeLoader.AccessorsContainer.AllAccessors.ContainsKey(type))
                    continue;
                var componentEditor = ComponentEditor.Create(component, _runtimeLoader.AccessorsContainer);
                if (componentEditor == null)
                    continue;
                NewInspectorListView.Add(componentEditor);
                _inspectorComponentsEditors.Add(componentEditor);
            }

            _avaliableComponents.Clear();
            var avaliableComponents = _runtimeLoader.Components;
            avaliableComponents.RemoveAll(x => Array.Exists(componentsTypes, c => c.Type.Equals(x)));
            foreach (var item in avaliableComponents)
                _avaliableComponents.Add(new(item.Name));
        }

        private void UpdateComponentsOld(EntityReference entityReference)
        {
            var components = entityReference.Entity.GetComponentTypes();
            _components.Clear();
            foreach (var component in components)
                _components.Add(new(component.Type.Name));

            _avaliableComponents.Clear();
            var avaliableComponents = _runtimeLoader.Components;
            avaliableComponents.RemoveAll(x => Array.Exists(components, c => c.Type.Equals(x)));
            foreach (var item in avaliableComponents)
                _avaliableComponents.Add(new(item.Name));
        }
    }
}
