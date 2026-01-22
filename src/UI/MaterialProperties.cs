using System.IO;
using Godot;
using KeepersCompound.Dark.Resources;
using KeepersCompound.Formats.Model;
using Serilog;

namespace KeepersCompound.ModelEditor.UI;

public partial class MaterialProperties : FoldableContainer
{
    #region Nodes

    private LineEdit _materialName = null!;
    private Button _materialNameBrowse = null!;
    private OptionButton _materialType = null!;
    private SpinBox _materialSlot = null!;
    private SpinBox _materialTransparency = null!;
    private SpinBox _materialSelfIllumination = null!;
    private ColorPickerButton _materialColor = null!;
    private SpinBox _materialPaletteIndex = null!;
    private HBoxContainer _transparencyContainer = null!;
    private HBoxContainer _selfIlluminationContainer = null!;
    private HBoxContainer _colorContainer = null!;
    private HBoxContainer _paletteIndexContainer = null!;

    #endregion

    private ResourceManager? _resourceManager;
    private ModelDocument? _modelDocument;
    private int _materialIndex;
    private PackedScene _itemSelectorScene = GD.Load<PackedScene>("uid://b1otvvvkdloah");

    #region Overrides

    public override void _Ready()
    {
        _materialName = GetNode<LineEdit>("%MaterialName");
        _materialNameBrowse = GetNode<Button>("%MaterialNameBrowse");
        _materialType = GetNode<OptionButton>("%MaterialType");
        _materialSlot = GetNode<SpinBox>("%MaterialSlot");
        _materialTransparency = GetNode<SpinBox>("%MaterialTransparency");
        _materialSelfIllumination = GetNode<SpinBox>("%MaterialSelfIllumination");
        _materialColor = GetNode<ColorPickerButton>("%MaterialColor");
        _materialPaletteIndex = GetNode<SpinBox>("%MaterialPaletteIndex");
        _transparencyContainer = GetNode<HBoxContainer>("%Transparency");
        _selfIlluminationContainer = GetNode<HBoxContainer>("%SelfIllumination");
        _colorContainer = GetNode<HBoxContainer>("%Color");
        _paletteIndexContainer = GetNode<HBoxContainer>("%PaletteIndex");

        _materialName.TextSubmitted += MaterialNameOnTextSubmitted;
        _materialNameBrowse.Pressed += MaterialNameBrowseOnPressed;
    }

    public override void _ExitTree()
    {
        _modelDocument?.ActionDone -= ModelDocumentOnActionDone;
        _materialName.TextSubmitted -= MaterialNameOnTextSubmitted;
        _materialNameBrowse.Pressed -= MaterialNameBrowseOnPressed;
    }

    #endregion

    #region Event Handling

    private void MaterialNameOnTextSubmitted(string newText)
    {
        DoNameChange(newText);
    }

    private void MaterialNameBrowseOnPressed()
    {
        if (_resourceManager == null)
        {
            return;
        }

        if (_itemSelectorScene.Instantiate() is not ItemSelectorWindow instance)
        {
            Log.Error("Item Selector Window failed to initialise.");
            return;
        }

        // We want to be showing both OM and FM model textures
        foreach (var campaign in new[] { "", _resourceManager.ActiveCampaign })
        {
            _resourceManager.SetActiveCampaign(campaign);
            foreach (var name in _resourceManager.GetModelTextureNames())
            {
                Log.Debug("Model Texture: {name}", name);
                instance.AddItem(Path.GetFileName(name).ToLower());
            }
        }

        AddChild(instance);
        instance.TrySelectItem(_materialName.Text.ToLower());
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

    public void SetModelMaterial(ResourceManager resourceManager, ModelDocument modelDocument, int index)
    {
        _resourceManager = resourceManager;
        _modelDocument = modelDocument;
        _materialIndex = index;

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
        if (_materialIndex < 0 || _materialIndex >= modelFile.Materials.Count)
        {
            Log.Error("Material index {idx} out of range.", _materialIndex);
            return;
        }

        var modelMaterial = modelFile.Materials[_materialIndex];
        Title = $"Material #{_materialIndex}";
        _materialName.Text = modelMaterial.Name;
        _materialType.Selected = (int)modelMaterial.Type;
        _materialSlot.Value = modelMaterial.Slot;
        _materialTransparency.Value = modelMaterial.Transparency;
        _materialSelfIllumination.Value = modelMaterial.SelfIllumination;
        _materialColor.Color = modelMaterial.Color.ToGodot();
        _materialPaletteIndex.Value = modelMaterial.PaletteIndex;

        if (modelFile.Version < 4)
        {
            _transparencyContainer.Visible = false;
            _selfIlluminationContainer.Visible = false;
        }

        if (modelMaterial.Type == ModelMaterialType.Texture)
        {
            _materialNameBrowse.Visible = true;
            _colorContainer.Visible = false;
            _paletteIndexContainer.Visible = false;
        }
    }

    private void DoNameChange(string newName)
    {
        if (_modelDocument == null)
        {
            return;
        }

        _modelDocument.Model.Materials[_materialIndex].Name = newName;
        _modelDocument.TriggerActionDone();
    }
}