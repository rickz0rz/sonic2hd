using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

namespace SonicOrca.Core;

public class ParticleManager : IDisposable
{
	private const string HeatParticleTextureResourceKey = "SONICORCA/PARTICLE/HEAT";

	private const string SmokeParticleTextureResourceKey = "SONICORCA/PARTICLE/SMOKE";

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
		_workCanvas = _gameContext.Window.GraphicsContext.CreateFrameBuffer(128, 128);
		_enabled = _gameContext.Configuration.GetPropertyBoolean("graphics", "particles", defaultValue: true);
	}

	public async Task LoadAsync(CancellationToken ct = default(CancellationToken))
	{
		ResourceTree resourceTree = _gameContext.ResourceTree;
		_resourceSession.PushDependencies("SONICORCA/PARTICLE/HEAT", "SONICORCA/PARTICLE/SMOKE");
		await _resourceSession.LoadAsync();
		_heatParticleTexture = resourceTree.GetLoadedResource<ITexture>("SONICORCA/PARTICLE/HEAT");
		_smokeParticleTexture = resourceTree.GetLoadedResource<ITexture>("SONICORCA/PARTICLE/SMOKE");
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
		return _particles.Count((Particle x) => x.Layer == layer);
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
		_particles.RemoveAll((Particle x) => x.Time <= 0);
	}

	public void Draw(Renderer renderer, Viewport viewport, LevelLayer layer, IVideoSettings videoSettings)
	{
		if (!_enabled || _particles.Count == 0)
		{
			return;
		}
		IFramebuffer currentFramebuffer = renderer.Window.GraphicsContext.CurrentFramebuffer;
		I2dRenderer i2dRenderer = renderer.Get2dRenderer();
		IHeatRenderer heatRenderer = renderer.GetHeatRenderer();
		heatRenderer.DistortionTexture = _heatParticleTexture;
		Matrix4 modelMatrix = i2dRenderer.ModelMatrix;
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
				renderer.DeativateRenderer();
				Rectanglei destination = ((Rectanglei)new Rectangle(particle.Position.X - (double)(_heatParticleTexture.Width / 2), particle.Position.Y - (double)(_heatParticleTexture.Height / 2), _heatParticleTexture.Width, _heatParticleTexture.Height)).OffsetBy(viewport.Bounds.TopLeft * -1);
				if (destination.IntersectsWith(new Rectanglei(0, 0, currentFramebuffer.Width, currentFramebuffer.Height)))
				{
					heatRenderer.DistortionAmount = 1.0 / (double)_heatParticleTexture.Width * 8.0;
					if (particle.Time < 60)
					{
						heatRenderer.DistortionAmount *= (double)particle.Time / 60.0;
					}
					_workCanvas.Activate();
					i2dRenderer.BlendMode = BlendMode.Opaque;
					i2dRenderer.Colour = Colours.White;
					i2dRenderer.ClipRectangle = new Rectangle(0.0, 0.0, 1920.0, 1080.0);
					i2dRenderer.ModelMatrix = Matrix4.Identity;
					i2dRenderer.RenderTexture(currentFramebuffer.Textures[0], new Rectanglei(destination.X, 1080 - destination.Bottom, destination.Width, destination.Height), new Rectanglei(0, 0, destination.Width, destination.Height));
					i2dRenderer.Deactivate();
					currentFramebuffer.Activate();
					heatRenderer.Render(_workCanvas.Textures[0], new Rectanglei(0, 0, destination.Width, destination.Height), destination);
				}
				break;
			}
			case ParticleType.Smoke:
			{
				Vector2 vector2 = particle.Position - viewport.Bounds.TopLeft;
				if (((Rectanglei)new Rectangle(vector2.X - (double)(_smokeParticleTexture.Width / 2), vector2.Y - (double)(_smokeParticleTexture.Height / 2), _smokeParticleTexture.Width, _smokeParticleTexture.Height)).IntersectsWith(new Rectanglei(0, 0, currentFramebuffer.Width, currentFramebuffer.Height)))
				{
					double a = ((particle.Time < 120) ? ((double)particle.Time / 120.0) : 1.0) * 0.2;
					currentFramebuffer.Activate();
					i2dRenderer.BlendMode = BlendMode.Alpha;
					i2dRenderer.Colour = new Colour(a, 1.0, 1.0, 1.0);
					i2dRenderer.ClipRectangle = new Rectangle(0.0, 0.0, 1920.0, 1080.0);
					i2dRenderer.ModelMatrix = Matrix4.CreateTranslation(vector2.X, vector2.Y) * Matrix4.CreateRotationZ(particle.Angle);
					i2dRenderer.RenderTexture(_smokeParticleTexture, default(Vector2), false, false);
				}
				break;
			}
			case ParticleType.Custom:
			{
				ITexture customTexture = particle.CustomTexture;
				if (customTexture != null)
				{
					Vector2 vector = particle.Position - viewport.Bounds.TopLeft;
					if (((Rectanglei)new Rectangle(vector.X - (double)(customTexture.Width / 2), vector.Y - (double)(customTexture.Height / 2), customTexture.Width, customTexture.Height)).IntersectsWith(new Rectanglei(0, 0, currentFramebuffer.Width, currentFramebuffer.Height)))
					{
						currentFramebuffer.Activate();
						i2dRenderer.BlendMode = BlendMode.Alpha;
						i2dRenderer.Colour = new Colour(1.0, 1.0, 1.0, 1.0);
						i2dRenderer.ClipRectangle = new Rectangle(0.0, 0.0, 1920.0, 1080.0);
						i2dRenderer.ModelMatrix = Matrix4.CreateTranslation(vector.X, vector.Y) * Matrix4.CreateRotationZ(particle.Angle) * Matrix4.CreateScale(particle.Size, particle.Size);
						i2dRenderer.RenderTexture(customTexture, default(Vector2), false, false);
					}
				}
				break;
			}
			}
		}
		renderer.DeativateRenderer();
		i2dRenderer.ModelMatrix = modelMatrix;
	}
}
