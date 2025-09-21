using Godot;

namespace KeepersCompound.ModelEditor;

public partial class OrbitCamera : Node3D
{
    #region Exports

    [Export] public float Distance { get; set; } = 5.0f;
    [Export] public float MinDistance { get; set; } = 0.5f;
    [Export] public float MaxDistance { get; set; } = 15.0f;
    [Export] public float ZoomStep { get; set; } = 0.5f;
    [Export] public float LerpSpeed { get; set; } = 10.0f;
    [Export] public float OrbitSensitivity { get; set; } = 0.25f;
    [Export] public float PanSensitivity { get; set; } = 0.1f;

    #endregion

    #region Nodes

    private Camera3D? _camera;

    #endregion

    private Vector2 _mouseMotion = Vector2.Zero;
    private float _cameraPitch;
    private bool _panning;

    #region Overrides

    public override void _Ready()
    {
        _camera = GetNode<Camera3D>("%Camera");
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        switch (inputEvent)
        {
            case InputEventMouseMotion motion:
                _mouseMotion += motion.Relative;
                break;
            case InputEventMouseButton button:
                switch (button.ButtonIndex)
                {
                    case MouseButton.Right:
                        _panning = button.Pressed;
                        break;
                    case MouseButton.Middle:
                        Input.SetMouseMode(button.Pressed ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible);
                        break;
                    case MouseButton.WheelUp:
                        Distance = float.Clamp(Distance - ZoomStep, MinDistance, MaxDistance);
                        break;
                    case MouseButton.WheelDown:
                        Distance = float.Clamp(Distance + ZoomStep, MinDistance, MaxDistance);
                        break;
                }
                break;
        }
    }

    public override void _Process(double delta)
    {
        if (Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            var (yaw, pitch) = _mouseMotion * OrbitSensitivity;
            pitch = float.Clamp(pitch, -90.0f - _cameraPitch, 90.0f - _cameraPitch);
            RotateY(float.DegreesToRadians(-yaw));
            RotateObjectLocal(Vector3.Right, float.DegreesToRadians(-pitch));
            _cameraPitch += pitch;
        } else if (_panning)
        {
            var offset = new Vector3(-_mouseMotion.X, _mouseMotion.Y, 0) * PanSensitivity;
            Position = Position.Lerp(Position + offset, LerpSpeed * (float)delta);
        }

        _mouseMotion = Vector2.Zero;
        _camera?.Position = _camera.Position.Lerp(new Vector3(0, 0, Distance), LerpSpeed * (float)delta);
    }

    #endregion
}