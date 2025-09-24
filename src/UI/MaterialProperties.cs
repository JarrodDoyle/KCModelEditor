using Godot;
using KeepersCompound.Formats.Model;
using Serilog;

namespace KeepersCompound.ModelEditor.UI;

public partial class MaterialProperties : FoldableContainer
{
    #region Events

    public delegate void MaterialEditedEventHandler();

    public event MaterialEditedEventHandler MaterialEdited;

    #endregion

    private ModelMaterial? _modelMaterial;

    #region Nodes

    private LineEdit? _materialName;
    private OptionButton? _materialType;
    private SpinBox? _materialSlot;
    private SpinBox? _materialTransparency;
    private SpinBox? _materialSelfIllumination;
    private ColorPickerButton? _materialColor;
    private SpinBox? _materialPaletteIndex;
    private HBoxContainer? _transparencyContainer;
    private HBoxContainer? _selfIlluminationContainer;
    private HBoxContainer? _colorContainer;
    private HBoxContainer? _paletteIndexContainer;

    #endregion

    public override void _Ready()
    {
        _materialName = GetNode<LineEdit>("%MaterialName");
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
    }

    public void SetModelMaterial(ModelFile modelFile, int index)
    {
        if (index < 0 || index >= modelFile.Materials.Count)
        {
            Log.Error("Material index {idx} out of range.", index);
            return;
        }

        _modelMaterial = modelFile.Materials[index];

        Title = $"Material #{index}";
        _materialName?.Text = _modelMaterial.Name;
        _materialType?.Selected = (int)_modelMaterial.Type;
        _materialSlot?.Value = _modelMaterial.Slot;
        _materialTransparency?.Value = _modelMaterial.Transparency;
        _materialSelfIllumination?.Value = _modelMaterial.SelfIllumination;
        _materialColor?.Color = _modelMaterial.Color.ToGodot();
        _materialPaletteIndex?.Value = _modelMaterial.PaletteIndex;

        if (modelFile.Version < 4)
        {
            _transparencyContainer?.Visible = false;
            _selfIlluminationContainer?.Visible = false;
        }

        if (_modelMaterial.Type == ModelMaterialType.Texture)
        {
            _colorContainer?.Visible = false;
            _paletteIndexContainer?.Visible = false;
        }
    }

    private void MaterialNameOnTextSubmitted(string newText)
    {
        _modelMaterial?.Name = newText;
        MaterialEdited.Invoke();
    }
}