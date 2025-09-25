using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Godot;

namespace KeepersCompound.ModelEditor.UI;

public partial class ItemSelectorWindow : Window
{
    #region Events

    public delegate void CanceledEventHandler();

    public delegate void SelectedEventHandler(int index);

    public event CanceledEventHandler? Canceled;
    public event SelectedEventHandler? Selected;

    #endregion

    #region Nodes

    private LineEdit? _searchBar;
    private ItemList? _itemList;
    private Button? _cancelButton;
    private Button? _selectButton;

    #endregion

    private readonly List<string> _items = [];

    #region Overrides

    public override void _Ready()
    {
        _searchBar = GetNode<LineEdit>("%SearchBar");
        _itemList = GetNode<ItemList>("%ItemList");
        _cancelButton = GetNode<Button>("%CancelButton");
        _selectButton = GetNode<Button>("%SelectButton");

        CloseRequested += TriggerCanceled;
        _cancelButton.Pressed += TriggerCanceled;
        _selectButton.Pressed += TriggerSelected;
        _itemList.ItemSelected += ItemListOnItemSelected;
        _itemList.ItemActivated += _ => TriggerSelected();
        _searchBar.TextChanged += _ => UpdateItemList();

        UpdateItemList();
    }

    #endregion

    public void AddItem(string item)
    {
        _items.Add(item);
    }

    public bool TryGetItem(int index, [MaybeNullWhen(false)] out string item)
    {
        item = _itemList?.GetItemText(index);
        return item != null;
    }

    private void TriggerCanceled()
    {
        Canceled?.Invoke();
        QueueFree();
    }

    private void TriggerSelected()
    {
        QueueFree();
        if (_itemList == null)
        {
            return;
        }

        var selectedItems = _itemList.GetSelectedItems();
        if (selectedItems.Length > 0)
        {
            Selected?.Invoke(selectedItems[0]);
        }
    }

    private void UpdateItemList()
    {
        _itemList?.Clear();
        var search = _searchBar == null ? "" : _searchBar.Text;
        foreach (var item in _items)
        {
            if (item.Contains(search, StringComparison.InvariantCultureIgnoreCase))
            {
                _itemList?.AddItem(item);
            }
        }

        _itemList?.SortItemsByText();
        _selectButton?.Disabled = true;
    }

    #region Event Handling

    private void ItemListOnItemSelected(long index)
    {
        _selectButton?.Disabled = false;
    }

    #endregion
}