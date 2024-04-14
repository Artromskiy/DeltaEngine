using Arch.Core;
using Arch.Core.Extensions;
using Delta.ECS.Components;
using Delta.Runtime;
using DeltaEditor.Inspector;
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

        private readonly InspectorView _inspector;

        public MainPage(RuntimeLoader runtimeLoader, IProjectPath projectData)
        {
            _runtimeLoader = runtimeLoader;
            _projectPath = projectData;
            InitializeComponent();
            _inspector = new InspectorView(_runtimeLoader.AccessorsContainer, _runtimeLoader.Components);
            HierarchyListView.ItemsSource = _entities;
            InspectorAvaliableComponents.ItemsSource = _inspector.AvaliableComponents;
            InspectorScrollView.Content = _inspector;
            _runtimeLoader.OnUICallLoop += UpdateInspectors;
        }

        private void CreateScene(object sender, EventArgs e)
        {
            Runtime.CreateTestScene();
        }

        private void RunScene(object sender, ToggledEventArgs e)
        {
            var value = e.Value;
            _runtimeLoader.RuntimeRunning = value;
        }

        private void SaveScene(object sender, EventArgs e)
        {
            _runtimeLoader.Runtime.SaveScene("scene");
        }

        private void TryCompile(object sender, EventArgs e)
        {
            _runtimeLoader.ReloadRuntime();
            // TODO Inspector clear
            //_avaliableComponents.Clear();
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

        private Task UpdateInspectors()
        {
            return MainThread.InvokeOnMainThreadAsync(_inspector.UpdateComponentsData);
        }

        private void HierarchyListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            using var _ = _runtimeLoader.Runtime.Pause;
            var entityReference = _entities[e.SelectedItemIndex].EntityReference;
            if (!entityReference.IsAlive())
                return;
            UpdateComponentsNewest(entityReference);
        }

        private void UpdateComponentsNewest(EntityReference entityReference)
        {
            _inspector.UpdateComponentsEntity(entityReference);
        }

        private void CheckAccessors_Clicked(object sender, EventArgs e)
        {
            _runtimeLoader.CheckAccessors();
        }
    }
}
