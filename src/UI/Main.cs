using System;
using Godot;
using KeepersCompound.Dark;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace KeepersCompound.ModelEditor.UI;

public partial class Main : Node
{
    #region Nodes

    private InstallManager _installManager = InstantiatePackedScene<InstallManager>("uid://dm8et7nwwnq34");
    private ModelEditor _modelEditor = InstantiatePackedScene<ModelEditor>("uid://bmvch3t460c6k");

    #endregion

    #region Godot Overrides

    public override void _EnterTree()
    {
        ConfigureLogger();
    }

    public override void _Ready()
    {
        _installManager.LoadInstall += InstallManagerOnLoadEditor;
        _modelEditor.QuitToInstalls += ModelEditorOnQuitToInstalls;

        AddChild(_installManager);
    }

    public override void _ExitTree()
    {
        _installManager.LoadInstall -= InstallManagerOnLoadEditor;
        _modelEditor.QuitToInstalls -= ModelEditorOnQuitToInstalls;
        _installManager.Config.Save();
    }

    #endregion

    #region Event Handling

    private void InstallManagerOnLoadEditor(string installPath)
    {
        var installContext = new InstallContext(installPath);
        if (installContext.Valid)
        {
            _modelEditor.SetEditorState(new EditorState(_installManager.Config, installContext));
            RemoveChild(_installManager);
            AddChild(_modelEditor);
        }
    }

    private void ModelEditorOnQuitToInstalls()
    {
        RemoveChild(_modelEditor);
        AddChild(_installManager);
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

    private static T InstantiatePackedScene<T>(string uid) where T : Node
    {
        return (T)GD.Load<PackedScene>(uid).Instantiate();
    }
}