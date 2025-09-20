using System.Globalization;
using Godot;
using KeepersCompound.Formats.Model;

namespace KeepersCompound.ModelEditor.UI;

public partial class ModelInspector : PanelContainer
{
    private ModelFile? _modelFile;

    #region Nodes

    private LineEdit? _modelName;
    private LineEdit? _modelVersion;
    private LineEdit? _modelRadius;
    private SpinBox? _modelCenterX;
    private SpinBox? _modelCenterY;
    private SpinBox? _modelCenterZ;
    private LineEdit? _modelVertexCount;
    private LineEdit? _modelPolygonCount;

    #endregion

    public override void _Ready()
    {
        _modelName = GetNode<LineEdit>("%ModelName");
        _modelVersion = GetNode<LineEdit>("%ModelVersion");
        _modelRadius = GetNode<LineEdit>("%ModelRadius");
        _modelCenterX = GetNode<SpinBox>("%ModelCenterX");
        _modelCenterY = GetNode<SpinBox>("%ModelCenterY");
        _modelCenterZ = GetNode<SpinBox>("%ModelCenterZ");
        _modelVertexCount = GetNode<LineEdit>("%ModelVertexCount");
        _modelPolygonCount = GetNode<LineEdit>("%ModelPolygonCount");
    }

    public void SetModel(ModelFile modelFile)
    {
        _modelFile = modelFile;

        // Update all the properties
        _modelName?.Text = _modelFile.Name;
        _modelVersion?.Text = _modelFile.Version.ToString();
        _modelRadius?.Text = _modelFile.Radius.ToString(CultureInfo.InvariantCulture);
        _modelCenterX?.Value = _modelFile.Center.X;
        _modelCenterY?.Value = _modelFile.Center.Y;
        _modelCenterZ?.Value = _modelFile.Center.Z;
        _modelVertexCount?.Text = _modelFile.VertexPositions.Count.ToString();
        _modelPolygonCount?.Text = _modelFile.Polygons.Count.ToString();
    }
}