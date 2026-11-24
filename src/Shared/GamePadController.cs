using Godot;
using System;

public partial class GamePadController : Node3D
{
	#region Zmienne
	[Export] public int DeviceId = 0;
	[Export] public float IdleWeak = 0.01f;
	[Export] public float IdleStrong = 0.04f;
	[Export] public float MaxWeak = 0.2f;
	[Export] public float MaxStrong = 0.1f;

	private float _currentWeak = 0f;
	private float _currentStrong = 0f;
	#endregion

	#region Lokalne Funkcje
	#endregion

	public void UpdateEngineVibration(float throttle, float normalizedSpeed, float delta)
	{
		if (Input.GetConnectedJoypads().Count == 0)
			return;

		float targetWeak = Mathf.Lerp(IdleWeak, MaxWeak, throttle * 0.7f + normalizedSpeed * 0.3f);
		float targetStrong = Mathf.Lerp(IdleStrong, MaxStrong, throttle);

		if (throttle <= 0.01f && normalizedSpeed <= 0.02f)
		{
			targetWeak = IdleWeak;
			targetStrong = IdleStrong;
		}

		float smooth = 10f * delta;
		_currentWeak = Mathf.Lerp(_currentWeak, targetWeak, smooth);
		_currentStrong = Mathf.Lerp(_currentStrong, targetStrong, smooth);

		Input.StartJoyVibration(DeviceId, _currentWeak, _currentStrong, 0.1f);
	}

	public void Pulse(float weakMagnitude = 0.5f, float strongMagnitude = 0.8f, float durationSec = 0.2f)
	{
		if (Input.GetConnectedJoypads().Count == 0)
			return;

		float weak = Mathf.Clamp(weakMagnitude, 0f, 1f);
		float strong = Mathf.Clamp(strongMagnitude, 0f, 1f);
		float duration = Mathf.Max(0.01f, durationSec);

		Input.StartJoyVibration(DeviceId, weak, strong, duration);
	}

	public void Stop()
	{
		Input.StopJoyVibration(DeviceId);
	}

	public void TriggerImpact(float intensity = 1.0f, float duration = 0.25f)
	{
		if (Input.GetConnectedJoypads().Count == 0)
			return;

		float clamped = Mathf.Clamp(intensity, 0f, 1f);
		Input.StartJoyVibration(DeviceId, clamped * 0.5f, clamped, duration);
	}

	public override void _ExitTree()
	{
		Input.StopJoyVibration(DeviceId);
	}
}
