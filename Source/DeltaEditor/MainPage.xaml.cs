using Arch.Core;
using Arch.Core.Extensions;
using Delta.ECS.Components;
using DeltaEditor.Inspector;
using DeltaEditorLib.Scripting;
using System.Collections.ObjectModel;

namespace DeltaEditor;

public partial class MainPage : ContentPage
{
    private readonly RuntimeLoader _runtimeLoader;

    private readonly ObservableCollection<HierarchyEntityView> _entities = [];

    private readonly InspectorView _inspector;

    public MainPage(RuntimeLoader runtimeLoader)
    {
        _runtimeLoader = runtimeLoader;
        InitializeComponent();
        _inspector = new InspectorView(_runtimeLoader);
        HierarchyListView.ItemsSource = _entities;
        InspectorScrollView.Content = _inspector;
        _runtimeLoader.OnUIThreadLoop += _inspector.UpdateComponentsData;
    }

    private void CreateScene(object sender, EventArgs e)
    {
        _runtimeLoader.OnRuntimeThread += (r) => r.CreateTestScene();
    }

    private void RunScene(object sender, ToggledEventArgs e)
    {
        var value = e.Value;
        //_runtimeLoader.RuntimeRunning = value;
    }

    private void SaveScene(object sender, EventArgs e)
    {
        _runtimeLoader.OnRuntimeThread += (r) => r.SaveScene("scene");
    }

    private void TryCompile(object sender, EventArgs e)
    {
        _runtimeLoader.ReloadRuntime();
        // TODO Inspector clear
        //_avaliableComponents.Clear();
    }


    private void OpenProjectFolder(object sender, EventArgs e)
    {
        _runtimeLoader.OpenProjectFolder();
    }

    private void PauseButton_Clicked(object sender, EventArgs e)
    {

    }

    private void NextButton_Clicked(object sender, EventArgs e)
    {

    }

    private void UpdateHierarchyButton_Clicked(object sender, EventArgs e)
    {
        _runtimeLoader.OnUIThread += (r) =>
        {
            _entities.Clear();
            var entities = r.GetEntities();
            foreach (var entityReference in entities)
            {
                if (!entityReference.Entity.IsAlive())
                    continue;

                string name = EntityString(entityReference);
                _entities.Add(new HierarchyEntityView(entityReference, name));
            }
        };
    }

    private void HierarchyListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
    {
        _runtimeLoader.OnUIThread += (r) =>
        {
            var entityReference = _entities[e.SelectedItemIndex].EntityReference;
            if (!entityReference.IsAlive())
                return;
            _inspector.UpdateComponentsEntity(r, entityReference);
        };
    }

    private static string EntityString(EntityReference entityReference)
    {
        var entity = entityReference.Entity;
        if (entity.TryGet<EntityName>(out var entityName))
            return entityName.text;

        return $"id: {entity.Id}, ver: {entityReference.Version}";
    }
}