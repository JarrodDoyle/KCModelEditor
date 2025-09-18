using Godot;

namespace KeepersCompound.ModelEditor;

public partial class FreeLookCamera : Camera3D
{
    private const float Sensitivity = 0.25f;
    private const float Acceleration = 30.0f;
    private const float Deceleration = 10.0f;

    private Vector2 _mouseDirection = Vector2.Zero;
    private Vector3 _velocity = Vector3.Zero;
    private float _velocityMultiplier = 5.0f;
    private float _totalPitch;

    private bool _forwardPressed;
    private bool _backPressed;
    private bool _leftPressed;
    private bool _rightPressed;
    private bool _upPressed;
    private bool _downPressed;

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        switch (inputEvent)
        {
            case InputEventMouseMotion motion:
                _mouseDirection += motion.Relative;
                break;
            case InputEventMouseButton button:
                switch (button.ButtonIndex)
                {
                    case MouseButton.Right:
                        Input.SetMouseMode(button.Pressed ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible);
                        break;
                    case MouseButton.WheelUp:
                        _velocityMultiplier = float.Clamp(_velocityMultiplier * 1.1f, 0.2f, 20.0f);
                        break;
                    case MouseButton.WheelDown:
                        _velocityMultiplier = float.Clamp(_velocityMultiplier / 1.1f, 0.2f, 20.0f);
                        break;
                }

                break;
            case InputEventKey key:
                switch (key.Keycode)
                {
                    case Key.W:
                        _forwardPressed = key.Pressed;
                        break;
                    case Key.S:
                        _backPressed = key.Pressed;
                        break;
                    case Key.A:
                        _leftPressed = key.Pressed;
                        break;
                    case Key.D:
                        _rightPressed = key.Pressed;
                        break;
                    case Key.Q:
                        _upPressed = key.Pressed;
                        break;
                    case Key.E:
                        _downPressed = key.Pressed;
                        break;
                }

                break;
        }
    }

    public override void _Process(double delta)
    {
        UpdateMouseLook();
        UpdateMovement((float)delta);
    }

    private void UpdateMouseLook()
    {
        if (Input.MouseMode != Input.MouseModeEnum.Captured)
        {
            _mouseDirection = Vector2.Zero;
            return;
        }

        var (yaw, pitch) = _mouseDirection * Sensitivity;
        _mouseDirection = Vector2.Zero;

        // Prevent looking too far up/down
        pitch = float.Clamp(pitch, -90.0f - _totalPitch, 90.0f - _totalPitch);
        _totalPitch += pitch;

        RotateY(float.DegreesToRadians(-yaw));
        RotateObjectLocal(Vector3.Right, float.DegreesToRadians(-pitch));
    }

    private void UpdateMovement(float delta)
    {
        var direction = Input.MouseMode == Input.MouseModeEnum.Captured
            ? new Vector3(
                (_rightPressed ? 1.0f : 0.0f) - (_leftPressed ? 1.0f : 0.0f),
                (_downPressed ? 1.0f : 0.0f) - (_upPressed ? 1.0f : 0.0f),
                (_backPressed ? 1.0f : 0.0f) - (_forwardPressed ? 1.0f : 0.0f)).Normalized()
            : Vector3.Zero;

        var offset = (direction * Acceleration - _velocity.Normalized() * Deceleration) * _velocityMultiplier * delta;
        if (direction == Vector3.Zero && offset.LengthSquared() >= _velocity.LengthSquared())
        {
            // Prevents jitter around zero velocity
            _velocity = Vector3.Zero;
        }
        else
        {
            _velocity = (_velocity + offset).Clamp(-_velocityMultiplier, _velocityMultiplier);
            Translate(_velocity * delta);
        }
    }
}