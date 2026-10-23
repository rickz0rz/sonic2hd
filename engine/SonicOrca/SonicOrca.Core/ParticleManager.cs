using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

namespace SonicOrca.Core;

/// <summary>
/// Keeps, updates and draws the particles of a level.
/// </summary>
public class ParticleManager : IDisposable
{
	private const string HeatParticleTextureResourceKey = "SONICORCA/PARTICLE/HEAT";

	private const string SmokeParticleTextureResourceKey = "SONICORCA/PARTICLE/SMOKE";

	/// <summary>
	/// The width and height of the framebuffer that holds a copy of the screen area behind a heat particle.
	/// </summary>
	private const int WorkCanvasSize = 128;

	private const int ScreenWidth = 1920;

	private const int ScreenHeight = 1080;

	/// <summary>
	/// A heat particle fades out during its last frames.
	/// </summary>
	private const int HeatFadeOutTime = 60;

	/// <summary>
	/// A smoke particle fades out during its last frames.
	/// </summary>
	private const int SmokeFadeOutTime = 120;

	private const double SmokeOpacity = 0.2;

	private readonly SonicOrcaGameContext _gameContext;

	private readonly Level _level;

	private readonly Random _random = SonicOrca.Core.Debugging.TestHooks.CreateRandom();

	private readonly List<Particle> _particles = new List<Particle>();

	private readonly ResourceSession _resourceSession;

	private ITexture _heatParticleTexture;

	private ITexture _smokeParticleTexture;

	private IFramebuffer _workCanvas;

	private bool _enabled;

	public Random Random => _random;

	public ParticleManager(Level level)
	{
		_gameContext = level.GameContext;
		_level = level;
		_resourceSession = new ResourceSession(_gameContext.ResourceTree);
		_workCanvas = _gameContext.Window.GraphicsContext.CreateFrameBuffer(WorkCanvasSize, WorkCanvasSize);
		_enabled = _gameContext.Configuration.GetPropertyBoolean("graphics", "particles", defaultValue: true);
	}

	public async Task LoadAsync(CancellationToken ct = default(CancellationToken))
	{
		ResourceTree resourceTree = _gameContext.ResourceTree;
		_resourceSession.PushDependencies(HeatParticleTextureResourceKey, SmokeParticleTextureResourceKey);
		await _resourceSession.LoadAsync();
		_heatParticleTexture = resourceTree.GetLoadedResource<ITexture>(HeatParticleTextureResourceKey);
		_smokeParticleTexture = resourceTree.GetLoadedResource<ITexture>(SmokeParticleTextureResourceKey);
	}

	public void Dispose()
	{
		_resourceSession.Dispose();
		if (_workCanvas != null)
		{
			_workCanvas.Dispose();
		}
	}

	public int GetNumParticlesOnLayer(LevelLayer layer)
	{
		return _particles.Count(x => x.Layer == layer);
	}

	public void Add(Particle p)
	{
		if (_enabled)
		{
			_particles.Add(p);
		}
	}

	public void Clear()
	{
		_particles.Clear();
	}

	public void Update()
	{
		if (!_enabled)
		{
			return;
		}
		foreach (Particle particle in _particles)
		{
			particle.Update();
		}
		_particles.RemoveAll(x => x.Time <= 0);
	}

	public void Draw(Renderer renderer, Viewport viewport, LevelLayer layer, IVideoSettings videoSettings)
	{
		if (!_enabled || _particles.Count == 0)
		{
			return;
		}
		IFramebuffer currentFramebuffer = renderer.Window.GraphicsContext.CurrentFramebuffer;
		I2dRenderer renderer2d = renderer.Get2dRenderer();
		IHeatRenderer heatRenderer = renderer.GetHeatRenderer();
		heatRenderer.DistortionTexture = _heatParticleTexture;
		Matrix4 modelMatrix = renderer2d.ModelMatrix;
		foreach (Particle particle in _particles)
		{
			if (particle.Layer != layer)
			{
				continue;
			}
			switch (particle.Type)
			{
				case ParticleType.Heat:
					{
						if (!videoSettings.EnableHeatEffects)
						{
							break;
						}
						// Copy the screen area behind the particle to the work canvas.
						// Then draw the copy back with a distortion.
						renderer.DeativateRenderer();
						Rectanglei destination = ((Rectanglei)new Rectangle(particle.Position.X - _heatParticleTexture.Width / 2, particle.Position.Y - _heatParticleTexture.Height / 2, _heatParticleTexture.Width, _heatParticleTexture.Height)).OffsetBy(viewport.Bounds.TopLeft * -1);
						if (destination.IntersectsWith(new Rectanglei(0, 0, currentFramebuffer.Width, currentFramebuffer.Height)))
						{
							heatRenderer.DistortionAmount = 1.0 / _heatParticleTexture.Width * 8.0;
							if (particle.Time < HeatFadeOutTime)
							{
								heatRenderer.DistortionAmount *= particle.Time / (double)HeatFadeOutTime;
							}
							_workCanvas.Activate();
							renderer2d.BlendMode = BlendMode.Opaque;
							renderer2d.Colour = Colours.White;
							renderer2d.ClipRectangle = new Rectangle(0.0, 0.0, ScreenWidth, ScreenHeight);
							renderer2d.ModelMatrix = Matrix4.Identity;
							// The framebuffer texture is upside down, so the source Y counts from the bottom.
							renderer2d.RenderTexture(currentFramebuffer.Textures[0], new Rectanglei(destination.X, ScreenHeight - destination.Bottom, destination.Width, destination.Height), new Rectanglei(0, 0, destination.Width, destination.Height));
							renderer2d.Deactivate();
							currentFramebuffer.Activate();
							heatRenderer.Render(_workCanvas.Textures[0], new Rectanglei(0, 0, destination.Width, destination.Height), destination);
						}
						break;
					}
				case ParticleType.Smoke:
					{
						Vector2 screenPosition = particle.Position - viewport.Bounds.TopLeft;
						if (((Rectanglei)new Rectangle(screenPosition.X - _smokeParticleTexture.Width / 2, screenPosition.Y - _smokeParticleTexture.Height / 2, _smokeParticleTexture.Width, _smokeParticleTexture.Height)).IntersectsWith(new Rectanglei(0, 0, currentFramebuffer.Width, currentFramebuffer.Height)))
						{
							double opacity = ((particle.Time < SmokeFadeOutTime) ? (particle.Time / (double)SmokeFadeOutTime) : 1.0) * SmokeOpacity;
							currentFramebuffer.Activate();
							renderer2d.BlendMode = BlendMode.Alpha;
							renderer2d.Colour = new Colour(opacity, 1.0, 1.0, 1.0);
							renderer2d.ClipRectangle = new Rectangle(0.0, 0.0, ScreenWidth, ScreenHeight);
							renderer2d.ModelMatrix = Matrix4.CreateTranslation(screenPosition.X, screenPosition.Y) * Matrix4.CreateRotationZ(particle.Angle);
							renderer2d.RenderTexture(_smokeParticleTexture, default(Vector2), false, false);
						}
						break;
					}
				case ParticleType.Custom:
					{
						ITexture customTexture = particle.CustomTexture;
						if (customTexture != null)
						{
							Vector2 screenPosition = particle.Position - viewport.Bounds.TopLeft;
							if (((Rectanglei)new Rectangle(screenPosition.X - customTexture.Width / 2, screenPosition.Y - customTexture.Height / 2, customTexture.Width, customTexture.Height)).IntersectsWith(new Rectanglei(0, 0, currentFramebuffer.Width, currentFramebuffer.Height)))
							{
								currentFramebuffer.Activate();
								renderer2d.BlendMode = BlendMode.Alpha;
								renderer2d.Colour = new Colour(1.0, 1.0, 1.0, 1.0);
								renderer2d.ClipRectangle = new Rectangle(0.0, 0.0, ScreenWidth, ScreenHeight);
								renderer2d.ModelMatrix = Matrix4.CreateTranslation(screenPosition.X, screenPosition.Y) * Matrix4.CreateRotationZ(particle.Angle) * Matrix4.CreateScale(particle.Size, particle.Size);
								renderer2d.RenderTexture(customTexture, default(Vector2), false, false);
							}
						}
						break;
					}
			}
		}
		renderer.DeativateRenderer();
		renderer2d.ModelMatrix = modelMatrix;
	}
}
