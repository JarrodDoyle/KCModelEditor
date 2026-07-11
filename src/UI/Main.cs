using System;
using Godot;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace KeepersCompound.ModelEditor.UI;

public partial class Main : Node
{
    public override void _Ready()
    {
        ConfigureLogger();
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