using System;
using Godot;
using KeepersCompound.Dark;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace KeepersCompound.ModelEditor.UI;

public partial class Main : Node
{
    #region Nodes

    private InstallManager _installManager = null!;
    private ModelEditor _modelEditor = null!;

    #endregion

    #region Godot Overrides

    public override void _EnterTree()
    {
        ConfigureLogger();
    }

    public override void _Ready()
    {
        _installManager = GetNode<InstallManager>("%InstallManager");
        _modelEditor = GetNode<ModelEditor>("%ModelEditor");

        _installManager.LoadInstall += LoadEditor;
        _modelEditor.QuitToInstalls += ModelEditorOnQuitToInstalls;
    }

    public override void _ExitTree()
    {
        _installManager.LoadInstall -= LoadEditor;
        _modelEditor.QuitToInstalls -= ModelEditorOnQuitToInstalls;
        EditorConfig.Instance.Save();
    }

    #endregion

    #region Event Handling

    private void LoadEditor(string installPath)
    {
        var installContext = new InstallContext(installPath);
        if (!installContext.Valid)
        {
            return;
        }

        _modelEditor.SetInstallContext(installContext);
        _modelEditor.Visible = true;
        _installManager.Visible = false;
    }

    private void ModelEditorOnQuitToInstalls()
    {
        _modelEditor.Visible = false;
        _installManager.Visible = true;
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
}