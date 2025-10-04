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

        EditorConfig.Instance.ShowBoundingBoxChanged += EditorConfigOnShowBoundingBoxChanged;
        EditorConfig.Instance.ShowWireframeChanged += EditorConfigOnShowWireframeChanged;
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
}