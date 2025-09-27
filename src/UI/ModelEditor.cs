using System.Collections.Generic;
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
    private InstallContext _installContext;
    private ResourceManager _resourceManager = new();
    private ModelFile? _currentModel;

    #region Nodes

    private ModelSelectorPanel _modelSelectorPanel;
    private ModelViewport _modelViewport;
    private ModelInspector _modelInspector;
    private PopupMenu _fileMenu;
    private PopupMenu _viewMenu;
    private FileDialog _saveAsDialog;

    #endregion

    public override void _Ready()
    {
        _modelSelectorPanel = GetNode<ModelSelectorPanel>("%ModelSelectorPanel");
        _modelViewport = GetNode<ModelViewport>("%ModelViewport");
        _modelInspector = GetNode<ModelInspector>("%ModelInspector");
        _fileMenu = GetNode<PopupMenu>("%File");
        _viewMenu = GetNode<PopupMenu>("%View");
        _saveAsDialog = GetNode<FileDialog>("%SaveAsDialog");

        _modelSelectorPanel.CampaignSelected += OnCampaignSelected;
        _modelSelectorPanel.ModelSelected += OnModelSelected;
        _modelInspector.ModelEdited += OnModelEdited;
        _viewMenu.IndexPressed += ViewMenuOnIndexPressed;
        _fileMenu.IndexPressed += FileMenuOnIndexPressed;
        _saveAsDialog.FileSelected += SaveAsDialogOnFileSelected;
    }

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
        if (_viewMenu.IsItemCheckable(index))
        {
            _viewMenu.SetItemChecked(index, !_viewMenu.IsItemChecked(index));
        }

        switch (index)
        {
            case 0:
                _modelViewport.BoundingBoxVisible = _viewMenu.IsItemChecked(index);
                break;
            case 1:
                _modelViewport.WireframesVisible = _viewMenu.IsItemChecked(index);
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
        var modelName = _modelSelectorPanel.Model;
        if (_resourceManager.TryGetModel(modelName, out var modelFile))
        {
            _currentModel = modelFile;
            _modelViewport.RenderModel(_resourceManager, _currentModel);
            _modelInspector.SetModel(_resourceManager, _currentModel);
        }
    }

    private void OnCampaignSelected()
    {
        var campaignName = _modelSelectorPanel.Campaign;
        var campaignPath = Path.Join(_installContext.FmsDir, campaignName);
        _resourceManager.Initialise(_installContext, campaignName);
        _saveAsDialog.CurrentDir = campaignPath;
        _modelSelectorPanel.SetModels(new SortedSet<string>(_resourceManager.ModelNames));
    }

    #endregion

    public void SetInstallContext(InstallContext installContext)
    {
        _installContext = installContext;
        _modelSelectorPanel.SetCampaigns(new SortedSet<string>(_installContext.Fms));
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