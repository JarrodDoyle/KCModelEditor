using System.Drawing;
using System.Numerics;

namespace KeepersCompound.ModelEditor;

public static class GodotConversions
{
    public const float InverseScale = 4.0f;

    public static Godot.Vector3 ToGodot(this Vector3 vec, bool scale = true)
    {
        return new Godot.Vector3(vec.Y, vec.Z, vec.X) / (scale ? InverseScale : 1.0f);
    }

    public static Godot.Vector2 ToGodot(this Vector2 vec, bool scale = true)
    {
        return new Godot.Vector2(vec.X, vec.Y) / (scale ? InverseScale : 1.0f);
    }

    public static Godot.Transform3D ToGodot(this Matrix4x4 mat, bool scale = true)
    {
        var t = mat.Translation / (scale ? InverseScale : 1.0f);
        return new Godot.Transform3D(mat.M22, mat.M32, mat.M12, mat.M23, mat.M33, mat.M13, mat.M21, mat.M31, mat.M11, t.Y, t.Z, t.X);
    }

    public static Godot.Color ToGodot(this Color color)
    {
        return new Godot.Color(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f);
    }
}