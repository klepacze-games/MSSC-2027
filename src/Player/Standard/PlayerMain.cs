#nullable enable
using Godot;
using MSSC2027.Vehicles.SnowMobile;
using GameManager = MSSC2027.AutoLoad.GameManager;

namespace MSSC2027.Player.Standard;

public partial class PlayerMain : CharacterBody3D
{
	#region Zmienne
	// Eksporty
	[ExportCategory("Referencje")]
	[Export] private Node3D? _head;
	[Export] private Camera3D? _camera;
	[Export] private Panel? _staminaBar;
	[Export] private Node3D? _cameraCrouchPosition;
	[Export] private SnowMobile? _snowMobile; // Na razie tak, jak coś to później się będzie zmieniać
	[Export] public PlayerStats? Stats;


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

	// Ustawienia Głowy
	private Vector3 _standardHeadPosition;
	private Vector3 _targetHeadPosition;

	// Upadek
	private float _fallValue;
	private float _fallTimer;
	private float _currentFallVelocity;

	// Inne
	private float _timeBob;
	private bool _isOnVehicle;
	#endregion

	public override void _Ready()
	{
		// AutoLoad
		if (GameManager.Instance != null)
		{
			_mouseSensitivity = GameManager.Instance.MouseSensitivity;
			_stickSensitivity = GameManager.Instance.StickSensitivity;
			_baseFov = GameManager.Instance.BaseFov;
		}

		// Podpinanie sygnałów od skutera
		if (_snowMobile != null)
		{
			_snowMobile.PlayerEntered += OnPlayerEnteredVehicle;
			_snowMobile.PlayerExited += OnPlayerExitedVehicle;
		}

		// Ustawianie domyślnych wartości
		_camera.Fov = _baseFov;
		_speed = Stats.StandardSpeed;
		Stamina = Stats.MaxStamina;
		Input.MouseMode = Input.MouseModeEnum.Captured;
		_standardHeadPosition = _head.Position;
	}

	private void Look(float yaw, float pitch)
	{
		RotateY(yaw);
		_head.RotateX(pitch);
		Vector3 rot = _head.Rotation;
		rot.X = Mathf.Clamp(rot.X, Mathf.DegToRad(-89f), Mathf.DegToRad(89f));
		_head.Rotation = rot;
	}

	private void Crouch()
	{
		if (_isCrouching)
		{
			_targetHeadPosition = _cameraCrouchPosition.Position;
			_speed = Stats.CrouchSpeed;
		}
		else
		{
			_targetHeadPosition = _standardHeadPosition;
			_speed = Stats.StandardSpeed;
		}
	}

	private void Sprint(Vector3 direction)
	{
		if (_isSprinting && direction != Vector3.Zero)
		{
			_speed = Stats.SprintSpeed;
			Stamina = Mathf.Max(0.0f, Stamina - Stats.StaminaDrain);

			if (!(Stamina <= 0.0f)) return;
			_isSprinting = false;
			_isExhausted = true;
		}
		else
		{
			if (!_isOnVehicle)
			{
				if (Stamina < Stats.MaxStamina)
					Stamina = Mathf.Min(Stats.MaxStamina, Stamina + Stats.StaminaRegeneration);

				if (_isExhausted && Stamina >= Stats.MinStaminaToSprint)
					_isExhausted = false;
			}
		}
	}

	private void UpdateStaminaBar()
	{
		_staminaBar.SetDeferred(Control.PropertyName.Size, new Vector2(1920 * Stamina / Stats.MaxStamina, _staminaBar.Size.Y));
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			Look(-mouseMotion.Relative.X * _mouseSensitivity, -mouseMotion.Relative.Y * _mouseSensitivity);
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
		float staminaGained = (float)timeSpentOnVehicle * Stats.StaminaRegeneration * 60f;
		Stamina = Mathf.Min(Stamina + staminaGained, Stats.MaxStamina);
		_staminaBar.Visible = true;

	}

	private Vector3 HeadBob(float time)
	{
		Vector3 pos = Vector3.Zero;
		pos.Y = Mathf.Sin(time * Stats.BobFrequency) * Stats.BobAmplitude;
		pos.X = Mathf.Cos(time * Stats.BobFrequency / 2) * Stats.BobAmplitude;
		return pos;
	}

	private void CalculateViewOffset(float delta, Vector3 velocity)
	{
		Vector3 angles = Vector3.Zero;
		Vector3 offset = Vector3.Zero;

		_fallTimer -= delta;

		// Nachylenie
		if (Stats.EnableTilt)
		{
			Vector3 forward = GlobalTransform.Basis.Z;
			Vector3 right = GlobalTransform.Basis.X;

			float forwardDot = velocity.Dot(forward);
			float forwardTilt = Mathf.Clamp(forwardDot * Mathf.DegToRad(Stats.RunPitch), Mathf.DegToRad(-Stats.MaxPitch), Mathf.DegToRad(Stats.MaxPitch));
			angles.X += forwardTilt;

			float rightDot = velocity.Dot(right);
			float sideTilt = Mathf.Clamp(rightDot * Mathf.DegToRad(Stats.RunRoll), Mathf.DegToRad(-Stats.MaxRoll), Mathf.DegToRad(Stats.MaxRoll));
			angles.Z -= sideTilt;
		}

		// Upadek
		if (Stats.EnableFallKick)
		{
			float fallRatio = Mathf.Max(0f, _fallTimer / Stats.FallTime);
			float fallKickAmount = fallRatio * _fallValue;
			angles.X -= fallKickAmount;
			offset.Y -= fallKickAmount;
		}

		// Kiwanie Głową
		if (Stats.EnableBob)
		{
			if (IsOnFloor() && velocity.Length() > 0.1f)
			{
				_timeBob += delta * velocity.Length();
			}
			offset += HeadBob(_timeBob);
		}

		_camera.Position = offset;
		_camera.Rotation = _camera.Rotation.Lerp(angles, Stats.CameraTiltSpeed * delta);
	}

	private void AddFallKick(float fallStrength)
	{
		_fallValue = Mathf.DegToRad(fallStrength);
		_fallTimer = Stats.FallTime;
	}

	private void OnAirborneStatePhysicsProcessing()
	{
		if (IsOnFloor())
		{
			if (_currentFallVelocity < Stats.FallVelocityThreshold)
				AddFallKick(Stats.FallStrength);
		}
		_currentFallVelocity = Velocity.Y;
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
			velocity.Y = Stats.JumpVelocity;
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
				velocity.X = Mathf.Lerp(velocity.X, direction.X * _speed, d * Stats.Acceleration);
				velocity.Z = Mathf.Lerp(velocity.Z, direction.Z * _speed, d * Stats.Acceleration);
			}
			else
			{
				velocity.X = Mathf.Lerp(velocity.X, 0.0f, d * Stats.Deceleration);
				velocity.Z = Mathf.Lerp(velocity.Z, 0.0f, d * Stats.Deceleration);
			}
		}
		else
		{
			velocity.X = Mathf.Lerp(velocity.X, direction.X * _speed, d * 3.0f);
			velocity.Z = Mathf.Lerp(velocity.Z, direction.Z * _speed, d * 3.0f);
		}

		// Funkcje do działania crouchu i sprintu
		Crouch();
		Sprint(direction);

		// Obracanie kamerą na gamepadzie
		Vector2 look = Input.GetVector("LookLeft", "LookRight", "LookUp", "LookDown");
		if (look != Vector2.Zero)
		{
			Look(-look.X * _stickSensitivity * d, -look.Y * _stickSensitivity * d);
		}

		// Płynne przejście między crouchem i standard position
		Vector3 currentPosition = _head.Position;
		currentPosition.Y = Mathf.Lerp(currentPosition.Y, _targetHeadPosition.Y, 7.0f * d);
		_head.Position = currentPosition;

		// Nachylenie Kamery i Upadek
		CalculateViewOffset(d, velocity);
		OnAirborneStatePhysicsProcessing();

		// FOV
		float velocityClamped = Mathf.Clamp(velocity.Length(), 0.5f, Stats.SprintSpeed * 2.0f);
		float targetFov = _baseFov + Stats.FovChange + velocityClamped;
		_camera.Fov = Mathf.Lerp(_camera.Fov, targetFov, d * 8.0f);

		Velocity = velocity;
		MoveAndSlide();
	}
}
