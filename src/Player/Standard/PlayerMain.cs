using Godot;
using GameManager = MSSC2027.AutoLoad.GameManager;

namespace MSSC2027.Player.Standard;

public partial class PlayerMain : CharacterBody3D
{
    #region Zmienne
    // Eksporty
    [ExportCategory("Referencje")]
    [Export] private Node3D _head;
    [Export] private Camera3D _camera;
    [Export] private Panel _staminaBar;
    [Export] private Node3D _cameraCrouchPosition;

    [ExportCategory("Rodzaje prękdkości i ruchu")]
    [Export] private float _jumpVelocity = 4.5f;
    [Export] private float _acceleration = 0.5f;
    [Export] private float _deceleration = 0.3f;

    [ExportGroup("Prędkości")]
    [Export] private float _sprintSpeed = 10.0f;
    [Export] private float _standardSpeed = 5.0f;
    [Export] private float _crouchSpeed = 2.5f;

    [ExportCategory("Ustawienia Staminy")]
    [Export] private float _staminaRegeneration = 0.05f;
    [Export] private float _staminaDrain = 0.1f;
    [Export] private float _maxStamina = 100.0f;
    [Export] private float _minStaminaToSprint = 5.0f;

    [ExportCategory("Ustawienia Kamery")]
    [Export] private float _fovChange = 1.5f;
    [Export] private bool _enableTilt = true;
    [Export] private bool _enableBob = true;

    [ExportGroup("Nachylenie")]
    [Export] private float _cameraTiltSpeed = 10.0f;
    [Export] private float _runPitch = 0.1f;
    [Export] private float _runRoll = 0.25f;
    [Export] private float _maxPitch = 1.0f;
    [Export] private float _maxRoll = 2.5f;

    [ExportGroup("Kiwanie")]
    [Export] private float _bobFrequency = 2.0f;
    [Export] private float _bobAmplitude = 0.08f;

    // AutoLoad
    private float _mouseSensitivity;
    private float _stickSensitivity;
    private float _baseFov;

    // Stamina i speed
    private float _speed;
    private float _stamina;
    private float Stamina
    {
        get => _stamina;
        set
        {
            _stamina = value;
            UpdateStaminaBar();
        }
    }

    // Zmienne Logiczne
    private bool _isSprinting;
    private bool _isCrouching;
    private bool _isExhausted;

    // Inne
    private Vector3 _standardPosition;
    private Vector3 _targetHeadPosition;
    private float _timeBob;
    #endregion

    public override void _Ready()
    {
        // AutoLoad
        _mouseSensitivity = GameManager.Instance.MouseSensitivity;
        _stickSensitivity = GameManager.Instance.StickSensitivity;
        _baseFov = GameManager.Instance.BaseFov;

        // Ustawianie domyślnych wartości
        _camera.Fov = _baseFov;
        _speed = _standardSpeed;
        Stamina = _maxStamina;
        Input.MouseMode = Input.MouseModeEnum.Captured;
        _standardPosition = _head.Position;
    }

    private void Look(float yaw, float pitch)
    {
        RotateY(yaw);
        _head.RotateX(pitch);
        var rot = _head.Rotation;
        rot.X = Mathf.Clamp(rot.X, Mathf.DegToRad(-89f), Mathf.DegToRad(89f));
        _head.Rotation = rot;
    }

    private void Crouch()
    {
        if (_isCrouching)
        {
            _targetHeadPosition = _cameraCrouchPosition.Position;
            _speed = _crouchSpeed;
        }
        else
        {
            _targetHeadPosition = _standardPosition;
            _speed = _standardSpeed;
        }
    }

    private void Sprint()
    {
        if (_isSprinting)
        {
            _speed = _sprintSpeed;
            Stamina = Mathf.Max(0.0f, Stamina - _staminaDrain);

            if (!(Stamina <= 0.0f)) return;
            _isSprinting = false;
            _isExhausted = true;
        }
        else
        {
            if (Stamina < _maxStamina)
                Stamina = Mathf.Min(_maxStamina, Stamina + _staminaRegeneration);
            if (_isExhausted && Stamina >= _minStaminaToSprint)
                _isExhausted = false;
        }
    }

    private void UpdateStaminaBar()
    {
        _staminaBar.SetDeferred(
            Control.PropertyName.Size,
            new Vector2(1920 * Stamina / _maxStamina, _staminaBar.Size.Y)
        );
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            Look(
                -mouseMotion.Relative.X * _mouseSensitivity,
                -mouseMotion.Relative.Y * _mouseSensitivity
            );
        }
    }

    private Vector3 HeadBob(float time)
    {
        Vector3 pos = Vector3.Zero;
        pos.Y = Mathf.Sin(time * _bobFrequency) * _bobAmplitude;
        pos.X = Mathf.Cos(time * _bobFrequency / 2) * _bobAmplitude;
        return pos;
    }

    private void CalculateViewOffset(float delta, Vector3 velocity)
    {
        Vector3 angles = Vector3.Zero;

        if (_enableTilt)
        {
            Vector3 forward = GlobalTransform.Basis.Z;
            Vector3 right = GlobalTransform.Basis.X;

            float forwardDot = velocity.Dot(forward);
            float forwardTilt = Mathf.Clamp(
                forwardDot * Mathf.DegToRad(_runPitch),
                Mathf.DegToRad(-_maxPitch),
                Mathf.DegToRad(_maxPitch)
            );
            angles.X += forwardTilt;

            float rightDot = velocity.Dot(right);
            float sideTilt = Mathf.Clamp(
                rightDot * Mathf.DegToRad(_runRoll),
                Mathf.DegToRad(-_maxRoll),
                Mathf.DegToRad(_maxRoll)
            );
            angles.Z -= sideTilt;
        }

        _camera.Rotation = _camera.Rotation.Lerp(angles, _cameraTiltSpeed * delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;
        float d = (float)delta;

        // Dodana grawitacja
        if (!IsOnFloor())
        {
            velocity += GetGravity() * d * 1.5f;
        }

        // Zarządzanie jumpem
        if (Input.IsActionJustPressed("Jump") && IsOnFloor() && !_isCrouching)
        {
            velocity.Y = _jumpVelocity;
        }

        // Zarządzanie sprintem
        if (Input.IsActionPressed("Shift") && IsOnFloor() && !_isCrouching && !_isExhausted)
        {
            _isSprinting = Stamina > 0.0f;
        }
        if (Input.IsActionJustReleased("Shift") && !_isCrouching)
        {
            _isSprinting = false;
        }

        // Zarządzanie crouchem
        if (Input.IsActionPressed("Crouch") && IsOnFloor() && !_isSprinting)
        {
            _isCrouching = true;
        }
        if (Input.IsActionJustReleased("Crouch") && !_isSprinting)
        {
            _isCrouching = false;
        }

        // Sterowanie
        Vector2 inputDir = Input.GetVector("MoveLeft", "MoveRight", "MoveForward", "MoveBackward");
        Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
        if (IsOnFloor())
        {
            if (direction != Vector3.Zero)
            {
                velocity.X = direction.X * _speed;
                velocity.Z = direction.Z * _speed;
            }
            else
            {
                velocity.X = Mathf.Lerp(velocity.X, direction.X * _speed, d * 7.0f);
                velocity.Z = Mathf.Lerp(velocity.Z, direction.Z * _speed, d * 7.0f);
            }
        }
        else
        {
            velocity.X = Mathf.Lerp(velocity.X, direction.X * _speed, d * 3.0f);
            velocity.Z = Mathf.Lerp(velocity.Z, direction.Z * _speed, d * 3.0f);
        }

        // Obracanie kamerą na gamepadzie
        Vector2 look = Input.GetVector("LookLeft", "LookRight", "LookUp", "LookDown");
        if (look != Vector2.Zero)
        {
            Look(-look.X * _stickSensitivity * d, -look.Y * _stickSensitivity * d);
        }

        // Funkcje do działania crouchu i sprintu
        Crouch();
        Sprint();

        // Płynne przejście między crouchem i standard position
        Vector3 currentPosition = _head.Position;
        currentPosition.Y = Mathf.Lerp(currentPosition.Y, _targetHeadPosition.Y, 7.0f * d);
        _head.Position = currentPosition;

        // Kiwanie głową
        if (_enableBob)
        {
            if (IsOnFloor() && velocity.Length() > 0.1f)
            {
                _timeBob += d * velocity.Length();
            }
            Vector3 camPos = _camera.Position;
            camPos.Y = HeadBob(_timeBob).Y;
            _camera.Position = camPos;
        }

        // Nachylenie Kamery
        CalculateViewOffset(d, velocity);

        // FOV
        float velocityClamped = Mathf.Clamp(velocity.Length(), 0.5f, _sprintSpeed * 2.0f);
        float targetFov = _baseFov + _fovChange + velocityClamped;
        _camera.Fov = Mathf.Lerp(_camera.Fov, targetFov, d * 8.0f);

        Velocity = velocity;
        MoveAndSlide();
    }
}
