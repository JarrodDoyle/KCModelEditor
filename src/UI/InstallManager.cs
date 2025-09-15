using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace KeepersCompound.ModelEditor.UI;

public partial class InstallManager : Control
{
    public delegate void LoadInstallEventHandler(string installPath);

    public event LoadInstallEventHandler LoadInstall;
    
    private ConfigFile _configFile;
    private string _configFilePath;
    private LineEdit _searchBar;
    private Button _addButton;
    private Button _removeButton;
    private Button _loadButton;
    private ItemList _installPaths;
    private FileDialog _folderSelect;

    public override void _Ready()
    {
        _searchBar = GetNode<LineEdit>("%SearchBar");
        _addButton = GetNode<Button>("%AddButton");
        _removeButton = GetNode<Button>("%RemoveButton");
        _loadButton = GetNode<Button>("%LoadButton");
        _installPaths = GetNode<ItemList>("%InstallPaths");
        _folderSelect = GetNode<FileDialog>("%FolderSelect");

        _addButton.Pressed += () => _folderSelect.Visible = true;
        _removeButton.Pressed += RemoveDir;
        _loadButton.Pressed += LoadDir;
        _folderSelect.DirSelected += AddDir;
        _installPaths.ItemActivated += _ => LoadDir();
        _installPaths.ItemSelected += _ =>
        {
            _removeButton.Disabled = false;
            _loadButton.Disabled = false;
        };

        _configFile = new ConfigFile();
    }

    public void LoadConfig(string configFilePath)
    {
        _configFilePath = configFilePath;
        if (_configFile.Load(_configFilePath) == Error.Ok)
        {
            var paths = _configFile.GetValue("general", "install_paths", Array.Empty<string>()).AsStringArray();
            foreach (var path in paths)
            {
                _installPaths.AddItem(path);
            }

            if (paths.Length > 0)
            {
                _installPaths.Select(0);
                _removeButton.Disabled = false;
                _loadButton.Disabled = false;
            }
        }
    }

    private void AddDir(string path)
    {
        _installPaths.AddItem(path);
        _installPaths.SortItemsByText();
        UpdateConfig();
    }

    private void RemoveDir()
    {
        var idx = _installPaths.GetSelectedItems().FirstOrDefault(0);
        _installPaths.RemoveItem(idx);
        UpdateConfig();

        _removeButton.Disabled = true;
        _loadButton.Disabled = true;
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

    private void LoadDir()
    {
        var idx = _installPaths.GetSelectedItems().FirstOrDefault(0);
        var path = _installPaths.GetItemText(idx);
        LoadInstall?.Invoke(path);
    }
}