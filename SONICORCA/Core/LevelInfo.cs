namespace SonicOrca.Core;

/// <summary>
/// The data of one level (zone): the game it is from, its short name, its full name and its number of acts.
/// </summary>
internal class LevelInfo
{
	public int GameIndex { get; }

	public string Mnemonic { get; }

	public string Name { get; }

	public int Acts { get; }

	public bool Unreleased { get; }

	public LevelInfo(int gameIndex, string mnemonic, string name, int acts, bool unreleased = false)
	{
		GameIndex = gameIndex;
		Mnemonic = mnemonic;
		Name = name;
		Acts = acts;
		Unreleased = unreleased;
	}
}
