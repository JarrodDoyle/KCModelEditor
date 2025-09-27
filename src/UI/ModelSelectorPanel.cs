using System.Collections.Generic;
using Godot;

namespace KeepersCompound.ModelEditor.UI;

public partial class ModelSelectorPanel : PanelContainer
{
    #region Events

    public delegate void CampaignSelectedEventHandler();

    public delegate void ModelSelectedEventHandler();

    public event CampaignSelectedEventHandler? CampaignSelected;
    public event ModelSelectedEventHandler? ModelSelected;

    #endregion

    #region Nodes

    private OptionButton _campaignsOptionButton = null!;
    private Button _reloadResourcesButton = null!;
    private Tree _modelsTree = null!;

    #endregion

    public string Campaign { get; private set; } = "";
    public string Model { get; private set; } = "";

    private SortedSet<string> _campaignItems = [];
    private SortedSet<string> _modelItems = [];
    private bool _campaignItemsUpdated;
    private bool _modelItemsUpdated;

    #region Overrides

    public override void _Ready()
    {
        _campaignsOptionButton = GetNode<OptionButton>("%CampaignsOptionButton");
        _reloadResourcesButton = GetNode<Button>("%ReloadResourcesButton");
        _modelsTree = GetNode<Tree>("%ModelsTree");

        _campaignsOptionButton.ItemSelected += OnCampaignsOptionButtonItemSelected;
        _modelsTree.ItemSelected += OnModelsTreeItemSelected;
    }

    public override void _ExitTree()
    {
        _campaignsOptionButton.ItemSelected -= OnCampaignsOptionButtonItemSelected;
        _modelsTree.ItemSelected -= OnModelsTreeItemSelected;
    }

    public override void _Process(double delta)
    {
        if (_campaignItemsUpdated)
        {
            _campaignItemsUpdated = false;
            _campaignsOptionButton.Clear();

            var popupMenu = _campaignsOptionButton.GetPopup();
            foreach (var item in _campaignItems)
            {
                _campaignsOptionButton.AddItem(item);
                popupMenu.SetItemAsRadioCheckable(popupMenu.ItemCount - 1, false);
            }
        }

        if (_modelItemsUpdated)
        {
            _modelItemsUpdated = false;
            _modelsTree.Clear();

            var root = _modelsTree.CreateItem();
            foreach (var item in _modelItems)
            {
                var child = root.CreateChild();
                child.SetText(0, item);
            }
        }
    }

    #endregion

    #region Event Responses

    private void OnModelsTreeItemSelected()
    {
        Model = _modelsTree.GetSelected().GetText(0);
        ModelSelected?.Invoke();
    }

    private void OnCampaignsOptionButtonItemSelected(long index)
    {
        Campaign = _campaignsOptionButton.GetItemText((int)index);
        CampaignSelected?.Invoke();
    }

    #endregion

    public void SetCampaigns(SortedSet<string> campaigns)
    {
        _campaignItemsUpdated = true;
        _campaignItems = campaigns;
    }

    public void SetModels(SortedSet<string> models)
    {
        _modelItemsUpdated = true;
        _modelItems = models;
    }
}