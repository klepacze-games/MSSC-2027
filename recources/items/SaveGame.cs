#nullable enable
using System.IO;
using Godot;
using FileAccess = Godot.FileAccess;

namespace MSSC2027.recorces.items;

public partial class SaveGame : Resource
{
	private static readonly string _saveGamePath = "user://save.res";

	[Export] public PlayerStats? PlayerStats;
	[Export] public Vector3 GlobalPosition;

	public void write_save_game()
	{
		ResourceSaver.Save(this, _saveGamePath);
	}

	public static bool SaveExists()
	{
		return ResourceLoader.Exists(_saveGamePath);
	}

	public static Resource? Load_save_game()
	{
		if (ResourceLoader.HasCached(_saveGamePath))
			return ResourceLoader.Load(_saveGamePath);

		//przenosi dane zapisu na plik chyba
		var file = FileAccess.Open(_saveGamePath, FileAccess.ModeFlags.Read);
		if (file == null)
		{
			GD.PrintErr("Save game file is empty");
			return null;
		}

		string data = file.GetAsText();
		file.Close();

		// robi ranomowy path ,do którego da SAVE_GAME_PATH jeśli będzie cach'owany? to zrobi to od nowa
		string tmpFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
		while (ResourceLoader.HasCached(tmpFilePath))
			tmpFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

		file = FileAccess.Open(tmpFilePath, FileAccess.ModeFlags.Write);
		if (file == null)
		{
			GD.PrintErr("Save game file is empty");
			return null;
		}

		file.StoreString(data);
		file.Close();

		Resource save = ResourceLoader.Load(tmpFilePath);
		//save.TakeOverPath(_saveGamePath);

		return save;
	}
}
