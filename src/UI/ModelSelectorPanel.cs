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

    public delegate void ModelSelectedEventHandler();

    public event ModelSelectedEventHandler? ModelSelected;

    #endregion

    #region Nodes

    private Button _reloadResourcesButton = null!;
    private LineEdit _searchBar = null!;
    private PopupMenu _sortMenu = null!;
    private Tree _modelsTree = null!;

    #endregion

    public string Campaign { get; private set; } = "";
    public string Model { get; private set; } = "";

    private EditorState _state = null!;
    private SortMode _currentSortMode;
    private Texture2D _folderIcon = ResourceLoader.Load<Texture2D>("uid://w5l7qwkxn1wo");
    private Texture2D _modelIcon = ResourceLoader.Load<Texture2D>("uid://5qhdsw7gx3h2");

    #region Overrides

    public override void _Ready()
    {
        _reloadResourcesButton = GetNode<Button>("%ReloadResourcesButton");
        _searchBar = GetNode<LineEdit>("%SearchBar");
        _sortMenu = GetNode<MenuButton>("%SortMenu").GetPopup();
        _modelsTree = GetNode<Tree>("%ModelsTree");

        _searchBar.TextChanged += OnSearchBarTextChanged;
        _sortMenu.IndexPressed += OnSortMenuIndexPressed;
        _modelsTree.ItemSelected += OnModelsTreeItemSelected;
    }

    public override void _ExitTree()
    {
        _searchBar.TextChanged -= OnSearchBarTextChanged;
        _sortMenu.IndexPressed -= OnSortMenuIndexPressed;
        _modelsTree.ItemSelected -= OnModelsTreeItemSelected;
    }

    #endregion

    #region Event Responses

    private void OnSearchBarTextChanged(string newText)
    {
        FilterTree(newText);
    }

    private void OnModelsTreeItemSelected()
    {
        // Avoids resending event when using search bar
        var item = _modelsTree.GetSelected();
        var metaData = item.GetMetadata(0).AsStringArray();
        var newCampaign = metaData[0];
        var newModel = metaData[1];
        if (newModel != Model || newCampaign != Campaign)
        {
            Model = newModel;
            Campaign = newCampaign;
            ModelSelected?.Invoke();
        }
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
        }

        if (treeRecalculationNeeded)
        {
            RebuildTree();
        }
    }

    #endregion

    public void SetEditorState(EditorState state)
    {
        _state = state;
        RebuildTree();
    }

    private void RebuildTree()
    {
        _modelsTree.Clear();

        var root = _modelsTree.CreateItem();
        _state.Resources.SetActiveCampaign("");
        AddCurrentCampaignModels("Original Resources");

        var fmsRaw = new SortedSet<string>(_state.Resources.Context.Fms);
        var fms = _currentSortMode switch
        {
            SortMode.NameAscending => fmsRaw,
            SortMode.NameDescending => fmsRaw.Reverse(),
            _ => throw new ArgumentOutOfRangeException()
        };
        foreach (var fm in fms)
        {
            _state.Resources.SetActiveCampaign(fm);
            AddCurrentCampaignModels(fm);
        }

        return;

        void AddCurrentCampaignModels(string parentText)
        {
            var campaignItem = root.CreateChild();
            campaignItem.SetSelectable(0, false);
            campaignItem.SetText(0, parentText);
            campaignItem.SetIcon(0, _folderIcon);
            campaignItem.Collapsed = true;

            var campaign = _state.Resources.ActiveCampaign;
            var rawItems = new SortedSet<string>(_state.Resources.GetModelNames());
            var items = _currentSortMode switch
            {
                SortMode.NameAscending => rawItems,
                SortMode.NameDescending => rawItems.Reverse(),
                _ => throw new ArgumentOutOfRangeException()
            };

            foreach (var item in items)
            {
                var child = campaignItem.CreateChild();
                child.SetText(0, item);
                child.SetMetadata(0, new[] { campaign, item });
                child.SetIcon(0, _modelIcon);
                if (campaign == Campaign && item == Model)
                {
                    child.Select(0);
                }
            }
        }
    }

    private void FilterTree(string filter)
    {
        var root = _modelsTree.GetRoot();
        foreach (var campaignItem in root.GetChildren())
        {
            var visibleChildren = 0;
            foreach (var modelItem in campaignItem.GetChildren())
            {
                var metaData = modelItem.GetMetadata(0).AsStringArray();
                var campaign = metaData[0];
                var model = metaData[1];
                modelItem.Visible =
                    campaign.Contains(filter, StringComparison.InvariantCultureIgnoreCase) ||
                    model.Contains(filter, StringComparison.InvariantCultureIgnoreCase);

                if (!modelItem.Visible)
                {
                    continue;
                }

                visibleChildren++;
                if (campaign == Campaign && model == Model)
                {
                    modelItem.Select(0);
                }
            }

            campaignItem.Visible = visibleChildren > 0;
        }
    }
}