using System.IO;
using System.Text;
using Godot;
using KeepersCompound.Dark;
using KeepersCompound.Dark.Resources;
using KeepersCompound.Formats.Model;
using KeepersCompound.ModelEditor.Render;
using KeepersCompound.ModelEditor.UI.Menu;
using Serilog;

namespace KeepersCompound.ModelEditor.UI;

public partial class ModelEditor : Control
{
    private InstallContext _installContext = null!;
    private ResourceManager _resourceManager = null!;
    private ModelFile? _currentModel;

    #region Nodes

    private EditorMenu _editorMenu = null!;
    private ModelSelectorPanel _modelSelectorPanel = null!;
    private ModelViewport _modelViewport = null!;
    private ModelInspector _modelInspector = null!;
    private FileDialog _saveAsDialog = null!;

    #endregion

    #region Overrides

    public override void _Ready()
    {
        _editorMenu = GetNode<EditorMenu>("%EditorMenu");
        _modelSelectorPanel = GetNode<ModelSelectorPanel>("%ModelSelectorPanel");
        _modelViewport = GetNode<ModelViewport>("%ModelViewport");
        _modelInspector = GetNode<ModelInspector>("%ModelInspector");
        _saveAsDialog = GetNode<FileDialog>("%SaveAsDialog");

        _editorMenu.SavePressed += EditorMenuOnSavePressed;
        _editorMenu.SaveAsPressed += EditorMenuOnSaveAsPressed;
        _modelSelectorPanel.ModelSelected += OnModelSelected;
        _modelInspector.ModelEdited += OnModelEdited;
        _saveAsDialog.FileSelected += SaveAsDialogOnFileSelected;
    }

    public override void _ExitTree()
    {
        _editorMenu.SavePressed -= EditorMenuOnSavePressed;
        _editorMenu.SaveAsPressed -= EditorMenuOnSaveAsPressed;
        _modelSelectorPanel.ModelSelected -= OnModelSelected;
        _modelInspector.ModelEdited -= OnModelEdited;
        _saveAsDialog.FileSelected -= SaveAsDialogOnFileSelected;
    }

    #endregion

    #region EventHandling

    private void EditorMenuOnSavePressed()
    {
        if (_currentModel == null)
        {
            return;
        }

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

    private void EditorMenuOnSaveAsPressed()
    {
        if (_currentModel != null)
        {
            _saveAsDialog.Show();
        }
    }

    private void SaveAsDialogOnFileSelected(string path)
    {
        if (_currentModel != null)
        {
            Save(path, _currentModel);
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

    private static void Save(string path, ModelFile modelFile)
    {
        var parser = new ModelFileParser();
        using var outStream = File.Open(path, FileMode.Create);
        using var writer = new BinaryWriter(outStream, Encoding.UTF8, false);
        parser.Write(writer, modelFile);
        Log.Information("Saved model to {path}", path);
    }
}