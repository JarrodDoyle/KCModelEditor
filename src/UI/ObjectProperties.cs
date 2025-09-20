using Godot;
using KeepersCompound.Formats.Model;
using Serilog;

namespace KeepersCompound.ModelEditor.UI;

public partial class ObjectProperties : FoldableContainer
{
    private ModelObject? _modelObject;

    #region Nodes

    private LineEdit? _objectName;
    private OptionButton? _objectJointType;
    private SpinBox? _objectJointIndex;

    #endregion

    public override void _Ready()
    {
        _objectName = GetNode<LineEdit>("%ObjectName");
        _objectJointType = GetNode<OptionButton>("%ObjectJointType");
        _objectJointIndex = GetNode<SpinBox>("%ObjectJointIndex");
    }

    public void SetModelObject(ModelFile modelFile, int index)
    {
        if (index < 0 || index >= modelFile.Objects.Count)
        {
            Log.Error("Object index {idx} out of range.", index);
            return;
        }

        _modelObject = modelFile.Objects[index];

        Title = $"Object #{index}";
        _objectName?.Text = _modelObject.Name;
        _objectJointType?.Selected = (int)_modelObject.JointType;
        _objectJointIndex?.Value = (float)_modelObject.JointIndex;
    }
}