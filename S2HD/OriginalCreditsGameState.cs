using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca;
using SonicOrca.Audio;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Input;
using SonicOrca.Original;
using SonicOrca.Resources;

namespace S2HD;

/// <summary>
/// Plays the credits of the original game: slides of text that fade in and out, then the Sonic 2 logo that flashes.
/// The timing and the fades are the same as in EndgameCredits of the original. The import of the disassembly makes
/// the text of the slides and an image for each character of the font (see <see cref="OriginalCredits"/>). This state
/// makes each line of text from the images of its characters. Without the import, this state ends at once.
/// </summary>
internal sealed class OriginalCreditsGameState : IGameState, IDisposable
{
	private const string ResourceKey = OriginalCredits.ResourceKey;

	private const string SlidesResourceKey = ResourceKey + "/" + OriginalCredits.SlidesKey;

	private const string MusicResourceKey = "SONICORCA/MUSIC/CREDITS/ORIGINAL";

	private const int EnterScancode = 40;

	/// <summary>The size of the screen of the original.</summary>
	private const double ScreenWidth = 320.0;

	private const double ScreenHeight = 224.0;

	/// <summary>The palette lines of the text, and the colour of each line (Mega Drive 0BGR): white and yellow.</summary>
	private static readonly ushort[] TextColours = { 0x0EEE, 0x00EE };

	/// <summary>A fade of the original (Pal_FadeFromBlack or Pal_FadeToBlack) takes 22 frames.</summary>
	private const int FadeFrames = 22;

	/// <summary>The frames that a slide shows at full colour: $18E + 1 at 60 frames per second.</summary>
	private const int SlideFrames = 0x18E + 1;

	/// <summary>The frames before the logo starts to flash.</summary>
	private const int LogoWaitFrames = 0x3B + 1;

	/// <summary>The value of the logo counter after which a button ends the credits.</summary>
	private const int LogoSkipIndex = 0x5E;

	/// <summary>The frames that the logo shows after <see cref="LogoSkipIndex"/>, without a button.</summary>
	private const int LogoEndFrames = 0x257 + 1;

	/// <summary>The logo counter after which the logo stops to flash.</summary>
	private const int LogoFlashEnd = 0x24;

	/// <summary>The palette of the logo for each second value of the counter (byte_A0EC).</summary>
	private static readonly int[] LogoFlashPalettes = { 0, 1, 2, 3, 4, 3, 2, 1, 0, 5, 6, 7, 8, 7, 6, 5, 0, 0 };

	private const int LogoPalettes = 9;

	private readonly SonicOrcaGameContext _gameContext;

	private ResourceSession _resourceSession;

	private List<OriginalCredits.Slide> _slides = new List<OriginalCredits.Slide>();

	/// <summary>The image of each character of the font. A character without an image is not drawn.</summary>
	private readonly Dictionary<char, ITexture> _characters = new Dictionary<char, ITexture>();

	private readonly List<ITexture> _logo = new List<ITexture>();

	private SampleInstance _music;

	/// <summary>The slide that shows, or -1. The colours of its palette lines, Mega Drive 0BGR.</summary>
	private int _slide = -1;

	private readonly ushort[] _colours = new ushort[2];

	/// <summary>The palette of the logo that shows, or -1.</summary>
	private int _logoPalette = -1;

	/// <summary>The folder of the logo: LOGO, or LOGOK for Knuckles in Sonic 2.</summary>
	private readonly string _logoFolder = "LOGO";

	/// <param name="knuckles">
	/// True after the game of Knuckles: the logo of Knuckles in Sonic 2, with the banner "KNUCKLES THE ECHIDNA IN", when
	/// the import made it.
	/// </param>
	public OriginalCreditsGameState(SonicOrcaGameContext gameContext, bool knuckles = false)
	{
		_gameContext = gameContext;
		if (knuckles && gameContext.ResourceTree[$"{ResourceKey}/LOGOK/PALETTE0"]?.Resource != null)
		{
			_logoFolder = "LOGOK";
		}
	}

	/// <summary>True when the import made the original credits.</summary>
	public static bool IsAvailable(ResourceTree tree) => tree[SlidesResourceKey]?.Resource != null;

	public IEnumerable<UpdateResult> Update()
	{
		ResourceTree tree = _gameContext.ResourceTree;
		if (!IsAvailable(tree))
		{
			yield break;
		}
		_resourceSession = new ResourceSession(tree);
		_resourceSession.PushDependency(SlidesResourceKey);
		Task load = _resourceSession.LoadAsync(CancellationToken.None);
		while (!load.IsCompleted)
		{
			yield return UpdateResult.Next();
		}
		if (load.IsFaulted)
		{
			yield break;
		}
		_slides = OriginalCredits.ParseSlides(tree.GetLoadedResource<XmlLoadedResource>(SlidesResourceKey).XmlDocument);
		char[] characters = _slides.SelectMany(slide => slide.Items).SelectMany(item => item.Text).Distinct()
			.Where(c => tree[GetCharacterKey(c)]?.Resource != null)
			.ToArray();
		foreach (char c in characters)
		{
			_resourceSession.PushDependency(GetCharacterKey(c));
		}
		for (int p = 0; p < LogoPalettes; p++)
		{
			_resourceSession.PushDependency(GetLogoKey(_logoFolder, p));
		}
		bool hasMusic = tree[MusicResourceKey]?.Resource != null;
		if (hasMusic)
		{
			_resourceSession.PushDependency(MusicResourceKey);
		}
		load = _resourceSession.LoadAsync(CancellationToken.None);
		while (!load.IsCompleted)
		{
			yield return UpdateResult.Next();
		}
		if (load.IsFaulted)
		{
			yield break;
		}
		foreach (char c in characters)
		{
			_characters[c] = GetTexture(GetCharacterKey(c));
		}
		for (int p = 0; p < LogoPalettes; p++)
		{
			_logo.Add(GetTexture(GetLogoKey(_logoFolder, p)));
		}
		if (hasMusic)
		{
			_music = new SampleInstance(_gameContext.Audio, tree.GetLoadedResource<SampleInfo>(MusicResourceKey)) { Classification = SampleInstanceClassification.Music };
			_music.Play();
		}

		bool skip = false;
		for (int i = 0; i < _slides.Count && !skip; i++)
		{
			_slide = i;
			_colours[0] = 0;
			_colours[1] = 0;
			for (int frame = 0; frame < FadeFrames; frame++)
			{
				for (int line = 0; line < _colours.Length; line++)
				{
					_colours[line] = MegaDriveFade.FadeIn(_colours[line], TextColours[line]);
				}
				yield return UpdateResult.Next();
			}
			for (int frame = 0; frame < SlideFrames && !skip; frame++)
			{
				skip = IsSkipPressed();
				yield return UpdateResult.Next();
			}
			for (int frame = 0; frame < FadeFrames; frame++)
			{
				for (int line = 0; line < _colours.Length; line++)
				{
					_colours[line] = MegaDriveFade.FadeOut(_colours[line]);
				}
				yield return UpdateResult.Next();
			}
		}
		// The original fades to black once more after the last slide
		_slide = -1;
		for (int frame = 0; frame < FadeFrames; frame++)
		{
			yield return UpdateResult.Next();
		}

		// The logo: EndgameLogoFlash with counter 0, a wait, then the flash. A button ends it after the flash.
		int counter = 0;
		_logoPalette = GetLogoPalette(counter, 0);
		for (int frame = 0; frame < LogoWaitFrames; frame++)
		{
			yield return UpdateResult.Next();
		}
		int endFrames = LogoEndFrames;
		while (true)
		{
			yield return UpdateResult.Next();
			counter++;
			_logoPalette = GetLogoPalette(counter, _logoPalette);
			if (counter < LogoSkipIndex)
			{
				continue;
			}
			if (IsSkipPressed() || --endFrames <= 0)
			{
				break;
			}
		}
	}

	public void Draw()
	{
		Renderer renderer = _gameContext.Renderer;
		I2dRenderer g = renderer.Get2dRenderer();
		g.BlendMode = BlendMode.Alpha;
		g.RenderQuad(Colours.Black, new Rectangle(0.0, 0.0, 1920.0, 1080.0));
		// The screen of the original fills the height of the screen, in the centre
		double scale = 1080.0 / ScreenHeight;
		Rectangle screen = new Rectangle((1920.0 - ScreenWidth * scale) / 2.0, 0.0, ScreenWidth * scale, 1080.0);
		if (_slide >= 0)
		{
			foreach (OriginalCredits.TextItem item in _slides[_slide].Items)
			{
				if (item.Line >= 0 && item.Line < _colours.Length && _colours[item.Line] != 0)
				{
					g.Colour = ToColour(_colours[item.Line]);
					DrawText(g, screen, scale, item);
				}
			}
			g.Colour = Colours.White;
		}
		if (_logoPalette >= 0)
		{
			// RenderQuad leaves its colour in the renderer, so set white for the texture
			g.Colour = Colours.White;
			g.RenderTexture(_logo[_logoPalette], screen);
		}
	}

	/// <summary>
	/// Draws a line of text, one character after the other. A character is <see cref="OriginalCredits.CharacterHeight"/>
	/// pixels of the original high, and its image gives its width: an image of 2 x 1 characters is 16 x 8 pixels of
	/// the original. As on plane A of the original, the text stops at the right edge of the screen.
	/// </summary>
	private void DrawText(I2dRenderer g, Rectangle screen, double scale, OriginalCredits.TextItem item)
	{
		double x = item.X;
		foreach (char c in item.Text)
		{
			ITexture texture = _characters.GetValueOrDefault(c);
			double width = texture != null
				? OriginalCredits.CharacterHeight * texture.Width / (double)texture.Height
				: c == ' ' ? OriginalCredits.SpaceWidth : OriginalCredits.TileSize * 2;
			if (x + width > ScreenWidth)
			{
				break;
			}
			if (texture != null)
			{
				g.RenderTexture(texture, new Rectangle(screen.X + x * scale, screen.Y + item.Y * scale, width * scale, OriginalCredits.CharacterHeight * scale));
			}
			x += width;
		}
	}

	public void Dispose()
	{
		_music?.Stop();
		_music?.Dispose();
		_music = null;
		_resourceSession?.Dispose();
		_resourceSession = null;
	}

	/// <summary>The palette of the logo after EndgameLogoFlash with a counter. It changes on even values below $24.</summary>
	private static int GetLogoPalette(int counter, int palette)
	{
		if (counter >= LogoFlashEnd || counter % 2 != 0)
		{
			return palette;
		}
		return LogoFlashPalettes[counter / 2];
	}

	private bool IsSkipPressed()
	{
		InputState pressed = _gameContext.Input.Pressed;
		GamePadInputState pad = pressed.GamePad[0];
		return pressed.Keyboard[EnterScancode] || pad.Start || pad.South || pad.East || pad.West;
	}

	private ITexture GetTexture(string key)
	{
		ITexture texture = _gameContext.ResourceTree.GetLoadedResource<ITexture>(key);
		texture.Filtering = TextureFiltering.NearestNeighbour;
		return texture;
	}

	private static Colour ToColour(ushort colour)
	{
		return new Colour((byte)((colour >> 1 & 7) * 255 / 7), (byte)((colour >> 5 & 7) * 255 / 7), (byte)((colour >> 9 & 7) * 255 / 7));
	}

	private static string GetCharacterKey(char c) => $"{ResourceKey}/{OriginalCredits.FontFolder}/{OriginalCredits.GetCharacterName(c)}";

	private static string GetLogoKey(string folder, int palette) => $"{ResourceKey}/{folder}/PALETTE{palette}";
}
