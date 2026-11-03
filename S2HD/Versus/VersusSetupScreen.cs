using System;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Input;

namespace S2HD.Versus;

/// <summary>
/// The setup of a versus match, as LevelSelectMenu2P of the original: the zones, with the winner of each zone that
/// the players played, the items of the monitors, and the object layouts of Casino Night. Player 1 chooses a zone that
/// the players did not play.
/// </summary>
internal sealed class VersusSetupScreen : VersusScreen
{
	private const double ListTop = 280.0;
	private const double RowHeight = 90.0;

	private readonly VersusMatch _match;

	/// <summary>The selected row: a zone, or an option row after the zones.</summary>
	private int _row;

	/// <summary>The zone to play, or null when player 1 went back to the title screen.</summary>
	public int? Result { get; private set; }

	public VersusSetupScreen(SonicOrcaGameContext gameContext, VersusMatch match)
		: base(gameContext, MenuMusicResourceKey)
	{
		_match = match;
		_row = Math.Max(0, Array.FindIndex(match.Zones, zone => zone == null));
	}

	private int ItemsRow => _match.Zones.Length;

	private int LayoutsRow => _match.Zones.Length + 1;

	private int RowCount => _match.Zones.Length + (_match.HasTwoPlayerLayouts ? 2 : 1);

	public override void Update()
	{
		base.Update();
		Controller pressed = GameContext.Pressed[0];
		if (BackPressed())
		{
			GameContext.Audio.PlaySound(BackSound);
			Finish();
			return;
		}
		if (pressed.DirectionLeft.Y <= -0.5 || pressed.DirectionLeft.Y >= 0.5)
		{
			_row = (_row + (pressed.DirectionLeft.Y < 0 ? RowCount - 1 : 1)) % RowCount;
			GameContext.Audio.PlaySound(CursorSound);
		}
		else if (_row == ItemsRow && (Math.Abs(pressed.DirectionLeft.X) >= 0.5 || pressed.Start || pressed.Action1))
		{
			_match.TeleportOnly = !_match.TeleportOnly;
			GameContext.Audio.PlaySound(CursorSound);
		}
		else if (_row == LayoutsRow && (Math.Abs(pressed.DirectionLeft.X) >= 0.5 || pressed.Start || pressed.Action1))
		{
			_match.TwoPlayerLayouts = !_match.TwoPlayerLayouts;
			GameContext.Audio.PlaySound(CursorSound);
		}
		else if (_row < ItemsRow && (pressed.Start || pressed.Action1))
		{
			// A zone that the players played shows its result, and cannot start again until the match ends
			if (_match.Zones[_row] != null)
			{
				GameContext.Audio.PlaySound(BackSound);
				return;
			}
			GameContext.Audio.PlaySound(YesSound);
			Result = _row;
			Finish();
		}
	}

	public override void Draw(Renderer renderer)
	{
		DrawBackground(renderer, "2P VS");
		I2dRenderer g = renderer.Get2dRenderer();
		IFontRenderer fontRenderer = renderer.GetFontRenderer();
		for (int i = 0; i < RowCount; i++)
		{
			double y = ListTop + i * RowHeight + (i >= ItemsRow ? 40.0 : 0.0);
			bool selected = i == _row;
			if (selected)
			{
				g.RenderQuad(PanelColour, new Rectangle(400.0, y - RowHeight / 2.0 + 10.0, 1120.0, RowHeight - 20.0));
			}
			Colour colour = selected ? SelectedColour : TextColour;
			if (i == ItemsRow)
			{
				DrawText(fontRenderer, "ITEMS", 460.0, y, colour, FontAlignment.MiddleY);
				DrawText(fontRenderer, _match.TeleportOnly ? "< TELEPORT ONLY >" : "< ALL KINDS >", 1180.0, y, colour);
				continue;
			}
			if (i == LayoutsRow)
			{
				// The objects of Casino Night: the two-player layouts of the original, or the layouts of the main game
				DrawText(fontRenderer, "CASINO NIGHT", 460.0, y, colour, FontAlignment.MiddleY);
				DrawText(fontRenderer, _match.TwoPlayerLayouts ? "< 2P OBJECTS >" : "< 1P OBJECTS >", 1180.0, y, colour);
				continue;
			}
			VersusZoneResult zone = _match.Zones[i];
			DrawText(fontRenderer, _match.ZoneNames[i], 460.0, y, zone == null ? colour : new Colour(0.6, 0.75, 0.85, 1.0), FontAlignment.MiddleY);
			if (zone != null)
			{
				DrawText(fontRenderer, WinnerText(zone.Winner), 1180.0, y, WinnerColour(zone.Winner));
			}
		}
		string score = $"{PlayerNames[0]}  {_match.Player1Wins}  -  {_match.Player2Wins}  {PlayerNames[1]}";
		DrawText(fontRenderer, score, 960.0, 860.0, Colours.White);
		DrawText(fontRenderer, "UP, DOWN: CHOOSE    LEFT, RIGHT: OPTION    START: PLAY    ESC: BACK", 960.0, 1040.0, new Colour(0.6, 1.0, 1.0, 1.0));
	}
}
