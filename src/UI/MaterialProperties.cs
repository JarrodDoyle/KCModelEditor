using System.IO;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using KeepersCompound.Formats.Model;
using Serilog;

namespace KeepersCompound.ModelEditor.UI;

[Meta(typeof(IAutoNode))]
public partial class MaterialProperties : FoldableContainer
{
    public override void _Notification(int what) => this.Notify(what);

    [Node] private LineEdit MaterialName { get; set; } = null!;
    [Node] private Button MaterialNameBrowse { get; set; } = null!;
    [Node] private OptionButton MaterialType { get; set; } = null!;
    [Node] private SpinBox MaterialSlot { get; set; } = null!;
    [Node] private SpinBox MaterialTransparency { get; set; } = null!;
    [Node] private SpinBox MaterialSelfIllumination { get; set; } = null!;
    [Node] private ColorPickerButton MaterialColor { get; set; } = null!;
    [Node] private SpinBox MaterialPaletteIndex { get; set; } = null!;
    [Node] private HBoxContainer TransparencyContainer { get; set; } = null!;
    [Node] private HBoxContainer SelfIlluminationContainer { get; set; } = null!;
    [Node] private HBoxContainer ColorContainer { get; set; } = null!;
    [Node] private HBoxContainer PaletteIndexContainer { get; set; } = null!;

    private EditorState _state = null!;
    private ModelDocument _document = null!;
    private int _materialIndex;
    private PackedScene _itemSelectorScene = GD.Load<PackedScene>("uid://b1otvvvkdloah");

    public void OnReady()
    {
        _document.ActionDone += ModelDocumentOnActionDone;
        MaterialName.TextSubmitted += MaterialNameOnTextSubmitted;
        MaterialNameBrowse.Pressed += MaterialNameBrowseOnPressed;

        RefreshUi();
    }

    public void OnExitTree()
    {
        _document.ActionDone -= ModelDocumentOnActionDone;
        MaterialName.TextSubmitted -= MaterialNameOnTextSubmitted;
        MaterialNameBrowse.Pressed -= MaterialNameBrowseOnPressed;
    }

    #region Event Handling

    private void MaterialNameOnTextSubmitted(string newText)
    {
        DoNameChange(newText);
    }

    private void MaterialNameBrowseOnPressed()
    {
        if (_itemSelectorScene.Instantiate() is not ItemSelectorWindow instance)
        {
            Log.Error("Item Selector Window failed to initialise.");
            return;
        }

        // We want to be showing both OM and FM model textures
        foreach (var campaign in new[] { "", _state.Resources.ActiveCampaign })
        {
            _state.Resources.SetActiveCampaign(campaign);
            foreach (var name in _state.Resources.GetModelTextureNames())
            {
                Log.Debug("Model Texture: {name}", name);
                instance.AddItem(Path.GetFileName(name).ToLower());
            }
        }

        AddChild(instance);
        instance.TrySelectItem(MaterialName.Text.ToLower());
        instance.Selected += index =>
        {
            if (instance.TryGetItem(index, out var item))
            {
                DoNameChange(item);
            }
        };
    }

    private void ModelDocumentOnActionDone()
    {
        RefreshUi();
    }

    #endregion

    public void SetState(EditorState state, ModelDocument modelDocument, int index)
    {
        _state = state;
        _document = modelDocument;
        _materialIndex = index;
    }

    private void RefreshUi()
    {
        var modelFile = _document.Model;
        if (_materialIndex < 0 || _materialIndex >= modelFile.Materials.Count)
        {
            Log.Error("Material index {idx} out of range.", _materialIndex);
            return;
        }

        var modelMaterial = modelFile.Materials[_materialIndex];
        Title = $"Material #{_materialIndex}";
        MaterialName.Text = modelMaterial.Name;
        MaterialType.Selected = (int)modelMaterial.Type;
        MaterialSlot.Value = modelMaterial.Slot;
        MaterialTransparency.Value = modelMaterial.Transparency;
        MaterialSelfIllumination.Value = modelMaterial.SelfIllumination;
        MaterialColor.Color = modelMaterial.Color.ToGodot();
        MaterialPaletteIndex.Value = modelMaterial.PaletteIndex;

        if (modelFile.Version < 4)
        {
            TransparencyContainer.Visible = false;
            SelfIlluminationContainer.Visible = false;
        }

        if (modelMaterial.Type == ModelMaterialType.Texture)
        {
            MaterialNameBrowse.Visible = true;
            ColorContainer.Visible = false;
            PaletteIndexContainer.Visible = false;
        }
    }

    private void DoNameChange(string newName)
    {
        var previousName = _document.Model.Materials[_materialIndex].Name;
        _document.DoAction((
            m => { m.Materials[_materialIndex].Name = newName; },
            m => { m.Materials[_materialIndex].Name = previousName; }
        ));
    }
}