using System;
using Godot;
using KeepersCompound.Dark;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace KeepersCompound.ModelEditor.UI;

public partial class Main : Node
{
    [Export(PropertyHint.File)] private string _configFilePath = "user://config.ini";
    private InstallContext _installContext;
    private InstallManager _installManager;
    private ModelEditor _modelEditor;
    private EditorConfig _editorConfig;

    public override void _Ready()
    {
        ConfigureLogger();

        _editorConfig = new EditorConfig(_configFilePath);
        _installManager = GetNode<InstallManager>("%InstallManager");
        _modelEditor = GetNode<ModelEditor>("%ModelEditor");

        _installManager.SetConfig(_editorConfig);
        _modelEditor.SetConfig(_editorConfig);

        _installManager.LoadInstall += LoadEditor;
    }

    public override void _ExitTree()
    {
        _editorConfig.Save();
    }

    private void LoadEditor(string installPath)
    {
        _installContext = new InstallContext(installPath);
        if (!_installContext.Valid)
        {
            return;
        }
        
        _modelEditor.SetInstallContext(_installContext);
        _modelEditor.Visible = true;
        _installManager.Visible = false;
    }

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