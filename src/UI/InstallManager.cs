using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using KeepersCompound.Dark;

namespace KeepersCompound.ModelEditor.UI;

public partial class InstallManager : Control
{
    public delegate void LoadInstallEventHandler(string installPath);

    public event LoadInstallEventHandler LoadInstall;

    private string _configFilePath;
    private LineEdit _searchBar;
    private Button _addButton;
    private Button _editButton;
    private Button _removeButton;
    private Button _loadButton;
    private ItemList _installPaths;
    private FileDialog _folderSelect;
    private ConfirmationDialog _invalidPathDialog;

    private bool _editMode = false;
    private Dictionary<string, bool> _validityMap;
    private Texture2D _invalidIcon = ResourceLoader.Load<Texture2D>("uid://dwnx0x7y5n0gu");
    private Texture2D? _blankIcon;

    public override void _Ready()
    {
        _searchBar = GetNode<LineEdit>("%SearchBar");
        _addButton = GetNode<Button>("%AddButton");
        _editButton = GetNode<Button>("%EditButton");
        _removeButton = GetNode<Button>("%RemoveButton");
        _loadButton = GetNode<Button>("%LoadButton");
        _installPaths = GetNode<ItemList>("%InstallPaths");
        _folderSelect = GetNode<FileDialog>("%FolderSelect");
        _invalidPathDialog = GetNode<ConfirmationDialog>("%InvalidPathDialog");

        _addButton.Pressed += () => _folderSelect.Visible = true;
        _editButton.Pressed += EditInstallPath;
        _removeButton.Pressed += RemoveInstallPath;
        _loadButton.Pressed += LoadInstallPath;
        _folderSelect.DirSelected += SelectDir;
        _folderSelect.Canceled += () => _editMode = false;
        _invalidPathDialog.Confirmed += () => _folderSelect.Visible = true;
        _installPaths.ItemActivated += _ => LoadInstallPath();
        _installPaths.ItemSelected += SelectedInstallPath;

        var width = _invalidIcon.GetWidth();
        var height = _invalidIcon.GetHeight();
        _blankIcon = ImageTexture.CreateFromImage(Image.CreateEmpty(width, height, false, Image.Format.Rgba8));

        _validityMap = new Dictionary<string, bool>();
        var paths = EditorConfig.Instance.InstallPaths;
        foreach (var path in paths)
        {
            var valid = IsInstallPathValid(path);
            _installPaths.AddItem(path, valid ? _blankIcon : _invalidIcon);
            _validityMap.Add(path, valid);
        }

        if (paths.Count > 0)
        {
            _installPaths.Select(0);
            SelectedInstallPath(0);
        }
    }

    private void SelectDir(string path)
    {
        if (IsInstallPathValid(path))
        {
            if (_editMode)
            {
                RemoveInstallPath();
            }

            AddInstallPath(path);
        }
        else
        {
            _invalidPathDialog.Show();
        }
    }

    private void SelectedInstallPath(long index)
    {
        var path = _installPaths.GetItemText((int)index);
        var valid = _validityMap.GetValueOrDefault(path, false);

        _editButton.Disabled = false;
        _removeButton.Disabled = false;
        _loadButton.Disabled = !valid;
    }

    private void AddInstallPath(string path)
    {
        _validityMap.Add(path, true);
        _installPaths.AddItem(path, _blankIcon);
        _installPaths.SortItemsByText();
        EditorConfig.Instance.InstallPaths.Add(path);
    }

    private void EditInstallPath()
    {
        var idx = _installPaths.GetSelectedItems().FirstOrDefault(0);
        var path = _installPaths.GetItemText(idx);

        _folderSelect.CurrentDir = path;
        _folderSelect.Visible = true;
        _editMode = true;
    }

    private void RemoveInstallPath()
    {
        var idx = _installPaths.GetSelectedItems().FirstOrDefault(0);
        var path = _installPaths.GetItemText(idx);
        _installPaths.RemoveItem(idx);
        _validityMap.Remove(path);
        EditorConfig.Instance.InstallPaths.Remove(path);

        _editButton.Disabled = true;
        _removeButton.Disabled = true;
        _loadButton.Disabled = true;
    }

    private void LoadInstallPath()
    {
        var idx = _installPaths.GetSelectedItems().FirstOrDefault(0);
        var path = _installPaths.GetItemText(idx);
        if (_validityMap.GetValueOrDefault(path, false))
        {
            LoadInstall?.Invoke(path);
        }
    }

    private static bool IsInstallPathValid(string path)
    {
        return new InstallContext(path).Valid;
    }
}