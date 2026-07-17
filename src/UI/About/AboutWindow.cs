using System.IO;
using System.Text.Json;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Serilog;

namespace KeepersCompound.ModelEditor.UI.About;

[Meta(typeof(IAutoNode))]
public partial class AboutWindow : Window
{
    public override void _Notification(int what) => this.Notify(what);

    [Node("%License")] private RichTextLabel LicenseLabel { get; set; } = null!;
    [Node] private ItemList ThirdPartyItemList { get; set; } = null!;
    [Node] private RichTextLabel ThirdPartyLicenseLabel { get; set; } = null!;

    private const string LicenseFolderPath = "res://assets/licensing/";
    private LicenseMeta? Meta { get; set; }

    public void OnReady()
    {
        CloseRequested += OnCloseRequested;
        ThirdPartyItemList.ItemSelected += ThirdPartyItemListOnItemSelected;
        SetLicenseInfo();
    }

    public void OnExitTree()
    {
        CloseRequested -= OnCloseRequested;
    }

    private void OnCloseRequested()
    {
        QueueFree();
    }

    private void ThirdPartyItemListOnItemSelected(long index)
    {
        if (Meta == null)
        {
            Log.Error("Meta is null, can't set license text");
            return;
        }

        SetLicenseText(ThirdPartyLicenseLabel, Meta.ThirdParties[index], Meta.Licenses);
    }

    private void SetLicenseInfo()
    {
        LoadMeta();
        if (Meta == null)
        {
            Log.Error("Failed to parse license meta: {Path}", LicenseFolderPath);
            return;
        }

        SetLicenseText(LicenseLabel, Meta.Primary, Meta.Licenses);

        foreach (var thirdParty in Meta.ThirdParties)
        {
            ThirdPartyItemList.AddItem(thirdParty.Name);
        }
    }

    private void LoadMeta()
    {
        var path = Path.Join(ProjectSettings.GlobalizePath(LicenseFolderPath), "meta.json");
        var lines = File.ReadAllText(path);
        Meta = JsonSerializer.Deserialize<LicenseMeta>(lines, JsonSourceGenerationContext.Default.LicenseMeta);
    }

    private static void SetLicenseText(RichTextLabel label, LicenseUsage usage, LicenseDefinition[] definitions)
    {
        label.Clear();
        label.PushMono();
        foreach (var line in usage.Copyright)
        {
            label.AddText($"Copyright {line}\n");
        }

        label.AddText("\n");
        foreach (var license in definitions)
        {
            if (license.Name != usage.License) continue;
            var path = Path.Join(ProjectSettings.GlobalizePath(LicenseFolderPath), license.File);
            label.AddText(File.ReadAllText(path));
        }
    }
}