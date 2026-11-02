using SonicOrca.Core.Debugging;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace S2HD.Versus;

/// <summary>
/// A results screen of a versus match, as TwoPlayerResults of the original. The act results compare the five
/// categories of the act. The zone results show the winner of each act, and the game results show the winner of each
/// zone. Either player presses Start to go on.
/// </summary>
internal sealed class VersusResultsScreen : VersusScreen
{
	private const double TableTop = 260.0;
	private const double RowHeight = 90.0;
	private const double NameX = 560.0;
	private const double Player1X = 1060.0;
	private const double Player2X = 1400.0;

	private readonly string _title;

	private readonly string[] _rowNames;

	private readonly string[][] _rowValues;

	private readonly int[] _rowWinners;

	private readonly int _winner;

	private VersusResultsScreen(SonicOrcaGameContext gameContext, string title, string[] rowNames, string[][] rowValues, int[] rowWinners, int winner)
		: base(gameContext, playMusic: true)
	{
		_title = title;
		_rowNames = rowNames;
		_rowValues = rowValues;
		_rowWinners = rowWinners;
		_winner = winner;
	}

	/// <summary>The results of an act: the value of each category for each player, and the points.</summary>
	public static VersusResultsScreen ForAct(SonicOrcaGameContext gameContext, string zoneName, int act, VersusActResult result)
	{
		int count = VersusActResult.CategoryCount;
		string[] names = new string[count + 1];
		string[][] values = new string[count + 1][];
		int[] winners = new int[count + 1];
		for (int i = 0; i < count; i++)
		{
			names[i] = VersusActResult.CategoryNames[i];
			values[i] = new[] { VersusActResult.FormatCategory(result.Player1, i), VersusActResult.FormatCategory(result.Player2, i) };
			winners[i] = result.CategoryWinners[i];
		}
		names[count] = "POINTS";
		values[count] = new[] { result.Player1Points.ToString(), result.Player2Points.ToString() };
		winners[count] = result.Winner;
		return new VersusResultsScreen(gameContext, $"{zoneName}  ACT {act}", names, values, winners, result.Winner);
	}

	/// <summary>The results of the special stage that broke a tie: the rings of each player in each segment.</summary>
	public static VersusResultsScreen ForSpecialStage(SonicOrcaGameContext gameContext, string zoneName, VersusSpecialStageResult result)
	{
		int count = result.Segments.Count;
		string[] names = new string[count + 1];
		string[][] values = new string[count + 1][];
		int[] winners = new int[count + 1];
		for (int i = 0; i < count; i++)
		{
			names[i] = $"SEGMENT {i + 1}";
			values[i] = new[] { result.Segments[i].Player1.ToString(), result.Segments[i].Player2.ToString() };
			winners[i] = result.SegmentWinners[i];
		}
		names[count] = "POINTS";
		values[count] = new[] { result.Player1Points.ToString(), result.Player2Points.ToString() };
		winners[count] = result.Winner;
		return new VersusResultsScreen(gameContext, $"{zoneName}  SPECIAL STAGE", names, values, winners, result.Winner);
	}

	/// <summary>The results of a zone: the winner of each act, and of the special stage that broke a tie.</summary>
	public static VersusResultsScreen ForZone(SonicOrcaGameContext gameContext, VersusZoneResult zone, int firstAct)
	{
		int acts = zone.Acts.Count;
		int count = acts + (zone.SpecialStage != null ? 1 : 0);
		string[] names = new string[count + 1];
		string[][] values = new string[count + 1][];
		int[] winners = new int[count + 1];
		for (int i = 0; i < acts; i++)
		{
			VersusActResult act = zone.Acts[i];
			names[i] = $"ACT {firstAct + i}";
			values[i] = new[] { act.Player1Points.ToString(), act.Player2Points.ToString() };
			winners[i] = act.Winner;
		}
		if (zone.SpecialStage is VersusSpecialStageResult special)
		{
			names[acts] = "SPECIAL STAGE";
			values[acts] = new[] { special.Player1Points.ToString(), special.Player2Points.ToString() };
			winners[acts] = special.Winner;
		}
		names[count] = "ROUNDS WON";
		values[count] = new[] { zone.Player1Wins.ToString(), zone.Player2Wins.ToString() };
		winners[count] = zone.Winner;
		return new VersusResultsScreen(gameContext, $"{zone.Name}  RESULTS", names, values, winners, zone.Winner);
	}

	/// <summary>The results of the game: the winner of each zone.</summary>
	public static VersusResultsScreen ForGame(SonicOrcaGameContext gameContext, VersusMatch match)
	{
		int count = match.Zones.Length;
		string[] names = new string[count + 1];
		string[][] values = new string[count + 1][];
		int[] winners = new int[count + 1];
		for (int i = 0; i < count; i++)
		{
			VersusZoneResult zone = match.Zones[i];
			names[i] = match.ZoneNames[i];
			values[i] = new[] { zone.Player1Wins.ToString(), zone.Player2Wins.ToString() };
			winners[i] = zone.Winner;
		}
		names[count] = "ZONES WON";
		values[count] = new[] { match.Player1Wins.ToString(), match.Player2Wins.ToString() };
		winners[count] = match.Winner;
		return new VersusResultsScreen(gameContext, "GAME RESULTS", names, values, winners, match.Winner);
	}

	public override void Update()
	{
		base.Update();
		int? testFrames = TestHooks.VersusResultsFrames;
		// A short wait, so that the Start press that ended the act does not also end the results
		if ((testFrames == null && Frames > 30 && ConfirmPressed()) || Frames == testFrames)
		{
			GameContext.Audio.PlaySound(YesSound);
			Finish();
		}
	}

	public override void Draw(Renderer renderer)
	{
		DrawBackground(renderer, _title);
		I2dRenderer g = renderer.Get2dRenderer();
		IFontRenderer fontRenderer = renderer.GetFontRenderer();
		DrawText(fontRenderer, PlayerNames[0], Player1X, TableTop - RowHeight, PlayerColours[0]);
		DrawText(fontRenderer, PlayerNames[1], Player2X, TableTop - RowHeight, PlayerColours[1]);
		for (int row = 0; row < _rowNames.Length; row++)
		{
			bool total = row == _rowNames.Length - 1;
			double y = TableTop + row * RowHeight + (total ? 30.0 : 0.0);
			if (total)
			{
				g.RenderQuad(PanelColour, new Rectangle(NameX - 40.0, y - RowHeight / 2.0 + 8.0, Player2X - NameX + 240.0, RowHeight - 16.0));
			}
			DrawText(fontRenderer, _rowNames[row], NameX, y, TextColour, FontAlignment.MiddleY);
			for (int player = 0; player < 2; player++)
			{
				// The winner of the row shows in their colour
				Colour colour = _rowWinners[row] == player + 1 ? PlayerColours[player] : new Colour(0.6, 0.75, 0.85, 1.0);
				DrawText(fontRenderer, _rowValues[row][player], player == 0 ? Player1X : Player2X, y, colour);
			}
		}
		double winnerY = TableTop + _rowNames.Length * RowHeight + 90.0;
		DrawText(fontRenderer, WinnerText(_winner), 960.0, winnerY, WinnerColour(_winner));
		if (Frames / 30 % 2 == 0)
		{
			DrawText(fontRenderer, "PRESS START", 960.0, 1020.0, new Colour(0.6, 1.0, 1.0, 1.0));
		}
	}
}
