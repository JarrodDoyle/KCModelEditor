using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace KeepersCompound.ModelEditor.Render;

public partial class LineRenderer : Node3D
{
    public List<Vector3> Vertices { get; set; } = [];
    public Color LineColor { get; set; } = Colors.White;

    #region Godot Overrides

    public override void _Ready()
    {
        var array = new Array();
        array.Resize((int)Mesh.ArrayType.Max);
        array[(int)Mesh.ArrayType.Vertex] = Vertices.ToArray();

        var arrayMesh = new ArrayMesh();
        arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, array);
        arrayMesh.SurfaceSetMaterial(0, new StandardMaterial3D { AlbedoColor = LineColor });

        AddChild(new MeshInstance3D { Mesh = arrayMesh });
    }

    #endregion

    public static LineRenderer CreateAabb(Aabb aabb, Color color = new())
    {
        var v0 = aabb.Position;
        var v1 = aabb.End;
        var v2 = v0 with { X = v1.X };
        var v3 = v0 with { Y = v1.Y };
        var v4 = v0 with { Z = v1.Z };
        var v5 = v1 with { X = v0.X };
        var v6 = v1 with { Y = v0.Y };
        var v7 = v1 with { Z = v0.Z };

        return new LineRenderer
        {
            Vertices = [v0, v2, v2, v7, v7, v3, v3, v0, v1, v5, v5, v4, v4, v6, v6, v1, v0, v4, v2, v6, v7, v1, v3, v5],
            LineColor = color,
        };
    }
}