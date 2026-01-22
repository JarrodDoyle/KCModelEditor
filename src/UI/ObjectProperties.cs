using Godot;
using Serilog;

namespace KeepersCompound.ModelEditor.UI;

public partial class ObjectProperties : FoldableContainer
{
    private ModelDocument? _modelDocument;
    private int _objectIndex;

    #region Nodes

    private LineEdit _objectName = null!;
    private OptionButton _objectJointType = null!;
    private SpinBox _objectJointIndex = null!;

    #endregion

    #region Overrides

    public override void _Ready()
    {
        _objectName = GetNode<LineEdit>("%ObjectName");
        _objectJointType = GetNode<OptionButton>("%ObjectJointType");
        _objectJointIndex = GetNode<SpinBox>("%ObjectJointIndex");
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

    public void SetModelObject(ModelDocument modelDocument, int index)
    {
        _modelDocument = modelDocument;
        _objectIndex = index;
        _modelDocument.ActionDone += ModelDocumentOnActionDone;
        RefreshUi();
    }

    private void RefreshUi()
    {
        if (_modelDocument == null)
        {
            return;
        }

        var modelFile = _modelDocument.Model;
        if (_objectIndex < 0 || _objectIndex >= modelFile.Objects.Count)
        {
            Log.Error("Object index {idx} out of range.", _objectIndex);
            return;
        }

        var modelObject = modelFile.Objects[_objectIndex];
        Title = $"Object #{_objectIndex}";
        _objectName.Text = modelObject.Name;
        _objectJointType.Selected = (int)modelObject.JointType;
        _objectJointIndex.Value = (float)modelObject.JointIndex;
    }
}