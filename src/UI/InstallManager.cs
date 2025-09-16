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
    
    private ConfigFile _configFile;
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

        _configFile = new ConfigFile();
        _validityMap = new Dictionary<string, bool>();
    }

    public void LoadConfig(string configFilePath)
    {
        _validityMap.Clear();
        _configFilePath = configFilePath;
        if (_configFile.Load(_configFilePath) == Error.Ok)
        {
            // The blank icon is needed so that each path has the same padding
            var invalidIcon = ResourceLoader.Load<Texture2D>("uid://dwnx0x7y5n0gu");
            var width = invalidIcon.GetWidth();
            var height = invalidIcon.GetHeight();
            var blankTexture = ImageTexture.CreateFromImage(Image.CreateEmpty(width, height, false, Image.Format.Rgba8));

            var paths = _configFile.GetValue("general", "install_paths", Array.Empty<string>()).AsStringArray();
            foreach (var path in paths)
            {
                var valid = IsInstallPathValid(path);
                _installPaths.AddItem(path, valid ? blankTexture : invalidIcon);
                _validityMap.Add(path, valid);
            }

            if (paths.Length > 0)
            {
                _installPaths.Select(0);
                SelectedInstallPath(0);
            }
        }
    }

    private void UpdateConfig()
    {
        var count = _installPaths.ItemCount;
        var paths = new List<string>();
        for (var i = 0; i < count; i++)
        {
            paths.Add(_installPaths.GetItemText(i));
        }

        _configFile.SetValue("general", "install_paths", paths.ToArray());
        _configFile.Save(_configFilePath);
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
        _installPaths.AddItem(path);
        _installPaths.SortItemsByText();
        UpdateConfig();
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
        UpdateConfig();

        _editButton.Disabled = true;
        _removeButton.Disabled = true;
        _loadButton.Disabled = true;
    }

    private void LoadInstallPath()
    {
        var idx = _installPaths.GetSelectedItems().FirstOrDefault(0);
        var path = _installPaths.GetItemText(idx);
        LoadInstall?.Invoke(path);
    }

    private static bool IsInstallPathValid(string path)
    {
        return new InstallContext(path).Valid;
    }
}