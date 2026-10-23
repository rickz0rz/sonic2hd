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
/// A test screen for a 3D half-pipe special stage. The camera moves along a track that the segment layout makes.
/// </summary>
internal class HalfPipeSpecialStageScreen : Screen
{
	private enum SegmentGeometry
	{
		Straight,
		Drop,
		Rise,
		TurnLeft,
		TurnRight
	}

	/// <summary>
	/// A point on the track. The angle has the yaw in X and the pitch in Y, in radians.
	/// </summary>
	private struct TrackNode
	{
		public Vector3 Position { get; set; }

		public Vector2 Angle { get; set; }

		public TrackNode(Vector3 position, Vector2 angle)
		{
			Position = position;
			Angle = angle;
		}

		public override string ToString()
		{
			return $"{Position}, {MathX.ToDegrees(Angle.X):0} DEG, {MathX.ToDegrees(Angle.Y):0} DEG";
		}
	}

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

	private const string BallResourceKey = "SONICORCA/SPECIALSTAGE/HALFPIPE/BALL";

	// SDL scan codes of the keys that change the field of view.
	private const int ScancodeMinus = 45;

	private const int ScancodeEquals = 46;

	/// <summary>The number of track nodes in front of and behind the current node that are drawn.</summary>
	private const int DrawDistance = 128;

	/// <summary>The number of rings in each row across the pipe.</summary>
	private const int RingsPerRow = 7;

	private readonly SonicOrcaGameContext _gameContext;

	private readonly IGraphicsContext _graphicsContext;

	private readonly ResourceSession _resourceSession;

	private Font _font;

	private ITexture _backgroundTexture;

	private ITexture _floorTexture;

	private ITexture _ballTexture;

	private SampleInfo _musicSampleInfo;

	private Matrix4 _projectionMatrix;

	private Matrix4 _modelViewMatrix;

	private VertexBuffer _vertexBuffer;

	private ManagedShaderProgram _pipeShader;

	private ManagedShaderProgram _objectShader;

	private SampleInstance _musicInstance;

	private double _fov = 60.0;

	private int _currentTrackNode;

	private SegmentGeometry[] _layout = new SegmentGeometry[92]
	{
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.TurnRight,
		SegmentGeometry.TurnRight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.TurnRight,
		SegmentGeometry.TurnRight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.TurnRight,
		SegmentGeometry.TurnRight,
		SegmentGeometry.Rise,
		SegmentGeometry.TurnRight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.TurnRight,
		SegmentGeometry.Drop,
		SegmentGeometry.TurnLeft,
		SegmentGeometry.TurnRight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.TurnRight,
		SegmentGeometry.TurnRight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.TurnRight,
		SegmentGeometry.TurnRight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.TurnLeft,
		SegmentGeometry.TurnRight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.TurnRight,
		SegmentGeometry.TurnRight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.TurnLeft,
		SegmentGeometry.TurnRight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.Straight,
		SegmentGeometry.TurnRight,
		SegmentGeometry.Rise
	};

	private const double PipeRadius = 4.0;

	private const int NumPointsWide = 32;

	private const int PointsPerSegment = 28;

	private const int PointLength = 2;

	/// <summary>The yaw change at each track node of a turn segment. A segment turns by 90 degrees.</summary>
	private const double TurnDegreesPerPoint = 90.0 / PointsPerSegment;

	/// <summary>The pitch change at each track node of a rise or drop segment.</summary>
	private const double PitchDegreesPerPoint = 180.0 / PointsPerSegment;

	private List<TrackNode> _trackNodes = new List<TrackNode>();

	private int _ticks;

	private Vector3 _camera;

	private Vector3 _cameraTarget;

	public HalfPipeSpecialStageScreen(SonicOrcaGameContext gameContext)
	{
		_gameContext = gameContext;
		_graphicsContext = _gameContext.Window.GraphicsContext;
		_resourceSession = new ResourceSession(gameContext.ResourceTree);
	}

	public override async Task LoadAsync(ScreenLoadingProgress progress, CancellationToken ct = default)
	{
		_resourceSession.PushDependency(FontResourceKey);
		_resourceSession.PushDependency(BackgroundResourceKey);
		_resourceSession.PushDependency(FloorResourceKey);
		_resourceSession.PushDependency(BallResourceKey);
		_resourceSession.PushDependency(MusicResourceKey);
		await _resourceSession.LoadAsync(ct);
		_font = _gameContext.ResourceTree.GetLoadedResource<Font>(FontResourceKey);
		_backgroundTexture = _gameContext.ResourceTree.GetLoadedResource<ITexture>(BackgroundResourceKey);
		_floorTexture = _gameContext.ResourceTree.GetLoadedResource<ITexture>(FloorResourceKey);
		_ballTexture = _gameContext.ResourceTree.GetLoadedResource<ITexture>(BallResourceKey);
		_musicSampleInfo = _gameContext.ResourceTree.GetLoadedResource<SampleInfo>(MusicResourceKey);
	}

	public override void Initialise()
	{
		_pipeShader = OrcaShader.CreateFromFile(_graphicsContext, "shaders/halfpipe/test.shader");
		_objectShader = OrcaShader.CreateFromFile(_graphicsContext, "shaders/halfpipe/object.shader");
		_vertexBuffer = _graphicsContext.CreateVertexBuffer(3, 2);
		_projectionMatrix = Matrix4.Perspective(MathX.ToRadians(_fov), 16.0 / 9.0, 1.0, 1024.0);
		CreateTrackNodes();
	}

	private void CreateTrackNodes()
	{
		double yaw = 0.0;
		Vector3 position = default;
		foreach (SegmentGeometry segmentGeometry in _layout)
		{
			// Each segment starts level. A rise or a drop turns the pitch through a half circle.
			double pitch = MathX.ToRadians(90.0);
			for (int j = 0; j < PointsPerSegment; j++)
			{
				switch (segmentGeometry)
				{
					case SegmentGeometry.TurnLeft:
						yaw -= MathX.ToRadians(TurnDegreesPerPoint);
						break;
					case SegmentGeometry.TurnRight:
						yaw += MathX.ToRadians(TurnDegreesPerPoint);
						break;
					case SegmentGeometry.Rise:
						pitch += MathX.ToRadians(PitchDegreesPerPoint);
						break;
					case SegmentGeometry.Drop:
						pitch -= MathX.ToRadians(PitchDegreesPerPoint);
						break;
				}
				Vector3 nextPosition = position + new Vector3(Math.Sin(yaw), 0.0 - Math.Cos(pitch), Math.Cos(yaw)) * 2.0;
				// Keep the camera pitch in the range -90 to 90 degrees.
				double cameraPitch = pitch - MathX.ToRadians(90.0);
				if (cameraPitch > MathX.ToRadians(90.0))
				{
					cameraPitch = MathX.ToRadians(180.0) - cameraPitch;
				}
				else if (cameraPitch < MathX.ToRadians(-90.0))
				{
					cameraPitch = MathX.ToRadians(-180.0) - cameraPitch;
				}
				_trackNodes.Add(new TrackNode(nextPosition, new Vector2(yaw, cameraPitch)));
				position = nextPosition;
			}
		}
	}

	public override void Update()
	{
		KeyboardState keyboard = _gameContext.Input.Pressed.Keyboard;
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
		if (_ticks % 2 == 0)
		{
			_currentTrackNode = (_currentTrackNode + 1) % _trackNodes.Count;
		}
		UpdateCamera();
		_ticks++;
	}

	private void UpdateCamera()
	{
		// The track moves one node each two ticks. Interpolate between the current node and the next node.
		int nextIndex = Math.Min(_trackNodes.Count - 1, _currentTrackNode + 1);
		double progress = _ticks % 2 / 2.0;
		TrackNode currentNode = _trackNodes[_currentTrackNode];
		TrackNode nextNode = _trackNodes[nextIndex];
		Vector3 currentPosition = currentNode.Position + new Vector3(0.0, -2.0, 0.0);
		Vector2 currentAngle = currentNode.Angle;
		Vector3 nextPosition = nextNode.Position + new Vector3(0.0, -2.0, 0.0);
		Vector2 nextAngle = nextNode.Angle;
		Vector3 camera = currentPosition + (nextPosition - currentPosition) * progress;
		Vector2 angle = new Vector2(currentAngle.X + (nextAngle.X - currentAngle.X) * progress, currentAngle.Y + (nextAngle.Y - currentAngle.Y) * progress);
		Vector3 direction = new Vector3(Math.Sin(angle.X), Math.Sin(angle.Y), Math.Cos(angle.X));
		_camera = camera;
		_cameraTarget = _camera + direction;
	}

	public override void Draw(Renderer renderer)
	{
		DrawBackground(renderer);
		DrawPipe(renderer);
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

	private void DrawPipe(Renderer renderer)
	{
		_modelViewMatrix = Matrix4.LookAt(_camera.X, _camera.Y, 0.0 - _camera.Z, _cameraTarget.X, _cameraTarget.Y, 0.0 - _cameraTarget.Z, 0.0, 1.0, 0.0);
		_modelViewMatrix *= Matrix4.CreateScale(1.0, 1.0, -1.0);
		_graphicsContext.ClearDepthBuffer();
		_graphicsContext.DepthTesting = true;
		List<TexturedQuad> quads = new List<TexturedQuad>();
		double textureScale = 8.0 / _floorTexture.Width;
		// The number of track nodes that one repeat of the floor texture covers.
		int textureRepeatNodes = (int)(_floorTexture.Height * textureScale / 2.0);
		Vector3[] previousSection = null;
		for (int i = 0; i < _trackNodes.Count; i++)
		{
			if (i < _currentTrackNode - DrawDistance)
			{
				continue;
			}
			if (i > _currentTrackNode + DrawDistance)
			{
				break;
			}
			TrackNode trackNode = _trackNodes[i];
			Matrix4 transform = Matrix4.Identity.RotateY(trackNode.Angle.X).Translate(trackNode.Position);
			Vector3[] section = (from x in GetCrossSection()
								 select transform * x).ToArray();

			// Join this cross section to the previous one with floor quads.
			if (previousSection != null)
			{
				for (int j = 0; j < section.Length - 1; j++)
				{
					int textureRow = i % textureRepeatNodes;
					quads.Add(new TexturedQuad(new Vector3[4]
					{
						section[j],
						previousSection[j],
						previousSection[j + 1],
						section[j + 1]
					}, new Vector2[4]
					{
						new Vector2(j / (double)(section.Length - 1), 1.0 - (textureRow + 1) / (double)textureRepeatNodes),
						new Vector2(j / (double)(section.Length - 1), 1.0 - textureRow / (double)textureRepeatNodes),
						new Vector2((j + 1) / (double)(section.Length - 1), 1.0 - textureRow / (double)textureRepeatNodes),
						new Vector2((j + 1) / (double)(section.Length - 1), 1.0 - (textureRow + 1) / (double)textureRepeatNodes)
					}, _floorTexture));
				}
			}
			previousSection = section;

			// Put a row of rings across the pipe once in each texture repeat.
			if ((i + textureRepeatNodes / 2 + 1) % textureRepeatNodes != 0)
			{
				continue;
			}
			double ringHalfSize = 0.5;
			double ringRadius = 3.0;
			for (int ring = 0; ring < RingsPerRow; ring++)
			{
				double ringAngle = ring * (MathX.ToRadians(180.0) / 6.0);
				Vector3 ringCentre = new Vector3((0.0 - Math.Cos(ringAngle)) * ringRadius, Math.Sin(ringAngle) * ringRadius, 0.0);
				quads.Add(new TexturedQuad(new Vector3[4]
				{
					new Vector3(ringCentre.X - ringHalfSize, ringCentre.Y - ringHalfSize, 0.0),
					new Vector3(ringCentre.X - ringHalfSize, ringCentre.Y + ringHalfSize, 0.0),
					new Vector3(ringCentre.X + ringHalfSize, ringCentre.Y + ringHalfSize, 0.0),
					new Vector3(ringCentre.X + ringHalfSize, ringCentre.Y - ringHalfSize, 0.0)
				}.Select(x => transform * x).ToArray(), new Vector2[4]
				{
					new Vector2(0.0, 0.0),
					new Vector2(0.0, 1.0),
					new Vector2(1.0, 1.0),
					new Vector2(1.0, 0.0)
				}, _ballTexture));
			}
		}
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
		program.SetUniform("InputLightSource", _trackNodes[_currentTrackNode].Position + new Vector3(0.0, 0.0, 0.0));
		int firstQuad = 0;
		foreach (Tuple<int, ITexture> batch in batches)
		{
			_graphicsContext.SetTexture(batch.Item2);
			_vertexBuffer.Render(PrimitiveType.Quads, firstQuad * 4, batch.Item1 * 4);
			firstQuad += batch.Item1;
		}
		_graphicsContext.DepthTesting = false;
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
		renderer.GetFontRenderer().RenderString($"SEGMENT: {_currentTrackNode} / {_trackNodes.Count}", default, FontAlignment.Left, _font, 0);
	}
}
