using Godot;

namespace KeepersCompound.ModelEditor.Render;

public partial class VHotRenderer : Node3D
{
    public string DisplayName { get; init; } = "";

    #region Nodes

    private MeshInstance3D _mesh = null!;
    private Label3D _label = null!;

    #endregion

    #region Godot Overrides

    public override void _Ready()
    {
        var arrayMesh = new ArrayMesh();
        var sphere = new SphereMesh();
        arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, sphere.GetMeshArrays());
        _mesh = new MeshInstance3D { Mesh = arrayMesh };
        _mesh.Scale = Vector3.One * 0.05f;

        _label = new Label3D();
        _label.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
        _label.Text = DisplayName;
        _label.Position = Vector3.Up * 0.05f;
        _label.PixelSize = 0.0005f;
        _label.FontSize = 96;
        _label.AlphaCut = Label3D.AlphaCutMode.OpaquePrepass;
        _label.OutlineSize = 0;
        _label.FixedSize = true;

        AddChild(_mesh);
        AddChild(_label);
    }

    #endregion
}