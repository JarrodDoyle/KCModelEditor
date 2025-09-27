using System;
using System.Collections.Generic;
using Godot;
using KeepersCompound.Dark.Resources;

namespace KeepersCompound.ModelEditor.UI;

public partial class ModelSelectorPanel : PanelContainer
{
    private enum SortMode
    {
        NameAscending,
        NameDescending,
    }

    #region Events

    public delegate void CampaignSelectedEventHandler();

    public delegate void ModelSelectedEventHandler();

    public event CampaignSelectedEventHandler? CampaignSelected;
    public event ModelSelectedEventHandler? ModelSelected;

    #endregion

    #region Nodes

    private OptionButton _campaignsOptionButton = null!;
    private Button _reloadResourcesButton = null!;
    private LineEdit _searchBar = null!;
    private PopupMenu _sortMenu = null!;
    private Tree _modelsTree = null!;

    #endregion

    public string Campaign { get; private set; } = "";
    public string Model { get; private set; } = "";

    private ResourceManager _resourceManager = new();
    private SortedSet<string> _campaignItems = [];
    private SortedSet<string> _modelItems = [];
    private bool _campaignItemsUpdated;
    private bool _modelItemsUpdated;
    private SortMode _currentSortMode;
    private bool _showOriginalModels;

    #region Overrides

    public override void _Ready()
    {
        _campaignsOptionButton = GetNode<OptionButton>("%CampaignsOptionButton");
        _reloadResourcesButton = GetNode<Button>("%ReloadResourcesButton");
        _searchBar = GetNode<LineEdit>("%SearchBar");
        _sortMenu = GetNode<MenuButton>("%SortMenu").GetPopup();
        _modelsTree = GetNode<Tree>("%ModelsTree");

        _campaignsOptionButton.ItemSelected += OnCampaignsOptionButtonItemSelected;
        _searchBar.TextChanged += OnSearchBarTextChanged;
        _sortMenu.IndexPressed += OnSortMenuIndexPressed;
        _modelsTree.ItemSelected += OnModelsTreeItemSelected;
    }

    public override void _ExitTree()
    {
        _campaignsOptionButton.ItemSelected -= OnCampaignsOptionButtonItemSelected;
        _searchBar.TextChanged -= OnSearchBarTextChanged;
        _sortMenu.IndexPressed -= OnSortMenuIndexPressed;
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
            RecalculateModelsTree();
        }
    }

    #endregion

    #region Event Responses

    private void OnSearchBarTextChanged(string newText)
    {
        RecalculateModelsTree();
    }

    private void OnModelsTreeItemSelected()
    {
        // Avoids resending event when using search bar
        var newModel = _modelsTree.GetSelected().GetText(0);
        if (newModel != Model)
        {
            Model = newModel;
            ModelSelected?.Invoke();
        }
    }

    private void OnCampaignsOptionButtonItemSelected(long index)
    {
        Campaign = _campaignsOptionButton.GetItemText((int)index);
        CampaignSelected?.Invoke();
    }

    private void OnSortMenuIndexPressed(long indexLong)
    {
        var treeRecalculationNeeded = false;
        var index = (int)indexLong;
        switch (index)
        {
            case 1:
                _sortMenu.SetItemChecked(1, true);
                _sortMenu.SetItemChecked(2, false);
                treeRecalculationNeeded = _currentSortMode != SortMode.NameAscending;
                _currentSortMode = SortMode.NameAscending;
                break;
            case 2:
                _sortMenu.SetItemChecked(1, false);
                _sortMenu.SetItemChecked(2, true);
                treeRecalculationNeeded = _currentSortMode != SortMode.NameDescending;
                _currentSortMode = SortMode.NameDescending;
                break;
            case 4:
                _showOriginalModels = !_sortMenu.IsItemChecked(4);
                _sortMenu.SetItemChecked(4, _showOriginalModels);
                treeRecalculationNeeded = true;
                break;
        }

        if (treeRecalculationNeeded)
        {
            RecalculateModelsTree();
        }
    }

    #endregion

    public void SetResourceManager(ResourceManager resourceManager)
    {
        _resourceManager = resourceManager;
    }

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

    private void RecalculateModelsTree()
    {
        _modelsTree.Clear();

        var filter = _searchBar.Text;
        var items = _currentSortMode switch {
            SortMode.NameAscending => _modelItems,
            SortMode.NameDescending => _modelItems.Reverse(),
            _ => throw new ArgumentOutOfRangeException()
        };
        var root = _modelsTree.CreateItem();
        foreach (var item in items)
        {
            if (!item.Contains(filter, StringComparison.InvariantCultureIgnoreCase))
            {
                continue;
            }

            if (!_showOriginalModels && !_resourceManager.TryGetFilePath($"obj/{item}.bin", out _))
            {
                continue;
            }

            var child = root.CreateChild();
            child.SetText(0, item);
            if (item == Model)
            {
                child.Select(0);
            }
        }
    }
}