using System.IO;
using Godot;
using KeepersCompound.Dark.Resources;
using KeepersCompound.Formats.Model;
using Serilog;

namespace KeepersCompound.ModelEditor.UI;

public partial class MaterialProperties : FoldableContainer
{
    #region Events

    public delegate void MaterialEditedEventHandler();

    public event MaterialEditedEventHandler? MaterialEdited; 

    #endregion

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
    private ModelMaterial? _modelMaterial;
    private PackedScene _itemSelectorScene = GD.Load<PackedScene>("uid://b1otvvvkdloah");

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

    public void SetModelMaterial(ResourceManager resourceManager, ModelFile modelFile, int index)
    {
        if (index < 0 || index >= modelFile.Materials.Count)
        {
            Log.Error("Material index {idx} out of range.", index);
            return;
        }

        _resourceManager = resourceManager;
        _modelMaterial = modelFile.Materials[index];

        Title = $"Material #{index}";
        _materialName.Text = _modelMaterial.Name;
        _materialType.Selected = (int)_modelMaterial.Type;
        _materialSlot.Value = _modelMaterial.Slot;
        _materialTransparency.Value = _modelMaterial.Transparency;
        _materialSelfIllumination.Value = _modelMaterial.SelfIllumination;
        _materialColor.Color = _modelMaterial.Color.ToGodot();
        _materialPaletteIndex.Value = _modelMaterial.PaletteIndex;

        if (modelFile.Version < 4)
        {
            _transparencyContainer.Visible = false;
            _selfIlluminationContainer.Visible = false;
        }

        if (_modelMaterial.Type == ModelMaterialType.Texture)
        {
            _materialNameBrowse.Visible = true;
            _colorContainer.Visible = false;
            _paletteIndexContainer.Visible = false;
        }
    }

    #region Event Handling

    private void MaterialNameOnTextSubmitted(string newText)
    {
        _modelMaterial?.Name = newText;
        MaterialEdited?.Invoke();
    }

    private void MaterialNameBrowseOnPressed()
    {
        if (_resourceManager == null)
        {
            return;
        }

        if (_itemSelectorScene?.Instantiate() is not ItemSelectorWindow instance)
        {
            Log.Error("Item Selector Window failed to initialise.");
            return;
        }

        // We want to be showing both OM and FM model textures
        foreach (var campaign in new [] { "", _resourceManager.ActiveCampaign })
        {
            _resourceManager.SetActiveCampaign(campaign);
            foreach (var name in _resourceManager.GetModelTextureNames())
            {
                Log.Debug("Model Texture: {name}", name);
                instance.AddItem(Path.GetFileName(name));
            }
        }

        AddChild(instance);
        instance.TrySelectItem(_materialName.Text);
        instance.Selected += index =>
        {
            if (instance.TryGetItem(index, out var item))
            {
                _materialName.Text = item;
                _modelMaterial?.Name = item;
                MaterialEdited?.Invoke();
            }
        };
    }

    #endregion
}