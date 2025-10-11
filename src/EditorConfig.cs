using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using KeepersCompound.ModelEditor.Render;
using Serilog;

namespace KeepersCompound.ModelEditor;

public class EditorConfig
{
    #region Events

    public delegate void ShowBoundingBoxChangedEventHandler(bool value);

    public delegate void ShowWireframeChangedEventHandler(bool value);

    public delegate void ShowVHotsChangedEventHandler(bool value);

    public delegate void TextureModeChangedEventHandler(TextureMode value);

    public event ShowBoundingBoxChangedEventHandler? ShowBoundingBoxChanged;
    public event ShowWireframeChangedEventHandler? ShowWireframeChanged;
    public event ShowVHotsChangedEventHandler? ShowVHotsChanged;
    public event TextureModeChangedEventHandler? TextureModeChanged;

    #endregion

    public static EditorConfig Instance { get; } = new();
    public HashSet<string> InstallPaths { get; } = [];

    public bool ShowBoundingBox
    {
        get;
        set
        {
            if (field != value)
            {
                ShowBoundingBoxChanged?.Invoke(value);
            }

            field = value;
        }
    }

    public bool ShowWireframe
    {
        get;
        set
        {
            if (field != value)
            {
                ShowWireframeChanged?.Invoke(value);
            }

            field = value;
        }
    }

    public bool ShowVHots
    {
        get;
        set
        {
            if (field != value)
            {
                ShowVHotsChanged?.Invoke(value);
                field = value;
            }
        }
    }

    public TextureMode TextureMode
    {
        get;
        set
        {
            if (field != value)
            {
                TextureModeChanged?.Invoke(value);
                field = value;
            }
        }
    }

    private const string ConfigFilePath = "user://config.ini";
    private readonly ConfigFile _configFile = new();

    private EditorConfig()
    {
        Log.Information("Initialising editor config: {path}", ConfigFilePath);
        if (_configFile.Load(ConfigFilePath) != Error.Ok)
        {
            Log.Warning("Editor config file does not exist. Initialising with default values.");
            return;
        }

        var installPaths = _configFile.GetValue("general", "install_paths", Array.Empty<string>()).AsStringArray();
        foreach (var path in installPaths)
        {
            InstallPaths.Add(path);
        }

        ShowBoundingBox = _configFile.GetValue("viewport", "show_bounds", false).AsBool();
        ShowWireframe = _configFile.GetValue("viewport", "show_wireframe", false).AsBool();
        ShowVHots = _configFile.GetValue("viewport", "show_vhots", false).AsBool();
        TextureMode = (TextureMode)_configFile.GetValue("viewport", "texture_mode", 0).AsInt32();
    }

    public void Save()
    {
        Log.Information("Saving config file: {path}", ConfigFilePath);
        _configFile.SetValue("general", "install_paths", InstallPaths.ToArray());
        _configFile.SetValue("viewport", "show_bounds", ShowBoundingBox);
        _configFile.SetValue("viewport", "show_wireframe", ShowWireframe);
        _configFile.SetValue("viewport", "show_vhots", ShowVHots);
        _configFile.SetValue("viewport", "texture_mode", (int)TextureMode);
        _configFile.Save(ConfigFilePath);
    }
}