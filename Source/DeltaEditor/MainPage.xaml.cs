using DeltaEditor.Inspector;
using DeltaEditorLib.Loader;

namespace DeltaEditor;

public partial class MainPage : ContentPage
{
    private readonly RuntimeLoader _runtimeLoader;

    private readonly InspectorView _inspector;
    private readonly HierarchyView _hierarchy;

    public MainPage(RuntimeLoader runtimeLoader)
    {
        _runtimeLoader = runtimeLoader;
        InitializeComponent();
        _hierarchy = new HierarchyView(_runtimeLoader);
        _inspector = new InspectorView(_runtimeLoader);
        InspectorScrollView.Content = _inspector;
        HierarchyScrollView.Content = _hierarchy;
        _runtimeLoader.OnUIThreadLoop += _inspector.UpdateComponentsData;
        _runtimeLoader.OnUIThreadLoop += _hierarchy.UpdateHierarchy;
        _hierarchy.OnEntitySelected += _inspector.UpdateComponentsEntity;
    }

    private void CreateScene(object sender, EventArgs e)
    {
        _runtimeLoader.OnRuntimeThread += (r) => r.Context.SceneManager.CreateTestScene();
    }

    private void RunScene(object sender, ToggledEventArgs e)
    {
        var value = e.Value;
        // _runtimeLoader.OnRuntimeThread+=r=>r.
    }

    private void SaveScene(object sender, EventArgs e)
    {
        _runtimeLoader.OnRuntimeThread += (r) => r.Context.SceneManager.SaveScene("scene");
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


    private void ImportFbx_Clicked(object sender, EventArgs e)
    {
        //var filePick = await FilePicker.Default.PickAsync();
    }

    /*

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
    */
}