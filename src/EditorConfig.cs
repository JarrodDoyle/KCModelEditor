using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Serilog;

namespace KeepersCompound.ModelEditor;

public class EditorConfig
{
    public HashSet<string> InstallPaths { get; } = [];

    private readonly string _configFilePath;
    private readonly ConfigFile _configFile = new();

    public EditorConfig(string configPath)
    {
        _configFilePath = configPath;
        Log.Information("Initialising editor config: {path}", _configFilePath);
        if (_configFile.Load(_configFilePath) != Error.Ok)
        {
            Log.Warning("Editor config file does not exist. Initialising with default values.");
            return;
        }

        var installPaths = _configFile.GetValue("general", "install_paths", Array.Empty<string>()).AsStringArray();
        foreach (var path in installPaths)
        {
            InstallPaths.Add(path);
        }
    }

    public void Save()
    {
        Log.Information("Saving config file: {path}", _configFilePath);
        _configFile.SetValue("general", "install_paths", InstallPaths.ToArray());
        _configFile.Save(_configFilePath);
    }
}