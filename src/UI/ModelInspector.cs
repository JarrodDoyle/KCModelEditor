using System.Collections.Generic;
using System.Globalization;
using Godot;
using KeepersCompound.Dark.Resources;
using Serilog;

namespace KeepersCompound.ModelEditor.UI;

public partial class ModelInspector : PanelContainer
{
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

    private ResourceManager? _resourceManager;
    private ModelDocument? _modelDocument;
    private PackedScene _objectPropertiesScene = GD.Load<PackedScene>("uid://dm7t23ax6kh1s");
    private PackedScene _materialPropertiesScene = GD.Load<PackedScene>("uid://g8haby7whlv2");

    #region Overrides

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

    public override void _ExitTree()
    {
        _modelDocument?.ActionDone -= ModelDocumentOnActionDone;
    }

    #endregion

    #region Event Handling

    private void ModelDocumentOnActionDone()
    {
        RefreshUi();
    }

    #endregion

    public void SetModel(ResourceManager resourceManager, ModelDocument modelDocument)
    {
        _resourceManager = resourceManager;
        _modelDocument = modelDocument;
        _modelDocument.ActionDone += ModelDocumentOnActionDone;
        RefreshUi();
    }

    private void RefreshUi()
    {
        if (_resourceManager == null)
        {
            Log.Error("Resource manager is null.");
            return;
        }

        if (_modelDocument == null)
        {
            foreach (var node in _objectProperties)
            {
                node.QueueFree();
            }

            foreach (var node in _materialProperties)
            {
                node.QueueFree();
            }

            _objectProperties.Clear();
            _materialProperties.Clear();
            return;
        }

        var modelFile = _modelDocument.Model;
        _modelName.Text = modelFile.Name;
        _modelVersion.Text = modelFile.Version.ToString();
        _modelRadius.Text = modelFile.Radius.ToString(CultureInfo.InvariantCulture);
        _modelCenterX.Value = modelFile.Center.X;
        _modelCenterY.Value = modelFile.Center.Y;
        _modelCenterZ.Value = modelFile.Center.Z;
        _modelVertexCount.Text = modelFile.VertexPositions.Count.ToString();
        _modelPolygonCount.Text = modelFile.Polygons.Count.ToString();

        for (var i = _objectProperties.Count - 1; i >= modelFile.Objects.Count; i--)
        {
            _objectProperties[i].QueueFree();
            _objectProperties.RemoveAt(i);
        }

        for (var i = 0; i < modelFile.Objects.Count; i++)
        {
            if (i >= _objectProperties.Count)
            {
                if (_objectPropertiesScene.Instantiate() is not ObjectProperties instance)
                {
                    Log.Error("Object Properties inspector scene is null.");
                    continue;
                }

                _objectPropertiesContainer.AddChild(instance);
                _objectProperties.Add(instance);
            }

            _objectProperties[i].SetModelObject(_modelDocument, i);
        }

        for (var i = _materialProperties.Count - 1; i >= modelFile.Materials.Count; i--)
        {
            _materialProperties[i].QueueFree();
            _materialProperties.RemoveAt(i);
        }

        for (var i = 0; i < modelFile.Materials.Count; i++)
        {
            if (i >= _materialProperties.Count)
            {
                if (_materialPropertiesScene.Instantiate() is not MaterialProperties instance)
                {
                    Log.Error("Object Properties inspector scene is null.");
                    continue;
                }

                _materialPropertiesContainer.AddChild(instance);
                _materialProperties.Add(instance);
            }

            _materialProperties[i].SetModelMaterial(_resourceManager, _modelDocument, i);
        }
    }
}