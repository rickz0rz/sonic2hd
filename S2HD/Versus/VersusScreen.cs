using System.Threading;
using System.Threading.Tasks;
using SonicOrca.Core.Debugging;
using SonicOrca;
using SonicOrca.Audio;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Input;
using SonicOrca.Menu;
using SonicOrca.Resources;

namespace S2HD.Versus;

/// <summary>
/// The base of the screens of a versus match: the font, the sounds, the background, and the input of both players.
/// </summary>
internal abstract class VersusScreen : Screen
{
	private const string FontResourceKey = "SONICORCA/FONTS/IMPACT/REGULAR";
	private const string CursorSoundResourceKey = "SONICORCA/SOUND/NAVIGATE/CURSOR";
	private const string YesSoundResourceKey = "SONICORCA/SOUND/NAVIGATE/YES";
	private const string BackSoundResourceKey = "SONICORCA/SOUND/NAVIGATE/BACK";
	private const string MusicResourceKey = "SONICORCA/MUSIC/OPTIONS";

	private const int EscapeScancode = 41;

	/// <summary>The SDL scancode of the Enter key.</summary>
	private const int EnterScancode = 40;

	protected static readonly Colour TextColour = new Colour(1.0, 0.75, 0.85, 1.0);

	protected static readonly Colour SelectedColour = new Colour(1.0, 1.0, 0.85, 0.2);

	protected static readonly Colour PanelColour = new Colour(0.72, 0.04, 0.08, 0.22);

	/// <summary>The colours of player 1 (Sonic, blue) and player 2 (Tails, orange).</summary>
	protected static readonly Colour[] PlayerColours = { new Colour(1.0, 0.35, 0.6, 1.0), new Colour(1.0, 1.0, 0.65, 0.2) };

	protected static readonly string[] PlayerNames = { "SONIC", "TAILS" };

	protected readonly SonicOrcaGameContext GameContext;

	private readonly ResourceSession _resourceSession;

	private readonly bool _playMusic;

	private SampleInstance _music;

	/// <summary>The capture number of the first frame of this screen, for the tests.</summary>
	private int _captureNumber;

	protected Font Font { get; private set; }

	protected Sample CursorSound { get; private set; }

	protected Sample YesSound { get; private set; }

	protected Sample BackSound { get; private set; }

	/// <summary>The frames since the screen started.</summary>
	protected int Frames { get; private set; }

	protected VersusScreen(SonicOrcaGameContext gameContext, bool playMusic)
	{
		GameContext = gameContext;
		_playMusic = playMusic;
		_resourceSession = new ResourceSession(gameContext.ResourceTree);
	}

	public override async Task LoadAsync(ScreenLoadingProgress progress, CancellationToken ct = default)
	{
		foreach (string key in new[] { FontResourceKey, CursorSoundResourceKey, YesSoundResourceKey, BackSoundResourceKey })
		{
			_resourceSession.PushDependency(key);
		}
		if (_playMusic && GameContext.ResourceTree[MusicResourceKey]?.Resource != null)
		{
			_resourceSession.PushDependency(MusicResourceKey);
		}
		await _resourceSession.LoadAsync(ct);
		ResourceTree tree = GameContext.ResourceTree;
		Font = tree.GetLoadedResource<Font>(FontResourceKey);
		CursorSound = tree.GetLoadedResource<Sample>(CursorSoundResourceKey);
		YesSound = tree.GetLoadedResource<Sample>(YesSoundResourceKey);
		BackSound = tree.GetLoadedResource<Sample>(BackSoundResourceKey);
	}

	public override void Initialise()
	{
		_captureNumber = TestHooks.VersusScreenCaptureOffset + 1000 * TestHooks.VersusScreensShown++;
		SampleInfo music = _playMusic ? GameContext.ResourceTree.GetLoadedResource<SampleInfo>(MusicResourceKey) : null;
		if (music != null)
		{
			_music = new SampleInstance(GameContext.Audio, music) { Classification = SampleInstanceClassification.Music };
			_music.Play();
		}
	}

	public override void Update()
	{
		Frames++;
	}

	/// <summary>True when either player pressed Start or the jump button, or the Enter key.</summary>
	protected bool ConfirmPressed()
	{
		for (int i = 0; i < 2; i++)
		{
			Controller pressed = GameContext.Pressed[i];
			if (pressed.Start || pressed.Action1)
			{
				return true;
			}
		}
		return GameContext.Input.Pressed.Keyboard[EnterScancode];
	}

	/// <summary>True when player 1 pressed Escape, Select or the B button.</summary>
	protected bool BackPressed()
	{
		InputState input = GameContext.Input.Pressed;
		return input.Keyboard[EscapeScancode] || input.GamePad[0].Select || input.GamePad[0].East;
	}

	protected void DrawBackground(Renderer renderer, string title)
	{
		TestHooks.DrawnLevelTick = _captureNumber + Frames;
		I2dRenderer g = renderer.Get2dRenderer();
		IFontRenderer fontRenderer = renderer.GetFontRenderer();
		g.BlendMode = BlendMode.Alpha;
		g.RenderQuad(new Colour(1.0, 0.05, 0.12, 0.35), new Rectangle(0.0, 0.0, 1920.0, 1080.0));
		// Diagonal bands in the colours of the players
		for (int i = 0; i < 2; i++)
		{
			Colour colour = PlayerColours[i];
			g.RenderQuad(new Colour(0.12, colour.Red / 255.0, colour.Green / 255.0, colour.Blue / 255.0), new Rectangle(i == 0 ? 0.0 : 1440.0, 0.0, 480.0, 1080.0));
		}
		fontRenderer.RenderStringWithShadow(title, new Rectangle(960.0, 90.0, 0.0, 0.0), FontAlignment.MiddleX | FontAlignment.MiddleY, Font, Colours.White);
	}

	protected void DrawText(IFontRenderer fontRenderer, string text, double x, double y, Colour colour, FontAlignment alignment = FontAlignment.MiddleX | FontAlignment.MiddleY)
	{
		fontRenderer.RenderStringWithShadow(text, new Rectangle(x, y, 0.0, 0.0), alignment, Font, colour);
	}

	/// <summary>The name of the winner, or "DRAW" for a tie.</summary>
	protected static string WinnerText(int winner) => winner == 0 ? "DRAW" : PlayerNames[winner - 1] + " WINS";

	protected static Colour WinnerColour(int winner) => winner == 0 ? Colours.White : PlayerColours[winner - 1];

	public override void Unload()
	{
		_music?.Stop();
		_music?.Dispose();
		_music = null;
		_resourceSession.Dispose();
	}
}
