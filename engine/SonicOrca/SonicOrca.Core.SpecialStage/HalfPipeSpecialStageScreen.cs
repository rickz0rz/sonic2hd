using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca.Audio;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Input;
using SonicOrca.Menu;
using SonicOrca.Resources;

namespace SonicOrca.Core.SpecialStage;

/// <summary>
/// The 3D half-pipe special stage. The player runs along a track that the segment layout makes, moves around the
/// pipe with left and right, and jumps with an action button. Rings count toward the goal of each checkpoint, and
/// bombs take rings away. Tails can follow Sonic under computer control, or under the control of the second pad.
/// Start it with --halfpipe. Escape closes it. The minus and equals keys change the field
/// of view.
/// </summary>
public class HalfPipeSpecialStageScreen : Screen
{
	/// <summary>
	/// A quad with four corners, four texture coordinates and a texture.
	/// </summary>
	private struct TexturedQuad
	{
		public Vector3[] Position { get; set; }

		public Vector2[] TextureMapping { get; set; }

		public ITexture Texture { get; set; }

		public TexturedQuad(Vector3[] position, Vector2[] textureMapping, ITexture texture)
		{
			Position = position;
			TextureMapping = textureMapping;
			Texture = texture;
		}
	}

	private const string MusicResourceKey = "SONICORCA/MUSIC/SPECIALSTAGE/S2";

	private const string FontResourceKey = "SONICORCA/FONTS/HUD";

	private const string BackgroundResourceKey = "SONICORCA/SPECIALSTAGE/HALFPIPE/BACKGROUND";

	private const string FloorResourceKey = "SONICORCA/SPECIALSTAGE/HALFPIPE/FLOOR";

	private const string SonicAnimationResourceKey = "SONICORCA/SPECIALSTAGE/ANIGROUP";

	private const string RingAnimationResourceKey = "SONICORCA/SPECIALSTAGE/RINGANIM";

	private const string RingEffectAnimationResourceKey = "SONICORCA/SPECIALSTAGE/RINGEFFECTANIM";

	private const string ExplosionAnimationResourceKey = "SONICORCA/SPECIALSTAGE/EXPLOSIONANIM";

	private const string BombResourceKey = "SONICORCA/SPECIALSTAGE/BOMB";

	private const string ShadowResourceKey = "SONICORCA/SPECIALSTAGE/SHADOW";

	private const string RingsLabelResourceKey = "SONICORCA/SPECIALSTAGE/RINGS";

	private const string TailsAnimationResourceKey = "SONICORCA/SPECIALSTAGE/TAILSANIM";

	private const string SonicLabelResourceKey = "SONICORCA/SPECIALSTAGE/SONICHUD";

	private const string TailsLabelResourceKey = "SONICORCA/SPECIALSTAGE/TAILSHUD";

	private const string TotalLabelResourceKey = "SONICORCA/SPECIALSTAGE/TOTAL";

	private const string RingSoundResourceKey = "SONICORCA/SOUND/RING";

	private const string JumpSoundResourceKey = "SONICORCA/SOUND/JUMP";

	private const string RingLossSoundResourceKey = "SONICORCA/SOUND/RINGSCATTER";

	private const string BombSoundResourceKey = "SONICORCA/SOUND/BADNIKEXPLOSION";

	private const string CheckpointSoundResourceKey = "SONICORCA/SOUND/STARPOST";

	private const string ErrorSoundResourceKey = "SONICORCA/SOUND/NAVIGATE/NO";

	private const string TallySoundResourceKey = "SONICORCA/SOUND/TALLY/SWITCH";

	private const string TallyEndSoundResourceKey = "SONICORCA/SOUND/TALLY/END";

	private const string EmeraldMusicResourceKey = "SONICORCA/MUSIC/JINGLE/STAGECLEAR/S1";

	// SDL scan codes of the keys that change the field of view, and of Escape.
	private const int ScancodeMinus = 45;

	private const int ScancodeEquals = 46;

	private const int ScancodeEscape = 41;

	/// <summary>The stage layouts, one for each stage: SONICORCA/SPECIALSTAGE/STAGE1 to STAGE7.</summary>
	private const string StageResourceKeyPrefix = "SONICORCA/SPECIALSTAGE/STAGE";

	/// <summary>The number of track nodes in front of the current node that are drawn. A node is one track unit.</summary>
	private const int DrawDistanceAhead = 240;

	private const int DrawDistanceBehind = 16;

	/// <summary>The number of track nodes in front of the camera that objects are drawn.</summary>
	private const int ObjectDrawDistance = 180;

	/// <summary>
	/// The number of track nodes between the camera and the player. With <see cref="CameraHeight"/> and
	/// <see cref="CameraTilt"/>, the view is as in the original: the bottom of the pipe at the player is 73% down the
	/// screen, the centre of the pipe 24% down, and the walls at 69% of the width (SS_Offset_X and SS_Offset_Y, with
	/// the H32 screen of the original). The track goes away above the player, so the player does not hide the rings
	/// and bombs that come.
	/// </summary>
	private const int PlayerLead = 6;

	/// <summary>The height of the camera above the centre line of the pipe, in track units (the pipe radius is 4).</summary>
	private const double CameraHeight = 0.7;

	/// <summary>The angle in degrees that the camera looks down, under the slope of the track.</summary>
	private const double CameraTilt = 23.4;

	/// <summary>A bomb takes this number of rings, or all of the rings when the player has fewer.</summary>
	private const int BombRingLoss = 10;

	// The player touches a ring or a bomb when the angles differ by less than this, in original angle units
	// (s2disasm: loc_35036 and the bomb code before Obj61_TestCollision)
	private const int RingTouchAngle = 10;

	private const int BombTouchAngle = 8;

	/// <summary>Converts pixels in the pipe of the original (radius $6E) to track units (radius 4).</summary>
	private const double PixelsToUnits = PipeRadius / HalfPipePlayer.InnerRadius;

	/// <summary>The track nodes ahead of the player where the emerald appears: depth $36 at 4 depth units per track frame.</summary>
	private const int EmeraldAppearOffset = 0x36 * HalfPipeTrackBuilder.NodesPerTrackFrame / 4;

	/// <summary>The track nodes between the player and the won emerald.</summary>
	private const int EmeraldStopOffset = 3;

	/// <summary>After a failed emerald test, the track moves one node each 12 ticks (60 ticks for each track frame).</summary>
	private const int SlowTrackTicks = 12;

	/// <summary>The music fades out over this number of ticks.</summary>
	private const int MusicFadeTicks = 30;

	private const double EmeraldSize = 1.2;

	/// <summary>
	/// The light, middle and dark colour of the emerald of each stage, from art/palettes/SS Emerald.bin in the Sonic 2
	/// disassembly. Mega Drive colours: 0BGR, 3 bits for each channel.
	/// </summary>
	private static readonly ushort[,] EmeraldColours =
	{
		{ 0x0EE8, 0x0EC0, 0x0E80 },
		{ 0x0E6A, 0x0C08, 0x0806 },
		{ 0x0A6E, 0x000E, 0x000A },
		{ 0x0EAE, 0x0E6E, 0x0E4E },
		{ 0x0AEE, 0x00CE, 0x008A },
		{ 0x00EA, 0x00C6, 0x0082 },
		{ 0x0CCC, 0x0AAA, 0x0888 },
	};

	// Sprite sizes in track units. The pipe radius is 4. Sonic is 0.36 of the radius, as in the original (40 of 110 pixels).
	private const double SonicHeight = 1.45;

	/// <summary>Tails is smaller than Sonic, as in the art (227 against 264 pixels).</summary>
	private const double TailsHeight = SonicHeight * 227.0 / 264.0;

	/// <summary>A player in the outer lane shows this far behind a player in the inner lane, nearer to the camera.</summary>
	private const double OuterLaneDepth = 1.0;

	private const double RingSize = 0.7;

	private const double BombSize = 0.75;

	private const double EffectSize = 0.75;

	private const double ShadowWidth = 0.75;

	/// <summary>Sprites stand this far above the surface, so the surface does not hide them.</summary>
	private const double SurfaceGap = 0.05;

	private readonly SonicOrcaGameContext _gameContext;

	private readonly IGraphicsContext _graphicsContext;

	private readonly ResourceSession _resourceSession;

	private readonly HalfPipePlayer _player = new HalfPipePlayer();

	/// <summary>Tails, or null when Sonic plays alone.</summary>
	private readonly HalfPipePlayer _tails;

	private readonly HalfPipeTailsCpu _tailsCpu = new HalfPipeTailsCpu();

	/// <summary>
	/// The lanes of the players (SS_Swap_Positions_Flag). When false, Sonic is in the inner lane and Tails in the outer
	/// lane. A jump changes it. A bomb that hurts a player puts that player in the outer lane.
	/// </summary>
	private bool _lanesSwapped;

	private readonly int _stage;

	private Font _font;

	private ITexture _backgroundTexture;

	private ITexture _floorTexture;

	private ITexture _bombTexture;

	private ITexture _shadowTexture;

	private ITexture _ringsLabelTexture;

	private AnimationInstance _sonicAnimation;

	private AnimationInstance _tailsAnimation;

	private ITexture _sonicLabelTexture;

	private ITexture _tailsLabelTexture;

	private ITexture _totalLabelTexture;

	private AnimationInstance _ringAnimation;

	private AnimationGroup _ringEffectAnimation;

	private AnimationGroup _explosionAnimation;

	private SampleInfo _musicSampleInfo;

	private Sample _ringSound;

	private Sample _jumpSound;

	private Sample _ringLossSound;

	private Sample _bombSound;

	private Sample _checkpointSound;

	private Sample _errorSound;

	private Sample _tallySound;

	private Sample _tallyEndSound;

	private Sample _emeraldJingle;

	private ITexture _emeraldTexture;

	private HalfPipeStageFlow _flow;

	private HalfPipeResults _results;

	/// <summary>All rings collected, also those that bombs took away, for the perfect bonus.</summary>
	private int _ringsCollected;

	private int _musicFadeTicksLeft;

	private Matrix4 _projectionMatrix;

	private Matrix4 _modelViewMatrix;

	private VertexBuffer _vertexBuffer;

	private ManagedShaderProgram _pipeShader;

	private ManagedShaderProgram _objectShader;

	private SampleInstance _musicInstance;

	private double _fov = 60.0;

	private int _currentTrackNode;

	private HalfPipeStageData _stageData;

	private HalfPipeTrack _track;

	private List<HalfPipeObject> _objects;

	private List<HalfPipeCheckpoint> _checkpoints;

	/// <summary>The rings of Sonic. Tails has separate rings, and the goals use the total.</summary>
	private int _rings;

	private int _tailsRings;

	private const double PipeRadius = 4.0;

	/// <summary>
	/// The original draws the pipe and the positions on it 0.8 times as wide as high (SSAnglePos multiplies X by
	/// $CC / $100). The sprites keep their shape. On a 4:3 screen this gives a path of about 0.46 screen heights to
	/// each side, so the player crosses the same part of the screen in the same time as in the original.
	/// </summary>
	private const double PipeWidthScale = 0xCC / 256.0;

	private const int NumPointsWide = 32;

	private int _ticks;

	private Vector3 _camera;

	private Vector3 _cameraTarget;

	/// <param name="stage">The special stage number, from 0 to 6. It selects the ring quotas.</param>
	/// <param name="withTails">True to play with Tails. The ring quotas are then those of Sonic and Tails.</param>
	public HalfPipeSpecialStageScreen(SonicOrcaGameContext gameContext, int stage = 0, bool withTails = false)
	{
		_stage = stage;
		if (withTails)
		{
			_tails = new HalfPipePlayer(outerLane: true);
		}
		_gameContext = gameContext;
		_graphicsContext = _gameContext.Window.GraphicsContext;
		_resourceSession = new ResourceSession(gameContext.ResourceTree);
	}

	/// <summary>The rings that the players have together.</summary>
	public int Rings => _rings + _tailsRings;

	/// <summary>True when Tails plays with Sonic.</summary>
	public bool WithTails => _tails != null;

	/// <summary>True when the player won the emerald.</summary>
	public bool Cleared => _flow?.EmeraldWon ?? false;

	/// <summary>The results of the stage, after it ended. Null while the stage runs.</summary>
	public HalfPipeResults Results => _results;

	private int PlayerTrackNode => Math.Min(_currentTrackNode + PlayerLead, _track.Nodes.Count - 1);

	/// <summary>True when the stage uses an imported original layout, not the generated one.</summary>
	public bool UsesOriginalLayout { get; private set; }

	public override async Task LoadAsync(ScreenLoadingProgress progress, CancellationToken ct = default)
	{
		string[] keys =
		{
			FontResourceKey, BackgroundResourceKey, FloorResourceKey, SonicAnimationResourceKey, RingAnimationResourceKey,
			RingEffectAnimationResourceKey, ExplosionAnimationResourceKey, BombResourceKey, ShadowResourceKey,
			RingsLabelResourceKey, MusicResourceKey, RingSoundResourceKey, JumpSoundResourceKey, RingLossSoundResourceKey,
			BombSoundResourceKey, CheckpointSoundResourceKey, ErrorSoundResourceKey, TallySoundResourceKey,
			TallyEndSoundResourceKey, EmeraldMusicResourceKey,
		};
		foreach (string key in keys)
		{
			_resourceSession.PushDependency(key);
		}
		if (_tails != null)
		{
			foreach (string key in new[] { TailsAnimationResourceKey, SonicLabelResourceKey, TailsLabelResourceKey, TotalLabelResourceKey })
			{
				_resourceSession.PushDependency(key);
			}
		}
		// The original layouts are not in the game data. A mod can add them (see tools/s2hd-data).
		string stageKey = StageResourceKeyPrefix + (_stage + 1);
		bool hasStage = _gameContext.ResourceTree[stageKey]?.Resource != null;
		if (hasStage)
		{
			_resourceSession.PushDependency(stageKey);
		}
		await _resourceSession.LoadAsync(ct);
		ResourceTree tree = _gameContext.ResourceTree;
		if (hasStage)
		{
			_stageData = HalfPipeStageData.FromXml(tree.GetLoadedResource<XmlLoadedResource>(stageKey).XmlDocument);
			UsesOriginalLayout = true;
		}
		_font = tree.GetLoadedResource<Font>(FontResourceKey);
		_backgroundTexture = tree.GetLoadedResource<ITexture>(BackgroundResourceKey);
		_floorTexture = tree.GetLoadedResource<ITexture>(FloorResourceKey);
		_bombTexture = tree.GetLoadedResource<ITexture>(BombResourceKey);
		_shadowTexture = tree.GetLoadedResource<ITexture>(ShadowResourceKey);
		_ringsLabelTexture = tree.GetLoadedResource<ITexture>(RingsLabelResourceKey);
		_sonicAnimation = new AnimationInstance(tree, SonicAnimationResourceKey);
		_ringAnimation = new AnimationInstance(tree, RingAnimationResourceKey);
		_ringEffectAnimation = tree.GetLoadedResource<AnimationGroup>(RingEffectAnimationResourceKey);
		_explosionAnimation = tree.GetLoadedResource<AnimationGroup>(ExplosionAnimationResourceKey);
		_musicSampleInfo = tree.GetLoadedResource<SampleInfo>(MusicResourceKey);
		_ringSound = tree.GetLoadedResource<Sample>(RingSoundResourceKey);
		_jumpSound = tree.GetLoadedResource<Sample>(JumpSoundResourceKey);
		_ringLossSound = tree.GetLoadedResource<Sample>(RingLossSoundResourceKey);
		_bombSound = tree.GetLoadedResource<Sample>(BombSoundResourceKey);
		_checkpointSound = tree.GetLoadedResource<Sample>(CheckpointSoundResourceKey);
		_errorSound = tree.GetLoadedResource<Sample>(ErrorSoundResourceKey);
		_tallySound = tree.GetLoadedResource<Sample>(TallySoundResourceKey);
		_tallyEndSound = tree.GetLoadedResource<Sample>(TallyEndSoundResourceKey);
		_emeraldJingle = tree.GetLoadedResource<Sample>(EmeraldMusicResourceKey);
		if (_tails != null)
		{
			_tailsAnimation = new AnimationInstance(tree, TailsAnimationResourceKey);
			_sonicLabelTexture = tree.GetLoadedResource<ITexture>(SonicLabelResourceKey);
			_tailsLabelTexture = tree.GetLoadedResource<ITexture>(TailsLabelResourceKey);
			_totalLabelTexture = tree.GetLoadedResource<ITexture>(TotalLabelResourceKey);
		}
	}

	public override void Initialise()
	{
		_pipeShader = OrcaShader.CreateFromFile(_graphicsContext, "shaders/halfpipe/test.shader");
		_objectShader = OrcaShader.CreateFromFile(_graphicsContext, "shaders/halfpipe/object.shader");
		_vertexBuffer = _graphicsContext.CreateVertexBuffer(3, 2);
		_projectionMatrix = Matrix4.Perspective(MathX.ToRadians(_fov), 16.0 / 9.0, 1.0, 1024.0);
		_track = HalfPipeTrackBuilder.Build(_stageData ?? HalfPipeLayout.CreateGenerated(), HalfPipeLayout.GetRingQuotas(_stage, duo: _tails != null));
		_objects = _track.Objects;
		_checkpoints = _track.Checkpoints;
		_flow = new HalfPipeStageFlow(_track);
		_emeraldTexture = CreateEmeraldTexture(_graphicsContext, _stage);
	}

	public override void Update()
	{
		KeyboardState keyboard = _gameContext.Input.Pressed.Keyboard;
		if (keyboard[ScancodeEscape])
		{
			Finish();
			return;
		}
		if (keyboard[ScancodeEquals])
		{
			_fov++;
		}
		else if (keyboard[ScancodeMinus])
		{
			_fov--;
		}
		if (_ticks == 0)
		{
			_musicInstance = new SampleInstance(_gameContext, _musicSampleInfo);
			_musicInstance.Play();
		}

		UpdateMusicFade();
		if (_results != null)
		{
			UpdateResults();
		}
		else
		{
			UpdatePlayer();
			// The track moves one node each tick (a track frame each 5 ticks), until the player reaches the end
			bool moves = !_flow.TrackSlowed || _ticks % SlowTrackTicks == 0;
			if (moves && PlayerTrackNode < _track.Nodes.Count - 1)
			{
				_currentTrackNode++;
			}
			UpdateObjects();
			UpdateFlow();
		}

		_ringAnimation.Animate();
		foreach (HalfPipeObject used in _objects)
		{
			if (used.Used)
			{
				used.UsedTicks++;
			}
		}
		UpdateCamera();
		_ticks++;
	}

	private void UpdatePlayer()
	{
		// From the emerald on, only START has an effect
		bool masked = _flow.InputMasked;
		HalfPipeInput sonicInput = ReadInput(0, masked, out _);
		// Sonic records the input for Tails first, so Tails does what Sonic did 16 frames before
		_tailsCpu.Record(sonicInput);
		_player.OuterLane = _lanesSwapped;
		if (_player.Update(sonicInput.Left, sonicInput.Right, sonicInput.JumpPressed))
		{
			OnJump();
		}
		Animate(_player, _sonicAnimation);
		if (_tails == null)
		{
			return;
		}
		HalfPipeInput pad2 = ReadInput(1, masked, out bool pad2Held);
		// The computer controls Tails only on the ground. In the air, Tails uses only the second pad.
		HalfPipeInput tailsInput = _tails.OnGround && !_tails.IsHurt ? _tailsCpu.GetGroundInput(pad2, pad2Held) : pad2;
		_tails.OuterLane = !_lanesSwapped;
		if (_tails.Update(tailsInput.Left, tailsInput.Right, tailsInput.JumpPressed))
		{
			OnJump();
		}
		Animate(_tails, _tailsAnimation);
	}

	/// <param name="held">True while a direction or a jump button is held.</param>
	private HalfPipeInput ReadInput(int pad, bool masked, out bool held)
	{
		Controller current = _gameContext.Current[pad];
		Controller pressed = _gameContext.Pressed[pad];
		held = !masked && (current.DirectionLeft.Length > 0.5 || current.Action1 || current.Action2 || current.Action3);
		return masked ? default : new HalfPipeInput(current.DirectionLeft.X < -0.5, current.DirectionLeft.X > 0.5, pressed.Action1);
	}

	private void OnJump()
	{
		_gameContext.Audio.PlaySound(_jumpSound);
		// With two players, each jump changes the lanes
		if (_tails != null)
		{
			_lanesSwapped = !_lanesSwapped;
		}
	}

	private static void Animate(HalfPipePlayer player, AnimationInstance animation)
	{
		// The ball while jumping. A fall from the top keeps the running animation.
		int index = player.IsJumping ? 1 : 0;
		if (animation.Index != index)
		{
			animation.Index = index;
		}
		animation.Animate();
	}

	/// <summary>Collects the rings and hits the bombs that the players touch.</summary>
	private void UpdateObjects()
	{
		int playerNode = PlayerTrackNode;
		foreach (HalfPipeObject item in _objects)
		{
			// The window of three nodes is about the 2.5 frames during which an object of the original can collide
			if (item.Used || Math.Abs(item.TrackNode - playerNode) > 1)
			{
				continue;
			}
			// The original tests the player in the inner lane first, so that player gets an object that both touch
			if (_tails != null && _tails.Radius < _player.Radius)
			{
				if (!Touch(item, _tails))
				{
					Touch(item, _player);
				}
			}
			else if (!Touch(item, _player) && _tails != null)
			{
				Touch(item, _tails);
			}
		}
	}

	/// <summary>Collects the ring or hits the bomb when the player touches it. Returns true when the player touched it.</summary>
	private bool Touch(HalfPipeObject item, HalfPipePlayer player)
	{
		// Only a player on the ground who is not hurt can touch objects
		if (!player.OnGround || player.IsHurt)
		{
			return false;
		}
		int window = item.Kind == HalfPipeObjectKind.Ring ? RingTouchAngle : BombTouchAngle;
		if (((player.Angle - item.OriginalAngle + window) & 0xFF) >= 2 * window)
		{
			return false;
		}
		item.Used = true;
		bool isTails = player == _tails;
		if (item.Kind == HalfPipeObjectKind.Ring)
		{
			if (isTails)
			{
				_tailsRings++;
			}
			else
			{
				_rings++;
			}
			_ringsCollected++;
			_gameContext.Audio.PlaySound(_ringSound);
			return true;
		}
		_gameContext.Audio.PlaySound(_bombSound);
		if (player.Hurt())
		{
			// A flashing player still makes the bomb explode, but takes no damage. The hurt player moves to the
			// outer lane, and loses only its own rings.
			_lanesSwapped = !isTails;
			int lost = Math.Min(isTails ? _tailsRings : _rings, BombRingLoss);
			if (isTails)
			{
				_tailsRings -= lost;
			}
			else
			{
				_rings -= lost;
			}
			if (lost > 0)
			{
				_gameContext.Audio.PlaySound(_ringLossSound);
			}
		}
		return true;
	}

	private void UpdateFlow()
	{
		foreach (HalfPipeFlowEvent flowEvent in _flow.Update(PlayerTrackNode, Rings, EmeraldAppearOffset))
		{
			switch (flowEvent)
			{
				case HalfPipeFlowEvent.CheckpointPassed:
					_gameContext.Audio.PlaySound(_checkpointSound);
					break;
				case HalfPipeFlowEvent.CheckpointFailed:
					_gameContext.Audio.PlaySound(_errorSound);
					_musicFadeTicksLeft = MusicFadeTicks;
					break;
				case HalfPipeFlowEvent.EmeraldNear:
					_musicFadeTicksLeft = MusicFadeTicks;
					break;
				case HalfPipeFlowEvent.EmeraldWon:
					_musicInstance?.Stop();
					_musicInstance?.Dispose();
					_musicInstance = new SampleInstance(_gameContext, _emeraldJingle);
					_musicInstance.Play();
					break;
				case HalfPipeFlowEvent.EmeraldMissed:
					_gameContext.Audio.PlaySound(_errorSound);
					break;
			}
		}
		if (_flow.Finished)
		{
			_results = new HalfPipeResults(Rings, _flow.EmeraldWon, _ringsCollected, _track.RingCount);
		}
	}

	private void UpdateResults()
	{
		if (_results.Update())
		{
			// The tally sound on every fourth step
			if (_ticks % 4 == 0)
			{
				_gameContext.Audio.PlaySound(_tallySound);
			}
		}
		else if (_results.TallyJustEnded)
		{
			_gameContext.Audio.PlaySound(_tallyEndSound);
		}
		if (_results.Finished)
		{
			Finish();
		}
	}

	private void UpdateMusicFade()
	{
		if (_musicFadeTicksLeft <= 0 || _musicInstance == null)
		{
			return;
		}
		_musicFadeTicksLeft--;
		_musicInstance.Volume = _musicFadeTicksLeft / (double)MusicFadeTicks;
		if (_musicFadeTicksLeft == 0)
		{
			_musicInstance.Stop();
		}
	}

	/// <summary>Draws a cut gem in the three colours of the stage's emerald, because the game data has no emerald art.</summary>
	private static ITexture CreateEmeraldTexture(IGraphicsContext graphicsContext, int stage)
	{
		const int Size = 64;
		var pixels = new byte[Size * Size * 4];
		Colour light = ToColour(EmeraldColours[stage, 0]);
		Colour middle = ToColour(EmeraldColours[stage, 1]);
		Colour dark = ToColour(EmeraldColours[stage, 2]);
		for (int y = 0; y < Size; y++)
		{
			for (int x = 0; x < Size; x++)
			{
				// A wide diamond: a flat top part, then a point at the bottom
				double u = (x + 0.5) / Size * 2.0 - 1.0;
				double v = (y + 0.5) / Size;
				bool inside = v < 0.35 ? Math.Abs(u) <= 0.6 + v / 0.35 * 0.4 : Math.Abs(u) <= (1.0 - v) / 0.65;
				if (!inside)
				{
					continue;
				}
				Colour colour = v < 0.35 ? (Math.Abs(u) < 0.35 ? light : middle) : (u < 0.0 ? middle : dark);
				int i = (y * Size + x) * 4;
				pixels[i] = colour.Red;
				pixels[i + 1] = colour.Green;
				pixels[i + 2] = colour.Blue;
				pixels[i + 3] = 255;
			}
		}
		return graphicsContext.CreateTexture(Size, Size, 4, pixels);
	}

	private static Colour ToColour(ushort megaDrive)
	{
		static byte Channel(int value) => (byte)(value * 255 / 14);
		return new Colour(255, Channel(megaDrive & 0xE), Channel((megaDrive >> 4) & 0xE), Channel((megaDrive >> 8) & 0xE));
	}

	public override void Unload()
	{
		_musicInstance?.Stop();
		_musicInstance?.Dispose();
		_musicInstance = null;
		_resourceSession.Dispose();
	}

	public override void Deinitialise()
	{
		_vertexBuffer?.Dispose();
		_emeraldTexture?.Dispose();
		_pipeShader?.Dispose();
		_objectShader?.Dispose();
	}

	private void UpdateCamera()
	{
		HalfPipeTrackNode node = _track.Nodes[_currentTrackNode];
		_camera = node.Position + new Vector3(0.0, CameraHeight, 0.0);
		double pitch = node.Pitch - MathX.ToRadians(CameraTilt);
		_cameraTarget = _camera + new Vector3(Math.Sin(node.Yaw) * Math.Cos(pitch), Math.Sin(pitch), Math.Cos(node.Yaw) * Math.Cos(pitch));
	}

	public override void Draw(Renderer renderer)
	{
		SonicOrca.Core.Debugging.TestHooks.DrawnLevelTick = SonicOrca.Core.Debugging.TestHooks.SpecialStageCaptureOffset + _ticks;
		DrawBackground(renderer);
		DrawScene(renderer);
		DrawHud(renderer);
	}

	private void DrawBackground(Renderer renderer)
	{
		I2dRenderer i2dRenderer = renderer.Get2dRenderer();
		i2dRenderer.BlendMode = BlendMode.Alpha;
		i2dRenderer.Colour = Colours.White;
		i2dRenderer.ClipRectangle = new Rectangle(0.0, 0.0, 1920.0, 1080.0);
		i2dRenderer.RenderTexture(_backgroundTexture, new Rectangle(0.0, 0.0, 1920.0, 1080.0));
		i2dRenderer.Deactivate();
	}

	private void DrawScene(Renderer renderer)
	{
		_modelViewMatrix = Matrix4.LookAt(_camera.X, _camera.Y, 0.0 - _camera.Z, _cameraTarget.X, _cameraTarget.Y, 0.0 - _cameraTarget.Z, 0.0, 1.0, 0.0);
		_modelViewMatrix *= Matrix4.CreateScale(1.0, 1.0, -1.0);
		_graphicsContext.ClearDepthBuffer();
		_graphicsContext.DepthTesting = true;
		_graphicsContext.BlendMode = BlendMode.Alpha;
		List<TexturedQuad> quads = new List<TexturedQuad>();
		AddPipeQuads(quads);
		AddObjectQuads(quads);
		AddPlayerQuads(quads, _player, _sonicAnimation, SonicHeight);
		if (_tails != null)
		{
			AddPlayerQuads(quads, _tails, _tailsAnimation, TailsHeight);
		}
		AddEmeraldQuad(quads);

		// Draw the quads from back to front. Group the quads that use the same texture into one draw call.
		List<Tuple<int, ITexture>> batches = new List<Tuple<int, ITexture>>();
		int batchCount = 0;
		ITexture texture = null;
		_vertexBuffer.Begin();
		foreach (TexturedQuad quad in quads.OrderByDescending(x => (_camera - x.Position[0]).Length))
		{
			for (int corner = 0; corner < 4; corner++)
			{
				_vertexBuffer.AddValue(0, quad.Position[corner]);
				_vertexBuffer.AddValue(1, quad.TextureMapping[corner]);
			}
			if (batchCount == 0)
			{
				texture = quad.Texture;
			}
			else if (texture != quad.Texture)
			{
				batches.Add(new Tuple<int, ITexture>(batchCount, texture));
				batchCount = 0;
				texture = quad.Texture;
			}
			batchCount++;
		}
		if (batchCount > 0)
		{
			batches.Add(new Tuple<int, ITexture>(batchCount, texture));
		}
		_vertexBuffer.End();
		IShaderProgram program = _pipeShader.Program;
		program.Activate();
		program.SetUniform("ProjectionMatrix", _projectionMatrix);
		program.SetUniform("ModelViewMatrix", _modelViewMatrix);
		program.SetUniform("InputTexture", 0);
		program.SetUniform("InputLightSource", _track.Nodes[_currentTrackNode].Position);
		int firstQuad = 0;
		foreach (Tuple<int, ITexture> batch in batches)
		{
			_graphicsContext.SetTexture(batch.Item2);
			_vertexBuffer.Render(PrimitiveType.Quads, firstQuad * 4, batch.Item1 * 4);
			firstQuad += batch.Item1;
		}
		_graphicsContext.DepthTesting = false;
	}

	private Matrix4 GetNodeTransform(int trackNode)
	{
		HalfPipeTrackNode node = _track.Nodes[trackNode];
		return Matrix4.Identity.RotateY(node.Yaw).Translate(node.Position);
	}

	private void AddPipeQuads(List<TexturedQuad> quads)
	{
		double textureScale = 8.0 / _floorTexture.Width;
		// The number of track nodes that one repeat of the floor texture covers.
		int textureRepeatNodes = (int)(_floorTexture.Height * textureScale / HalfPipeTrackBuilder.NodeSpacing);
		Vector3[] previousSection = null;
		int first = Math.Max(0, _currentTrackNode - DrawDistanceBehind);
		int last = Math.Min(_track.Nodes.Count - 1, _currentTrackNode + DrawDistanceAhead);
		for (int i = first; i <= last; i++)
		{
			Matrix4 transform = GetNodeTransform(i);
			Vector3[] section = GetCrossSection().Select(x => transform * x).ToArray();
			// Join this cross section to the previous one with floor quads.
			if (previousSection != null)
			{
				int textureRow = i % textureRepeatNodes;
				for (int j = 0; j < section.Length - 1; j++)
				{
					quads.Add(new TexturedQuad(new[]
					{
						section[j],
						previousSection[j],
						previousSection[j + 1],
						section[j + 1]
					}, new[]
					{
						new Vector2(j / (double)(section.Length - 1), 1.0 - (textureRow + 1) / (double)textureRepeatNodes),
						new Vector2(j / (double)(section.Length - 1), 1.0 - textureRow / (double)textureRepeatNodes),
						new Vector2((j + 1) / (double)(section.Length - 1), 1.0 - textureRow / (double)textureRepeatNodes),
						new Vector2((j + 1) / (double)(section.Length - 1), 1.0 - (textureRow + 1) / (double)textureRepeatNodes)
					}, _floorTexture));
				}
			}
			previousSection = section;
		}
	}

	private void AddObjectQuads(List<TexturedQuad> quads)
	{
		int first = _currentTrackNode;
		int last = Math.Min(_track.Nodes.Count - 1, _currentTrackNode + ObjectDrawDistance);
		foreach (HalfPipeObject item in _objects)
		{
			if (item.TrackNode < first || item.TrackNode > last)
			{
				continue;
			}
			Matrix4 transform = GetNodeTransform(item.TrackNode);
			if (!item.Used)
			{
				if (item.Kind == HalfPipeObjectKind.Ring)
				{
					quads.Add(CreateSprite(transform, item.Angle, 0.0, RingSize, RingSize, _ringAnimation.CurrentTexture, _ringAnimation.CurrentFrame.Source));
				}
				else
				{
					quads.Add(CreateSprite(transform, item.Angle, 0.0, BombSize, BombSize, _bombTexture, new Rectanglei(0, 0, _bombTexture.Width, _bombTexture.Height)));
				}
				continue;
			}
			// A used ring sparkles and a used bomb explodes, once through the animation
			AnimationGroup effect = item.Kind == HalfPipeObjectKind.Ring ? _ringEffectAnimation : _explosionAnimation;
			Animation animation = effect[0];
			int frame = item.UsedTicks / Math.Max(1, animation.Frames[0].Delay);
			if (frame < animation.Frames.Count)
			{
				Animation.Frame effectFrame = animation.Frames[frame];
				quads.Add(CreateSprite(transform, item.Angle, 0.0, EffectSize, EffectSize, effect.Textures[effectFrame.TextureIndex], effectFrame.Source));
			}
		}
	}

	private void AddEmeraldQuad(List<TexturedQuad> quads)
	{
		if (!_flow.EmeraldVisible)
		{
			return;
		}
		// The emerald stays where it appeared until it reaches the player. A won emerald then stays in front of the
		// player and bobs: -1, 0, +1, 0 pixels, 4 frames each.
		int node = Math.Max(_flow.EmeraldAppearNode, PlayerTrackNode + EmeraldStopOffset);
		node = Math.Min(node, _track.Nodes.Count - 1);
		double bob = _flow.EmeraldWon ? new[] { -1, 0, 1, 0 }[_flow.EmeraldWonFrames / 4 % 4] * PixelsToUnits : 0.0;
		// The emerald floats at 3/4 of the pipe radius from the centre
		double height = PipeRadius / 4.0 + bob;
		quads.Add(CreateSprite(GetNodeTransform(node), 0.0, height, EmeraldSize, EmeraldSize, _emeraldTexture,
			new Rectanglei(0, 0, _emeraldTexture.Width, _emeraldTexture.Height)));
	}

	private void AddPlayerQuads(List<TexturedQuad> quads, HalfPipePlayer player, AnimationInstance animation, double spriteHeight)
	{
		Matrix4 transform = GetNodeTransform(PlayerTrackNode);
		(double x, double y) = player.Position;
		double distance = Math.Sqrt(x * x + y * y);
		double angle = HalfPipePlayer.ToRadiansFromBottom(player.Angle);
		// The outer lane is nearer to the camera, a little behind the inner lane
		double lane = (player.Radius - HalfPipePlayer.InnerRadius) / (double)(HalfPipePlayer.OuterRadius - HalfPipePlayer.InnerRadius);
		double depth = -lane * OuterLaneDepth;
		if (player.InAir)
		{
			// The shadow falls on the wall below the player
			double shadowAngle = Math.Asin(Math.Clamp(x / player.Radius, -1.0, 1.0));
			double shadowHeight = ShadowWidth * _shadowTexture.Height / _shadowTexture.Width;
			quads.Add(CreateSprite(transform, shadowAngle, 0.0, ShadowWidth, shadowHeight, _shadowTexture, new Rectanglei(0, 0, _shadowTexture.Width, _shadowTexture.Height), depth: depth));
		}
		// The player shows on alternate frames while flashing after a bomb
		if (player.FlashFramesLeft > 0 && _ticks % 2 == 0)
		{
			return;
		}
		Rectanglei source = animation.CurrentFrame.Source;
		double width = spriteHeight * source.Width / source.Height;
		// In the air the player is nearer to the centre than the surface
		double height = Math.Max(0.0, (player.Radius - distance) * PixelsToUnits);
		// The hurt animation turns the sprite once
		double spin = player.IsHurt ? player.HurtProgress * 2.0 * Math.PI : 0.0;
		quads.Add(CreateSprite(transform, angle, height, width, spriteHeight, animation.CurrentTexture, source, spin, depth));
	}

	/// <summary>
	/// Makes an upright sprite that stands on the pipe surface at an angle from the bottom, and faces along the track.
	/// The sprite leans with the surface, so that its bottom edge is on the surface and its top points to the centre.
	/// </summary>
	private static TexturedQuad CreateSprite(Matrix4 transform, double angle, double height, double width, double spriteHeight,
		ITexture texture, Rectanglei source, double spin = 0.0, double depth = 0.0)
	{
		// Theta is the angle of the cross section: 0 at the left edge, pi / 2 at the bottom, pi at the right edge
		double theta = Math.PI / 2.0 + angle;
		Vector3 surface = new Vector3(-Math.Cos(theta) * PipeRadius, -Math.Sin(theta) * PipeRadius, 0.0);
		Vector3 up = new Vector3(Math.Cos(theta), Math.Sin(theta), 0.0);
		Vector3 right = new Vector3(Math.Sin(theta), -Math.Cos(theta), 0.0);
		// Depth moves the sprite along the track: a negative depth is nearer to the camera
		Vector3 bottomCentre = surface + up * (SurfaceGap + height) + new Vector3(0.0, 0.0, depth);
		// The positions are narrower, as in the original, but the sprites keep their shape
		bottomCentre = new Vector3(bottomCentre.X * PipeWidthScale, bottomCentre.Y, bottomCentre.Z);
		if (spin != 0.0)
		{
			// Turn the sprite in its own plane, about its centre
			Vector3 centre = bottomCentre + up * (spriteHeight / 2.0);
			Vector3 turnedUp = up * Math.Cos(spin) + right * Math.Sin(spin);
			right = right * Math.Cos(spin) - up * Math.Sin(spin);
			up = turnedUp;
			bottomCentre = centre - up * (spriteHeight / 2.0);
		}
		Vector3 halfWidth = right * (width / 2.0);
		Vector3 top = up * spriteHeight;
		double u0 = source.X / (double)texture.Width;
		double u1 = (source.X + source.Width) / (double)texture.Width;
		double vTop = source.Y / (double)texture.Height;
		double vBottom = (source.Y + source.Height) / (double)texture.Height;
		return new TexturedQuad(new[]
		{
			transform * (bottomCentre - halfWidth),
			transform * (bottomCentre - halfWidth + top),
			transform * (bottomCentre + halfWidth + top),
			transform * (bottomCentre + halfWidth)
		}, new[]
		{
			new Vector2(u0, vBottom),
			new Vector2(u0, vTop),
			new Vector2(u1, vTop),
			new Vector2(u1, vBottom)
		}, texture);
	}

	private Vector3[] GetCrossSection()
	{
		// A half circle of points from the left side of the pipe, through the bottom, to the right side.
		Vector3[] points = new Vector3[NumPointsWide];
		for (int i = 0; i < points.Length; i++)
		{
			double angle = i / 31.0 * Math.PI;
			points[i] = new Vector3((0.0 - Math.Cos(angle)) * PipeRadius * PipeWidthScale, (0.0 - Math.Sin(angle)) * PipeRadius, 0.0);
		}
		return points;
	}

	private void DrawHud(Renderer renderer)
	{
		I2dRenderer g = renderer.Get2dRenderer();
		g.BlendMode = BlendMode.Alpha;
		g.Colour = Colours.White;
		if (_results != null)
		{
			// The results: a dark screen with the bonuses
			g.RenderQuad(new Colour(200, 0, 0, 40), new Rectangle(0.0, 0.0, 1920.0, 1080.0));
			g.Deactivate();
			DrawResults(renderer.GetFontRenderer());
			return;
		}
		IFontRenderer fontRenderer;
		double counterY = 160.0;
		if (_tails == null)
		{
			g.RenderTexture(_ringsLabelTexture, new Rectangle(48.0, 40.0, _ringsLabelTexture.Width, _ringsLabelTexture.Height));
			g.Deactivate();
			fontRenderer = renderer.GetFontRenderer();
			fontRenderer.RenderString(_rings.ToString(), new Rectangle(260.0, 48.0, 0.0, 0.0), FontAlignment.Left, _font, 0);
		}
		else
		{
			// As in the original: the rings of Sonic on the left, the total in the centre, the rings of Tails on the right
			g.RenderTexture(_sonicLabelTexture, new Rectangle(48.0, 40.0, _sonicLabelTexture.Width, _sonicLabelTexture.Height));
			g.RenderTexture(_totalLabelTexture, new Rectangle(960.0 - _totalLabelTexture.Width / 2.0, 24.0, _totalLabelTexture.Width, _totalLabelTexture.Height));
			double tailsX = 1920.0 - 48.0 - _tailsLabelTexture.Width;
			g.RenderTexture(_tailsLabelTexture, new Rectangle(tailsX, 40.0, _tailsLabelTexture.Width, _tailsLabelTexture.Height));
			g.Deactivate();
			fontRenderer = renderer.GetFontRenderer();
			fontRenderer.RenderString(_rings.ToString(), new Rectangle(48.0 + _sonicLabelTexture.Width / 2.0, 96.0, 0.0, 0.0), FontAlignment.MiddleX, _font, 0);
			// The total goes inside the box of the label
			fontRenderer.RenderString(Rings.ToString(), new Rectangle(960.0, 24.0 + _totalLabelTexture.Height / 2.0 - 16.0, 0.0, 0.0), FontAlignment.MiddleX, _font, 0);
			fontRenderer.RenderString(_tailsRings.ToString(), new Rectangle(tailsX + _tailsLabelTexture.Width / 2.0, 96.0, 0.0, 0.0), FontAlignment.MiddleX, _font, 0);
			counterY = 220.0;
		}
		if (_flow.Counter != null)
		{
			fontRenderer.RenderString(_flow.Counter, new Rectangle(960.0, counterY, 0.0, 0.0), FontAlignment.MiddleX, _font, 0);
		}
		if (_flow.Message != null)
		{
			fontRenderer.RenderString(_flow.Message, new Rectangle(960.0, 440.0, 0.0, 0.0), FontAlignment.MiddleX, _font, 0);
		}
	}

	private void DrawResults(IFontRenderer fontRenderer)
	{
		void Line(string label, int value, double y)
		{
			fontRenderer.RenderString(label, new Rectangle(560.0, y, 0.0, 0.0), FontAlignment.Left, _font, 0);
			fontRenderer.RenderString(value.ToString(), new Rectangle(1360.0, y, 0.0, 0.0), FontAlignment.Right, _font, 0);
		}
		string title = _results.EmeraldWon ? "CHAOS EMERALD" : "SPECIAL STAGE";
		fontRenderer.RenderString(title, new Rectangle(960.0, 240.0, 0.0, 0.0), FontAlignment.MiddleX, _font, 0);
		if (_tails == null)
		{
			Line("SONIC RINGS", _results.Rings, 400.0);
		}
		else
		{
			Line("SONIC RINGS", _rings, 360.0);
			Line("TAILS RINGS", _tailsRings, 420.0);
		}
		Line("RING BONUS", _results.RingBonus, 500.0);
		Line("GEMS BONUS", _results.GemsBonus, 580.0);
		if (_results.PerfectBonus > 0 || _results.Total >= HalfPipeResults.PerfectBonusPoints)
		{
			Line("PERFECT BONUS", _results.PerfectBonus, 660.0);
		}
		Line("TOTAL", _results.Total, 780.0);
	}
}
