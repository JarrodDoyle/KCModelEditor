using Godot;
using KeepersCompound.Dark;

namespace KeepersCompound.ModelEditor.UI;

public partial class Main : Node
{
    [Export(PropertyHint.File)] private string _configFilePath = "user://config.ini";
    private InstallContext _installContext;
    private InstallManager _installManager;
    private ModelEditor _modelEditor;

    public override void _Ready()
    {
        _installManager = GetNode<InstallManager>("%InstallManager");
        _modelEditor = GetNode<ModelEditor>("%ModelEditor");

        _installManager.LoadConfig(_configFilePath);
        _installManager.LoadInstall += LoadEditor;
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
}