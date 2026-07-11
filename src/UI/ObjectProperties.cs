using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Serilog;

namespace KeepersCompound.ModelEditor.UI;

[Meta(typeof(IAutoNode))]
public partial class ObjectProperties : FoldableContainer
{
    public override void _Notification(int what) => this.Notify(what);

    private ModelDocument _document = null!;
    private int _objectIndex;

    [Node] private LineEdit ObjectName {get; set;} = null!;
    [Node] private OptionButton ObjectJointType {get; set;} = null!;
    [Node] private SpinBox ObjectJointIndex {get; set;} = null!;

    public void OnReady()
    {
        _document.ActionDone += ModelDocumentOnActionDone;

        RefreshUi();
    }

    public void OnExitTree()
    {
        _document.ActionDone -= ModelDocumentOnActionDone;
    }

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
        ObjectName.Text = modelObject.Name;
        ObjectJointType.Selected = (int)modelObject.JointType;
        ObjectJointIndex.Value = (float)modelObject.JointIndex;
    }
}