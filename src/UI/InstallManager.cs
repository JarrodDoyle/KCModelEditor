using System.Collections.Generic;
using System.Linq;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using KeepersCompound.Dark;
using Serilog;

namespace KeepersCompound.ModelEditor.UI;

[Meta(typeof(IAutoNode))]
public partial class InstallManager : Control
{
    public override void _Notification(int what) => this.Notify(what);

    [Node] private LineEdit SearchBar { get; set; } = null!;
    [Node] private Button AddButton { get; set; } = null!;
    [Node] private Button EditButton { get; set; } = null!;
    [Node] private Button RemoveButton { get; set; } = null!;
    [Node] private Button LoadButton { get; set; } = null!;
    [Node] private ItemList InstallPaths { get; set; } = null!;
    [Node] private FileDialog FolderSelect { get; set; } = null!;
    [Node] private ConfirmationDialog InvalidPathDialog { get; set; } = null!;

    private EditorConfig Config { get; } = new();
    private bool _editMode = false;
    private readonly Dictionary<string, bool> _validityMap = new();
    private Texture2D _invalidIcon = ResourceLoader.Load<Texture2D>("uid://dwnx0x7y5n0gu");
    private Texture2D? _blankIcon;

    public void OnReady()
    {
        AddButton.Pressed += () => FolderSelect.Visible = true;
        EditButton.Pressed += EditInstallPath;
        RemoveButton.Pressed += RemoveInstallPath;
        LoadButton.Pressed += LoadInstallPath;
        FolderSelect.DirSelected += SelectDir;
        FolderSelect.Canceled += () => _editMode = false;
        InvalidPathDialog.Confirmed += () => FolderSelect.Visible = true;
        InstallPaths.ItemActivated += _ => LoadInstallPath();
        InstallPaths.ItemSelected += SelectedInstallPath;

        var width = _invalidIcon.GetWidth();
        var height = _invalidIcon.GetHeight();
        _blankIcon = ImageTexture.CreateFromImage(Image.CreateEmpty(width, height, false, Image.Format.Rgba8));

        var paths = Config.InstallPaths;
        foreach (var path in paths)
        {
            var valid = IsInstallPathValid(path);
            InstallPaths.AddItem(path, valid ? _blankIcon : _invalidIcon);
            _validityMap.Add(path, valid);
        }

        if (paths.Count > 0)
        {
            InstallPaths.Select(0);
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
            InvalidPathDialog.Show();
        }
    }

    private void SelectedInstallPath(long index)
    {
        var path = InstallPaths.GetItemText((int)index);
        var valid = _validityMap.GetValueOrDefault(path, false);

        EditButton.Disabled = false;
        RemoveButton.Disabled = false;
        LoadButton.Disabled = !valid;
    }

    private void AddInstallPath(string path)
    {
        _validityMap.Add(path, true);
        InstallPaths.AddItem(path, _blankIcon);
        InstallPaths.SortItemsByText();
        Config.InstallPaths.Add(path);
    }

    private void EditInstallPath()
    {
        var idx = InstallPaths.GetSelectedItems().FirstOrDefault(0);
        var path = InstallPaths.GetItemText(idx);

        FolderSelect.CurrentDir = path;
        FolderSelect.Visible = true;
        _editMode = true;
    }

    private void RemoveInstallPath()
    {
        var idx = InstallPaths.GetSelectedItems().FirstOrDefault(0);
        var path = InstallPaths.GetItemText(idx);
        InstallPaths.RemoveItem(idx);
        _validityMap.Remove(path);
        Config.InstallPaths.Remove(path);

        EditButton.Disabled = true;
        RemoveButton.Disabled = true;
        LoadButton.Disabled = true;
    }

    private void LoadInstallPath()
    {
        var idx = InstallPaths.GetSelectedItems().FirstOrDefault(0);
        var path = InstallPaths.GetItemText(idx);
        if (_validityMap.GetValueOrDefault(path, false))
        {
            var context = new InstallContext(path);
            if (!context.Valid)
            {
                Log.Error("Invalid install context, cannot load editor.");
                return;
            }

            var editorState = new EditorState(Config, context);
            var editor = (ModelEditor)GD.Load<PackedScene>(SceneUids.ModelEditor).Instantiate();
            editor.EditorState = editorState;
            var result = GetTree().ChangeSceneToNode(editor);
            if (result != Error.Ok)
            {
                Log.Error("Failed to change to editor scene.");
            }
        }
    }

    private static bool IsInstallPathValid(string path)
    {
        return new InstallContext(path).Valid;
    }
}