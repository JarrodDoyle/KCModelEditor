using System.IO;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using KeepersCompound.ModelEditor.Render;
using KeepersCompound.ModelEditor.UI.Menu;
using Serilog;

namespace KeepersCompound.ModelEditor.UI;

[Meta(typeof(IAutoNode))]
public partial class ModelEditor : Control
{
    public override void _Notification(int what) => this.Notify(what);

    [Node] private EditorMenu EditorMenu { get; set; } = null!;
    [Node] private ModelSelectorPanel ModelSelectorPanel { get; set; } = null!;
    [Node] private ModelViewport ModelViewport { get; set; } = null!;
    [Node] private ModelInspector ModelInspector { get; set; } = null!;
    [Node] private FileDialog SaveAsDialog { get; set; } = null!;

    private EditorState _state = null!;
    private ModelDocument? _document;

    #region Overrides

    public void OnReady()
    {
        EditorMenu.SavePressed += EditorMenuOnSavePressed;
        EditorMenu.SaveAsPressed += EditorMenuOnSaveAsPressed;
        EditorMenu.QuitPressed += EditorMenuOnQuitPressed;
        EditorMenu.QuitToInstallsPressed += EditorMenuOnQuitToInstallsPressed;
        EditorMenu.RefocusCameraPressed += EditorMenuOnRefocusCameraPressed;
        SaveAsDialog.FileSelected += SaveAsDialogOnFileSelected;

        EditorMenu.SetState(_state);
        ModelViewport.SetState(_state);
        ModelInspector.SetState(_state);
        ModelSelectorPanel.SetEditorState(_state);
    }

    public void OnExitTree()
    {
        EditorMenu.SavePressed -= EditorMenuOnSavePressed;
        EditorMenu.SaveAsPressed -= EditorMenuOnSaveAsPressed;
        EditorMenu.RefocusCameraPressed -= EditorMenuOnRefocusCameraPressed;
        SaveAsDialog.FileSelected -= SaveAsDialogOnFileSelected;
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
            SaveAsDialog.Show();
        }
    }

    private void EditorMenuOnSaveAsPressed()
    {
        if (_document != null)
        {
            SaveAsDialog.Show();
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
        var result = GetTree().ChangeSceneToFile(SceneUids.InstallManager);
        if (result != Error.Ok)
        {
            Log.Error("Failed to change scene: {UID}", SceneUids.InstallManager);
            GetTree().Quit();
        }
    }

    private void EditorMenuOnRefocusCameraPressed()
    {
        ModelViewport.RefocusCamera();
    }

    private void SaveAsDialogOnFileSelected(string path)
    {
        _document?.Save(path);
    }

    private void StateOnActiveModelChanged(ModelDocument document)
    {
        _document = document;
        SaveAsDialog.CurrentDir = Path.Join(_state.Context.FmsDir, document.Campaign);
    }

    #endregion

    public void SetEditorState(EditorState state)
    {
        _state = state;
        _state.ActiveModelChanged += StateOnActiveModelChanged;
    }
}