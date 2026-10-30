using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca;
using SonicOrca.Audio;
using SonicOrca.Core;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

namespace S2HD.Ending;

/// <summary>
/// The ending cutscene of the original game (EndingSequence and ObjCA in s2.asm), before the credits. It shows four
/// photos, then Sonic falls through the sky with clouds, in front of the background of Death Egg Zone. The Tornado
/// comes with birds, takes Sonic, and flies near (see EndingRescue.cs). The logic is the
/// logic of the original objects, in the coordinates of the screen of the original (320 x 224). Each image is a
/// resource of the import (SONICORCA/ENDING), so a mod can replace it. The image of a sprite has four times the size
/// of the original, as the other sprites of the game.
/// </summary>
internal sealed partial class EndingGameState : IGameState, IDisposable
{
	private const string ResourceKey = "SONICORCA/ENDING";

	private const string MusicResourceKey = "SONICORCA/MUSIC/ENDING";

	private const string BackgroundMapKey = "SONICORCA/LEVELS/DEZ/MAP";

	private const string BackgroundTileSetKey = "SONICORCA/LEVELS/DEZ/TILESET";

	private const double ScreenWidth = 320.0;

	private const double ScreenHeight = 224.0;

	/// <summary>The images of the sprites and the tiles have four times the size of the original.</summary>
	private const double ArtScale = 4.0;

	private const int PictureCount = 4;

	/// <summary>The position of a photo on plane A, in tiles (planeLoc(64,14,8)).</summary>
	private const int PictureColumn = 14;

	private const int PictureRow = 8;

	private const int TileSize = 8;

	/// <summary>The size of a tile of the HD map, in pixels of the HD map.</summary>
	private const int MapTileSize = 64;

	/// <summary>The value of the frame counter of ObjCA at which the credits start (60 frames per second).</summary>
	private const int CreditsFrame = 0x1140;

	/// <summary>The start of the vertical scroll of the background (Camera_BG_Y_pos).</summary>
	private const int BackgroundStartY = 0xC8;

	/// <summary>The fade to black before the credits (Pal_FadeToBlack in EndgameCredits).</summary>
	private const int FadeOutFrames = 22;

	private readonly SonicOrcaGameContext _gameContext;

	private readonly EndingRandom _random = new EndingRandom();

	private readonly EndingStarRows _starRows = new EndingStarRows();

	private readonly EndingPalette _palette = new EndingPalette();

	private readonly List<EndingSprite> _sprites = new List<EndingSprite>();

	private ResourceSession _resourceSession;

	private readonly ITexture[] _pictures = new ITexture[PictureCount];

	private AnimationGroup _sonicGroup;

	private AnimationGroup _cloudsGroup;

	private AnimationGroup _tornadoGroup;

	private AnimationGroup _pilotGroup;

	private AnimationGroup _birdsGroup;

	private AnimationGroup _planeGroup;

	private ITexture _closeUp;

	private LevelLayer _background;

	private TileSet _backgroundTiles;

	private SampleInstance _music;

	/// <summary>The photo on plane A, or -1.</summary>
	private int _picture = -1;

	/// <summary>Camera_BG_Y_pos.</summary>
	private int _backgroundY = BackgroundStartY;

	/// <summary>The opacity of the fade to black at the end.</summary>
	private double _fadeOut;

	private EndingSprite _character;

	// The variables of ObjCA (the cutscene)
	private int _routine = 6;

	private int _timer = 0x60;

	private int _counter;

	private int _pictureIndex;

	private int _cloudTimer = 1;

	private int _nextOrder;

	private bool _creditsTriggered;

	public EndingGameState(SonicOrcaGameContext gameContext)
	{
		_gameContext = gameContext;
		_random.Seed = (uint)SonicOrca.Core.Debugging.TestHooks.CreateRandom().Next();
	}

	/// <summary>
	/// The character of the ending, as EndingSequence chooses it: Tails when he plays alone, else Super Sonic with the
	/// 7 emeralds, else Sonic.
	/// </summary>
	public static EndingCharacter ChooseCharacter(LevelPrepareSettings settings)
	{
		if (settings.ProtagonistCharacter == CharacterType.Tails && settings.SidekickCharacter == CharacterType.Null)
		{
			return EndingCharacter.Tails;
		}
		return settings.Emeralds >= 7 ? EndingCharacter.SuperSonic : EndingCharacter.Sonic;
	}

	/// <summary>True when the import made the art of the ending.</summary>
	public static bool IsAvailable(ResourceTree tree) => tree[GetPictureKey(0)]?.Resource != null;

	public IEnumerable<UpdateResult> Update()
	{
		ResourceTree tree = _gameContext.ResourceTree;
		if (!IsAvailable(tree))
		{
			yield break;
		}
		_resourceSession = new ResourceSession(tree);
		for (int i = 0; i < PictureCount; i++)
		{
			_resourceSession.PushDependency(GetPictureKey(i));
		}
		_resourceSession.PushDependencies(ResourceKey + "/SONIC/ANIGROUP", ResourceKey + "/CLOUDS/ANIGROUP", ResourceKey + "/TORNADO/ANIGROUP",
			ResourceKey + "/TORNADO/PILOT/ANIGROUP", ResourceKey + "/BIRDS/ANIGROUP", ResourceKey + "/PLANE/ANIGROUP", ResourceKey + "/CLOSEUP");
		bool hasBackground = tree[BackgroundMapKey]?.Resource != null && tree[BackgroundTileSetKey]?.Resource != null;
		if (hasBackground)
		{
			_resourceSession.PushDependencies(BackgroundMapKey, BackgroundTileSetKey);
		}
		bool hasMusic = tree[MusicResourceKey]?.Resource != null;
		if (hasMusic)
		{
			_resourceSession.PushDependency(MusicResourceKey);
		}
		Task load = _resourceSession.LoadAsync(CancellationToken.None);
		while (!load.IsCompleted)
		{
			yield return UpdateResult.Next();
		}
		if (load.IsFaulted)
		{
			yield break;
		}
		for (int i = 0; i < PictureCount; i++)
		{
			_pictures[i] = tree.GetLoadedResource<ITexture>(GetPictureKey(i));
		}
		_sonicGroup = tree.GetLoadedResource<AnimationGroup>(ResourceKey + "/SONIC/ANIGROUP");
		_cloudsGroup = tree.GetLoadedResource<AnimationGroup>(ResourceKey + "/CLOUDS/ANIGROUP");
		_tornadoGroup = tree.GetLoadedResource<AnimationGroup>(ResourceKey + "/TORNADO/ANIGROUP");
		_pilotGroup = tree.GetLoadedResource<AnimationGroup>(ResourceKey + "/TORNADO/PILOT/ANIGROUP");
		_birdsGroup = tree.GetLoadedResource<AnimationGroup>(ResourceKey + "/BIRDS/ANIGROUP");
		_planeGroup = tree.GetLoadedResource<AnimationGroup>(ResourceKey + "/PLANE/ANIGROUP");
		_closeUp = tree.GetLoadedResource<ITexture>(ResourceKey + "/CLOSEUP");
		if (hasBackground)
		{
			_background = tree.GetLoadedResource<LevelMap>(BackgroundMapKey).Layers.FirstOrDefault();
			_backgroundTiles = tree.GetLoadedResource<TileSet>(BackgroundTileSetKey);
		}
		if (hasMusic)
		{
			_music = new SampleInstance(_gameContext.Audio, tree.GetLoadedResource<SampleInfo>(MusicResourceKey)) { Classification = SampleInstanceClassification.Music };
			_music.Play();
		}

		// The main loop of EndingSequence: RandomNumber, then the objects in the order of their slots
		while (!_creditsTriggered)
		{
			_random.Next();
			_palette.Update();
			UpdateCutScene();
			foreach (EndingSprite sprite in _sprites.ToArray())
			{
				sprite.Update?.Invoke(sprite);
				sprite.Animate();
			}
			_sprites.RemoveAll(sprite => sprite.Deleted);
			yield return UpdateResult.Next();
		}
		// EndgameCredits fades to black before the credits
		for (int frame = 1; frame <= FadeOutFrames; frame++)
		{
			_fadeOut = frame / (double)FadeOutFrames;
			if (_music != null)
			{
				_music.Volume = 1.0 - _fadeOut;
			}
			yield return UpdateResult.Next();
		}
	}

	/// <summary>ObjCA: the cutscene. Each value of the routine is a state of the original.</summary>
	private void UpdateCutScene()
	{
		_counter++;
		switch (_routine)
		{
			case 0:
				// ObjCA_Init: the photo fades in from white, and shows for $180 frames
				StartPaletteFade(4, 0x180);
				break;
			case 2:
			case 6:
				// loc_A240: wait
				if (--_timer < 0)
				{
					_routine += 2;
				}
				break;
			case 4:
				// loc_A24E: the photo fades to white, and the next one comes $80 frames later
				StartPaletteFade(6, 0x80);
				break;
			case 8:
				if (_pictureIndex < PictureCount)
				{
					// loc_A256: the next photo
					_picture = _pictureIndex++;
					_routine = 0;
					break;
				}
				// The photos end. Plane A clears, and the character comes.
				_picture = -1;
				_character = new EndingSprite(_sonicGroup) { Priority = 2, Line = 0, Animation = EndingArtAnimations.SonicFall };
				AddSprite(_character);
				StartPaletteFade(8, 0x80);
				_timer = 0x40;
				break;
			case 0xA:
				// loc_A30A: the character falls, then the background fades in
				if (--_timer < 0)
				{
					StartPaletteFade(0xA, 0x80);
					_timer = 0xC0;
				}
				_character.X = 0xA0;
				_character.Y = 0x50;
				break;
			case 0xC:
				// loc_A34C: the background moves up 1 pixel on each frame (Camera_Y_pos_diff $100), and the stars move
				if (--_timer >= 0)
				{
					_backgroundY++;
					_starRows.Update();
					break;
				}
				_routine += 2;
				_timer = 0x100;
				break;
			case 0xE:
				// loc_A38E: the credits start at frame $1140. The Tornado (ObjCC) comes after $100 frames.
				if (_counter == CreditsFrame)
				{
					_routine += 2;
					_creditsTriggered = true;
					break;
				}
				if (--_timer == 0)
				{
					AddSprite(CreateTornado());
				}
				SpawnClouds();
				break;
		}
	}

	/// <summary>sub_A22A: a palette fade of ObjC9, the next routine, and a wait.</summary>
	private void StartPaletteFade(int subtype, int wait)
	{
		_palette.Start(subtype);
		_routine += 2;
		_timer = wait;
	}

	/// <summary>loc_AB9C: a cloud at random intervals of up to $1F frames.</summary>
	private void SpawnClouds()
	{
		if (--_cloudTimer >= 0)
		{
			return;
		}
		_cloudTimer = (int)(_random.Seed & 0x1F);
		AddSprite(CreateCloud());
	}

	/// <summary>
	/// ObjCB_Init: a cloud with a random frame and speed. Before the Tornado comes, it rises from the bottom of the
	/// screen. After, it flies from the right to the left, because the camera follows the plane.
	/// </summary>
	private EndingSprite CreateCloud()
	{
		uint seed = _random.RotateRight(1);
		int index = (int)(seed & 3);
		int[] frames = { 0, 1, 2, 0 };
		int[] speeds = { -0x300, -0x200, -0x100, -0x300 };
		EndingSprite cloud = new EndingSprite(_cloudsGroup)
		{
			Priority = 6,
			Line = 3,
			Animation = frames[index],
			YVelocity = speeds[index]
		};
		if (_cloudsSideways)
		{
			cloud.Y = seed & 0xFF;
			cloud.X = 0x150;
		}
		else
		{
			cloud.X = seed & 0x1FF;
			cloud.Y = 0x100;
		}
		cloud.Update = sprite =>
		{
			// loc_AA76: the cloud turns when the Tornado comes
			if (sprite.Routine == 0 && _cloudsSideways)
			{
				sprite.Routine = 1;
				sprite.XVelocity = sprite.YVelocity;
				sprite.YVelocity = 0;
			}
			sprite.Move();
			// loc_AA8A: the original deletes the cloud when it leaves the screen
			if (_cloudsSideways ? (int)Math.Floor(sprite.X) < -0x20 : sprite.Y < 0)
			{
				sprite.Deleted = true;
			}
		};
		return cloud;
	}

	/// <summary>Adds an object. Objects that come later are behind the earlier ones of the same priority.</summary>
	private void AddSprite(EndingSprite sprite)
	{
		sprite.Order = _nextOrder++;
		_sprites.Add(sprite);
	}

	public void Draw()
	{
		Renderer renderer = _gameContext.Renderer;
		I2dRenderer g = renderer.Get2dRenderer();
		g.BlendMode = BlendMode.Alpha;
		g.ModelMatrix = Matrix4.Identity;
		g.RenderQuad(Colours.Black, new Rectangle(0.0, 0.0, 1920.0, 1080.0));
		double scale = 1080.0 / ScreenHeight;
		double left = (1920.0 - ScreenWidth * scale) / 2.0;
		Rectangle screen = new Rectangle(left, 0.0, ScreenWidth * scale, 1080.0);
		// The images have four times the size of the original. The matrix scales them to the screen.
		Matrix4 matrix = Matrix4.CreateTranslation(left, 0.0) * Matrix4.CreateScale(scale / ArtScale, scale / ArtScale);

		// Plane B: the background of Death Egg Zone, on the backdrop (colour 0 of line 2: black, then blue for the Tornado)
		if (_backgroundCleared)
		{
			g.RenderQuad(BackdropColour, screen);
		}
		else
		{
			DrawBackground(g, matrix, left, scale);
		}
		g.ModelMatrix = Matrix4.Identity;
		g.ClipRectangle = new Rectangle(0.0, 0.0, 1920.0, 1080.0);
		g.RenderQuad(new Colour(_palette.GetWhiteness(2), 1.0, 1.0, 1.0), screen);

		// Plane A: the photo, or the close-up of the plane. The original screen clips the planes and the sprites.
		g.ClipRectangle = screen;
		g.ModelMatrix = matrix;
		if (_picture >= 0)
		{
			Rectangle destination = new Rectangle(PictureColumn * TileSize * ArtScale, PictureRow * TileSize * ArtScale, _pictures[_picture].Width, _pictures[_picture].Height);
			DrawWithWhiteness(g, _pictures[_picture], new Rectangle(0, 0, _pictures[_picture].Width, _pictures[_picture].Height), destination, _palette.GetWhiteness(0), false);
		}
		// The sprites without the priority bit are behind the tiles of plane A with the priority bit: the close-up
		DrawSprites(g, highPriority: false);
		DrawCloseUp(g);
		DrawSprites(g, highPriority: true);

		g.ModelMatrix = Matrix4.Identity;
		g.ClipRectangle = new Rectangle(0.0, 0.0, 1920.0, 1080.0);
		if (_fadeOut > 0.0)
		{
			g.RenderQuad(new Colour(_fadeOut, 0.0, 0.0, 0.0), screen);
		}
		g.Colour = Colours.White;
	}

	/// <summary>
	/// Draws the sprites with or without the priority bit. A higher priority of DisplaySprite is further back, and so
	/// is a later object of the same priority.
	/// </summary>
	private void DrawSprites(I2dRenderer g, bool highPriority)
	{
		foreach (EndingSprite sprite in _sprites.Where(sprite => sprite.HighPriority == highPriority)
			.OrderByDescending(sprite => sprite.Priority).ThenByDescending(sprite => sprite.Order))
		{
			if (sprite.Group == null || sprite.Animation >= sprite.Group.Count)
			{
				continue;
			}
			if (!sprite.Visible)
			{
				continue;
			}
			Animation animation = sprite.Group[sprite.Animation];
			Animation.Frame frame = animation.Frames[Math.Min(sprite.Frame, animation.Frames.Count - 1)];
			Rectanglei source = frame.Source;
			int offsetX = sprite.FlipX ? -frame.Offset.X : frame.Offset.X;
			Rectangle destination = new Rectangle(
				Math.Floor(sprite.X) * ArtScale + offsetX - source.Width / 2.0,
				Math.Floor(sprite.Y) * ArtScale + frame.Offset.Y - source.Height / 2.0,
				source.Width, source.Height);
			DrawWithWhiteness(g, sprite.Group.Textures[frame.TextureIndex], source, destination, _palette.GetWhiteness(sprite.Line), sprite.FlipX);
		}
	}

	/// <summary>
	/// Draws an image, and then the same image in white with the whiteness as its opacity. So the image fades between
	/// its colours and white, as a palette line of the original does in a fade of ObjC9.
	/// </summary>
	private static void DrawWithWhiteness(I2dRenderer g, ITexture texture, Rectangle source, Rectangle destination, double whiteness, bool flipX)
	{
		g.Colour = Colours.White;
		g.RenderTexture(texture, source, destination, flipX);
		if (whiteness <= 0.0)
		{
			return;
		}
		Colour additive = g.AdditiveColour;
		g.Colour = new Colour(whiteness, 1.0, 1.0, 1.0);
		g.AdditiveColour = Colours.White;
		g.RenderTexture(texture, source, destination, flipX);
		g.AdditiveColour = additive;
		g.Colour = Colours.White;
	}

	/// <summary>
	/// Draws the background layer of Death Egg Zone in rows. Each row of SwScrl_DEZ scrolls by its own value. The layer
	/// repeats to the left and the right.
	/// </summary>
	private void DrawBackground(I2dRenderer g, Matrix4 matrix, double left, double scale)
	{
		if (_background == null || _backgroundTiles == null || _background.Columns == 0)
		{
			return;
		}
		Renderer renderer = _gameContext.Renderer;
		int layerWidth = _background.Columns * MapTileSize;
		int top = 0;
		for (int row = 0; row < EndingStarRows.Heights.Length; row++)
		{
			int bottom = top + EndingStarRows.Heights[row];
			// The lines of the screen that show this row
			int screenTop = Math.Max(0, top - _backgroundY);
			int screenBottom = Math.Min((int)ScreenHeight, bottom - _backgroundY);
			int scroll = _starRows.Values[row];
			top = bottom;
			if (screenTop >= screenBottom)
			{
				continue;
			}
			g.ModelMatrix = Matrix4.Identity;
			g.ClipRectangle = new Rectangle(left, screenTop * scale, ScreenWidth * scale, (screenBottom - screenTop) * scale);
			g.ModelMatrix = matrix;
			int mapTop = (screenTop + _backgroundY) * (int)ArtScale;
			int mapBottom = (screenBottom + _backgroundY) * (int)ArtScale;
			int mapLeft = (int)(scroll * ArtScale) % layerWidth;
			for (int tileRow = mapTop / MapTileSize; tileRow * MapTileSize < mapBottom && tileRow < _background.Rows; tileRow++)
			{
				for (int x = -(mapLeft % MapTileSize); x < ScreenWidth * ArtScale; x += MapTileSize)
				{
					int column = ((mapLeft + x) / MapTileSize % _background.Columns + _background.Columns) % _background.Columns;
					int tile = _background.Tiles[column, tileRow];
					if (tile == 0)
					{
						continue;
					}
					_backgroundTiles.DrawTile(renderer, tile, new Rectanglei(x, tileRow * MapTileSize - _backgroundY * (int)ArtScale, MapTileSize, MapTileSize));
				}
			}
		}
		g.ModelMatrix = Matrix4.Identity;
		g.ClipRectangle = new Rectangle(0.0, 0.0, 1920.0, 1080.0);
	}

	public void Dispose()
	{
		_music?.Stop();
		_music?.Dispose();
		_music = null;
		_resourceSession?.Dispose();
		_resourceSession = null;
	}

	private static string GetPictureKey(int index) => $"{ResourceKey}/PICTURES/{index + 1}";
}

/// <summary>The character of the ending (Ending_Routine of the original).</summary>
internal enum EndingCharacter
{
	Sonic,
	SuperSonic,
	Tails
}

/// <summary>The animations of the groups of the ending, as the import makes them (SonicOrca.Original.EndingArt).</summary>
internal static class EndingArtAnimations
{
	public const int SonicFall = 0;

	public const int SonicWait = 1;
}

/// <summary>A sprite of the ending: an object of the original with a position, a speed, an animation and a priority.</summary>
internal sealed class EndingSprite
{
	public EndingSprite(AnimationGroup group)
	{
		Group = group;
	}

	public AnimationGroup Group { get; set; }

	/// <summary>The animation in the group.</summary>
	public int Animation { get; set; }

	/// <summary>The frame of the animation.</summary>
	public int Frame { get; set; }

	/// <summary>anim_frame_duration: the frames until the next frame of the animation.</summary>
	public int FrameTimer { get; set; }

	/// <summary>The position on the screen of the original, with a fraction.</summary>
	public double X { get; set; }

	public double Y { get; set; }

	/// <summary>The speed, in 1/256 pixels on each frame, as in the original.</summary>
	public int XVelocity { get; set; }

	public int YVelocity { get; set; }

	/// <summary>The priority of DisplaySprite: 0 is in front, 7 is at the back.</summary>
	public int Priority { get; set; }

	/// <summary>The palette line, for the fades.</summary>
	public int Line { get; set; }

	public bool FlipX { get; set; }

	public bool Visible { get; set; } = true;

	/// <summary>The priority bit of the art tile: the sprite is in front of the tiles of plane A with the priority bit.</summary>
	public bool HighPriority { get; set; }

	public bool Deleted { get; set; }

	/// <summary>The order in which the object came. Later objects are behind earlier objects of the same priority.</summary>
	public int Order { get; set; }

	/// <summary>The routine of the object (routine and routine_secondary of the original).</summary>
	public int Routine { get; set; }

	/// <summary>The timers and values of the object (objoff_30 to objoff_3C of the original).</summary>
	public int Timer { get; set; }

	public int Index { get; set; }

	public int Saved { get; set; }

	/// <summary>The start position of an object that moves with plane A (objoff_30 and objoff_32 of ObjCE and ObjCF).</summary>
	public int BaseX { get; set; }

	public int BaseY { get; set; }

	/// <summary>
	/// An animation script of mapping frames for a group with one animation for each mapping frame: the delay and the
	/// frames. Without a script, the object plays the frames of its animation in the group.
	/// </summary>
	public int[] Script { get; set; }

	public int ScriptDelay { get; set; }

	/// <summary>Sets a script and starts it at its first frame.</summary>
	public void SetScript(int delay, params int[] frames)
	{
		Script = frames;
		ScriptDelay = delay;
		ScriptIndex = 0;
		FrameTimer = delay;
		Animation = frames[0];
	}

	/// <summary>The position in <see cref="Script"/>.</summary>
	public int ScriptIndex { get; set; }

	/// <summary>The logic of the object on each frame.</summary>
	public Action<EndingSprite> Update { get; set; }

	/// <summary>ObjectMove: the position changes by the speed.</summary>
	public void Move()
	{
		X += XVelocity / 256.0;
		Y += YVelocity / 256.0;
	}

	/// <summary>AnimateSprite: the next frame of the animation after its delay. The animations of the ending loop.</summary>
	public void Animate()
	{
		if (Script != null)
		{
			// AnimateSprite: the next frame of the script after its delay. The scripts of the ending loop.
			if (--FrameTimer >= 0)
			{
				return;
			}
			FrameTimer = ScriptDelay;
			ScriptIndex = (ScriptIndex + 1) % Script.Length;
			Animation = Script[ScriptIndex];
			return;
		}
		if (Group == null || Animation >= Group.Count)
		{
			return;
		}
		Animation animation = Group[Animation];
		if (animation.Frames.Count <= 1 || --FrameTimer >= 0)
		{
			return;
		}
		Frame = (Frame + 1) % animation.Frames.Count;
		FrameTimer = animation.Frames[Frame].Delay;
	}
}
