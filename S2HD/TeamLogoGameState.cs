using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SonicOrca;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

#nullable disable
namespace S2HD;

/// <summary>
/// Shows the logo of the team that made the game. The logo fades in, stays for 4 seconds, and then fades out.
/// </summary>
internal class TeamLogoGameState : IGameState, IDisposable
{
	/// <summary>The duration of the fade in and of the fade out, in frames.</summary>
	private const int FadeTime = 60;
	/// <summary>The time that the screen stays fully visible, in frames.</summary>
	private const int ShowTime = 240;
	/// <summary>The change in opacity on each frame of a fade.</summary>
	private const float FadeStep = 1f / FadeTime;
	// This field holds the team logo. It keeps its old name because the resource attribute is on it.
	[ResourcePath("SONICORCA/TEAMLOGO")]
	private ITexture _disclaimerTexture;
	private readonly S2HDSonicOrcaGameContext _gameContext;
	private ResourceSession _resourceSession;
	private bool _loaded;
	private float _opacity;

	public TeamLogoGameState(S2HDSonicOrcaGameContext gameContext) => _gameContext = gameContext;

	public async Task LoadAsync()
	{
		ResourceTree resourceTree = _gameContext.ResourceTree;
		_resourceSession = new ResourceSession(resourceTree);
		_resourceSession.PushDependenciesByAttribute(this);
		await _resourceSession.LoadAsync();
		resourceTree.FullfillLoadedResourcesByAttribute(this);
		_loaded = true;
	}

	public void Dispose() => _resourceSession?.Dispose();

	public IEnumerable<UpdateResult> Update()
	{
		Task loadTask = LoadAsync();
		while (!loadTask.IsCompleted)
			yield return UpdateResult.Next();
		_opacity = 0.0f;
		while (_opacity < 1.0)
		{
			_opacity += FadeStep;
			_opacity = Math.Min(_opacity, 1f);
			yield return UpdateResult.Next();
		}
		int time = ShowTime;
		while (time > 0)
		{
			--time;
			yield return UpdateResult.Next();
		}
		while (_opacity > 0.0)
		{
			_opacity -= FadeStep;
			_opacity = Math.Max(_opacity, 0.0f);
			yield return UpdateResult.Next();
		}
	}

	public void Draw()
	{
		if (!_loaded)
			return;
		I2dRenderer g = _gameContext.Renderer.Get2dRenderer();
		Rectanglei screenBounds = (Rectanglei)new Rectangle(0.0, 0.0, 1920.0, 1080.0);
		g.BlendMode = BlendMode.Opaque;
		g.Colour = Colours.White;
		g.RenderTexture(_disclaimerTexture, (Vector2)screenBounds.Centre);
		Colour fadeColour = new Colour(1.0 - _opacity, 0.0, 0.0, 0.0);
		g.BlendMode = BlendMode.Alpha;
		g.RenderQuad(fadeColour, new Rectangle(0.0, 0.0, 1920.0, 1080.0));
	}
}
