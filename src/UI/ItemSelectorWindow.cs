using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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

    private LineEdit _searchBar = null!;
    private ItemList _itemList = null!;
    private Button _cancelButton = null!;
    private Button _selectButton = null!;

    #endregion

    private readonly List<string> _items = [];

    #region Overrides

    public override void _Ready()
    {
        _searchBar = GetNode<LineEdit>("%SearchBar");
        _itemList = GetNode<ItemList>("%ItemList");
        _cancelButton = GetNode<Button>("%CancelButton");
        _selectButton = GetNode<Button>("%SelectButton");

        CloseRequested += OnCloseRequested;
        _cancelButton.Pressed += CancelButtonOnPressed;
        _selectButton.Pressed += SelectButtonOnPressed;
        _itemList.ItemSelected += ItemListOnItemSelected;
        _itemList.ItemActivated += ItemListOnItemActivated;
        _searchBar.TextChanged += SearchBarOnTextChanged;

        UpdateItemList("");
    }

    public override void _ExitTree()
    {
        CloseRequested -= OnCloseRequested;
        _cancelButton.Pressed -= CancelButtonOnPressed;
        _selectButton.Pressed -= SelectButtonOnPressed;
        _itemList.ItemSelected -= ItemListOnItemSelected;
        _itemList.ItemActivated -= ItemListOnItemActivated;
        _searchBar.TextChanged -= SearchBarOnTextChanged;
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
        TriggerCanceled();
    }

    private void ItemListOnItemSelected(long index)
    {
        _selectButton.Disabled = false;
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
        item = _itemList.GetItemText(index);
        return item != null;
    }

    public bool TrySelectItem(string targetText)
    {
        for (var i = 0; i < _itemList.ItemCount; i++)
        {
            if (_itemList.GetItemText(i) != targetText)
            {
                continue;
            }

            _itemList.Select(i);
            _itemList.EnsureCurrentIsVisible();
            _selectButton.Disabled = false;
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
        var selectedItems = _itemList.GetSelectedItems();
        if (selectedItems.Length > 0)
        {
            Selected?.Invoke(selectedItems[0]);
        }
    }

    private void UpdateItemList(string search)
    {
        _selectButton.Disabled = true;
        var previousSelection = _itemList.IsAnythingSelected()
            ? _itemList.GetItemText(_itemList.GetSelectedItems()[0])
            : null;

        _itemList.Clear();
        foreach (var item in _items)
        {
            if (item.Contains(search, StringComparison.InvariantCultureIgnoreCase))
            {
                _itemList.AddItem(item);
                if (item != previousSelection)
                {
                    continue;
                }

                _itemList.Select(_itemList.ItemCount - 1);
                _selectButton.Disabled = false;
            }
        }

        _itemList.SortItemsByText();
        _itemList.EnsureCurrentIsVisible();
    }
}