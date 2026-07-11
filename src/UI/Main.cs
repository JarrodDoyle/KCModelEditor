using System;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using KeepersCompound.ModelEditor.Constants;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace KeepersCompound.ModelEditor.UI;

[Meta(typeof(IAutoNode))]
public partial class Main : Node
{
    public override void _Notification(int what) => this.Notify(what);

    public void OnResolved()
    {
        ConfigureLogger();
        SetProcess(true);
    }

    public void OnProcess(double delta)
    {
        // We do this in OnProcess to avoid the issues described here: https://github.com/godotengine/godot/issues/99651#issuecomment-2807113330
        var result = GetTree().ChangeSceneToFile(SceneUids.InstallManager);
        if (result != Error.Ok)
        {
            Log.Error("Failed to change scene: {UID}", SceneUids.InstallManager);
            GetTree().Quit();
        }
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