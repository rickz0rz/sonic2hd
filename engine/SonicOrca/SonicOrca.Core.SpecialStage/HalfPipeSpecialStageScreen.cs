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

	private struct TrackNode
	{
		public Vector3 Position { get; set; }

		public Vector2 Angle { get; set; }

		public TrackNode(Vector3 position, Vector2 angle)
		{
			this = default;
			Position = position;
			Angle = angle;
		}

		public override string ToString()
		{
			return $"{Position}, {MathX.ToDegrees(Angle.X):0} DEG, {MathX.ToDegrees(Angle.Y):0} DEG";
		}
	}

	private struct TexturedQuad
	{
		public Vector3[] Position { get; set; }

		public Vector2[] TextureMapping { get; set; }

		public ITexture Texture { get; set; }

		public TexturedQuad(Vector3[] position, Vector2[] textureMapping, ITexture texture)
		{
			this = default;
			Position = position;
			TextureMapping = textureMapping;
			Texture = texture;
		}
	}

	private const string MusicResourceKey = "SONICORCA/MUSIC/SPECIALSTAGE/S2";

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

	public override async Task LoadAsync(ScreenLoadingProgress progress, CancellationToken ct = default(CancellationToken))
	{
		_resourceSession.PushDependency("SONICORCA/FONTS/HUD");
		_resourceSession.PushDependency("SONICORCA/SPECIALSTAGE/HALFPIPE/BACKGROUND");
		_resourceSession.PushDependency("SONICORCA/SPECIALSTAGE/HALFPIPE/FLOOR");
		_resourceSession.PushDependency("SONICORCA/SPECIALSTAGE/HALFPIPE/BALL");
		_resourceSession.PushDependency("SONICORCA/MUSIC/SPECIALSTAGE/S2");
		await _resourceSession.LoadAsync(ct);
		_font = _gameContext.ResourceTree.GetLoadedResource<Font>("SONICORCA/FONTS/HUD");
		_backgroundTexture = _gameContext.ResourceTree.GetLoadedResource<ITexture>("SONICORCA/SPECIALSTAGE/HALFPIPE/BACKGROUND");
		_floorTexture = _gameContext.ResourceTree.GetLoadedResource<ITexture>("SONICORCA/SPECIALSTAGE/HALFPIPE/FLOOR");
		_ballTexture = _gameContext.ResourceTree.GetLoadedResource<ITexture>("SONICORCA/SPECIALSTAGE/HALFPIPE/BALL");
		_musicSampleInfo = _gameContext.ResourceTree.GetLoadedResource<SampleInfo>("SONICORCA/MUSIC/SPECIALSTAGE/S2");
	}

	public override void Initialise()
	{
		_pipeShader = OrcaShader.CreateFromFile(_graphicsContext, "shaders/halfpipe/test.shader");
		_objectShader = OrcaShader.CreateFromFile(_graphicsContext, "shaders/halfpipe/object.shader");
		_vertexBuffer = _graphicsContext.CreateVertexBuffer(3, 2);
		_projectionMatrix = Matrix4.Perspective(MathX.ToRadians(_fov), 1.7777777777777777, 1.0, 1024.0);
		CreateTrackNodes();
	}

	private void CreateTrackNodes()
	{
		double num = 0.0;
		Vector3 vector = default;
		SegmentGeometry[] layout = _layout;
		foreach (SegmentGeometry segmentGeometry in layout)
		{
			double y = vector.Y;
			double num2 = MathX.ToRadians(90.0);
			for (int j = 0; j < 28; j++)
			{
				switch (segmentGeometry)
				{
				case SegmentGeometry.TurnLeft:
					num -= MathX.ToRadians(3.2142857142857144);
					break;
				case SegmentGeometry.TurnRight:
					num += MathX.ToRadians(3.2142857142857144);
					break;
				case SegmentGeometry.Rise:
					num2 += MathX.ToRadians(6.428571428571429);
					_ = Math.Cos(num2) / -2.0;
					break;
				case SegmentGeometry.Drop:
					num2 -= MathX.ToRadians(6.428571428571429);
					_ = Math.Cos(num2) / -2.0;
					break;
				}
				Vector3 vector2 = vector + new Vector3(Math.Sin(num), 0.0 - Math.Cos(num2), Math.Cos(num)) * 2.0;
				double num3 = num2 - MathX.ToRadians(90.0);
				if (num3 > MathX.ToRadians(90.0))
				{
					num3 = MathX.ToRadians(180.0) - num3;
				}
				else if (num3 < MathX.ToRadians(-90.0))
				{
					num3 = MathX.ToRadians(-180.0) - num3;
				}
				_trackNodes.Add(new TrackNode(vector2, new Vector2(num, num3)));
				vector = vector2;
			}
		}
	}

	public override void Update()
	{
		KeyboardState keyboard = _gameContext.Input.Pressed.Keyboard;
		if (keyboard[46])
		{
			_fov++;
		}
		else if (keyboard[45])
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
		int index = Math.Min(_trackNodes.Count - 1, _currentTrackNode + 1);
		double num = (double)(_ticks % 2) / 2.0;
		TrackNode trackNode = _trackNodes[_currentTrackNode];
		TrackNode trackNode2 = _trackNodes[index];
		Vector3 vector = trackNode.Position + new Vector3(0.0, -2.0, 0.0);
		Vector2 angle = trackNode.Angle;
		Vector3 vector2 = trackNode2.Position + new Vector3(0.0, -2.0, 0.0);
		Vector2 angle2 = trackNode2.Angle;
		Vector3 camera = vector + (vector2 - vector) * num;
		Vector2 vector3 = new Vector2(angle.X + (angle2.X - angle.X) * num, angle.Y + (angle2.Y - angle.Y) * num);
		Vector3 vector4 = new Vector3(Math.Sin(vector3.X), Math.Sin(vector3.Y), Math.Cos(vector3.X));
		_camera = camera;
		_cameraTarget = _camera + vector4;
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
		List<TexturedQuad> list = new List<TexturedQuad>();
		double num = 8.0 / (double)_floorTexture.Width;
		int num2 = (int)((double)_floorTexture.Height * num / 2.0);
		Vector3[] array = null;
		for (int i = 0; i < _trackNodes.Count; i++)
		{
			if (i < _currentTrackNode - 128)
			{
				continue;
			}
			if (i > _currentTrackNode + 128)
			{
				break;
			}
			TrackNode trackNode = _trackNodes[i];
			Matrix4 transform = Matrix4.Identity.RotateY(trackNode.Angle.X).Translate(trackNode.Position);
			Vector3[] array2 = (from x in GetCrossSection()
				select transform * x).ToArray();
			if (array != null)
			{
				for (int num3 = 0; num3 < array2.Length - 1; num3++)
				{
					int num4 = i % num2;
					list.Add(new TexturedQuad(new Vector3[4]
					{
						array2[num3],
						array[num3],
						array[num3 + 1],
						array2[num3 + 1]
					}, new Vector2[4]
					{
						new Vector2((double)num3 / (double)(array2.Length - 1), 1.0 - (double)(num4 + 1) / (double)num2),
						new Vector2((double)num3 / (double)(array2.Length - 1), 1.0 - (double)num4 / (double)num2),
						new Vector2((double)(num3 + 1) / (double)(array2.Length - 1), 1.0 - (double)num4 / (double)num2),
						new Vector2((double)(num3 + 1) / (double)(array2.Length - 1), 1.0 - (double)(num4 + 1) / (double)num2)
					}, _floorTexture));
				}
			}
			array = array2;
			if ((i + num2 / 2 + 1) % num2 != 0)
			{
				continue;
			}
			double num5 = 0.5;
			double num6 = 3.0;
			for (int num7 = 0; num7 < 7; num7++)
			{
				double num8 = (double)num7 * (MathX.ToRadians(180.0) / 6.0);
				Vector3 vector = new Vector3((0.0 - Math.Cos(num8)) * num6, Math.Sin(num8) * num6, 0.0);
				list.Add(new TexturedQuad(new Vector3[4]
				{
					new Vector3(vector.X - num5, vector.Y - num5, 0.0),
					new Vector3(vector.X - num5, vector.Y + num5, 0.0),
					new Vector3(vector.X + num5, vector.Y + num5, 0.0),
					new Vector3(vector.X + num5, vector.Y - num5, 0.0)
				}.Select((Vector3 x) => transform * x).ToArray(), new Vector2[4]
				{
					new Vector2(0.0, 0.0),
					new Vector2(0.0, 1.0),
					new Vector2(1.0, 1.0),
					new Vector2(1.0, 0.0)
				}, _ballTexture));
			}
		}
		List<Tuple<int, ITexture>> list2 = new List<Tuple<int, ITexture>>();
		int num9 = 0;
		ITexture texture = null;
		_vertexBuffer.Begin();
		foreach (TexturedQuad item in list.OrderByDescending((TexturedQuad x) => (_camera - x.Position[0]).Length))
		{
			for (int num10 = 0; num10 < 4; num10++)
			{
				_vertexBuffer.AddValue(0, item.Position[num10]);
				_vertexBuffer.AddValue(1, item.TextureMapping[num10]);
			}
			if (num9 == 0)
			{
				texture = item.Texture;
			}
			else if (texture != item.Texture)
			{
				list2.Add(new Tuple<int, ITexture>(num9, texture));
				num9 = 0;
				texture = item.Texture;
			}
			num9++;
		}
		if (num9 > 0)
		{
			list2.Add(new Tuple<int, ITexture>(num9, texture));
		}
		_vertexBuffer.End();
		IShaderProgram program = _pipeShader.Program;
		program.Activate();
		program.SetUniform("ProjectionMatrix", _projectionMatrix);
		program.SetUniform("ModelViewMatrix", _modelViewMatrix);
		program.SetUniform("InputTexture", 0);
		program.SetUniform("InputLightSource", _trackNodes[_currentTrackNode].Position + new Vector3(0.0, 0.0, 0.0));
		int num11 = 0;
		foreach (Tuple<int, ITexture> item2 in list2)
		{
			_graphicsContext.SetTexture(item2.Item2);
			_vertexBuffer.Render(PrimitiveType.Quads, num11 * 4, item2.Item1 * 4);
			num11 += item2.Item1;
		}
		_graphicsContext.DepthTesting = false;
	}

	private Vector3[] GetCrossSection()
	{
		Vector3[] array = new Vector3[32];
		for (int i = 0; i < array.Length; i++)
		{
			double num = (double)i / 31.0 * Math.PI;
			array[i] = new Vector3((0.0 - Math.Cos(num)) * 4.0, (0.0 - Math.Sin(num)) * 4.0, 0.0);
		}
		return array;
	}

	private void DrawHud(Renderer renderer)
	{
		renderer.GetFontRenderer().RenderString($"SEGMENT: {_currentTrackNode} / {_trackNodes.Count}", default, FontAlignment.Left, _font, 0);
	}
}
