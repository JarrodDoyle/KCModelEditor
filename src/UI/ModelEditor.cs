using System;
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
    private ModelDocument? _currentModel;

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
        _saveAsDialog.FileSelected += SaveAsDialogOnFileSelected;
    }

    public override void _ExitTree()
    {
        _editorMenu.SavePressed -= EditorMenuOnSavePressed;
        _editorMenu.SaveAsPressed -= EditorMenuOnSaveAsPressed;
        _modelSelectorPanel.ModelSelected -= OnModelSelected;
        _saveAsDialog.FileSelected -= SaveAsDialogOnFileSelected;
    }

    #endregion

    #region EventHandling

    private void EditorMenuOnSavePressed()
    {
        if (_currentModel is not { Dirty: true })
        {
            return;
        }

        var modelName = _currentModel.Name;
        var campaignName = _currentModel.Campaign;
        var virtualPath = $"FMs/{campaignName}/obj/{modelName}.bin";
        if (campaignName != "" && _resourceManager.TryGetFilePath(virtualPath, out var path))
        {
            _currentModel.Save(path);
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
        _currentModel?.Save(path);
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
            _currentModel = new ModelDocument(modelFile, modelName, campaignName);
            _modelViewport.SetModel(_resourceManager, _currentModel);
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
}