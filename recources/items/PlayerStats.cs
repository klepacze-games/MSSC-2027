using Godot;

[GlobalClass]
public partial class PlayerStats : Resource
{

	[Export] private Vector2 _playerPos;


	[Export] public float JumpVelocity = 4.5f;
	[Export] public float Acceleration = 2.5f;
	[Export] public float Deceleration = 2.2f;

	[Export] public float SprintSpeed = 10.0f;
	[Export] public float StandardSpeed = 5.0f;
	[Export] public float CrouchSpeed = 2.5f;

	[Export] public float StaminaRegeneration = 0.05f;
	[Export] public float StaminaDrain = 0.1f;
	[Export] public float MaxStamina = 100.0f;
	[Export] public float MinStaminaToSprint = 5.0f;

	[Export] public float FovChange = 1.5f;
	[Export] public bool EnableTilt = true;
	[Export] public bool EnableBob = true;
	[Export] public bool EnableFallKick = true;

	[Export] public float CameraTiltSpeed = 10.0f;
	[Export] public float RunPitch = 0.1f;
	[Export] public float RunRoll = 0.25f;
	[Export] public float MaxPitch = 1.0f;
	[Export] public float MaxRoll = 2.5f;

	[Export] public float BobFrequency = 2.0f;
	[Export] public float BobAmplitude = 0.08f;

	[Export] public float FallTime = 0.3f;
	[Export] public float FallStrength = 3.0f;
	[Export] public float FallVelocityThreshold = -4f;

}
