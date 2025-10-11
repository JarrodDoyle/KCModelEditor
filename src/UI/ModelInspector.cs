using System.Collections.Generic;
using System.Globalization;
using Godot;
using KeepersCompound.Dark.Resources;
using KeepersCompound.Formats.Model;
using Serilog;

namespace KeepersCompound.ModelEditor.UI;

public partial class ModelInspector : PanelContainer
{
    #region Events

    public delegate void ModelEditedEventHandler();

    public event ModelEditedEventHandler? ModelEdited;

    #endregion

    #region Nodes

    private LineEdit _modelName = null!;
    private LineEdit _modelVersion = null!;
    private LineEdit _modelRadius = null!;
    private SpinBox _modelCenterX = null!;
    private SpinBox _modelCenterY = null!;
    private SpinBox _modelCenterZ = null!;
    private LineEdit _modelVertexCount = null!;
    private LineEdit _modelPolygonCount = null!;
    private VBoxContainer _objectPropertiesContainer = null!;
    private VBoxContainer _materialPropertiesContainer = null!;
    private readonly List<ObjectProperties> _objectProperties = [];
    private readonly List<MaterialProperties> _materialProperties = [];

    #endregion

    private ModelFile? _modelFile;
    private PackedScene _objectPropertiesScene = GD.Load<PackedScene>("uid://dm7t23ax6kh1s");
    private PackedScene _materialPropertiesScene = GD.Load<PackedScene>("uid://g8haby7whlv2");

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
        _objectPropertiesContainer = GetNode<VBoxContainer>("%ObjectPropertiesContainer");
        _materialPropertiesContainer = GetNode<VBoxContainer>("%MaterialPropertiesContainer");
    }

    public void SetModel(ResourceManager resourceManager, ModelFile modelFile)
    {
        _modelFile = modelFile;

        // Update all the properties
        _modelName.Text = _modelFile.Name;
        _modelVersion.Text = _modelFile.Version.ToString();
        _modelRadius.Text = _modelFile.Radius.ToString(CultureInfo.InvariantCulture);
        _modelCenterX.Value = _modelFile.Center.X;
        _modelCenterY.Value = _modelFile.Center.Y;
        _modelCenterZ.Value = _modelFile.Center.Z;
        _modelVertexCount.Text = _modelFile.VertexPositions.Count.ToString();
        _modelPolygonCount.Text = _modelFile.Polygons.Count.ToString();

        foreach (var node in _objectProperties)
        {
            node.QueueFree();
        }

        _objectProperties.Clear();
        for (var i = 0; i < modelFile.Objects.Count; i++)
        {
            if (_objectPropertiesScene.Instantiate() is not ObjectProperties instance)
            {
                Log.Error("Object Properties inspector scene is null.");
                continue;
            }

            _objectPropertiesContainer.AddChild(instance);
            _objectProperties.Add(instance);
            instance.SetModelObject(_modelFile, i);
        }

        foreach (var node in _materialProperties)
        {
            node.MaterialEdited -= OnModelEdited;
            node.QueueFree();
        }

        _materialProperties.Clear();
        for (var i = 0; i < modelFile.Materials.Count; i++)
        {
            if (_materialPropertiesScene.Instantiate() is not MaterialProperties instance)
            {
                Log.Error("Material Properties inspector scene is null.");
                continue;
            }

            _materialPropertiesContainer.AddChild(instance);
            _materialProperties.Add(instance);
            instance.SetModelMaterial(resourceManager, _modelFile, i);
            instance.MaterialEdited += OnModelEdited;
        }
    }

    private void OnModelEdited()
    {
        ModelEdited?.Invoke();
    }
}