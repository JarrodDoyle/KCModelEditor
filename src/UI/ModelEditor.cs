using System.IO;
using Godot;
using KeepersCompound.ModelEditor.Render;
using KeepersCompound.ModelEditor.UI.Menu;

namespace KeepersCompound.ModelEditor.UI;

public partial class ModelEditor : Control
{
    private EditorState _state = null!;
    private ModelDocument? _document;

    #region Nodes

    private EditorMenu _editorMenu = null!;
    private ModelSelectorPanel _modelSelectorPanel = null!;
    private ModelViewport _modelViewport = null!;
    private ModelInspector _modelInspector = null!;
    private FileDialog _saveAsDialog = null!;

    #endregion

    #region Events

    public delegate void QuitToInstallsEventHandler();

    public event QuitToInstallsEventHandler? QuitToInstalls;

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
        _editorMenu.QuitPressed += EditorMenuOnQuitPressed;
        _editorMenu.QuitToInstallsPressed += EditorMenuOnQuitToInstallsPressed;
        _editorMenu.UndoPressed += EditorMenuOnUndoPressed;
        _editorMenu.RedoPressed += EditorMenuOnRedoPressed;
        _modelSelectorPanel.ModelSelected += OnModelSelected;
        _saveAsDialog.FileSelected += SaveAsDialogOnFileSelected;

        _editorMenu.SetState(_state);
        _modelViewport.SetState(_state);
        _modelInspector.SetState(_state);
        _modelSelectorPanel.SetEditorState(_state);
    }

    public override void _ExitTree()
    {
        _editorMenu.SavePressed -= EditorMenuOnSavePressed;
        _editorMenu.SaveAsPressed -= EditorMenuOnSaveAsPressed;
        _editorMenu.UndoPressed -= EditorMenuOnUndoPressed;
        _editorMenu.RedoPressed -= EditorMenuOnRedoPressed;
        _modelSelectorPanel.ModelSelected -= OnModelSelected;
        _saveAsDialog.FileSelected -= SaveAsDialogOnFileSelected;
        _state.ActiveModelChanged -= StateOnActiveModelChanged;
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (inputEvent.IsActionPressed("ui_redo"))
        {
            _document?.RedoAction();
        }
        else if (inputEvent.IsActionPressed("ui_undo"))
        {
            _document?.UndoAction();
        }
    }

    #endregion

    #region EventHandling

    private void EditorMenuOnSavePressed()
    {
        if (_document is not { Dirty: true })
        {
            return;
        }

        var modelName = _document.Name;
        var campaignName = _document.Campaign;
        var virtualPath = $"FMs/{campaignName}/obj/{modelName}.bin";
        if (campaignName != "" && _state.Resources.TryGetFilePath(virtualPath, out var path))
        {
            _document.Save(path);
        }
        else
        {
            _saveAsDialog.Show();
        }
    }

    private void EditorMenuOnSaveAsPressed()
    {
        if (_document != null)
        {
            _saveAsDialog.Show();
        }
    }

    private void EditorMenuOnQuitPressed()
    {
        // TODO: Handle saving dirty file
        GetTree().Quit();
    }

    private void EditorMenuOnQuitToInstallsPressed()
    {
        // TODO: Handle saving dirty file
        QuitToInstalls?.Invoke();
    }

    private void EditorMenuOnUndoPressed()
    {
        _document?.UndoAction();
    }

    private void EditorMenuOnRedoPressed()
    {
        _document?.RedoAction();
    }

    private void SaveAsDialogOnFileSelected(string path)
    {
        _document?.Save(path);
    }

    private void OnModelSelected()
    {
        var campaignName = _modelSelectorPanel.Campaign;
        var campaignPath = Path.Join(_state.Context.FmsDir, campaignName);
        _state.Resources.SetActiveCampaign(campaignName);
        _saveAsDialog.CurrentDir = campaignPath;

        var modelName = _modelSelectorPanel.Model;
        if (_state.Resources.TryGetModel(modelName, out var modelFile))
        {
            _state.SetDocument(new ModelDocument(modelFile, modelName, campaignName));
        }
    }

    private void StateOnActiveModelChanged(ModelDocument document)
    {
        _document = document;
    }

    #endregion

    public void SetEditorState(EditorState state)
    {
        _state = state;
        _state.ActiveModelChanged += StateOnActiveModelChanged;
    }
}