using Godot;
using MSSC2027.Vehicles.SnowMobile;
using GameManager = MSSC2027.AutoLoad.GameManager;

namespace MSSC2027.Player.Standard;

public partial class PlayerMain : CharacterBody3D
{
    #region Zmienne
    // Eksporty
    [ExportCategory("Referencje")]
    [Export] private CameraController _cameraController;
    [Export] private Panel _staminaBar;
    [Export] private AnimationTree _animTree;
    [Export] private SnowMobile _snowMobile; // Na razie tak, jak coś to później się będzie zmieniać

    [ExportCategory("UStawienia prękdkości i Skoku")]
    [Export] private float _jumpVelocity = 4.5f;
    [Export] private float _acceleration = 2.5f;
    [Export] private float _deceleration = 2.2f;

    [ExportGroup("Prędkości")]
    [Export] private float _sprintSpeed = 10.0f;
    [Export] private float _standardSpeed = 5.0f;
    [Export] private float _crouchSpeed = 2.5f;

    [ExportCategory("Ustawienia Staminy")]
    [Export] private float _staminaRegeneration = 0.05f;
    [Export] private float _staminaDrain = 0.1f;
    [Export] private float _maxStamina = 100.0f;
    [Export] private float _minStaminaToSprint = 5.0f;

    // AutoLoad
    private float _mouseSensitivity;
    private float _stickSensitivity;
    private float _baseFov;

    // Stamina i speed
    private float _speed;
    private float _staminaBeforVehicle;
    private float _stamina;
    public float Stamina
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
    private bool _isOnVehicle;

    // Upadek
    private float _currentFallVelocity;
    #endregion

    public override void _Ready()
    {
        // AutoLoad
        _mouseSensitivity = GameManager.Instance.MouseSensitivity;
        _stickSensitivity = GameManager.Instance.StickSensitivity;
        _baseFov = GameManager.Instance.BaseFov;

        // Inicjalizcja Kamery
        _cameraController.Initialize(_baseFov);

        // Podpinanie sygnałów od skutera
        if (_snowMobile != null)
        {
            _snowMobile.PlayerEntered += OnPlayerEnteredVehicle;
            _snowMobile.PlayerExited += OnPlayerExitedVehicle;
        }

        // Ustawianie domyślnych wartości
        _speed = _standardSpeed;
        Stamina = _maxStamina;
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    private void Sprint(Vector3 direction)
    {
        if (_isSprinting && direction != Vector3.Zero)
        {
            _speed = _sprintSpeed;
            Stamina = Mathf.Max(0.0f, Stamina - _staminaDrain);

            if (!(Stamina <= 0.0f)) return;
            _isSprinting = false;
            _isExhausted = true;
        }
        else
        {
            _isSprinting = false;
            if (!_isOnVehicle)
            {
                if (Stamina < _maxStamina)
                    Stamina = Mathf.Min(_maxStamina, Stamina + _staminaRegeneration);

                if (_isExhausted && Stamina >= _minStaminaToSprint)
                    _isExhausted = false;
            }
        }
    }

    private void UpdateStaminaBar()
    {
        _staminaBar.SetDeferred(Control.PropertyName.Size, new Vector2(1920 * Stamina / _maxStamina, _staminaBar.Size.Y));
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            _cameraController.Look(this, -mouseMotion.Relative.X * _mouseSensitivity, -mouseMotion.Relative.Y * _mouseSensitivity);
        }
    }

    private void OnPlayerEnteredVehicle()
    {
        if (_isSprinting) _isSprinting = false;
        _isOnVehicle = true;
        _staminaBar.Visible = false;
    }

    private void OnPlayerExitedVehicle(double timeSpentOnVehicle)
    {
        _isOnVehicle = false;
        float staminaGained = (float)timeSpentOnVehicle * _staminaRegeneration * 60f;
        Stamina = Mathf.Min(Stamina + staminaGained, _maxStamina);
        _staminaBar.Visible = true;


    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;
        float d = (float)delta;

        // Grawitacja
        if (!IsOnFloor())
        {
            velocity += GetGravity() * d * 1.5f;
        }

        // Sterowanie
        Vector2 inputDir = Input.GetVector("MoveLeft", "MoveRight", "MoveForward", "MoveBackward");
        Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

        // Skok
        if (Input.IsActionJustPressed("Jump") && IsOnFloor() && !_isCrouching)
        {
            velocity.Y = _jumpVelocity;
        }

        // Sprint
        if (Input.IsActionPressed("Shift") && IsOnFloor() && !_isCrouching && !_isExhausted)
        {
            _isSprinting = Stamina > 0.0f;
        }
        bool wantsToSprint = Input.IsActionPressed("Shift") && IsOnFloor() && !_isCrouching && !_isExhausted;

        if (wantsToSprint && direction != Vector3.Zero)
        {
            _isSprinting = Stamina > 0.0f;
        }
        else
        {
            _isSprinting = false;
            _speed = _isCrouching ? _crouchSpeed : _standardSpeed;
        }

        // Zarządzanie crouchem
        if (Input.IsActionPressed("Crouch") && IsOnFloor() && !_isSprinting)
        {
            _isCrouching = true;
            _speed = _crouchSpeed;
        }
        if (Input.IsActionJustReleased("Crouch") && !_isSprinting)
        {
            _isCrouching = false;
            _speed = _standardSpeed;
        }

        if (IsOnFloor())
        {
            if (direction != Vector3.Zero)
            {
                velocity.X = Mathf.Lerp(velocity.X, direction.X * _speed, d * _acceleration);
                velocity.Z = Mathf.Lerp(velocity.Z, direction.Z * _speed, d * _acceleration);
            }
            else
            {
                velocity.X = Mathf.Lerp(velocity.X, 0.0f, d * _deceleration);
                velocity.Z = Mathf.Lerp(velocity.Z, 0.0f, d * _deceleration);
            }
        }
        else
        {
            velocity.X = Mathf.Lerp(velocity.X, direction.X * _speed, d * 3.0f);
            velocity.Z = Mathf.Lerp(velocity.Z, direction.Z * _speed, d * 3.0f);
        }

        Sprint(direction);

        // Obracanie padem
        Vector2 look = Input.GetVector("LookLeft", "LookRight", "LookUp", "LookDown");
        if (look != Vector2.Zero)
            _cameraController.Look(this, -look.X * _stickSensitivity * d, -look.Y * _stickSensitivity * d);

        _cameraController.Crouch(_isCrouching, d);
        _cameraController.CalculateViewOffset(d, velocity, IsOnFloor(), _sprintSpeed);
        _cameraController.CheckLandingFall(IsOnFloor(), _currentFallVelocity);
        _currentFallVelocity = velocity.Y;

        // Animacje
        _animTree.Set("parameters/WalkRun/blend_position", velocity.Length() / _speed);

        Velocity = velocity;
        MoveAndSlide();
    }
}
