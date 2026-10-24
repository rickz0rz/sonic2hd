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
/// bombs take rings away. Start it with --halfpipe. Escape closes it. The minus and equals keys change the field
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

	private enum StageState
	{
		Playing,
		Failed,
		Cleared
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

	private const string RingSoundResourceKey = "SONICORCA/SOUND/RING";

	private const string JumpSoundResourceKey = "SONICORCA/SOUND/JUMP";

	private const string RingLossSoundResourceKey = "SONICORCA/SOUND/RINGSCATTER";

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

	/// <summary>The number of track nodes between the camera and the player.</summary>
	private const int PlayerLead = 8;

	/// <summary>A bomb takes this number of rings, or all of the rings when the player has fewer.</summary>
	private const int BombRingLoss = 10;

	/// <summary>The player touches an object when the angles are nearer than this, in radians.</summary>
	private const double TouchAngle = 0.22;

	/// <summary>The player is above an object when the jump height is more than this.</summary>
	private const double TouchHeight = 0.9;

	/// <summary>The ticks that the result message shows before the stage closes.</summary>
	private const int ResultTicks = 180;

	// Sprite sizes in track units. The pipe radius is 4.
	private const double SonicHeight = 2.4;

	private const double RingSize = 1.1;

	private const double BombSize = 1.2;

	private const double EffectSize = 1.2;

	private const double ShadowWidth = 1.2;

	/// <summary>Sprites stand this far above the surface, so the surface does not hide them.</summary>
	private const double SurfaceGap = 0.05;

	private readonly SonicOrcaGameContext _gameContext;

	private readonly IGraphicsContext _graphicsContext;

	private readonly ResourceSession _resourceSession;

	private readonly HalfPipePlayer _player = new HalfPipePlayer();

	private readonly int _stage;

	private Font _font;

	private ITexture _backgroundTexture;

	private ITexture _floorTexture;

	private ITexture _bombTexture;

	private ITexture _shadowTexture;

	private ITexture _ringsLabelTexture;

	private AnimationInstance _sonicAnimation;

	private AnimationInstance _ringAnimation;

	private AnimationGroup _ringEffectAnimation;

	private AnimationGroup _explosionAnimation;

	private SampleInfo _musicSampleInfo;

	private Sample _ringSound;

	private Sample _jumpSound;

	private Sample _ringLossSound;

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

	private int _nextCheckpoint;

	private int _rings;

	private StageState _state = StageState.Playing;

	private int _resultTicks;

	private const double PipeRadius = 4.0;

	private const int NumPointsWide = 32;

	private int _ticks;

	private Vector3 _camera;

	private Vector3 _cameraTarget;

	/// <param name="stage">The special stage number, from 0 to 6. It selects the ring quotas.</param>
	public HalfPipeSpecialStageScreen(SonicOrcaGameContext gameContext, int stage = 0)
	{
		_stage = stage;
		_gameContext = gameContext;
		_graphicsContext = _gameContext.Window.GraphicsContext;
		_resourceSession = new ResourceSession(gameContext.ResourceTree);
	}

	/// <summary>The rings that the player has.</summary>
	public int Rings => _rings;

	/// <summary>True when the player reached the goal of every checkpoint.</summary>
	public bool Cleared => _state == StageState.Cleared;

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
		};
		foreach (string key in keys)
		{
			_resourceSession.PushDependency(key);
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
	}

	public override void Initialise()
	{
		_pipeShader = OrcaShader.CreateFromFile(_graphicsContext, "shaders/halfpipe/test.shader");
		_objectShader = OrcaShader.CreateFromFile(_graphicsContext, "shaders/halfpipe/object.shader");
		_vertexBuffer = _graphicsContext.CreateVertexBuffer(3, 2);
		_projectionMatrix = Matrix4.Perspective(MathX.ToRadians(_fov), 16.0 / 9.0, 1.0, 1024.0);
		_track = HalfPipeTrackBuilder.Build(_stageData ?? HalfPipeLayout.CreateGenerated(), HalfPipeLayout.GetRingQuotas(_stage, duo: false));
		_objects = _track.Objects;
		_checkpoints = _track.Checkpoints;
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

		if (_state == StageState.Playing)
		{
			UpdatePlayer();
			// The track moves one node each tick (a track frame each 5 ticks), until the player reaches the end
			if (PlayerTrackNode < _track.Nodes.Count - 1)
			{
				_currentTrackNode++;
			}
			UpdateObjects();
			UpdateCheckpoints();
		}
		else if (++_resultTicks >= ResultTicks)
		{
			Finish();
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
		Controller current = _gameContext.Current[0];
		Controller pressed = _gameContext.Pressed[0];
		if (_player.Update(current.DirectionLeft.X, pressed.Action1))
		{
			_gameContext.Audio.PlaySound(_jumpSound);
		}
		int animation = _player.IsJumping ? 1 : 0;
		if (_sonicAnimation.Index != animation)
		{
			_sonicAnimation.Index = animation;
		}
		_sonicAnimation.Animate();
	}

	/// <summary>Collects the rings and hits the bombs that the player touches.</summary>
	private void UpdateObjects()
	{
		int playerNode = PlayerTrackNode;
		foreach (HalfPipeObject item in _objects)
		{
			// The track moves one node each two ticks, so a window of two nodes sees each object once
			if (item.Used || item.TrackNode < playerNode - 1 || item.TrackNode > playerNode)
			{
				continue;
			}
			if (Math.Abs(item.Angle - _player.Angle) > TouchAngle || _player.Height > TouchHeight)
			{
				continue;
			}
			if (item.Kind == HalfPipeObjectKind.Ring)
			{
				item.Used = true;
				_rings++;
				_gameContext.Audio.PlaySound(_ringSound);
			}
			else if (!_player.IsHurt)
			{
				item.Used = true;
				_player.Hurt();
				int lost = Math.Min(_rings, BombRingLoss);
				_rings -= lost;
				if (lost > 0)
				{
					_gameContext.Audio.PlaySound(_ringLossSound);
				}
			}
		}
	}

	private void UpdateCheckpoints()
	{
		if (_nextCheckpoint >= _checkpoints.Count || PlayerTrackNode < _checkpoints[_nextCheckpoint].TrackNode)
		{
			return;
		}
		if (_rings < _checkpoints[_nextCheckpoint].RingGoal)
		{
			_state = StageState.Failed;
			return;
		}
		_nextCheckpoint++;
		if (_nextCheckpoint == _checkpoints.Count)
		{
			_state = StageState.Cleared;
		}
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
		_pipeShader?.Dispose();
		_objectShader?.Dispose();
	}

	private void UpdateCamera()
	{
		HalfPipeTrackNode node = _track.Nodes[_currentTrackNode];
		_camera = node.Position + new Vector3(0.0, -2.0, 0.0);
		_cameraTarget = _camera + new Vector3(Math.Sin(node.Yaw), Math.Sin(node.Pitch), Math.Cos(node.Yaw));
	}

	public override void Draw(Renderer renderer)
	{
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
		AddPlayerQuads(quads);

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

	private void AddPlayerQuads(List<TexturedQuad> quads)
	{
		Matrix4 transform = GetNodeTransform(PlayerTrackNode);
		if (_player.IsJumping)
		{
			double shadowHeight = ShadowWidth * _shadowTexture.Height / _shadowTexture.Width;
			quads.Add(CreateSprite(transform, _player.Angle, 0.0, ShadowWidth, shadowHeight, _shadowTexture, new Rectanglei(0, 0, _shadowTexture.Width, _shadowTexture.Height)));
		}
		// The player flashes while bombs cannot hurt him
		if (_player.IsHurt && _ticks / 4 % 2 == 0)
		{
			return;
		}
		Rectanglei source = _sonicAnimation.CurrentFrame.Source;
		double width = SonicHeight * source.Width / source.Height;
		quads.Add(CreateSprite(transform, _player.Angle, _player.Height, width, SonicHeight, _sonicAnimation.CurrentTexture, source));
	}

	/// <summary>
	/// Makes an upright sprite that stands on the pipe surface at an angle from the bottom, and faces along the track.
	/// The sprite leans with the surface, so that its bottom edge is on the surface and its top points to the centre.
	/// </summary>
	private static TexturedQuad CreateSprite(Matrix4 transform, double angle, double height, double width, double spriteHeight, ITexture texture, Rectanglei source)
	{
		// Theta is the angle of the cross section: 0 at the left edge, pi / 2 at the bottom, pi at the right edge
		double theta = Math.PI / 2.0 + angle;
		Vector3 surface = new Vector3(-Math.Cos(theta) * PipeRadius, -Math.Sin(theta) * PipeRadius, 0.0);
		Vector3 up = new Vector3(Math.Cos(theta), Math.Sin(theta), 0.0);
		Vector3 right = new Vector3(Math.Sin(theta), -Math.Cos(theta), 0.0);
		Vector3 bottomCentre = surface + up * (SurfaceGap + height);
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
			points[i] = new Vector3((0.0 - Math.Cos(angle)) * PipeRadius, (0.0 - Math.Sin(angle)) * PipeRadius, 0.0);
		}
		return points;
	}

	private void DrawHud(Renderer renderer)
	{
		I2dRenderer g = renderer.Get2dRenderer();
		g.BlendMode = BlendMode.Alpha;
		g.Colour = Colours.White;
		g.RenderTexture(_ringsLabelTexture, new Rectangle(48.0, 40.0, _ringsLabelTexture.Width, _ringsLabelTexture.Height));
		g.Deactivate();
		IFontRenderer fontRenderer = renderer.GetFontRenderer();
		fontRenderer.RenderString(_rings.ToString(), new Rectangle(260.0, 48.0, 0.0, 0.0), FontAlignment.Left, _font, 0);
		if (_nextCheckpoint < _checkpoints.Count && _state == StageState.Playing)
		{
			int toGo = Math.Max(0, _checkpoints[_nextCheckpoint].RingGoal - _rings);
			string goal = toGo > 0 ? $"GET {toGo} RINGS" : "COOL!";
			fontRenderer.RenderString(goal, new Rectangle(960.0, 48.0, 0.0, 0.0), FontAlignment.MiddleX, _font, 0);
		}
		string message = _state switch
		{
			StageState.Failed => "NOT ENOUGH RINGS",
			StageState.Cleared => "CHAOS EMERALD",
			_ => null,
		};
		if (message != null)
		{
			fontRenderer.RenderString(message, new Rectangle(960.0, 480.0, 0.0, 0.0), FontAlignment.MiddleX, _font, 0);
		}
	}
}
