using Godot;

namespace MSSC2027.Shared;

public partial class GamePadController : Node3D
{
    #region Zmienne

    [Export] private int _deviceId;
    [Export] private float _idleWeak = 0.01f;
    [Export] private float _idleStrong = 0.04f;
    [Export] private float _maxWeak = 0.2f;
    [Export] private float _maxStrong = 0.1f;

    private float _currentWeak;
    private float _currentStrong;

    #endregion

    #region Lokalne Funkcje

    /// <summary>
    /// Wibracje przy naciskaniu trigerów do skutera śnieżnego
    /// </summary>
    public void UpdateEngineVibration(float throttle, float normalizedSpeed, float delta)
    {
        if (Input.GetConnectedJoypads().Count == 0)
            return;

        float targetWeak = Mathf.Lerp(_idleWeak, _maxWeak, throttle * 0.7f + normalizedSpeed * 0.3f);
        float targetStrong = Mathf.Lerp(_idleStrong, _maxStrong, throttle);

        if (throttle <= 0.01f && normalizedSpeed <= 0.02f)
        {
            targetWeak = _idleWeak;
            targetStrong = _idleStrong;
        }

        float smooth = 10f * delta;
        _currentWeak = Mathf.Lerp(_currentWeak, targetWeak, smooth);
        _currentStrong = Mathf.Lerp(_currentStrong, targetStrong, smooth);

        Input.StartJoyVibration(_deviceId, _currentWeak, _currentStrong, 0.1f);
    }

    /// <summary>
    /// Jedno razowa wibracja ograniczona czasem
    /// </summary>
    public void Pulse(float weakMagnitude = 0.5f, float strongMagnitude = 0.8f, float durationSec = 0.2f)
    {
        if (Input.GetConnectedJoypads().Count == 0)
            return;

        float weak = Mathf.Clamp(weakMagnitude, 0f, 1f);
        float strong = Mathf.Clamp(strongMagnitude, 0f, 1f);
        float duration = Mathf.Max(0.01f, durationSec);

        Input.StartJoyVibration(_deviceId, weak, strong, duration);
    }

    /// <summary>
    /// Zatrzymywanie wibracji
    /// </summary>
    public void Stop()
    {
        Input.StopJoyVibration(_deviceId);
    }

    /// <summary>
    /// Zatrzymywanie wibracji przy exit Tree
    /// </summary>
    public override void _ExitTree()
    {
        Input.StopJoyVibration(_deviceId);
    }

    #endregion
}
