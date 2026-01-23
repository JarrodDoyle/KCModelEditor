using System.Collections.Generic;
using System.Globalization;
using Godot;
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

    private EditorState _state = null!;
    private ModelDocument? _document;
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
        _state.ActiveModelChanged -= StateOnActiveModelChanged;
        _document?.ActionDone -= DocumentOnActionDone;
    }

    #endregion

    #region Event Handling

    private void StateOnActiveModelChanged(ModelDocument document)
    {
        _document?.ActionDone -= DocumentOnActionDone;
        _document = document;
        _document.ActionDone += DocumentOnActionDone;
        RefreshUi();
    }

    private void DocumentOnActionDone()
    {
        RefreshUi();
    }

    #endregion

    public void SetState(EditorState state)
    {
        _state = state;
        _state.ActiveModelChanged += StateOnActiveModelChanged;
        RefreshUi();
    }

    private void RefreshUi()
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

        if (_document == null)
        {
            _modelName.Text = "";
            _modelVersion.Text = "";
            _modelRadius.Text = "";
            _modelCenterX.Value = 0;
            _modelCenterY.Value = 0;
            _modelCenterZ.Value = 0;
            _modelVertexCount.Text = "";
            _modelPolygonCount.Text = "";
            return;
        }

        var modelFile = _document.Model;
        _modelName.Text = modelFile.Name;
        _modelVersion.Text = modelFile.Version.ToString();
        _modelRadius.Text = modelFile.Radius.ToString(CultureInfo.InvariantCulture);
        _modelCenterX.Value = modelFile.Center.X;
        _modelCenterY.Value = modelFile.Center.Y;
        _modelCenterZ.Value = modelFile.Center.Z;
        _modelVertexCount.Text = modelFile.VertexPositions.Count.ToString();
        _modelPolygonCount.Text = modelFile.Polygons.Count.ToString();

        for (var i = 0; i < modelFile.Objects.Count; i++)
        {
            if (i < _objectProperties.Count)
            {
                continue;
            }

            if (_objectPropertiesScene.Instantiate() is not ObjectProperties instance)
            {
                Log.Error("Object Properties inspector scene is null.");
                continue;
            }

            instance.SetState(_document, i);
            _objectPropertiesContainer.AddChild(instance);
            _objectProperties.Add(instance);
        }

        for (var i = 0; i < modelFile.Materials.Count; i++)
        {
            if (i < _materialProperties.Count)
            {
                continue;
            }

            if (_materialPropertiesScene.Instantiate() is not MaterialProperties instance)
            {
                Log.Error("Object Properties inspector scene is null.");
                continue;
            }

            instance.SetState(_state, _document, i);
            _materialPropertiesContainer.AddChild(instance);
            _materialProperties.Add(instance);
        }
    }
}