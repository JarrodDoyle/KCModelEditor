using System.Collections.Generic;
using System.Globalization;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Serilog;

namespace KeepersCompound.ModelEditor.UI;

[Meta(typeof(IAutoNode))]
public partial class ModelInspector : PanelContainer
{
    public override void _Notification(int what) => this.Notify(what);

    [Node] private LineEdit ModelName { get; set; } = null!;
    [Node] private LineEdit ModelVersion { get; set; } = null!;
    [Node] private LineEdit ModelRadius { get; set; } = null!;
    [Node] private SpinBox ModelCenterX { get; set; } = null!;
    [Node] private SpinBox ModelCenterY { get; set; } = null!;
    [Node] private SpinBox ModelCenterZ { get; set; } = null!;
    [Node] private LineEdit ModelVertexCount { get; set; } = null!;
    [Node] private LineEdit ModelPolygonCount { get; set; } = null!;
    [Node] private VBoxContainer ObjectPropertiesContainer { get; set; } = null!;
    [Node] private VBoxContainer MaterialPropertiesContainer { get; set; } = null!;
    private readonly List<ObjectProperties> _objectProperties = [];
    private readonly List<MaterialProperties> _materialProperties = [];

    [Dependency] private EditorState EditorState => this.DependOn<EditorState>();
    private ModelDocument? _document;
    private PackedScene _objectPropertiesScene = GD.Load<PackedScene>("uid://dm7t23ax6kh1s");
    private PackedScene _materialPropertiesScene = GD.Load<PackedScene>("uid://g8haby7whlv2");

    public void OnResolved()
    {
        EditorState.ActiveModelChanged += EditorStateOnActiveModelChanged;
        RefreshUi();
    }

    public void OnExitTree()
    {
        EditorState.ActiveModelChanged -= EditorStateOnActiveModelChanged;
        _document?.ActionDone -= DocumentOnActionDone;
    }

    #region Event Handling

    private void EditorStateOnActiveModelChanged(ModelDocument document)
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
            ModelName.Text = "";
            ModelVersion.Text = "";
            ModelRadius.Text = "";
            ModelCenterX.Value = 0;
            ModelCenterY.Value = 0;
            ModelCenterZ.Value = 0;
            ModelVertexCount.Text = "";
            ModelPolygonCount.Text = "";
            return;
        }

        var modelFile = _document.Model;
        ModelName.Text = modelFile.Name;
        ModelVersion.Text = modelFile.Version.ToString();
        ModelRadius.Text = modelFile.Radius.ToString(CultureInfo.InvariantCulture);
        ModelCenterX.Value = modelFile.Center.X;
        ModelCenterY.Value = modelFile.Center.Y;
        ModelCenterZ.Value = modelFile.Center.Z;
        ModelVertexCount.Text = modelFile.VertexPositions.Count.ToString();
        ModelPolygonCount.Text = modelFile.Polygons.Count.ToString();

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
            ObjectPropertiesContainer.AddChild(instance);
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

            instance.SetState(EditorState, _document, i);
            MaterialPropertiesContainer.AddChild(instance);
            _materialProperties.Add(instance);
        }
    }
}