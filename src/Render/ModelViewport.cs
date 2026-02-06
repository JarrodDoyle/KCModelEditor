using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Godot;
using Godot.Collections;
using KeepersCompound.Dark;
using KeepersCompound.Formats.Images;
using KeepersCompound.Formats.Model;
using Serilog;

namespace KeepersCompound.ModelEditor.Render;

public partial class ModelViewport : SubViewport
{
    private EditorState _state = null!;
    private ModelDocument? _document;

    #region Nodes

    private Node3D _modelContainer = null!;
    private OrbitCamera _orbitCamera = null!;
    private LineRenderer? _boundingBox;
    private readonly List<LineRenderer> _wireframes = [];
    private readonly List<VHotRenderer> _vhots = [];

    #endregion

    #region Godot Overrides

    public override void _Ready()
    {
        _modelContainer = GetNode<Node3D>("%ModelContainer");
        _orbitCamera = GetNode<OrbitCamera>("%OrbitCamera");
    }

    public override void _ExitTree()
    {
        _state.Config.ShowBoundingBoxChanged -= EditorConfigOnShowBoundingBoxChanged;
        _state.Config.ShowWireframeChanged -= EditorConfigOnShowWireframeChanged;
        _state.Config.ShowVHotsChanged -= EditorConfigOnShowVHotsChanged;
        _state.ActiveModelChanged -= StateOnActiveModelChanged;
        _document?.ActionDone -= ModelDocumentOnActionDone;
    }

    #endregion

    #region Event Handling

    private void StateOnActiveModelChanged(ModelDocument document)
    {
        _document?.ActionDone -= ModelDocumentOnActionDone;
        _document = document;
        _document.ActionDone += ModelDocumentOnActionDone;
        RefreshRender();

        var minBounds = _document.Model.MinBounds.ToGodot();
        var maxBounds = _document.Model.MaxBounds.ToGodot();
        _orbitCamera.FocusBounds(new Aabb(minBounds, maxBounds - minBounds));
    }

    private void EditorConfigOnShowWireframeChanged(bool value)
    {
        foreach (var node in _wireframes)
        {
            node.Visible = value;
        }
    }

    private void EditorConfigOnShowBoundingBoxChanged(bool value)
    {
        _boundingBox?.Visible = value;
    }

    private void EditorConfigOnShowVHotsChanged(bool value)
    {
        foreach (var node in _vhots)
        {
            node.Visible = value;
        }
    }

    private void ModelDocumentOnActionDone()
    {
        RefreshRender();
    }

    #endregion

    public void SetState(EditorState state)
    {
        _state = state;
        _state.Config.ShowBoundingBoxChanged += EditorConfigOnShowBoundingBoxChanged;
        _state.Config.ShowWireframeChanged += EditorConfigOnShowWireframeChanged;
        _state.Config.ShowVHotsChanged += EditorConfigOnShowVHotsChanged;
        _state.ActiveModelChanged += StateOnActiveModelChanged;
        RefreshRender();
    }

    private void RefreshRender()
    {
        _vhots.Clear();
        _wireframes.Clear();
        foreach (var child in _modelContainer.GetChildren())
        {
            child.QueueFree();
        }

        if (_document == null)
        {
            return;
        }

        var modelFile = _document.Model;
        var defaultMaterial = new StandardMaterial3D();
        var materials = new System.Collections.Generic.Dictionary<int, StandardMaterial3D>();
        foreach (var rawMaterial in modelFile.Materials)
        {
            var slot = rawMaterial.Slot;

            if (rawMaterial.Type == 0)
            {
                var resName = PathUtils.ConvertSeparator(Path.GetFileNameWithoutExtension(rawMaterial.Name));
                if (!_state.Resources.TryGetObjectTextureVirtualPath(resName, out var virtualPath) ||
                    !TryLoadTexture(virtualPath, out var texture))
                {
                    Log.Warning(
                        "Failed to find model texture, or model texture format unsupported, adding default material: {Name}, {Slot}",
                        resName, slot);
                    materials.Add(slot, defaultMaterial);
                }
                else
                {
                    var material = new StandardMaterial3D
                    {
                        AlbedoTexture = texture,
                        Transparency = BaseMaterial3D.TransparencyEnum.AlphaDepthPrePass,
                        TextureFilter = _state.Config.TextureMode switch
                        {
                            TextureMode.Linear => BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic,
                            TextureMode.NearestNeighbour => BaseMaterial3D.TextureFilterEnum
                                .NearestWithMipmapsAnisotropic,
                            _ => BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic,
                        }
                    };
                    var name = rawMaterial.Name.ToLower();
                    for (var i = 0; i < 4; i++)
                    {
                        if (name.Contains($"replace{i}"))
                        {
                            material.SetMeta($"TxtRepl{i}", true);
                        }
                    }

                    materials.Add(slot, material);
                }
            }
            else
            {
                var r = rawMaterial.Color.R;
                var g = rawMaterial.Color.G;
                var b = rawMaterial.Color.B;
                var colour = new Color(r / 255.0f, g / 255.0f, b / 255.0f, 1.0f);
                materials.Add(slot, new StandardMaterial3D
                {
                    AlbedoColor = colour
                });
            }
        }

        var objCount = modelFile.Objects.Count;
        var meshes = new MeshInstance3D[objCount];
        for (var i = 0; i < objCount; i++)
        {
            var subObject = modelFile.Objects[i];
            var polyCount = modelFile.Polygons.Count;
            var transform = subObject.JointType == ModelObjectType.Static || subObject.JointIndex == -1
                ? Transform3D.Identity
                : subObject.Transform.ToGodot();

            var matPolyMap = new System.Collections.Generic.Dictionary<int, List<int>>();
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

                if (matPolyMap.ContainsKey(poly.Data))
                {
                    matPolyMap[poly.Data].Add(j);
                }
                else
                {
                    matPolyMap[poly.Data] = [j];
                }
            }

            var edges = new HashSet<(int, int)>();
            var arrayMesh = new ArrayMesh();
            foreach (var (slot, polyIdxs) in matPolyMap)
            {
                var material = materials.GetValueOrDefault(slot, defaultMaterial);
                var vertices = new List<Vector3>();
                var normals = new List<Vector3>();
                var uvs = new List<Vector2>();
                var indices = new List<int>();

                foreach (var polyIdx in polyIdxs)
                {
                    var poly = modelFile.Polygons[polyIdx];
                    var faceNormal = modelFile.FaceNormals[poly.NormalIndex].ToGodot();
                    var vertexCount = poly.VertexIndices.Count;
                    for (var j = 0; j < vertexCount; j++)
                    {
                        var vertexIndex = poly.VertexIndices[j];
                        var nextVertexIndex = poly.VertexIndices[(j + 1) % vertexCount];
                        edges.Add((vertexIndex.PositionIndex, nextVertexIndex.PositionIndex));
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

                var array = new Array();
                array.Resize((int)Mesh.ArrayType.Max);
                array[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
                array[(int)Mesh.ArrayType.Normal] = normals.ToArray();
                array[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
                array[(int)Mesh.ArrayType.Index] = indices.ToArray();

                var surfaceIdx = arrayMesh.GetSurfaceCount();
                arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, array);
                arrayMesh.SurfaceSetMaterial(surfaceIdx, material);
            }

            meshes[i] = new MeshInstance3D
            {
                Mesh = arrayMesh,
                Position = -modelFile.Center.ToGodot(),
                Transform = transform,
            };
            meshes[i].AddToGroup(GroupName.ModelMeshes);

            var lineVertices = new List<Vector3>();
            foreach (var (i0, i1) in edges)
            {
                lineVertices.Add(modelFile.VertexPositions[i0].ToGodot());
                lineVertices.Add(modelFile.VertexPositions[i1].ToGodot());
            }

            var objectWireframe = new LineRenderer { Vertices = lineVertices, LineColor = Colors.AliceBlue };
            objectWireframe.Visible = _state.Config.ShowWireframe;
            _wireframes.Add(objectWireframe);
            meshes[i].AddChild(objectWireframe);

            var vHotStart = modelFile.Objects[i].VHotStartIndex;
            var vHotEnd = vHotStart + modelFile.Objects[i].VHotCount;
            for (var j = vHotStart; j < vHotEnd; j++)
            {
                var modelVHot = modelFile.VHots[j];
                var vHot = new VHotRenderer
                {
                    DisplayName = ((int)modelVHot.Type).ToString(),
                    Position = modelVHot.Position.ToGodot(),
                    Visible = _state.Config.ShowVHots,
                };

                _vhots.Add(vHot);
                meshes[i].AddChild(vHot);
            }
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

        var minBounds = modelFile.MinBounds.ToGodot();
        var maxBounds = modelFile.MaxBounds.ToGodot();
        var boundsAabb = new Aabb(minBounds, maxBounds - minBounds);
        _boundingBox = LineRenderer.CreateAabb(boundsAabb, Colors.Brown);
        _modelContainer.AddChild(_boundingBox);
        _boundingBox.Visible = _state.Config.ShowBoundingBox;
    }

    private bool TryLoadTexture(string virtualPath,
        [MaybeNullWhen(false)] out Texture2D texture)
    {
        texture = null;
        if (!_state.Resources.TryGetFileMemoryStream(virtualPath, out var stream))
        {
            return false;
        }

        var ext = Path.GetExtension(virtualPath).ToLower();
        if (!DarkImageLoader.TryLoadImage(stream, ext, out var darkImage))
        {
            Log.Warning("Cannot load texture at virtual path ({VPath}). Unsupported file type.", virtualPath);
            return false;
        }

        var image = Image.CreateEmpty(darkImage.Width, darkImage.Height, true, Image.Format.Rgba8);
        image.LoadPngFromBuffer(darkImage.Stream.GetBuffer());
        texture = ImageTexture.CreateFromImage(image);
        return true;
    }
}