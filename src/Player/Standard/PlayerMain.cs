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
    [Export] private InventoryController _inventoryController;
    [Export] private Panel _staminaBar;
    [Export] private AnimationTree _animTree;
    [Export] private SnowMobile _snowMobile; // Na razie tak, jak coś to później się będzie zmieniać

    [ExportGroup("Animacje")]
    [Export] private string _locomotionStatePlaybackPath;
    [Export] private string _locomotionBlendPath;
    [Export] private string _crouchingBlendPath;
    [Export] private string _inventoryStateName;
    [Export] private string _fallingStateName;
    [Export] private string _walkingStateName;
    [Export] private string _crouchingStateName;

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
    private bool _isFalling;
    private bool _isOpenInventory;
    public bool CanMove;
    public bool IsOpenInventory
    {
        get => _isOpenInventory;
        set
        {
            _isOpenInventory = value;
            ToggleInventory();
        }
    }

    // Animacje
    private AnimationNodeStateMachinePlayback _playback;

    // Inne
    private float _currentFallVelocity;
    #endregion

    #region Lokalne Funkcje
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

    private void OnPlayerEnteredVehicle()
    {
        if (_isSprinting) _isSprinting = false;
        if (IsOpenInventory) IsOpenInventory = false;
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

    private void ToggleInventory()
    {
        _inventoryController.ToggleInventory(IsOpenInventory);
        if (IsOpenInventory)
        {
            _playback.Travel(_inventoryStateName);
            CanMove = false;
            _cameraController.ResetCamera();
        }
        else
        {
            _playback.Travel(_walkingStateName);
            CanMove = true;
        }
    }
    #endregion

    public override void _Ready()
    {
        _animTree.Active = true;
        _playback = (AnimationNodeStateMachinePlayback)_animTree.Get(_locomotionStatePlaybackPath);

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
        CanMove = true;
    }

    public override void _Process(double delta)
    {
        _animTree.Set(_locomotionBlendPath, Velocity.Length());
        _animTree.Set(_crouchingBlendPath, Velocity.Length());
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            if (!CanMove) return;
            _cameraController.Look(this, -mouseMotion.Relative.X * _mouseSensitivity, -mouseMotion.Relative.Y * _mouseSensitivity);
        }

        // Ekwipunek
        if (Input.IsActionJustPressed("Inventory") && IsOnFloor())
        {
            IsOpenInventory = !IsOpenInventory;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;
        float d = (float)delta;

        // Grawitacja
        if (!IsOnFloor())
        {
            velocity += GetGravity() * d * 1.1f;
            if (!_isFalling)
            {
                _isFalling = true;
                _playback.Travel(_fallingStateName);
            }
        }
        else if (_isFalling)
        {
            _isFalling = false;
            _playback.Travel(_walkingStateName);
        }

        if (!CanMove) return;

        // Sterowanie
        Vector2 inputDir = Input.GetVector("MoveLeft", "MoveRight", "MoveForward", "MoveBackward");
        Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

        // Skok
        if (Input.IsActionJustPressed("Jump") && IsOnFloor() && !_isCrouching)
        {
            _playback.Travel(_fallingStateName);
            velocity.Y = _jumpVelocity;
        }

        // Sprint
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

        // Kucanie
        if (Input.IsActionPressed("Crouch") && IsOnFloor() && !_isSprinting)
        {
            _isCrouching = true;
            _speed = _crouchSpeed;
            _playback.Travel(_crouchingStateName);
            _cameraController.Crouch(_isCrouching);
        }

        if (Input.IsActionJustReleased("Crouch") && !_isSprinting)
        {
            _isCrouching = false;
            _speed = _standardSpeed;
            _playback.Travel(_walkingStateName);
            _cameraController.Crouch(_isCrouching);
        }

        // Poruszanie sie gracza
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

        // Efekty kamery
        _cameraController.CalculateViewOffset(d, velocity, IsOnFloor(), _sprintSpeed);
        _cameraController.CheckLandingFall(IsOnFloor(), _currentFallVelocity);
        _currentFallVelocity = velocity.Y;

        Velocity = velocity;
        MoveAndSlide();
    }
}
