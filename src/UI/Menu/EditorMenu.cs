using System;
using Godot;
using KeepersCompound.ModelEditor.Render;
using Serilog;

namespace KeepersCompound.ModelEditor.UI.Menu;

public partial class EditorMenu : MenuBar
{
    #region Events

    public delegate void SavePressedEventHandler();

    public delegate void SaveAsPressedEventHandler();

    public delegate void QuitPressedEventHandler();

    public delegate void QuitToInstallsPressedEventHandler();

    public delegate void UndoPressedEventHandler();

    public delegate void RedoPressedEventHandler();

    public event SavePressedEventHandler? SavePressed;
    public event SaveAsPressedEventHandler? SaveAsPressed;
    public event QuitPressedEventHandler? QuitPressed;
    public event QuitToInstallsPressedEventHandler? QuitToInstallsPressed;
    public event UndoPressedEventHandler? UndoPressed;
    public event RedoPressedEventHandler? RedoPressed;

    #endregion

    #region Nodes

    private PopupMenu _fileMenu = null!;
    private PopupMenu _editMenu = null!;
    private PopupMenu _viewMenu = null!;

    #endregion

    private EditorState _state = null!;

    #region Godot Overrides

    public override void _Ready()
    {
        _fileMenu = GetNode<PopupMenu>("%File");
        _editMenu = GetNode<PopupMenu>("%Edit");
        _viewMenu = GetNode<PopupMenu>("%View");

        _fileMenu.IndexPressed += FileMenuOnIndexPressed;
        _editMenu.IndexPressed += EditMenuOnIndexPressed;
        _viewMenu.IndexPressed += ViewMenuOnIndexPressed;
    }

    public override void _ExitTree()
    {
        _fileMenu.IndexPressed -= FileMenuOnIndexPressed;
        _editMenu.IndexPressed -= EditMenuOnIndexPressed;
        _viewMenu.IndexPressed -= ViewMenuOnIndexPressed;
        _state.Config.ShowBoundingBoxChanged -= EditorConfigOnShowBoundingBoxChanged;
        _state.Config.ShowWireframeChanged -= EditorConfigOnShowWireframeChanged;
        _state.Config.ShowVHotsChanged -= EditorConfigOnShowVHotsChanged;
        _state.Config.TextureModeChanged -= EditorConfigOnTextureModeChanged;
    }

    #endregion

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
                UndoPressed?.Invoke();
                break;
            case EditMenuIndex.Redo:
                RedoPressed?.Invoke();
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
                _state.Config.ShowBoundingBox = !_viewMenu.IsItemChecked((int)index);
                break;
            case ViewMenuIndex.Wireframe:
                _state.Config.ShowWireframe = !_viewMenu.IsItemChecked((int)index);
                break;
            case ViewMenuIndex.VHots:
                _state.Config.ShowVHots = !_viewMenu.IsItemChecked((int)index);
                break;
            case ViewMenuIndex.Linear:
                _state.Config.TextureMode = TextureMode.Linear;
                break;
            case ViewMenuIndex.NearestNeighbour:
                _state.Config.TextureMode = TextureMode.NearestNeighbour;
                break;
            default:
                Log.Debug("Unknown view menu index pressed: {index}", index);
                break;
        }
    }

    private void EditorConfigOnShowWireframeChanged(bool value)
    {
        _viewMenu.SetItemChecked((int)ViewMenuIndex.Wireframe, value);
    }

    private void EditorConfigOnShowBoundingBoxChanged(bool value)
    {
        _viewMenu.SetItemChecked((int)ViewMenuIndex.BoundingBox, value);
    }

    private void EditorConfigOnShowVHotsChanged(bool value)
    {
        _viewMenu.SetItemChecked((int)ViewMenuIndex.VHots, value);
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

    public void SetState(EditorState state)
    {
        _state = state;

        _state.Config.ShowBoundingBoxChanged += EditorConfigOnShowBoundingBoxChanged;
        _state.Config.ShowWireframeChanged += EditorConfigOnShowWireframeChanged;
        _state.Config.ShowVHotsChanged += EditorConfigOnShowVHotsChanged;
        _state.Config.TextureModeChanged += EditorConfigOnTextureModeChanged;
        _viewMenu.SetItemChecked((int)ViewMenuIndex.BoundingBox, _state.Config.ShowBoundingBox);
        _viewMenu.SetItemChecked((int)ViewMenuIndex.Wireframe, _state.Config.ShowWireframe);
        _viewMenu.SetItemChecked((int)ViewMenuIndex.VHots, _state.Config.ShowVHots);
        SetFileMenuTextureMode(_state.Config.TextureMode);
    }

    private void SetFileMenuTextureMode(TextureMode textureMode)
    {
        switch (textureMode)
        {
            case TextureMode.Linear:
                _viewMenu.SetItemChecked((int)ViewMenuIndex.Linear, true);
                _viewMenu.SetItemChecked((int)ViewMenuIndex.NearestNeighbour, false);
                break;
            case TextureMode.NearestNeighbour:
                _viewMenu.SetItemChecked((int)ViewMenuIndex.Linear, false);
                _viewMenu.SetItemChecked((int)ViewMenuIndex.NearestNeighbour, true);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(textureMode), textureMode, null);
        }
    }
}