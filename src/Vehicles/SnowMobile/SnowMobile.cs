using Godot;
using Godot.Collections;
using GameManager = MSSC2027.AutoLoad.GameManager;

namespace MSSC2027.Vehicles.SnowMobile;

public partial class SnowMobile : RigidBody3D
{
    #region Zmienne
    // Eksporty
    [ExportCategory("Referencje")]
    [Export] private Node3D _head;
    [Export] private Camera3D _camera;
    [Export] private Marker3D _exitPoint;
    [Export] private Node3D _centerOfMassNode;
    [Export] private Node3D _steeringWheelNode;
    [Export] private Node3D _steeringSkidsNode;
    [Export] private Array<RayCast3D> _rayCasts;

    [ExportCategory("Ustawienia Kamery")]
    [Export] private float _fovChange = 1.0f;
    [Export] private bool _enableTilt = true;

    [ExportGroup("Nachylenie")]
    [Export] private float _cameraTiltSpeed = 10.0f;
    [Export] private float _driveRoll = 0.25f;
    [Export] private float _maxRoll = 2.5f;

    [ExportCategory("Parametry Pojazdu")]
    [Export] private float _enginePower = 6000f;
    [Export] private float _maxSpeed = 25f;
    [Export] private float _turnTorque = 2500f;
    [Export] private float _lateralGrip = 4f;

    [ExportCategory("Kierownica")]
    [Export] private float _maxSteerAngle = 20.0f;
    [Export] private float _steerReturnSpeed = 10.0f;

    [ExportCategory("Zawieszenie")]
    [Export] private float _restLength = 0.6f;
    [Export] private float _springStrength = 40f;
    [Export] private float _springDamping = 8f;

    [ExportCategory("System Przewrócenia")]
    [Export] private float _flipCheck = 2.0f;
    [Export] private float _flipDelay = 1.5f;

    // Syngały
    [Signal] public delegate void PlayerEnteredEventHandler();
    [Signal] public delegate void PlayerExitedEventHandler(double timeSpentOnVehicle);

    // AutoLoad
    private float _mouseSensitivity;
    private float _stickSensitivity;
    private float _baseFov;

    // Kamera
    private float _cameraPitch;
    private float _cameraYaw;

    // Wchodzenie/wychodzenie
    private bool _isOccupied;
    private CharacterBody3D _driver;
    private bool _isPlayerNearby;
    private CharacterBody3D _nearbyPlayer;

    // Inne
    private float _flipCheckTimer;
    private float _flipDelayTimer;
    private float _currentSteerAngle;
    private double _timeOnVehicle;
    #endregion

    public override void _Ready()
    {
        // AutoLoad
        _mouseSensitivity = GameManager.Instance.MouseSensitivity;
        _stickSensitivity = GameManager.Instance.StickSensitivity;
        _baseFov = GameManager.Instance.BaseFov;

        CenterOfMassMode = CenterOfMassModeEnum.Custom;
        CenterOfMass = _centerOfMassNode.Position;

        SetPhysicsProcess(false);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("Interact"))
        {
            if (!_isOccupied && _isPlayerNearby && _nearbyPlayer != null)
            {
                EnterVehicle(_nearbyPlayer);
            }
            else if (_isOccupied)
            {
                ExitVehicle();
            }
        }
    }

    private void Look(float yaw, float pitch)
    {
        _head.RotateY(yaw);
        _cameraPitch += pitch;
        _cameraPitch = Mathf.Clamp(_cameraPitch, Mathf.DegToRad(-89f), Mathf.DegToRad(89f));

        Vector3 rot = _head.Rotation;
        rot.X = _cameraPitch;
        _head.Rotation = rot;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            Look(-mouseMotion.Relative.X * _mouseSensitivity, -mouseMotion.Relative.Y * _mouseSensitivity);
        }
    }

    private void EnterVehicle(CharacterBody3D player)
    {
        _isOccupied = true;
        _timeOnVehicle = 0.0;
        _driver = player;

        _driver.Visible = false;
        _driver.ProcessMode = ProcessModeEnum.Disabled;

        _camera.MakeCurrent();
        EmitSignal(SignalName.PlayerEntered);

        SetPhysicsProcess(true);
    }

    private void ExitVehicle()
    {
        if (_driver == null) return;

        SetPhysicsProcess(false);

        _driver.GlobalPosition = _exitPoint.GlobalPosition;
        _driver.GlobalRotation = new Vector3(_driver.GlobalRotation.X, GlobalRotation.Y, _driver.GlobalRotation.Z);

        _driver.Visible = true;
        _driver.ProcessMode = ProcessModeEnum.Inherit;

        Camera3D playerCamera = _driver.GetNode<Node3D>("Head").GetNode<Camera3D>("Camera");
        playerCamera.MakeCurrent();

        _isOccupied = false;
        EmitSignal(SignalName.PlayerExited, _timeOnVehicle);
        _driver = null;
    }

    private void OnInteractionAreaBodyEntered(Node3D body)
    {
        if (body is not CharacterBody3D character) return;

        _isPlayerNearby = true;
        _nearbyPlayer = character;
    }

    private void OnInteractionAreaBodyExited(Node3D body)
    {
        if (body != _nearbyPlayer) return;

        _isPlayerNearby = false;
        _nearbyPlayer = null;
    }

    private void CalculateViewOffset(float delta, Vector3 velocity)
    {
        Vector3 angles = Vector3.Zero;

        if (_enableTilt)
        {
            Vector3 right = GlobalTransform.Basis.X;

            float rightDot = velocity.Dot(right);
            float sideTilt = Mathf.Clamp(rightDot * Mathf.DegToRad(_driveRoll) * 5.0f, Mathf.DegToRad(-_maxRoll), Mathf.DegToRad(_maxRoll));
            angles.Z += sideTilt;
        }

        _camera.Rotation = _camera.Rotation.Lerp(angles, _cameraTiltSpeed * delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        float d = (float)delta;
        CheckForFlip(d);

        if (_isOccupied) _timeOnVehicle += d;

        // Obracanie kamerą na gamepadzie
        Vector2 look = Input.GetVector("LookLeft", "LookRight", "LookUp", "LookDown");
        if (look != Vector2.Zero)
        {
            Look(-look.X * _stickSensitivity * d, -look.Y * _stickSensitivity * d);
        }

        // Sterowanie
        Basis basis = GlobalTransform.Basis;

        int frontGrounded = 0;
        int rearGrounded = 0;
        Vector3 normalSum = Vector3.Zero;

        if (_rayCasts.Count == 0) GD.PrintErr("BŁĄD: Raycasty nie są przypisane w skrypcie skutera, jeśli są to dodaj ponownie scenę skutera");

        for (int i = 0; i < _rayCasts.Count; i++)
        {
            RayCast3D ray = _rayCasts[i];
            if (!ray.IsColliding()) continue;

            Vector3 hit = ray.GetCollisionPoint();
            Vector3 normal = ray.GetCollisionNormal();
            float distance = ray.GlobalPosition.DistanceTo(hit);
            float compression = _restLength - distance;

            Vector3 offset = ray.GlobalPosition - GlobalPosition;
            Vector3 pointVelocity = LinearVelocity + AngularVelocity.Cross(offset);
            float springVelocity = normal.Dot(pointVelocity);

            float force = (compression * _springStrength - springVelocity * _springDamping) * Mass / _rayCasts.Count;
            force = Mathf.Max(force, 0f);

            ApplyForce(normal * force, offset);

            normalSum += normal;
            if (i < 2)
            {
                frontGrounded++;
            }
            else
            {
                rearGrounded++;
            }
        }

        int grounded = frontGrounded + rearGrounded;
        if (grounded == 0 && !_isOccupied) return;

        Vector3 groundNormal = normalSum.Normalized();
        Vector3 forward = (-basis.Z).Slide(groundNormal).Normalized();
        Vector3 right = basis.X.Slide(groundNormal).Normalized();

        float throttle = Input.GetAxis("Brake", "Throttle");
        float steer = Input.GetAxis("MoveRight", "MoveLeft");
        float forwardSpeed = LinearVelocity.Dot(forward);

        float traction = rearGrounded / 2f;
        if (Mathf.Abs(forwardSpeed) < _maxSpeed || Mathf.Sign(forwardSpeed) != Mathf.Sign(throttle))
        {
            if (throttle < 0) throttle = -0.25f;
            ApplyCentralForce(forward * throttle * _enginePower * traction);
        }

        float lateralSpeed = LinearVelocity.Dot(right);
        ApplyCentralForce(-right * lateralSpeed * _lateralGrip * Mass * (grounded / 4f));

        float skiContact = frontGrounded / 2f;
        float speedFactor = Mathf.Clamp(forwardSpeed / 10f, -1f, 1f);
        ApplyTorque(groundNormal * steer * _turnTorque * speedFactor * skiContact);

        // Nachylenie Kamery
        CalculateViewOffset(d, LinearVelocity);

        // Kierownica i narty
        float targetAngle = steer * Mathf.DegToRad(_maxSteerAngle);
        _currentSteerAngle = Mathf.Lerp(_currentSteerAngle, targetAngle, d * _steerReturnSpeed);

        Vector3 wheelRot = _steeringWheelNode.Rotation;
        wheelRot.Y = _currentSteerAngle;
        _steeringWheelNode.Rotation = wheelRot;
        _steeringSkidsNode.Rotation = wheelRot;

        // FOV
        float velocityClamped = Mathf.Clamp(LinearVelocity.Length(), 0.5f, _maxSpeed * 2.0f);
        float targetFov = _baseFov + _fovChange + velocityClamped;
        _camera.Fov = Mathf.Lerp(_camera.Fov, targetFov, d * 8.0f);
    }

    private void CheckForFlip(float delta)
    {
        if (!_isOccupied) return;

        float upDot = GlobalTransform.Basis.Y.Dot(Vector3.Up);

        bool isUpsideDown = upDot < 0.1f;
        bool isMovingSlowly = LinearVelocity.Length() < 1.5f;

        if (isUpsideDown && isMovingSlowly)
        {
            _flipCheckTimer += delta;

            if (_flipCheckTimer >= _flipCheck) RightVehicle();
        }
        else
        {
            _flipCheckTimer = 0.0f;
        }

        if (_flipDelayTimer > 0f)
        {
            _flipDelayTimer -= delta;
        }

        if (Input.IsActionJustPressed("ResetVehicle"))
        {
            if (_flipDelayTimer <= 0)
            {
                RightVehicle();
                _flipDelayTimer = _flipDelay;
            }
        }
    }

    private void RightVehicle()
    {
        Vector3 currentPos = GlobalPosition + new Vector3(0, 1.2f, 0);

        Vector3 currentRot = GlobalRotation;
        GlobalRotation = new Vector3(0, currentRot.Y, 0);

        GlobalPosition = currentPos;

        LinearVelocity = Vector3.Zero;
        AngularVelocity = Vector3.Zero;

        _flipCheckTimer = 0.0f;
    }
}
