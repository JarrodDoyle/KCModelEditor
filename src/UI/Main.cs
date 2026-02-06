using System;
using Godot;
using KeepersCompound.Dark;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace KeepersCompound.ModelEditor.UI;

public partial class Main : Node
{
    private PackedScene _installManagerScene = GD.Load<PackedScene>("uid://dm8et7nwwnq34");
    private PackedScene _modelEditorScene = GD.Load<PackedScene>("uid://bmvch3t460c6k");

    #region Nodes

    private InstallManager? _installManager;
    private ModelEditor? _modelEditor;

    #endregion

    #region Godot Overrides

    public override void _EnterTree()
    {
        ConfigureLogger();
    }

    public override void _Ready()
    {
        LoadInstallManager();
    }

    public override void _ExitTree()
    {
        _installManager?.LoadInstall -= InstallManagerOnLoadEditor;
        _modelEditor?.QuitToInstalls -= ModelEditorOnQuitToInstalls;
        _installManager?.Config.Save();
    }

    #endregion

    #region Event Handling

    private void InstallManagerOnLoadEditor(string installPath)
    {
        var installContext = new InstallContext(installPath);
        if (installContext.Valid)
        {
            LoadModelEditor(installContext);
        }
    }

    private void ModelEditorOnQuitToInstalls()
    {
        LoadInstallManager();
    }

    #endregion

    private static void ConfigureLogger()
    {
        const string outputTemplate = "{Timestamp:HH:mm:ss.fff} [{Level}] {Message:lj}{NewLine}{Exception}";
        var logPath = $"{AppDomain.CurrentDomain.BaseDirectory}/logs/{DateTime.Now:yyyyMMdd_HHmmss}.log";
        var config = new LoggerConfiguration();
#if DEBUG
        config.MinimumLevel.Debug();
#endif

        config.WriteTo.Console(theme: AnsiConsoleTheme.Sixteen, outputTemplate: outputTemplate);
        config.WriteTo.File(logPath, outputTemplate: outputTemplate);
        Log.Logger = config.CreateLogger();
    }

    private void LoadInstallManager()
    {
        if (_installManagerScene.Instantiate() is not InstallManager instance)
        {
            Log.Error("Install Manager failed to initialise.");
            return;
        }

        _installManager = instance;
        _installManager.LoadInstall += InstallManagerOnLoadEditor;
        AddChild(_installManager);

        if (_modelEditor != null)
        {
            _modelEditor.QuitToInstalls -= ModelEditorOnQuitToInstalls;
            _modelEditor.QueueFree();
        }
    }

    private void LoadModelEditor(InstallContext context)
    {
        if (_modelEditorScene.Instantiate() is not ModelEditor instance)
        {
            Log.Error("ModelEditor failed to initialise.");
            return;
        }

        _modelEditor = instance;
        _modelEditor.SetEditorState(new EditorState(_installManager?.Config ?? new EditorConfig(), context));
        _modelEditor.QuitToInstalls += ModelEditorOnQuitToInstalls;
        AddChild(_modelEditor);

        if (_installManager != null)
        {
            _installManager.LoadInstall -= InstallManagerOnLoadEditor;
            _installManager.QueueFree();
        }
    }
}