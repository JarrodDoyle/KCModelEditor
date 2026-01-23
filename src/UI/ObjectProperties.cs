using Godot;
using Serilog;

namespace KeepersCompound.ModelEditor.UI;

public partial class ObjectProperties : FoldableContainer
{
    private ModelDocument _document = null!;
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

        _document.ActionDone += ModelDocumentOnActionDone;

        RefreshUi();
    }

    public override void _ExitTree()
    {
        _document.ActionDone -= ModelDocumentOnActionDone;
    }

    #endregion

    #region Event Handling

    private void ModelDocumentOnActionDone()
    {
        RefreshUi();
    }

    #endregion

    public void SetState(ModelDocument modelDocument, int index)
    {
        _document = modelDocument;
        _objectIndex = index;
    }

    private void RefreshUi()
    {
        var modelFile = _document.Model;
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