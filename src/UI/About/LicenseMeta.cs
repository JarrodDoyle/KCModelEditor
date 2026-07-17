namespace KeepersCompound.ModelEditor.UI.About;

public class LicenseMeta
{
    public LicenseUsage Primary { get; set; } = null!;
    public LicenseUsage[] ThirdParties { get; set; } = null!;
    public LicenseDefinition[] Licenses { get; set; } = null!;
}