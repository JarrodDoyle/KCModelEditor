using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

namespace KeepersCompound.ModelEditor.UI;

[Meta(typeof(IAutoNode))]
public partial class ItemSelectorWindow : Window
{
    public override void _Notification(int what) => this.Notify(what);

    #region Events

    public delegate void CanceledEventHandler();
    public delegate void SelectedEventHandler(int index);
    public event CanceledEventHandler? Canceled;
    public event SelectedEventHandler? Selected;

    #endregion

    [Node] private LineEdit SearchBar { get; set; } = null!;
    [Node] private ItemList ItemList { get; set; } = null!;
    [Node] private Button CancelButton { get; set; } = null!;
    [Node] private Button SelectButton { get; set; } = null!;

    private readonly HashSet<string> _items = [];

    #region Overrides

    public void OnReady()
    {
        CloseRequested += OnCloseRequested;
        CancelButton.Pressed += CancelButtonOnPressed;
        SelectButton.Pressed += SelectButtonOnPressed;
        ItemList.ItemSelected += ItemListOnItemSelected;
        ItemList.ItemActivated += ItemListOnItemActivated;
        SearchBar.TextChanged += SearchBarOnTextChanged;

        UpdateItemList("");
    }

    public void OnExitTree()
    {
        CloseRequested -= OnCloseRequested;
        CancelButton.Pressed -= CancelButtonOnPressed;
        SelectButton.Pressed -= SelectButtonOnPressed;
        ItemList.ItemSelected -= ItemListOnItemSelected;
        ItemList.ItemActivated -= ItemListOnItemActivated;
        SearchBar.TextChanged -= SearchBarOnTextChanged;
    }

    #endregion

    #region Event Handling

    private void OnCloseRequested()
    {
        TriggerCanceled();
    }

    private void CancelButtonOnPressed()
    {
        TriggerCanceled();
    }

    private void SelectButtonOnPressed()
    {
        TriggerSelected();
    }

    private void ItemListOnItemSelected(long index)
    {
        SelectButton.Disabled = false;
    }

    private void ItemListOnItemActivated(long index)
    {
        TriggerSelected();
    }

    private void SearchBarOnTextChanged(string newText)
    {
        UpdateItemList(newText);
    }

    #endregion

    public void AddItem(string item)
    {
        _items.Add(item);
    }

    public bool TryGetItem(int index, [MaybeNullWhen(false)] out string item)
    {
        item = ItemList.GetItemText(index);
        return item != null;
    }

    public bool TrySelectItem(string targetText)
    {
        for (var i = 0; i < ItemList.ItemCount; i++)
        {
            if (ItemList.GetItemText(i) != targetText)
            {
                continue;
            }

            ItemList.Select(i);
            ItemList.EnsureCurrentIsVisible();
            SelectButton.Disabled = false;
            return true;
        }

        return false;
    }

    private void TriggerCanceled()
    {
        Canceled?.Invoke();
        QueueFree();
    }

    private void TriggerSelected()
    {
        QueueFree();
        var selectedItems = ItemList.GetSelectedItems();
        if (selectedItems.Length > 0)
        {
            Selected?.Invoke(selectedItems[0]);
        }
    }

    private void UpdateItemList(string search)
    {
        SelectButton.Disabled = true;
        var previousSelection = ItemList.IsAnythingSelected()
            ? ItemList.GetItemText(ItemList.GetSelectedItems()[0])
            : null;

        ItemList.Clear();
        foreach (var item in _items)
        {
            if (item.Contains(search, StringComparison.InvariantCultureIgnoreCase))
            {
                ItemList.AddItem(item);
                if (item != previousSelection)
                {
                    continue;
                }

                ItemList.Select(ItemList.ItemCount - 1);
                SelectButton.Disabled = false;
            }
        }

        ItemList.SortItemsByText();
        ItemList.EnsureCurrentIsVisible();
    }
}