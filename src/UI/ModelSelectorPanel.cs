using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

namespace KeepersCompound.ModelEditor.UI;

[Meta(typeof(IAutoNode))]
public partial class ModelSelectorPanel : PanelContainer
{
    public override void _Notification(int what) => this.Notify(what);

    private enum SortMode
    {
        NameAscending,
        NameDescending,
    }

    [Node] private Button ReloadResourcesButton { get; set; } = null!;
    [Node] private LineEdit SearchBar { get; set; } = null!;
    [Node] private MenuButton SortMenu { get; set; } = null!;
    [Node] private Tree ModelsTree { get; set; } = null!;
    private PopupMenu SortMenuPopup { get; set; } = null!;

    public string Campaign { get; private set; } = "";
    public string Model { get; private set; } = "";

    private EditorState _state = null!;
    private SortMode _currentSortMode;
    private Texture2D _folderIcon = ResourceLoader.Load<Texture2D>("uid://w5l7qwkxn1wo");
    private Texture2D _modelIcon = ResourceLoader.Load<Texture2D>("uid://5qhdsw7gx3h2");

    public void OnReady()
    {
        SortMenuPopup = SortMenu.GetPopup();

        SearchBar.TextChanged += OnSearchBarTextChanged;
        SortMenuPopup.IndexPressed += OnSortMenuIndexPressed;
        ModelsTree.ItemSelected += OnModelsTreeItemSelected;
    }

    public void OnExitTree()
    {
        SearchBar.TextChanged -= OnSearchBarTextChanged;
        SortMenuPopup.IndexPressed -= OnSortMenuIndexPressed;
        ModelsTree.ItemSelected -= OnModelsTreeItemSelected;
    }

    #region Event Responses

    private void OnSearchBarTextChanged(string newText)
    {
        FilterTree(newText);
    }

    private void OnModelsTreeItemSelected()
    {
        // Avoids resending event when using search bar
        var item = ModelsTree.GetSelected();
        var metaData = item.GetMetadata(0).AsStringArray();
        var newCampaign = metaData[0];
        var newModel = metaData[1];
        if (newModel != Model || newCampaign != Campaign)
        {
            Model = newModel;
            Campaign = newCampaign;
            _state.TrySetDocument(Campaign, Model);
        }
    }

    private void OnSortMenuIndexPressed(long indexLong)
    {
        var treeRecalculationNeeded = false;
        var index = (int)indexLong;
        switch (index)
        {
            case 1:
                SortMenuPopup.SetItemChecked(1, true);
                SortMenuPopup.SetItemChecked(2, false);
                treeRecalculationNeeded = _currentSortMode != SortMode.NameAscending;
                _currentSortMode = SortMode.NameAscending;
                break;
            case 2:
                SortMenuPopup.SetItemChecked(1, false);
                SortMenuPopup.SetItemChecked(2, true);
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
        ModelsTree.Clear();

        var root = ModelsTree.CreateItem();
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
        var root = ModelsTree.GetRoot();
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