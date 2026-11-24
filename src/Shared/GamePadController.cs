using Godot;
using System;

public partial class GamePadController : Node3D
{
	#region Zmienne
	[Export] private int _deviceId = 0;
	[Export] private float _idleWeak = 0.01f;
	[Export] private float _idleStrong = 0.04f;
	[Export] private float _maxWeak = 0.2f;
	[Export] private float _maxStrong = 0.1f;

	private float _currentWeak = 0f;
	private float _currentStrong = 0f;
	#endregion

	#region Lokalne Funkcje
	#endregion

	/// <summary>
	/// Wibracje przy naciskaniu trigerów do skutera śnieżnego
	/// <summary>
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

	/// <summary>
	/// Jedno razowa wibracja ograniczona czasem
	/// <summary>
	public void Pulse(float weakMagnitude = 0.5f, float strongMagnitude = 0.8f, float durationSec = 0.2f)
	{
		if (Input.GetConnectedJoypads().Count == 0)
			return;

		float weak = Mathf.Clamp(weakMagnitude, 0f, 1f);
		float strong = Mathf.Clamp(strongMagnitude, 0f, 1f);
		float duration = Mathf.Max(0.01f, durationSec);

		Input.StartJoyVibration(DeviceId, weak, strong, duration);
	}

	/// <summary>
	/// Zatrzymywanie wibracji
	/// <summary>
	public void Stop()
	{
		Input.StopJoyVibration(DeviceId);
	}

	/// <summary>
	/// Zatrzymywanie wibracji przy exit Tree
	/// <summary>
	public override void _ExitTree()
	{
		Input.StopJoyVibration(DeviceId);
	}
}
