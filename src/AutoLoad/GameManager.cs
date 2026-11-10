#nullable enable
using Godot;
using MSSC2027.Player.Standard;

namespace MSSC2027.AutoLoad;

public partial class GameManager : Node
{
	[Export] private PlayerMain? _playerMain;
	public float MouseSensitivity { get; set; } = 0.003f;
	public float StickSensitivity { get; set; } = 2.5f;
	public float BaseFov { get; set; } = 75.0f;
	public static GameManager? Instance { get; private set; }
	private recorces.items.SaveGame? _save;

	public override void _Ready()
	{
		Instance = this;
		CreateOrLodeSave();
	}

	public void CreateOrLodeSave()
	{
		_save = new recorces.items.SaveGame();

		if (recorces.items.SaveGame.SaveExists())
			_save = recorces.items.SaveGame.Load_save_game() as recorces.items.SaveGame;
		else
		{
			_save.PlayerStats = new PlayerStats();
			_save.GlobalPosition = new Vector3();

			_save.write_save_game();
		}

		if (_save == null)
		{
			GD.PrintErr("Save Game Not Found"); return;
		}
		else
		{
			GD.Print("Save Game Loaded");
		}

		if ( _playerMain == null)
			return;
		_playerMain.Stats = _save.PlayerStats as PlayerStats;
		_playerMain.GlobalPosition = _save.GlobalPosition;

	}

	public void save_game()
	{
		if (_save == null ||  _playerMain == null)
			return;
		_save.GlobalPosition =  _playerMain.GlobalPosition;
		_save.write_save_game();
	}
}
