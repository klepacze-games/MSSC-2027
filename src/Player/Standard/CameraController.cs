using Godot;

namespace MSSC2027.Player.Standard;

public partial class CameraController : Node3D
{
    #region Zmienne
    // Eksporty
    [ExportCategory("Referencje")]
    [Export] private Node3D _head;
    [Export] private Camera3D _camera;
    [Export] private Node3D _cameraCrouchPosition;
    [Export] private Node3D _armature;

    [ExportCategory("Ustawienia Kamery")]
    [Export] private float _fovChange = 1.5f;
    [Export] private bool _enableTilt = true;
    [Export] private bool _enableBob = true;
    [Export] private bool _enableBobX = true;
    [Export] private bool _enableFallKick = true;

    [ExportGroup("Nachylenie")]
    [Export] private float _cameraTiltSpeed = 10.0f;
    [Export] private float _runPitch = 0.1f;
    [Export] private float _runRoll = 0.25f;
    [Export] private float _maxPitch = 1.0f;
    [Export] private float _maxRoll = 2.5f;

    [ExportGroup("Kiwanie")]
    [Export] private float _bobFrequency = 2.0f;
    [Export] private float _bobAmplitude = 0.08f;

    [ExportGroup("Upadek")]
    [Export] private float _fallTime = 0.3f;
    [Export] private float _fallStrength = 3.0f;
    [Export] private float _fallVelocityThreshold = -4f;

    // Standardowe Wartości
    private float _baseFov;
    private Vector3 _standardHeadPosition;
    private Vector3 _targetHeadPosition;
    private Vector3 _standardArmaturePosition;

    // Upadek
    private float _timeBob;
    private float _fallValue = 0f;
    private float _fallTimer = 0f;
    #endregion

    #region Lokalne Funkcje
    private Vector3 HeadBob(float time)
    {
        Vector3 pos = Vector3.Zero;
        pos.Y = Mathf.Sin(time * _bobFrequency) * _bobAmplitude;
        if (_enableBobX) pos.X = Mathf.Cos(time * _bobFrequency / 2) * _bobAmplitude;
        return pos;
    }

    private void AddFallKick(float fallStrength)
    {
        _fallValue = Mathf.DegToRad(fallStrength);
        _fallTimer = _fallTime;
    }
    #endregion

    public override void _Ready()
    {
        if (_head != null) _standardHeadPosition = _head.Position;
        if (_armature != null) _standardArmaturePosition = _armature.Position;
        _targetHeadPosition = _standardHeadPosition;
    }

    public void Initialize(float baseFov)
    {
        _baseFov = baseFov;
        if (_camera != null) _camera.Fov = _baseFov;
    }

    public void Look(Node3D playerBody, float yaw, float pitch)
    {
        playerBody.RotateY(yaw);
        if (_head == null) return;
        RotateX(pitch);
        Vector3 rot = Rotation;
        rot.X = Mathf.Clamp(rot.X, Mathf.DegToRad(-89f), Mathf.DegToRad(89f));
        Rotation = rot;
    }

    public void Crouch(bool isCrouching, float delta)
    {
        if (isCrouching)
        {
            _targetHeadPosition = _cameraCrouchPosition.Position;
        }
        else
        {
            _targetHeadPosition = _standardHeadPosition;
        }
        Vector3 currentPos = Position;
        currentPos.Y = Mathf.Lerp(currentPos.Y, _targetHeadPosition.Y, 7.0f * delta);
        Position = currentPos;
    }

    public void CalculateViewOffset(float delta, Vector3 velocity, bool isOnFloor, float sprintSpeed)
    {
        Vector3 angles = Vector3.Zero;
        Vector3 offset = Vector3.Zero;
        Vector3 armOffset = Vector3.Zero;

        _fallTimer -= delta;

        // Nachylenie
        if (_enableTilt)
        {
            Vector3 forward = GlobalTransform.Basis.Z;
            Vector3 right = GlobalTransform.Basis.X;

            float forwardDot = velocity.Dot(forward);
            float forwardTilt = Mathf.Clamp(forwardDot * Mathf.DegToRad(_runPitch), Mathf.DegToRad(-_maxPitch), Mathf.DegToRad(_maxPitch));
            angles.X += forwardTilt;

            float rightDot = velocity.Dot(right);
            float sideTilt = Mathf.Clamp(rightDot * Mathf.DegToRad(_runRoll), Mathf.DegToRad(-_maxRoll), Mathf.DegToRad(_maxRoll));
            angles.Z -= sideTilt;
        }

        // Upadek
        if (_enableFallKick)
        {
            float fallRatio = Mathf.Max(0f, _fallTimer / _fallTime);
            float fallKickAmount = fallRatio * _fallValue;
            angles.X -= fallKickAmount;
            offset.Y -= fallKickAmount;
        }

        // Kiwanie Głową
        if (_enableBob)
        {
            if (isOnFloor && velocity.Length() > 0.1f)
            {
                _timeBob += delta * velocity.Length();
            }
            offset += HeadBob(_timeBob);
            armOffset += HeadBob(_timeBob);
        }
        Position = offset + _standardHeadPosition;
        if (_armature != null) _armature.Position = armOffset + _standardArmaturePosition;

        if (_camera != null)
        {
            _camera.Rotation = _camera.Rotation.Lerp(angles, _cameraTiltSpeed * delta);

            float velocityClamped = Mathf.Clamp(velocity.Length(), 0.5f, sprintSpeed * 2.0f);
            float targetFov = _baseFov + _fovChange + velocityClamped;
            _camera.Fov = Mathf.Lerp(_camera.Fov, targetFov, delta * 8.0f);
        }
    }

    public void CheckLandingFall(bool isOnFloor, float currentFallVelocity)
    {
        if (isOnFloor && currentFallVelocity < _fallVelocityThreshold)
        {
            AddFallKick(_fallStrength);
        }
    }
}
