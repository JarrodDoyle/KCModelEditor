using System.Collections.Generic;
using Godot;
using KeepersCompound.Dark;
using KeepersCompound.Dark.Resources;
using KeepersCompound.ModelEditor.Render;

namespace KeepersCompound.ModelEditor.UI;

public partial class ModelEditor : Control
{
    private InstallContext _installContext;
    private ResourceManager _resourceManager = new();
    private OptionButton _campaignsOptionButton;
    private Button _reloadResourcesButton;
    private Tree _modelsTree;
    private ModelViewport _modelViewport;

    public override void _Ready()
    {
        _campaignsOptionButton = GetNode<OptionButton>("%CampaignsOptionButton");
        _reloadResourcesButton = GetNode<Button>("%ReloadResourcesButton");
        _modelsTree = GetNode<Tree>("%ModelsTree");
        _modelViewport = GetNode<ModelViewport>("%ModelViewport");
        
        _campaignsOptionButton.ItemSelected += OnCampaignSelected;
        _modelsTree.ItemSelected += OnModelSelected;
    }

    private void OnModelSelected()
    {
        var modelName = _modelsTree.GetSelected().GetText(0);
        if (_resourceManager.TryGetModel(modelName, out var modelFile))
        {
            GD.Print(modelFile.VertexPositions.Count);
            _modelViewport.RenderModel(_resourceManager, modelFile);
        }
    }

    private void OnCampaignSelected(long index)
    {
        var campaignName = _campaignsOptionButton.GetItemText((int)index);
        _resourceManager.Initialise(_installContext, campaignName);
        
        _modelsTree.Clear();

        var modelNames = new SortedSet<string>(_resourceManager.ModelNames);
        var root = _modelsTree.CreateItem();
        foreach (var modelName in modelNames)
        {
            var child = root.CreateChild();
            child.SetText(0, modelName);
        }
    }

    public void SetInstallContext(InstallContext installContext)
    {
        _installContext = installContext;
        
        _modelsTree.Clear();
        _campaignsOptionButton.Clear();
        foreach (var fm in _installContext.Fms)
        {
            _campaignsOptionButton.AddItem(fm);
        }

        var popupMenu = _campaignsOptionButton.GetPopup();
        for (var i = 0; i < popupMenu.ItemCount; i++)
        {
            popupMenu.SetItemAsRadioCheckable(i, false);
        }
    }
}