using System;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using KeepersCompound.ModelEditor.Constants;
using KeepersCompound.ModelEditor.Render;
using KeepersCompound.ModelEditor.UI.About;
using Serilog;

namespace KeepersCompound.ModelEditor.UI.Menu;

[Meta(typeof(IAutoNode))]
public partial class EditorMenu : MenuBar
{
    public override void _Notification(int what) => this.Notify(what);

    #region Events

    public delegate void SavePressedEventHandler();
    public delegate void SaveAsPressedEventHandler();
    public delegate void QuitPressedEventHandler();
    public delegate void QuitToInstallsPressedEventHandler();
    public delegate void RefocusCameraEventHandler();
    public event SavePressedEventHandler? SavePressed;
    public event SaveAsPressedEventHandler? SaveAsPressed;
    public event QuitPressedEventHandler? QuitPressed;
    public event QuitToInstallsPressedEventHandler? QuitToInstallsPressed;
    public event RefocusCameraEventHandler? RefocusCameraPressed;

    #endregion

    [Node("%File")] private PopupMenu FileMenu { get; set; } = null!;
    [Node("%Edit")] private PopupMenu EditMenu { get; set; } = null!;
    [Node("%View")] private PopupMenu ViewMenu { get; set; } = null!;
    [Node("%Help")] private PopupMenu HelpMenu { get; set; } = null!;

    [Dependency] private EditorState EditorState => this.DependOn<EditorState>();

    private const string IssuesUrl = "https://codeberg.org/keepers-compound/kc-model-editor/issues/";

    public void OnReady()
    {
        FileMenu.IndexPressed += FileMenuOnIndexPressed;
        EditMenu.IndexPressed += EditMenuOnIndexPressed;
        ViewMenu.IndexPressed += ViewMenuOnIndexPressed;
        HelpMenu.IndexPressed += HelpMenuOnIndexPressed;
    }

    public void OnResolved()
    {
        EditorState.Config.ShowBoundingBoxChanged += EditorConfigOnShowBoundingBoxChanged;
        EditorState.Config.ShowWireframeChanged += EditorConfigOnShowWireframeChanged;
        EditorState.Config.ShowVHotsChanged += EditorConfigOnShowVHotsChanged;
        EditorState.Config.TextureModeChanged += EditorConfigOnTextureModeChanged;
        ViewMenu.SetItemChecked((int)ViewMenuIndex.BoundingBox, EditorState.Config.ShowBoundingBox);
        ViewMenu.SetItemChecked((int)ViewMenuIndex.Wireframe, EditorState.Config.ShowWireframe);
        ViewMenu.SetItemChecked((int)ViewMenuIndex.VHots, EditorState.Config.ShowVHots);
        SetFileMenuTextureMode(EditorState.Config.TextureMode);
    }

    public void OnExitTree()
    {
        FileMenu.IndexPressed -= FileMenuOnIndexPressed;
        EditMenu.IndexPressed -= EditMenuOnIndexPressed;
        ViewMenu.IndexPressed -= ViewMenuOnIndexPressed;
        HelpMenu.IndexPressed -= HelpMenuOnIndexPressed;
        EditorState.Config.ShowBoundingBoxChanged -= EditorConfigOnShowBoundingBoxChanged;
        EditorState.Config.ShowWireframeChanged -= EditorConfigOnShowWireframeChanged;
        EditorState.Config.ShowVHotsChanged -= EditorConfigOnShowVHotsChanged;
        EditorState.Config.TextureModeChanged -= EditorConfigOnTextureModeChanged;
    }

    #region Event Handling

    private void FileMenuOnIndexPressed(long indexLong)
    {
        var index = (FileMenuIndex)indexLong;
        switch (index)
        {
            case FileMenuIndex.Save:
                SavePressed?.Invoke();
                break;
            case FileMenuIndex.SaveAs:
                SaveAsPressed?.Invoke();
                break;
            case FileMenuIndex.Quit:
                QuitPressed?.Invoke();
                break;
            case FileMenuIndex.QuitToInstalls:
                QuitToInstallsPressed?.Invoke();
                break;
            default:
                Log.Debug("Unknown file menu index pressed: {index}", index);
                break;
        }
    }

    private void EditMenuOnIndexPressed(long indexLong)
    {
        var index = (EditMenuIndex)indexLong;
        switch (index)
        {
            case EditMenuIndex.Undo:
                if (EditorState.TryGetDocument(out var document))
                {
                    document.UndoAction();
                }

                break;
            case EditMenuIndex.Redo:
                if (EditorState.TryGetDocument(out document))
                {
                    document.RedoAction();
                }

                break;
            default:
                Log.Debug("Unknown edit menu index pressed: {index}", index);
                break;
        }
    }

    private void ViewMenuOnIndexPressed(long indexLong)
    {
        var index = (ViewMenuIndex)indexLong;
        switch (index)
        {
            case ViewMenuIndex.BoundingBox:
                EditorState.Config.ShowBoundingBox = !ViewMenu.IsItemChecked((int)index);
                break;
            case ViewMenuIndex.Wireframe:
                EditorState.Config.ShowWireframe = !ViewMenu.IsItemChecked((int)index);
                break;
            case ViewMenuIndex.VHots:
                EditorState.Config.ShowVHots = !ViewMenu.IsItemChecked((int)index);
                break;
            case ViewMenuIndex.Linear:
                EditorState.Config.TextureMode = TextureMode.Linear;
                break;
            case ViewMenuIndex.NearestNeighbour:
                EditorState.Config.TextureMode = TextureMode.NearestNeighbour;
                break;
            case ViewMenuIndex.RefocusCamera:
                RefocusCameraPressed?.Invoke();
                break;
            default:
                Log.Debug("Unknown view menu index pressed: {index}", index);
                break;
        }
    }

    private void HelpMenuOnIndexPressed(long indexLong)
    {
        var index = (HelpMenuIndex)indexLong;
        switch (index)
        {
            case HelpMenuIndex.ReportIssue:
                OS.ShellOpen(IssuesUrl);
                break;
            case HelpMenuIndex.About:
                if (GD.Load<PackedScene>(SceneUids.AboutWindow).Instantiate() is not AboutWindow instance)
                {
                    Log.Error("About Window failed to initialise.");
                    return;
                }

                AddChild(instance);
                break;
            default:
                Log.Debug("Unknown help menu index pressed: {index}", index);
                break;
        }
    }

    private void EditorConfigOnShowWireframeChanged(bool value)
    {
        ViewMenu.SetItemChecked((int)ViewMenuIndex.Wireframe, value);
    }

    private void EditorConfigOnShowBoundingBoxChanged(bool value)
    {
        ViewMenu.SetItemChecked((int)ViewMenuIndex.BoundingBox, value);
    }

    private void EditorConfigOnShowVHotsChanged(bool value)
    {
        ViewMenu.SetItemChecked((int)ViewMenuIndex.VHots, value);
    }

    private void EditorConfigOnTextureModeChanged(TextureMode value)
    {
        SetFileMenuTextureMode(value);
        var textureFilterMode = value switch
        {
            TextureMode.Linear => BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic,
            TextureMode.NearestNeighbour => BaseMaterial3D.TextureFilterEnum.NearestWithMipmapsAnisotropic,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

        foreach (var node in GetTree().GetNodesInGroup(GroupName.ModelMeshes))
        {
            if (node is not MeshInstance3D meshInstance)
            {
                continue;
            }

            var mesh = meshInstance.Mesh;
            for (var i = 0; i < mesh.GetSurfaceCount(); i++)
            {
                if (mesh.SurfaceGetMaterial(i) is StandardMaterial3D material)
                {
                    material.TextureFilter = textureFilterMode;
                }
            }
        }
    }

    #endregion

    private void SetFileMenuTextureMode(TextureMode textureMode)
    {
        switch (textureMode)
        {
            case TextureMode.Linear:
                ViewMenu.SetItemChecked((int)ViewMenuIndex.Linear, true);
                ViewMenu.SetItemChecked((int)ViewMenuIndex.NearestNeighbour, false);
                break;
            case TextureMode.NearestNeighbour:
                ViewMenu.SetItemChecked((int)ViewMenuIndex.Linear, false);
                ViewMenu.SetItemChecked((int)ViewMenuIndex.NearestNeighbour, true);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(textureMode), textureMode, null);
        }
    }
}