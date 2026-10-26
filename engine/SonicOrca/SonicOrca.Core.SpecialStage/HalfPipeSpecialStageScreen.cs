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
	/// <summary>How a quad is lit and blended.</summary>
	private enum QuadStyle
	{
		/// <summary>The light of the pipe: darker far from the camera.</summary>
		Lit,
		/// <summary>Always at full brightness, for the rings and the bombs.</summary>
		Bright,
		/// <summary>The yellow glow of a ring, added to the colours behind it.</summary>
		Glow,
		/// <summary>The red glow of a bomb, added to the colours behind it.</summary>
		BombGlow,
		/// <summary>A shadow on the pipe: it darkens the pipe below it.</summary>
		Shadow
	}

	private struct TexturedQuad
	{
		public Vector3[] Position { get; set; }

		public Vector2[] TextureMapping { get; set; }

		public ITexture Texture { get; set; }

		public QuadStyle Style { get; set; }

		public TexturedQuad(Vector3[] position, Vector2[] textureMapping, ITexture texture, QuadStyle style = QuadStyle.Lit)
		{
			Position = position;
			TextureMapping = textureMapping;
			Texture = texture;
			Style = style;
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

	/// <summary>The glow of the rings, the same texture as the glow of the rings in the levels.</summary>
	private const string GlowResourceKey = "SONICORCA/PARTICLE/GLOW";

	private const string ShadowResourceKey = "SONICORCA/SPECIALSTAGE/SHADOW";

	/// <summary>The round shadow of the rings, the bombs and the emerald.</summary>
	private const string BallShadowResourceKey = "SONICORCA/SPECIALSTAGE/BBSHADOW";

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

	/// <summary>The sound when "PERFECT BONUS" shows (SndID_Signpost).</summary>
	private const string PerfectSoundResourceKey = "SONICORCA/SOUND/SIGNPOST";

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

	/// <summary>
	/// The original runs the logic of the stage (the players, the objects and the messages) once each second frame,
	/// because the drawing of the track takes more than one frame. The results run each frame.
	/// </summary>
	private const int FramesPerLogicTick = 2;

	/// <summary>
	/// The track nodes that the track moves each frame. A track frame is <see cref="HalfPipeTrackBuilder.NodesPerTrackFrame"/>
	/// nodes. The timer of the original asks for a track frame each 5 frames, but the drawing cannot keep up: a
	/// recording of stage 1 shows a new track frame each 8 frames.
	/// </summary>
	private const double TrackNodesPerFrame = HalfPipeTrackBuilder.NodesPerTrackFrame / 8.0;

	/// <summary>After a failed emerald test, the track moves one node each 12 frames (60 frames for each track frame).</summary>
	private const double SlowTrackNodesPerFrame = 1.0 / 12.0;

	/// <summary>The jingle of the results screen: the imported "End of Act" song, else the jingle of the HD game.</summary>
	private const string ResultsJingleResourceKey = "SONICORCA/MUSIC/JINGLE/STAGECLEAR/S2/SAMPLE";

	/// <summary>The frames of the fades from white at the start and to white at the end (Pal_FadeFromWhite, Pal_FadeToWhite).</summary>
	private const int WhiteFadeFrames = 22;

	/// <summary>The START banner falls one pixel each tick to this height, then waits, then breaks (Obj5F).</summary>
	private const int BannerStopY = 0x48;

	private const int BannerStartY = -0x40;

	private const int BannerWaitTicks = 0xF;

	/// <summary>The speed of the pieces of the banner, in pixels each tick.</summary>
	private const int BannerPieceSpeed = 8;

	/// <summary>The ticks that the pieces of the banner fly before they are off the screen.</summary>
	private const int BannerPieceTicks = 40;

	/// <summary>The height of the winged hand of the checkpoint messages, in pixels of the original (Obj5A).</summary>
	private const int HandY = 0x48;

	/// <summary>The speed of the text of the results screen, in pixels each frame (Obj34_MoveTowardsTargetPosition).</summary>
	private const int ResultsTextSpeed = 0x10;

	/// <summary>The music fades out over this number of ticks.</summary>
	private const int MusicFadeTicks = 30;

	private const double EmeraldSize = 1.2;

	/// <summary>
	/// The step of the bob of the won emerald. The original moves the small sprite by one pixel, which is a large part
	/// of its size; this is the same part of the size of the emerald here.
	/// </summary>
	private const double EmeraldBob = EmeraldSize * 0.08;

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

	/// <summary>
	/// The glow behind a ring is this many times the size of the ring. It is small and faint, so that the ring looks
	/// brighter, not like a ring with a halo. The rings are small on the wide screen, so this makes them easier to see
	/// and to follow.
	/// </summary>
	private const double RingGlowScale = 1.6;

	/// <summary>The glow is a little behind the ring, so that it is drawn first.</summary>
	private const double RingGlowDepth = 0.05;

	/// <summary>The lowest light of the pipe, far from the camera. The rings, the bombs and their glows do not get darker.</summary>
	private const double MinimumLight = 0.25;

	/// <summary>The colour of the glow (red, green, blue), the yellow of the ring, and its strength (alpha).</summary>
	private static readonly Vector4 RingGlowColour = new Vector4(1.0, 0.8, 0.2, 0.22);

	/// <summary>The red glow of a bomb, as faint as the glow of a ring.</summary>
	private static readonly Vector4 BombGlowColour = new Vector4(1.0, 0.15, 0.1, 0.22);

	private const double BombSize = 0.75;

	private const double EffectSize = 0.75;

	/// <summary>
	/// The size of the shadow of a player across the pipe, and along the track. As in the original, a shadow is a flat
	/// oval: short along the track.
	/// </summary>
	private const double ShadowWidth = 0.8;

	/// <summary>The room for the ring count of Tails right of the word RINGS, in pixels of the screen.</summary>
	private const double RingCountWidth = 120.0;

	private const double ShadowLength = 0.55;

	/// <summary>The length of the round shadow of an object along the track, for each unit of its width.</summary>
	private const double ObjectShadowSquash = 0.5;

	/// <summary>The darkness of the shadows: the opacity of their black.</summary>
	private const double ShadowOpacity = 0.45;

	/// <summary>The shadows lie this far in front of the pipe surface, so the surface does not hide them.</summary>
	private const double ShadowGap = 0.02;

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

	private ITexture _glowTexture;

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

	private Sample _perfectSound;

	private Sample _emeraldJingle;

	private Sample _resultsJingle;

	private ITexture _ballShadowTexture;

	/// <summary>The art of the original, when a mod adds it, else null.</summary>
	private HalfPipeOriginalArt _originalArt;

	/// <summary>The emeralds that the player has before this stage, and the score, for the results screen.</summary>
	private readonly int _startEmeralds;

	private readonly int _startScore;

	/// <summary>The frames of the fade to white after the end of the stage, or -1 before the end.</summary>
	private int _endFadeFrames = -1;

	/// <summary>The frames since the results screen started.</summary>
	private int _resultsFrames;

	private string _lastMessage;

	/// <summary>
	/// The poses of the players before the last logic tick. The logic runs each second frame, so the frame between two
	/// ticks shows the players halfway between their poses, and the movement looks smooth at 60 frames each second.
	/// </summary>
	private PlayerPose _sonicPreviousPose;

	private PlayerPose _tailsPreviousPose;

	/// <summary>A position of a player in the pipe: from the centre, in pixels, the angle from the bottom and the lane.</summary>
	private readonly record struct PlayerPose(double X, double Y, double Radians, double Radius)
	{
		public static PlayerPose Of(HalfPipePlayer player) =>
			new PlayerPose(player.Position.X, player.Position.Y, HalfPipePlayer.ToRadiansFromBottom(player.Angle), player.Radius);

		public static PlayerPose Lerp(PlayerPose a, PlayerPose b, double t) =>
			new PlayerPose(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t, a.Radians + MathX.DifferenceRadians(a.Radians, b.Radians) * t,
				a.Radius + (b.Radius - a.Radius) * t);
	}

	/// <summary>The logic tick at which the current message started.</summary>
	private int _messageStartTick;

	/// <summary>The letters of the messages that ended, which fly away from the centre of the screen (Obj5A_TextFlyout).</summary>
	private readonly List<FlyingGlyph> _flyingGlyphs = new List<FlyingGlyph>();

	private sealed class FlyingGlyph
	{
		public HalfPipeGlyph Glyph;

		public double X;

		public double Y;

		public double VelocityX;

		public double VelocityY;
	}

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

	/// <summary>The pipe quads between two track nodes.</summary>
	private const int QuadsPerNode = NumPointsWide - 1;

	/// <summary>The quads of the whole pipe, made once. The quads between node i - 1 and node i come at (i - 1) * <see cref="QuadsPerNode"/>.</summary>
	private VertexBuffer _pipeBuffer;

	/// <summary>The logic ticks of the stage: one each <see cref="FramesPerLogicTick"/> frames.</summary>
	private int _ticks;

	/// <summary>The frames since the start of the stage.</summary>
	private int _frames;

	/// <summary>The fraction of a node that the track moved after <see cref="_currentTrackNode"/>, from 0 to 1.</summary>
	private double _trackFraction;

	private Vector3 _camera;

	private Vector3 _cameraTarget;

	/// <param name="stage">The special stage number, from 0 to 6. It selects the ring quotas.</param>
	/// <param name="withTails">True to play with Tails. The ring quotas are then those of Sonic and Tails.</param>
	/// <param name="emeralds">The emeralds that the player has, for the results screen.</param>
	/// <param name="score">The score of the player, for the results screen.</param>
	public HalfPipeSpecialStageScreen(SonicOrcaGameContext gameContext, int stage = 0, bool withTails = false, int emeralds = 0, int score = 0)
	{
		_stage = stage;
		_startEmeralds = emeralds;
		_startScore = score;
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
			RingEffectAnimationResourceKey, ExplosionAnimationResourceKey, BombResourceKey, ShadowResourceKey, BallShadowResourceKey, GlowResourceKey,
			RingsLabelResourceKey, MusicResourceKey, RingSoundResourceKey, JumpSoundResourceKey, RingLossSoundResourceKey,
			BombSoundResourceKey, CheckpointSoundResourceKey, ErrorSoundResourceKey, TallySoundResourceKey,
			TallyEndSoundResourceKey, PerfectSoundResourceKey, EmeraldMusicResourceKey,
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
		// The art of the original and the results jingle are not in the game data. A mod can add them (see tools/s2hd-data).
		ResourceTree resources = _gameContext.ResourceTree;
		bool hasOriginalArt = HalfPipeOriginalArt.IsAvailable(resources);
		if (hasOriginalArt)
		{
			foreach (string key in HalfPipeOriginalArt.ResourceKeys)
			{
				_resourceSession.PushDependency(key);
			}
			if (resources[HalfPipeOriginalArt.HudKey]?.Resource != null)
			{
				_resourceSession.PushDependency(HalfPipeOriginalArt.HudKey);
			}
		}
		string resultsJingleKey = resources[ResultsJingleResourceKey]?.Resource != null ? ResultsJingleResourceKey : EmeraldMusicResourceKey;
		_resourceSession.PushDependency(resultsJingleKey);
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
		_glowTexture = tree.GetLoadedResource<ITexture>(GlowResourceKey);
		_shadowTexture = tree.GetLoadedResource<ITexture>(ShadowResourceKey);
		_ballShadowTexture = tree.GetLoadedResource<ITexture>(BallShadowResourceKey);
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
		_perfectSound = tree.GetLoadedResource<Sample>(PerfectSoundResourceKey);
		_emeraldJingle = tree.GetLoadedResource<Sample>(EmeraldMusicResourceKey);
		_resultsJingle = tree.GetLoadedResource<Sample>(resultsJingleKey);
		_originalArt = hasOriginalArt ? new HalfPipeOriginalArt(tree) : null;
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
		_pipeBuffer = _graphicsContext.CreateVertexBuffer(3, 2);
		FillPipeBuffer();
		_originalArt?.UseSharpPixels();
		if (SonicOrca.Core.Debugging.TestHooks.SpecialStageResults is (int rings, bool emerald, bool perfect))
		{
			_rings = rings;
			StartResults(emerald, perfect ? _track.RingCount : 0);
		}
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
		if (_frames == 0 && _results == null)
		{
			_musicInstance = new SampleInstance(_gameContext, _musicSampleInfo);
			_musicInstance.Play();
		}

		UpdateMusicFade();
		if (_results != null)
		{
			_resultsFrames++;
			UpdateResults();
		}
		else if (_endFadeFrames >= 0)
		{
			// The stage stops while it fades to white, then the results screen starts
			if (++_endFadeFrames >= WhiteFadeFrames)
			{
				StartResults(_flow.EmeraldWon, _ringsCollected);
			}
		}
		else
		{
			MoveTrack();
			if (_frames % FramesPerLogicTick == 0)
			{
				_sonicPreviousPose = PlayerPose.Of(_player);
				if (_tails != null)
				{
					_tailsPreviousPose = PlayerPose.Of(_tails);
				}
				UpdatePlayer();
				UpdateObjects();
				UpdateFlow();
				UpdateFlyingGlyphs();
				if (_flow.Message != _lastMessage)
				{
					if (UsesOriginalText && _lastMessage != null)
					{
						StartFlyout(_lastMessage);
					}
					_lastMessage = _flow.Message;
					_messageStartTick = _ticks;
				}
				_ringAnimation.Animate();
				foreach (HalfPipeObject used in _objects)
				{
					if (used.Used)
					{
						used.UsedTicks++;
					}
				}
				_ticks++;
			}
		}
		UpdateCamera();
		_frames++;
	}

	/// <summary>The messages and the counters use the letters and the big digits of the original, when a mod has them.</summary>
	private bool UsesOriginalText => _originalArt?.Hud != null;

	/// <summary>The letters and digits of a message of the flow, at the places of the original.</summary>
	private static List<HalfPipeGlyph> GetMessageGlyphs(string message)
	{
		if (message == HalfPipeStageFlow.CheckpointPassedMessage || message == HalfPipeStageFlow.CheckpointFailedMessage)
		{
			return HalfPipeMessageText.Checkpoint(message == HalfPipeStageFlow.CheckpointPassedMessage);
		}
		string[] words = message.Split(' ');
		return words.Length == 3 && words[0] == "GET" && int.TryParse(words[1], out int goal) ? HalfPipeMessageText.RingRequirement(goal) : null;
	}

	/// <summary>Each letter of an ended message flies away from the centre of the screen, at 8 pixels a tick.</summary>
	private void StartFlyout(string message)
	{
		foreach (HalfPipeGlyph glyph in GetMessageGlyphs(message) ?? new List<HalfPipeGlyph>())
		{
			double dx = glyph.X - HalfPipeMessageText.CentreX;
			double dy = glyph.Y - HalfPipeMessageText.CentreY;
			// A letter at the centre goes down, as CalcAngle gives for (0, 0)
			double angle = dx == 0 && dy == 0 ? Math.PI / 2.0 : Math.Atan2(dy, dx);
			_flyingGlyphs.Add(new FlyingGlyph
			{
				Glyph = glyph,
				X = glyph.X,
				Y = glyph.Y,
				VelocityX = Math.Cos(angle) * HalfPipeMessageText.FlyoutSpeed,
				VelocityY = Math.Sin(angle) * HalfPipeMessageText.FlyoutSpeed
			});
		}
	}

	private void UpdateFlyingGlyphs()
	{
		foreach (FlyingGlyph glyph in _flyingGlyphs)
		{
			glyph.X += glyph.VelocityX;
			glyph.Y += glyph.VelocityY;
		}
		_flyingGlyphs.RemoveAll(glyph => glyph.X < 0 || glyph.X > 256 || glyph.Y < 0 || glyph.Y > 224);
	}

	/// <summary>Moves the track, until the player reaches the end.</summary>
	private void MoveTrack()
	{
		_trackFraction += _flow.TrackSlowed ? SlowTrackNodesPerFrame : TrackNodesPerFrame;
		while (_trackFraction >= 1.0)
		{
			_trackFraction -= 1.0;
			if (PlayerTrackNode < _track.Nodes.Count - 1)
			{
				_currentTrackNode++;
			}
		}
		if (PlayerTrackNode >= _track.Nodes.Count - 1)
		{
			_trackFraction = 0.0;
		}
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
		if (_flow.Finished && _endFadeFrames < 0)
		{
			_endFadeFrames = 0;
		}
	}

	/// <summary>Starts the results screen, with its jingle (MusID_EndLevel).</summary>
	private void StartResults(bool emeraldWon, int ringsCollected)
	{
		bool allEmeralds = _startEmeralds + (emeraldWon ? 1 : 0) >= HalfPipeLayout.StageCount;
		_results = new HalfPipeResults(Rings, emeraldWon, ringsCollected, _track.RingCount, allEmeralds);
		_resultsFrames = 0;
		_musicInstance?.Stop();
		_musicInstance?.Dispose();
		_musicInstance = new SampleInstance(_gameContext, _resultsJingle);
		_musicInstance.Play();
	}

	private void UpdateResults()
	{
		if (_results.Update())
		{
			// The tally sound on every fourth step
			if (_frames % 4 == 0)
			{
				_gameContext.Audio.PlaySound(_tallySound);
			}
		}
		else if (_results.TallyJustEnded)
		{
			_gameContext.Audio.PlaySound(_tallyEndSound);
		}
		if (_results.PerfectJustShown)
		{
			_gameContext.Audio.PlaySound(_perfectSound);
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
		_pipeBuffer?.Dispose();
		_emeraldTexture?.Dispose();
		_pipeShader?.Dispose();
		_objectShader?.Dispose();
	}

	/// <summary>The camera follows the track smoothly: it is between the current node and the next node.</summary>
	private void UpdateCamera()
	{
		(Vector3 position, double yaw, double pitch) = GetTrackPoint(_currentTrackNode, _trackFraction);
		_camera = position + new Vector3(0.0, CameraHeight, 0.0);
		pitch -= MathX.ToRadians(CameraTilt);
		_cameraTarget = _camera + new Vector3(Math.Sin(yaw) * Math.Cos(pitch), Math.Sin(pitch), Math.Cos(yaw) * Math.Cos(pitch));
	}

	/// <summary>The position and direction of the track at a fraction of the way from a node to the next node.</summary>
	private (Vector3 Position, double Yaw, double Pitch) GetTrackPoint(int trackNode, double fraction)
	{
		HalfPipeTrackNode node = _track.Nodes[trackNode];
		if (fraction <= 0.0 || trackNode + 1 >= _track.Nodes.Count)
		{
			return (node.Position, node.Yaw, node.Pitch);
		}
		HalfPipeTrackNode next = _track.Nodes[trackNode + 1];
		return (node.Position + (next.Position - node.Position) * fraction,
			node.Yaw + MathX.DifferenceRadians(node.Yaw, next.Yaw) * fraction,
			node.Pitch + (next.Pitch - node.Pitch) * fraction);
	}

	public override void Draw(Renderer renderer)
	{
		SonicOrca.Core.Debugging.TestHooks.DrawnLevelTick = SonicOrca.Core.Debugging.TestHooks.SpecialStageCaptureOffset + _frames;
		DrawBackground(renderer);
		DrawScene(renderer);
		DrawHud(renderer);
		DrawWhiteFade(renderer);
	}

	/// <summary>The stage fades from white at the start and to white at the end, as in the original.</summary>
	private void DrawWhiteFade(Renderer renderer)
	{
		double whiteness = 0.0;
		if (_results == null && _endFadeFrames >= 0)
		{
			whiteness = _endFadeFrames / (double)WhiteFadeFrames;
		}
		else if (_results == null && _frames < WhiteFadeFrames)
		{
			whiteness = 1.0 - _frames / (double)WhiteFadeFrames;
		}
		if (whiteness <= 0.0)
		{
			return;
		}
		I2dRenderer g = renderer.Get2dRenderer();
		g.BlendMode = BlendMode.Alpha;
		g.RenderQuad(new Colour((byte)(Math.Min(1.0, whiteness) * 255), 255, 255, 255), new Rectangle(0.0, 0.0, 1920.0, 1080.0));
		g.Deactivate();
	}

	/// <summary>True while a checkpoint message shows: the original shows the winged hand with it.</summary>
	private bool ShowsHand => _originalArt != null && (_flow.Message == HalfPipeStageFlow.CheckpointPassedMessage || _flow.Message == HalfPipeStageFlow.CheckpointFailedMessage);

	/// <summary>
	/// The START banner of the original (Obj5F): it falls to its place, waits, then breaks into its pieces, which fly
	/// away from the centre.
	/// </summary>
	private void DrawStartBanner(I2dRenderer g)
	{
		int breakTick = BannerStopY - BannerStartY + BannerWaitTicks;
		if (_ticks >= breakTick + BannerPieceTicks)
		{
			return;
		}
		AnimationGroup start = _originalArt.Start;
		const double x = 0x80;
		if (_ticks < breakTick)
		{
			double y = Math.Min(BannerStopY, BannerStartY + _ticks);
			HalfPipeOriginalArt.DrawFrame(g, start, HalfPipeOriginalArt.BannerFrame, HalfPipeOriginalArt.StageLeft, x, y);
			return;
		}
		// Each piece flies along the line from a point $28 pixels below the banner through the piece (CalcAngle)
		int[] pieceX = { -0x48, -0x28, -0x18, -8, 8, 0x18, 0x28 };
		double distance = (_ticks - breakTick) * BannerPieceSpeed;
		for (int i = 0; i < HalfPipeOriginalArt.BannerPieces; i++)
		{
			double angle = Math.Atan2(-0x28, pieceX[i]);
			HalfPipeOriginalArt.DrawFrame(g, start, HalfPipeOriginalArt.FirstBannerPiece + i, HalfPipeOriginalArt.StageLeft,
				x + Math.Cos(angle) * distance, BannerStopY + Math.Sin(angle) * distance);
		}
	}

	/// <summary>
	/// The winged hand of a checkpoint message (Obj5A_Handshake): the hand goes up and down by 4 pixels, one pixel each
	/// tick. A failed checkpoint shows it upside down: thumb down.
	/// </summary>
	private void DrawHand(I2dRenderer g)
	{
		AnimationGroup messages = _originalArt.Messages;
		const double x = 0x80;
		HalfPipeOriginalArt.DrawFrame(g, messages, HalfPipeOriginalArt.WingsFrame, HalfPipeOriginalArt.StageLeft, x, HandY);
		int phase = (_ticks - _messageStartTick) % 16;
		int bounce = phase < 4 ? -phase : phase < 12 ? phase - 8 : 16 - phase;
		bool failed = _flow.Message == HalfPipeStageFlow.CheckpointFailedMessage;
		HalfPipeOriginalArt.DrawFrame(g, messages, HalfPipeOriginalArt.HandFrame, HalfPipeOriginalArt.StageLeft, x, HandY + bounce, flipY: failed);
	}

	/// <summary>
	/// The results screen of the original (Obj6F): a white screen, the title, the emeralds of the player, the score and
	/// the counters, which slide in from the sides. The numbers count down during the tally.
	/// </summary>
	private void DrawOriginalResults(I2dRenderer g)
	{
		g.RenderQuad(Colours.White, new Rectangle(0.0, 0.0, 1920.0, 1080.0));
		AnimationGroup results = _originalArt.Results;
		double left = HalfPipeOriginalArt.ResultsLeft;
		const double centreX = 160.0;
		const double centreY = 112.0;
		// A text slides from its start towards the centre of the screen
		double Slide(double start) => start > centreX
			? Math.Max(centreX, start - _resultsFrames * ResultsTextSpeed)
			: Math.Min(centreX, start + _resultsFrames * ResultsTextSpeed);
		bool allEmeralds = _results.SuperMessage;
		int titleFrame = allEmeralds ? 25 : _results.EmeraldWon ? 4 : 0;
		if (_results.Phase == HalfPipeResultsPhase.SuperMessage)
		{
			DrawSuperMessage(g, results, left, titleFrame, centreY);
		}
		else
		{
			HalfPipeOriginalArt.DrawFrame(g, results, titleFrame, left, Slide(320 + 128), centreY - 70);
			if (_results.EmeraldWon)
			{
				// "Sonic got a" or "Sonic has all the", from the left
				HalfPipeOriginalArt.DrawFrame(g, results, allEmeralds ? 22 : 1, left, Slide(-128), centreY - 88);
			}
		}
		// The emeralds that the player has, in a ring around the last one. They show on every second frame
		// (Obj6F_Emerald0, bit 0 of Vint_runcount), which looks like half opacity at 60 frames a second.
		(int X, int Y)[] emeraldPositions = { (-8, -44), (16, -32), (16, -8), (-8, 4), (-32, -8), (-32, -32), (-8, -20) };
		for (int i = 0; i < emeraldPositions.Length && _frames % 2 == 1; i++)
		{
			bool has = i < _startEmeralds || (_results.EmeraldWon && i == _stage);
			if (has)
			{
				HalfPipeOriginalArt.DrawFrame(g, results, 5 + i, left, centreX + emeraldPositions[i].X, centreY + emeraldPositions[i].Y);
			}
		}
		// The rows have the places of the original: the score at 24, the rings of the players at 40 and 56 (48 for one
		// player), and the gems bonus at 72. "PERFECT BONUS" replaces the rings rows at 48 and flashes on bit 3 of
		// the frame counter (Obj6F_PerfectBonus).
		int ringsLeft = _results.RingBonus / HalfPipeResults.PointsPerRing;
		int tailsRingsLeft = _tails != null ? Math.Min(_tailsRings, ringsLeft) : 0;
		var rows = new List<(int Frame, int StartX, int Y, int Value)> { (12, 320 + 368, 24, _startScore + _results.Total) };
		if (_results.PerfectShown)
		{
			if (_frames % 16 < 8)
			{
				rows.Add((17, 320 + 384, 48, _results.PerfectBonus));
			}
		}
		else
		{
			rows.Add((13, 320 + 384, _tails != null ? 40 : 48, ringsLeft - tailsRingsLeft));
			if (_tails != null)
			{
				rows.Add((15, 320 + 400, 56, tailsRingsLeft));
			}
		}
		if (_results.EmeraldWon)
		{
			rows.Add((16, 320 + 416, 72, _results.GemsBonus));
		}
		foreach ((int frame, int startX, int y, int value) in rows)
		{
			double x = Slide(startX);
			HalfPipeOriginalArt.DrawFrame(g, results, frame, left, x, centreY + y);
			// The numbers end 96 pixels right of the centre of the row
			_originalArt.DrawNumber(g, value, left, x + 0x60, centreY + y);
		}
	}

	/// <summary>
	/// The Super Sonic message of the original (Obj6F_InitAndMoveSuperMsg): the title leaves to the right and "Sonic
	/// has all the" to the left. Then "NOW SONIC CAN" comes from the left, "CHANGE INTO" from the right, and "SUPER
	/// SONIC" from the left.
	/// </summary>
	private void DrawSuperMessage(I2dRenderer g, AnimationGroup results, double left, int titleFrame, double centreY)
	{
		const double centreX = 160.0;
		int frames = _results.PhaseFrames;
		if (frames <= HalfPipeResults.SuperMessageLeaveFrames)
		{
			HalfPipeOriginalArt.DrawFrame(g, results, titleFrame, left, Math.Min(320 + 128, centreX + frames * HalfPipeResults.LeaveSpeed), centreY - 70);
			HalfPipeOriginalArt.DrawFrame(g, results, 22, left, Math.Max(-128, centreX - frames * HalfPipeResults.LeaveSpeed), centreY - 88);
			return;
		}
		int moved = (frames - HalfPipeResults.SuperMessageLeaveFrames) * HalfPipeResults.TextSpeed;
		HalfPipeOriginalArt.DrawFrame(g, results, 26, left, Math.Min(centreX, -128 + moved), centreY - 96);
		HalfPipeOriginalArt.DrawFrame(g, results, 27, left, Math.Max(centreX, 320 + 128 - moved), centreY - 78);
		HalfPipeOriginalArt.DrawFrame(g, results, 28, left, Math.Min(centreX, -128 + moved), centreY - 60);
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
		IShaderProgram program = _pipeShader.Program;
		program.Activate();
		program.SetUniform("ProjectionMatrix", _projectionMatrix);
		program.SetUniform("ModelViewMatrix", _modelViewMatrix);
		program.SetUniform("InputTexture", 0);
		program.SetUniform("InputLightSource", GetTrackPoint(_currentTrackNode, _trackFraction).Position);
		DrawPipe(program);

		List<TexturedQuad> quads = new List<TexturedQuad>();
		AddObjectQuads(quads);
		AddPlayerQuads(quads, _player, GetDrawPose(_player, _sonicPreviousPose), _sonicAnimation, SonicHeight);
		if (_tails != null)
		{
			AddPlayerQuads(quads, _tails, GetDrawPose(_tails, _tailsPreviousPose), _tailsAnimation, TailsHeight);
		}
		AddEmeraldQuad(quads);

		// Draw the sprites from back to front, after the pipe, which is opaque. Group the quads that use the same
		// texture and style into one draw call.
		List<(int Count, ITexture Texture, QuadStyle Style)> batches = new List<(int, ITexture, QuadStyle)>();
		int batchCount = 0;
		ITexture texture = null;
		QuadStyle style = QuadStyle.Lit;
		_vertexBuffer.Begin();
		// The shadows lie on the pipe, under everything else, so they come first. A sprite writes the depth of its whole
		// quad, also where it is transparent, so a shadow after it would lose the part behind it.
		foreach (TexturedQuad quad in quads.OrderBy(x => x.Style == QuadStyle.Shadow ? 0 : 1).ThenByDescending(x => (_camera - x.Position[0]).Length))
		{
			for (int corner = 0; corner < 4; corner++)
			{
				_vertexBuffer.AddValue(0, quad.Position[corner]);
				_vertexBuffer.AddValue(1, quad.TextureMapping[corner]);
			}
			if (batchCount == 0)
			{
				texture = quad.Texture;
				style = quad.Style;
			}
			else if (texture != quad.Texture || style != quad.Style)
			{
				batches.Add((batchCount, texture, style));
				batchCount = 0;
				texture = quad.Texture;
				style = quad.Style;
			}
			batchCount++;
		}
		if (batchCount > 0)
		{
			batches.Add((batchCount, texture, style));
		}
		_vertexBuffer.End();
		int firstQuad = 0;
		foreach ((int count, ITexture batchTexture, QuadStyle batchStyle) in batches)
		{
			program.SetUniform("MinimumLight", batchStyle == QuadStyle.Lit ? MinimumLight : 1.0);
			bool glow = batchStyle == QuadStyle.Glow || batchStyle == QuadStyle.BombGlow;
			program.SetUniform("Tint", batchStyle switch
			{
				QuadStyle.Glow => RingGlowColour,
				QuadStyle.BombGlow => BombGlowColour,
				QuadStyle.Shadow => new Vector4(1.0, 1.0, 1.0, ShadowOpacity),
				_ => new Vector4(1.0, 1.0, 1.0, 1.0)
			});
			_graphicsContext.BlendMode = glow ? BlendMode.Additive : BlendMode.Alpha;
			_graphicsContext.SetTexture(batchTexture);
			_vertexBuffer.Render(PrimitiveType.Quads, firstQuad * 4, count * 4);
			firstQuad += count;
		}
		_graphicsContext.BlendMode = BlendMode.Alpha;
		_graphicsContext.DepthTesting = false;
	}

	private static TexturedQuad WithStyle(TexturedQuad quad, QuadStyle style)
	{
		quad.Style = style;
		return quad;
	}

	private Matrix4 GetNodeTransform(int trackNode)
	{
		HalfPipeTrackNode node = _track.Nodes[trackNode];
		return Matrix4.Identity.RotateY(node.Yaw).Translate(node.Position);
	}

	/// <summary>
	/// Makes the quads of the whole pipe once. The pipe does not change, so each frame draws a part of this buffer
	/// in one call, and only the sprites go into the vertex buffer of the frame.
	/// </summary>
	private void FillPipeBuffer()
	{
		double textureScale = 8.0 / _floorTexture.Width;
		// The number of track nodes that one repeat of the floor texture covers.
		int textureRepeatNodes = (int)(_floorTexture.Height * textureScale / HalfPipeTrackBuilder.NodeSpacing);
		Vector3[] crossSection = GetCrossSection();
		Vector3[] previousSection = null;
		Vector3[] section = new Vector3[crossSection.Length];
		_pipeBuffer.Begin();
		for (int i = 0; i < _track.Nodes.Count; i++)
		{
			Matrix4 transform = GetNodeTransform(i);
			for (int j = 0; j < crossSection.Length; j++)
			{
				section[j] = transform * crossSection[j];
			}
			// Join this cross section to the previous one with floor quads.
			if (previousSection != null)
			{
				int textureRow = i % textureRepeatNodes;
				double vNear = 1.0 - (textureRow + 1) / (double)textureRepeatNodes;
				double vFar = 1.0 - textureRow / (double)textureRepeatNodes;
				for (int j = 0; j < section.Length - 1; j++)
				{
					double u0 = j / (double)(section.Length - 1);
					double u1 = (j + 1) / (double)(section.Length - 1);
					_pipeBuffer.AddValue(0, section[j]);
					_pipeBuffer.AddValue(1, new Vector2(u0, vNear));
					_pipeBuffer.AddValue(0, previousSection[j]);
					_pipeBuffer.AddValue(1, new Vector2(u0, vFar));
					_pipeBuffer.AddValue(0, previousSection[j + 1]);
					_pipeBuffer.AddValue(1, new Vector2(u1, vFar));
					_pipeBuffer.AddValue(0, section[j + 1]);
					_pipeBuffer.AddValue(1, new Vector2(u1, vNear));
				}
			}
			previousSection = (Vector3[])section.Clone();
		}
		_pipeBuffer.End();
	}

	/// <summary>Draws the pipe from <see cref="DrawDistanceBehind"/> nodes behind the camera to <see cref="DrawDistanceAhead"/> nodes ahead.</summary>
	private void DrawPipe(IShaderProgram program)
	{
		int first = Math.Max(0, _currentTrackNode - DrawDistanceBehind);
		int last = Math.Min(_track.Nodes.Count - 1, _currentTrackNode + DrawDistanceAhead);
		if (last <= first)
		{
			return;
		}
		program.SetUniform("MinimumLight", MinimumLight);
		program.SetUniform("Tint", new Vector4(1.0, 1.0, 1.0, 1.0));
		_graphicsContext.BlendMode = BlendMode.Alpha;
		_graphicsContext.SetTexture(_floorTexture);
		// The quads between nodes first and first + 1 come first
		_pipeBuffer.Render(PrimitiveType.Quads, first * QuadsPerNode * 4, (last - first) * QuadsPerNode * 4);
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
				// The original gives a ring or a bomb a shadow only on the lower half of the pipe (loc_3529C)
				if (Math.Abs(item.Angle) <= Math.PI / 2.0)
				{
					double shadowSize = (item.Kind == HalfPipeObjectKind.Ring ? RingSize : BombSize) * 0.8;
					quads.Add(CreateSurfaceShadow(transform, item.Angle, shadowSize, shadowSize * ObjectShadowSquash, 0.0, _ballShadowTexture));
				}
				if (item.Kind == HalfPipeObjectKind.Ring)
				{
					// The glow is centred on the ring, so its bottom is below the surface, where the pipe hides it
					double glowSize = RingSize * RingGlowScale;
					quads.Add(WithStyle(CreateSprite(transform, item.Angle, (RingSize - glowSize) / 2.0, glowSize, glowSize, _glowTexture,
						new Rectanglei(0, 0, _glowTexture.Width, _glowTexture.Height), depth: RingGlowDepth), QuadStyle.Glow));
					quads.Add(WithStyle(CreateSprite(transform, item.Angle, 0.0, RingSize, RingSize, _ringAnimation.CurrentTexture, _ringAnimation.CurrentFrame.Source), QuadStyle.Bright));
				}
				else
				{
					double glowSize = BombSize * RingGlowScale;
					quads.Add(WithStyle(CreateSprite(transform, item.Angle, (BombSize - glowSize) / 2.0, glowSize, glowSize, _glowTexture,
						new Rectanglei(0, 0, _glowTexture.Width, _glowTexture.Height), depth: RingGlowDepth), QuadStyle.BombGlow));
					quads.Add(WithStyle(CreateSprite(transform, item.Angle, 0.0, BombSize, BombSize, _bombTexture,
						new Rectanglei(0, 0, _bombTexture.Width, _bombTexture.Height)), QuadStyle.Bright));
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
		double bob = _flow.EmeraldWon ? new[] { -1, 0, 1, 0 }[_flow.EmeraldWonFrames / 4 % 4] * EmeraldBob : 0.0;
		// The emerald floats at 3/4 of the pipe radius from the centre
		double height = PipeRadius / 4.0 + bob;
		quads.Add(CreateSurfaceShadow(GetNodeTransform(node), 0.0, EmeraldSize * 0.6, EmeraldSize * 0.6 * ObjectShadowSquash, 0.0, _ballShadowTexture));
		quads.Add(CreateSprite(GetNodeTransform(node), 0.0, height, EmeraldSize, EmeraldSize, _emeraldTexture,
			new Rectanglei(0, 0, _emeraldTexture.Width, _emeraldTexture.Height)));
	}

	/// <summary>
	/// The pose to draw a player: halfway between the poses before and after the last logic tick on the frame after
	/// the tick, else the pose after it. When the logic has stopped (the end of the stage), the pose does not change.
	/// </summary>
	private PlayerPose GetDrawPose(HalfPipePlayer player, PlayerPose previous)
	{
		PlayerPose current = PlayerPose.Of(player);
		bool logicRuns = _results == null && _endFadeFrames < 0;
		return logicRuns && _frames % FramesPerLogicTick == 1 ? PlayerPose.Lerp(previous, current, 0.5) : current;
	}

	private void AddPlayerQuads(List<TexturedQuad> quads, HalfPipePlayer player, PlayerPose pose, AnimationInstance animation, double spriteHeight)
	{
		// The players move with the camera, between the nodes
		(Vector3 position, double yaw, _) = GetTrackPoint(PlayerTrackNode, PlayerTrackNode < _track.Nodes.Count - 1 ? _trackFraction : 0.0);
		Matrix4 transform = Matrix4.Identity.RotateY(yaw).Translate(position);
		double x = pose.X;
		double y = pose.Y;
		double distance = Math.Sqrt(x * x + y * y);
		double angle = pose.Radians;
		// The outer lane is nearer to the camera, a little behind the inner lane
		double lane = (pose.Radius - HalfPipePlayer.InnerRadius) / (HalfPipePlayer.OuterRadius - HalfPipePlayer.InnerRadius);
		double depth = -lane * OuterLaneDepth;
		// The shadow is on the pipe, on the line from the centre through the player (Obj63). On the upper half of the
		// pipe, it is on the lower half, below the player.
		double shadowAngle = Math.Abs(angle) > Math.PI / 2.0 ? Math.Sign(angle) * (Math.PI - Math.Abs(angle)) : angle;
		quads.Add(CreateSurfaceShadow(transform, shadowAngle, ShadowWidth, ShadowLength, depth, _shadowTexture));
		// The player shows on alternate frames while flashing after a bomb
		if (player.FlashFramesLeft > 0 && _ticks % 2 == 0)
		{
			return;
		}
		Rectanglei source = animation.CurrentFrame.Source;
		double width = spriteHeight * source.Width / source.Height;
		// In the air the player is nearer to the centre than the surface
		double height = Math.Max(0.0, (pose.Radius - distance) * PixelsToUnits);
		// The hurt animation turns the sprite once
		double spin = player.IsHurt ? player.HurtProgress * 2.0 * Math.PI : 0.0;
		quads.Add(CreateSprite(transform, angle, height, width, spriteHeight, animation.CurrentTexture, source, spin, depth));
	}

	/// <summary>
	/// Makes a shadow that lies on the pipe surface, centred at an angle from the bottom. It is <paramref name="width"/>
	/// wide across the pipe and <paramref name="length"/> long along the track.
	/// </summary>
	private static TexturedQuad CreateSurfaceShadow(Matrix4 transform, double angle, double width, double length, double depth, ITexture texture)
	{
		double radius = PipeRadius - ShadowGap;
		double halfAngle = width / 2.0 / radius;
		// The whole shadow stays on the pipe: its centre is not nearer to the top edges than half its width
		double maxAngle = Math.PI / 2.0 - halfAngle;
		angle = Math.Clamp(angle, -maxAngle, maxAngle);
		Vector3 Point(double theta, double z) => transform * new Vector3(-Math.Cos(theta) * radius * PipeWidthScale, -Math.Sin(theta) * radius, z);
		// Theta is the angle of the cross section: 0 at the left edge, pi / 2 at the bottom, pi at the right edge
		double theta = Math.PI / 2.0 + angle;
		double near = depth - length / 2.0;
		double far = depth + length / 2.0;
		return new TexturedQuad(new[]
		{
			Point(theta - halfAngle, near),
			Point(theta - halfAngle, far),
			Point(theta + halfAngle, far),
			Point(theta + halfAngle, near)
		}, new[]
		{
			new Vector2(0.0, 1.0),
			new Vector2(0.0, 0.0),
			new Vector2(1.0, 0.0),
			new Vector2(1.0, 1.0)
		}, texture, QuadStyle.Shadow);
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
		if (_results != null && _originalArt != null)
		{
			DrawOriginalResults(g);
			g.Deactivate();
			return;
		}
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
			if (UsesOriginalText)
			{
				// The big white digits of the original, as tall as the word RINGS
				_originalArt.DrawHudNumber(g, _rings, 48.0 + _ringsLabelTexture.Width + 16.0, 40.0 + _ringsLabelTexture.Height / 2.0);
			}
			g.Deactivate();
			fontRenderer = renderer.GetFontRenderer();
			if (!UsesOriginalText)
			{
				fontRenderer.RenderString(_rings.ToString(), new Rectangle(260.0, 48.0, 0.0, 0.0), FontAlignment.Left, _font, 0);
			}
		}
		else
		{
			// As in the original: the rings of Sonic on the left, the total in the centre, the rings of Tails on the right.
			// Under the name of each player, the yellow word RINGS and the count.
			const double nameY = 24.0;
			double ringsY = nameY + _sonicLabelTexture.Height + 4.0;
			double tailsX = 1920.0 - 48.0 - _ringsLabelTexture.Width - RingCountWidth;
			g.RenderTexture(_sonicLabelTexture, new Rectangle(48.0, nameY, _sonicLabelTexture.Width, _sonicLabelTexture.Height));
			g.RenderTexture(_ringsLabelTexture, new Rectangle(48.0, ringsY, _ringsLabelTexture.Width, _ringsLabelTexture.Height));
			g.RenderTexture(_totalLabelTexture, new Rectangle(960.0 - _totalLabelTexture.Width / 2.0, 24.0, _totalLabelTexture.Width, _totalLabelTexture.Height));
			g.RenderTexture(_tailsLabelTexture, new Rectangle(tailsX, nameY, _tailsLabelTexture.Width, _tailsLabelTexture.Height));
			g.RenderTexture(_ringsLabelTexture, new Rectangle(tailsX, ringsY, _ringsLabelTexture.Width, _ringsLabelTexture.Height));
			double sonicCountX = 48.0 + _ringsLabelTexture.Width + 16.0;
			double tailsCountX = tailsX + _ringsLabelTexture.Width + 16.0;
			if (UsesOriginalText)
			{
				double ringsCentreY = ringsY + _ringsLabelTexture.Height / 2.0;
				_originalArt.DrawHudNumber(g, _rings, sonicCountX, ringsCentreY);
				// The total goes inside the box of the label
				_originalArt.DrawHudNumber(g, Rings, 960.0, 24.0 + _totalLabelTexture.Height / 2.0, centred: true);
				_originalArt.DrawHudNumber(g, _tailsRings, tailsCountX, ringsCentreY);
			}
			g.Deactivate();
			fontRenderer = renderer.GetFontRenderer();
			if (!UsesOriginalText)
			{
				double countY = ringsY + _ringsLabelTexture.Height / 2.0 - 24.0;
				fontRenderer.RenderString(_rings.ToString(), new Rectangle(sonicCountX, countY, 0.0, 0.0), FontAlignment.Left, _font, 0);
				fontRenderer.RenderString(Rings.ToString(), new Rectangle(960.0, 24.0 + _totalLabelTexture.Height / 2.0 - 16.0, 0.0, 0.0), FontAlignment.MiddleX, _font, 0);
				fontRenderer.RenderString(_tailsRings.ToString(), new Rectangle(tailsCountX, countY, 0.0, 0.0), FontAlignment.Left, _font, 0);
			}
			counterY = 220.0;
		}
		if (_flow.Counter != null && !UsesOriginalText)
		{
			fontRenderer.RenderString(_flow.Counter, new Rectangle(960.0, counterY, 0.0, 0.0), FontAlignment.MiddleX, _font, 0);
		}
		if (_flow.Message != null && !UsesOriginalText)
		{
			// Under the winged hand, when it shows
			double messageY = ShowsHand ? 560.0 : 440.0;
			fontRenderer.RenderString(_flow.Message, new Rectangle(960.0, messageY, 0.0, 0.0), FontAlignment.MiddleX, _font, 0);
		}
		if (_originalArt != null)
		{
			I2dRenderer overlay = renderer.Get2dRenderer();
			overlay.BlendMode = BlendMode.Alpha;
			overlay.Colour = Colours.White;
			DrawStartBanner(overlay);
			if (ShowsHand)
			{
				DrawHand(overlay);
			}
			if (UsesOriginalText)
			{
				DrawOriginalText(overlay);
			}
			overlay.Deactivate();
		}
	}

	/// <summary>The "xx RINGS TO GO!" counter, the message, and the letters that fly away, with the art of the original.</summary>
	private void DrawOriginalText(I2dRenderer g)
	{
		if (_flow.RingsToGo is int toGo)
		{
			foreach (HalfPipeGlyph glyph in HalfPipeMessageText.RingsToGo(toGo))
			{
				_originalArt.DrawGlyph(g, glyph, glyph.X, glyph.Y);
			}
		}
		if (_flow.Message != null)
		{
			foreach (HalfPipeGlyph glyph in GetMessageGlyphs(_flow.Message) ?? new List<HalfPipeGlyph>())
			{
				_originalArt.DrawGlyph(g, glyph, glyph.X, glyph.Y);
			}
		}
		foreach (FlyingGlyph flying in _flyingGlyphs)
		{
			_originalArt.DrawGlyph(g, flying.Glyph, flying.X, flying.Y);
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
		if (_results.PerfectShown)
		{
			Line("PERFECT BONUS", _results.PerfectBonus, 660.0);
		}
		if (_results.Phase == HalfPipeResultsPhase.SuperMessage)
		{
			fontRenderer.RenderString("NOW SONIC CAN CHANGE INTO SUPER SONIC", new Rectangle(960.0, 900.0, 0.0, 0.0), FontAlignment.MiddleX, _font, 0);
		}
		Line("TOTAL", _results.Total, 780.0);
	}
}
