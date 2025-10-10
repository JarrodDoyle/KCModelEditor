using System;
using System.IO;
using System.Text;
using Godot;
using KeepersCompound.Dark;
using KeepersCompound.Dark.Resources;
using KeepersCompound.Formats.Model;
using KeepersCompound.ModelEditor.Render;
using Serilog;

namespace KeepersCompound.ModelEditor.UI;

public partial class ModelEditor : Control
{
    private InstallContext _installContext = null!;
    private ResourceManager _resourceManager = null!;
    private ModelFile? _currentModel;

    #region Nodes

    private ModelSelectorPanel _modelSelectorPanel = null!;
    private ModelViewport _modelViewport = null!;
    private ModelInspector _modelInspector = null!;
    private PopupMenu _fileMenu = null!;
    private PopupMenu _viewMenu = null!;
    private FileDialog _saveAsDialog = null!;

    #endregion

    #region Overrides

    public override void _Ready()
    {
        _modelSelectorPanel = GetNode<ModelSelectorPanel>("%ModelSelectorPanel");
        _modelViewport = GetNode<ModelViewport>("%ModelViewport");
        _modelInspector = GetNode<ModelInspector>("%ModelInspector");
        _fileMenu = GetNode<PopupMenu>("%File");
        _viewMenu = GetNode<PopupMenu>("%View");
        _saveAsDialog = GetNode<FileDialog>("%SaveAsDialog");

        _viewMenu.SetItemChecked(0, EditorConfig.Instance.ShowBoundingBox);
        _viewMenu.SetItemChecked(1, EditorConfig.Instance.ShowWireframe);
        SetFileMenuTextureMode(EditorConfig.Instance.TextureMode);

        EditorConfig.Instance.ShowBoundingBoxChanged += EditorConfigOnShowBoundingBoxChanged;
        EditorConfig.Instance.ShowWireframeChanged += EditorConfigOnShowWireframeChanged;
        EditorConfig.Instance.TextureModeChanged += EditorConfigOnTextureModeChanged;
        _modelSelectorPanel.ModelSelected += OnModelSelected;
        _modelInspector.ModelEdited += OnModelEdited;
        _viewMenu.IndexPressed += ViewMenuOnIndexPressed;
        _fileMenu.IndexPressed += FileMenuOnIndexPressed;
        _saveAsDialog.FileSelected += SaveAsDialogOnFileSelected;
    }

    public override void _ExitTree()
    {
        _modelSelectorPanel.ModelSelected -= OnModelSelected;
        _modelInspector.ModelEdited -= OnModelEdited;
        _viewMenu.IndexPressed -= ViewMenuOnIndexPressed;
        _fileMenu.IndexPressed -= FileMenuOnIndexPressed;
        _saveAsDialog.FileSelected -= SaveAsDialogOnFileSelected;

        EditorConfig.Instance.ShowBoundingBoxChanged -= EditorConfigOnShowBoundingBoxChanged;
        EditorConfig.Instance.ShowWireframeChanged -= EditorConfigOnShowWireframeChanged;
        EditorConfig.Instance.TextureModeChanged -= EditorConfigOnTextureModeChanged;
    }

    #endregion

    #region EventHandling

    private void SaveAsDialogOnFileSelected(string path)
    {
        if (_currentModel != null)
        {
            Save(path, _currentModel);
        }
    }

    private void ViewMenuOnIndexPressed(long indexLong)
    {
        var index = (int)indexLong;
        switch (index)
        {
            case 0:
                EditorConfig.Instance.ShowBoundingBox = !_viewMenu.IsItemChecked(index);
                break;
            case 1:
                EditorConfig.Instance.ShowWireframe = !_viewMenu.IsItemChecked(index);
                break;
            case 3:
                EditorConfig.Instance.TextureMode = TextureMode.Linear;
                break;
            case 4:
                EditorConfig.Instance.TextureMode = TextureMode.NearestNeighbour;
                break;
        }
    }

    private void FileMenuOnIndexPressed(long indexLong)
    {
        var index = (int)indexLong;
        switch (index)
        {
            case 0:
                if (_currentModel != null)
                {
                    var modelName = _modelSelectorPanel.Model;
                    if (_resourceManager.TryGetFilePath($"obj/{modelName}.bin", out var path))
                    {
                        Save(path, _currentModel);
                    }
                    else
                    {
                        _saveAsDialog.Show();
                    }
                }

                break;
            case 1:
                if (_currentModel != null)
                {
                    _saveAsDialog.Show();
                }

                break;
        }
    }

    private void OnModelEdited()
    {
        if (_currentModel != null)
        {
            _modelViewport.RenderModel(_resourceManager, _currentModel);
        }
    }

    private void OnModelSelected()
    {
        var campaignName = _modelSelectorPanel.Campaign;
        var campaignPath = Path.Join(_installContext.FmsDir, campaignName);
        _resourceManager.SetActiveCampaign(campaignName);
        _saveAsDialog.CurrentDir = campaignPath;

        var modelName = _modelSelectorPanel.Model;
        if (_resourceManager.TryGetModel(modelName, out var modelFile))
        {
            _currentModel = modelFile;
            _modelViewport.RenderModel(_resourceManager, _currentModel);
            _modelInspector.SetModel(_resourceManager, _currentModel);
        }
    }

    private void EditorConfigOnTextureModeChanged(TextureMode value)
    {
        SetFileMenuTextureMode(value);
        var textureFilterMode = value switch {
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

    public void SetInstallContext(InstallContext installContext)
    {
        _installContext = installContext;
        _resourceManager = new ResourceManager(installContext);
        _modelSelectorPanel.SetResourceManager(_resourceManager);
    }

    private void EditorConfigOnShowWireframeChanged(bool value)
    {
        _viewMenu.SetItemChecked(1, value);
    }

    private void EditorConfigOnShowBoundingBoxChanged(bool value)
    {
        _viewMenu.SetItemChecked(0, value);
    }

    private static void Save(string path, ModelFile modelFile)
    {
        var parser = new ModelFileParser();
        using var outStream = File.Open(path, FileMode.Create);
        using var writer = new BinaryWriter(outStream, Encoding.UTF8, false);
        parser.Write(writer, modelFile);
        Log.Information("Saved model to {path}", path);
    }

    private void SetFileMenuTextureMode(TextureMode textureMode)
    {
        switch (textureMode)
        {
            case TextureMode.Linear:
                _viewMenu.SetItemChecked(3, true);
                _viewMenu.SetItemChecked(4, false);
                break;
            case TextureMode.NearestNeighbour:
                _viewMenu.SetItemChecked(3, false);
                _viewMenu.SetItemChecked(4, true);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}