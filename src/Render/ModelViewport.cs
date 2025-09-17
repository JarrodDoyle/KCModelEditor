using System.Collections.Generic;
using Godot;
using Godot.Collections;
using KeepersCompound.Formats.Model;

namespace KeepersCompound.ModelEditor.Render;

public partial class ModelViewport : SubViewport
{
    #region Nodes

    private Node3D _modelContainer;

    #endregion

    public override void _Ready()
    {
        _modelContainer = GetNode<Node3D>("%ModelContainer");
    }

    public void RenderModel(ModelFile modelFile)
    {
        foreach (var child in _modelContainer.GetChildren())
        {
            child.QueueFree();
        }

        var objCount = modelFile.Objects.Count;
        var meshes = new MeshInstance3D[objCount];
        for (var i = 0; i < objCount; i++)
        {
            var subObject = modelFile.Objects[i];
            var polyCount = modelFile.Polygons.Count;
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var indices = new List<int>();

            for (var j = 0; j < polyCount; j++)
            {
                var poly = modelFile.Polygons[j];

                // Discard any polys that don't belong to this object
                var startIdx = poly.VertexIndices[0].PositionIndex;
                if (startIdx < subObject.VertexPositionStartIndex ||
                    startIdx >= subObject.VertexPositionStartIndex + subObject.VertexPositionCount)
                {
                    continue;
                }

                var faceNormal = modelFile.FaceNormals[poly.NormalIndex].ToGodot();
                foreach (var vertexIndex in poly.VertexIndices)
                {
                    vertices.Add(modelFile.VertexPositions[vertexIndex.PositionIndex].ToGodot());
                    normals.Add(poly.UseVertexNormals
                        ? modelFile.VertexNormals[vertexIndex.NormalIndex].Normal.ToGodot()
                        : faceNormal);
                    uvs.Add(poly.Type == ModelPolygonType.Textured
                        ? modelFile.VertexUvs[vertexIndex.UvIndex].ToGodot(false)
                        : Vector2.Zero);
                }

                var polyVertexCount = poly.VertexIndices.Count;
                var indexOffset = vertices.Count - polyVertexCount;
                for (var k = 1; k < polyVertexCount - 1; k++)
                {
                    indices.Add(indexOffset);
                    indices.Add(indexOffset + k);
                    indices.Add(indexOffset + k + 1);
                }
            }

            var transform = subObject.JointType == ModelObjectType.Static || subObject.JointIndex == -1
                ? Transform3D.Identity
                : subObject.Transform.ToGodot();
            var array = new Array();
            var arrayMesh = new ArrayMesh();
            array.Resize((int)Mesh.ArrayType.Max);
            array[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
            array[(int)Mesh.ArrayType.Normal] = normals.ToArray();
            array[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
            array[(int)Mesh.ArrayType.Index] = indices.ToArray();
            arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, array);

            meshes[i] = new MeshInstance3D
            {
                Mesh = arrayMesh,
                Position = -modelFile.Center.ToGodot(),
                Transform = transform,
            };
        }

        _modelContainer.AddChild(meshes[0]);
        for (var i = 0; i < objCount; i++)
        {
            var childIndex = modelFile.Objects[i].ChildObjectIndex;
            while (childIndex != -1)
            {
                // This can only happen if there's a loop in the relationship. This shouldn't ever be the case, but for
                // some reason a few Thief 2 objects have this.
                if (childIndex == i)
                {
                    break;
                }

                meshes[i].AddChild(meshes[childIndex]);
                childIndex = modelFile.Objects[childIndex].SiblingObjectIndex;
            }
        }
    }
}