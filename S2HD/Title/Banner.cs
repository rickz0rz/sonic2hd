using System;
using System.Collections.Generic;
using SonicOrca;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

#nullable disable
namespace S2HD.Title;

/// <summary>
/// The "Sonic the Hedgehog 2 HD" banner of the title screen, with Sonic and Tails, the spinning stars before
/// the banner appears, and the shine that moves over the title.
/// </summary>
internal class Banner
{
	/// <summary>The tick when the banner starts to fade in.</summary>
	private const int BannerStartTime = 332;
	/// <summary>The positions of the stars that spin before the banner appears, relative to the banner centre.</summary>
	private static readonly Vector2i[] SpinningStarOffsets = new Vector2i[8]
	{
		new Vector2i(-214, -41),
		new Vector2i(-190, -129),
		new Vector2i(-130, -193),
		new Vector2i(-50, -233),
		new Vector2i(50, -233),
		new Vector2i(130, -193),
		new Vector2i(190, -129),
		new Vector2i(214, -41)
	};
	private readonly S2HDSonicOrcaGameContext _gameContext;
	private readonly IMaskRenderer _maskRenderer;
	private AnimationGroup _animationGroup;
	[ResourcePath("SONICORCA/TITLE/FRAMES/0")]
	private ITexture _bannerTexture;
	[ResourcePath("SONICORCA/TITLE/FRAMES/1")]
	private ITexture _bannerInsideTexture;
	[ResourcePath("SONICORCA/TITLE/ADDFRAMES/0")]
	private ITexture _hdStarTexture;
	[ResourcePath("SONICORCA/TITLE/ADDFRAMES/1")]
	private ITexture _maskTexture;
	[ResourcePath("SONICORCA/TITLE/ADDFRAMES/2")]
	private ITexture _titleOutlineTexture;
	private AnimationInstance[] _preBannerStarSpin;
	private AnimationInstance _sonicAnimationInstance;
	private AnimationInstance _sonicHandAnimationInstance;
	private AnimationInstance _tailsAnimationInstance;
	private AnimationInstance _tailsTailsAnimationInstance;
	private AnimationInstance _sonic2AnimationInstance;
	private AnimationInstance _theHedgehogAnimationInstance;
	private AnimationInstance _hdAnimationInstance;
	private readonly EffectEventManager _effectEventManager = new EffectEventManager();
	private int _ticks;
	private double _bannerInsideOpacity;
	private double _bannerOpacity;
	private double _sonic2Opacity;
	private double _maskOffset;

	public Vector2i Position { get; set; }

	public bool ShowStarLensFare { get; set; }

	public Banner(S2HDSonicOrcaGameContext gameContext, IMaskRenderer maskRenderer)
	{
		_gameContext = gameContext;
		_maskRenderer = maskRenderer;
		GetResourceReferences();
		Position = new Vector2i(960, 476);
	}

	/// <remarks>
	/// The resource attributes on the texture fields are not used. This method sets the fields, and it gives
	/// _titleOutlineTexture and _hdStarTexture the opposite keys of their attributes.
	/// </remarks>
	private void GetResourceReferences()
	{
		ResourceTree resourceTree = _gameContext.ResourceTree;
		_animationGroup = resourceTree.GetLoadedResource<AnimationGroup>(TitleResources.AnimationGroupResourceKey);
		_bannerTexture = resourceTree.GetLoadedResource<ITexture>("SONICORCA/TITLE/FRAMES/0");
		_bannerInsideTexture = resourceTree.GetLoadedResource<ITexture>("SONICORCA/TITLE/FRAMES/1");
		_titleOutlineTexture = resourceTree.GetLoadedResource<ITexture>("SONICORCA/TITLE/ADDFRAMES/0");
		_maskTexture = resourceTree.GetLoadedResource<ITexture>("SONICORCA/TITLE/ADDFRAMES/1");
		_hdStarTexture = resourceTree.GetLoadedResource<ITexture>("SONICORCA/TITLE/ADDFRAMES/2");
	}

	public void Reset()
	{
		_ticks = 0;
		_preBannerStarSpin = new AnimationInstance[8];
		_sonicAnimationInstance = null;
		_sonicHandAnimationInstance = null;
		_tailsAnimationInstance = null;
		_tailsTailsAnimationInstance = null;
		_sonic2AnimationInstance = null;
		_theHedgehogAnimationInstance = null;
		_hdAnimationInstance = null;
		_bannerInsideOpacity = 0.0;
		_bannerOpacity = 0.0;
		_sonic2Opacity = 0.0;
		_maskOffset = -1.0;
		ShowStarLensFare = false;
	}

	/// <summary>Starts the shine that moves over the title. It takes <paramref name="duration"/> ticks.</summary>
	public void DoShine(int duration)
	{
		_effectEventManager.BeginEvent(EffectShine(duration));
	}

	private IEnumerable<UpdateResult> EffectShine(int duration)
	{
		_maskOffset = -1.0;
		do
		{
			yield return UpdateResult.Next();
			_maskOffset += 2.0 / duration;
		}
		while (_maskOffset < 1.0);
		_maskOffset = 1.0;
	}

	public void Update()
	{
		if (_ticks == 268)
			DoShine(368);
		if (_ticks >= 296)
			_bannerInsideOpacity = Math.Min(1.0, _bannerInsideOpacity + 1.0 / 72.0);
		if (_ticks >= BannerStartTime)
			_bannerOpacity = Math.Min(1.0, _bannerOpacity + 1.0 / 32.0);
		for (int index = 0; index < _preBannerStarSpin.Length; ++index)
		{
			if (_preBannerStarSpin[index] == null)
			{
				// The stars start one after the other, 4 ticks apart.
				if (_ticks >= 298 + index * 4)
					_preBannerStarSpin[index] = new AnimationInstance(_animationGroup, TitleResources.SpinningStarAnimationIndex);
			}
			else if (_preBannerStarSpin[index].Cycles == 0)
			{
				_preBannerStarSpin[index].Animate();
			}
		}
		if (_ticks >= 346)
		{
			_sonic2Opacity = Math.Min(1.0, _sonic2Opacity + 1.0 / 16.0);
			if (_sonic2AnimationInstance == null)
				_sonic2AnimationInstance = new AnimationInstance(_animationGroup, TitleResources.Sonic2AnimationIndex);
		}
		if (_ticks >= 360)
		{
			if (_theHedgehogAnimationInstance == null)
				_theHedgehogAnimationInstance = new AnimationInstance(_animationGroup, TitleResources.TheHedgehogAnimationIndex);
			_theHedgehogAnimationInstance.Animate();
		}
		if (_ticks >= 364)
		{
			if (_sonicAnimationInstance == null)
				_sonicAnimationInstance = new AnimationInstance(_animationGroup, TitleResources.SonicAnimationIndex);
			_sonicAnimationInstance.Animate();
		}
		if (_ticks >= 413)
		{
			if (_sonicHandAnimationInstance == null)
				_sonicHandAnimationInstance = new AnimationInstance(_animationGroup, TitleResources.SonicHandAnimationIndex);
			_sonicHandAnimationInstance.Animate();
		}
		if (_ticks >= 400)
		{
			if (_tailsAnimationInstance == null)
			{
				_tailsAnimationInstance = new AnimationInstance(_animationGroup, TitleResources.TailsAnimationIndex);
				_tailsTailsAnimationInstance = new AnimationInstance(_animationGroup, TitleResources.TailsTailsAnimationIndex);
			}
			_tailsAnimationInstance.Animate();
			_tailsTailsAnimationInstance.Animate();
		}
		if (_ticks >= 436)
			_sonic2AnimationInstance.Animate();
		if (_ticks >= 454)
		{
			if (_hdAnimationInstance == null)
				_hdAnimationInstance = new AnimationInstance(_animationGroup);
			_hdAnimationInstance.Animate();
		}
		_effectEventManager.Update();
		++_ticks;
	}

	public void Draw(Renderer renderer)
	{
		I2dRenderer g = renderer.Get2dRenderer();
		g.BlendMode = BlendMode.Alpha;
		using (g.BeginMatixState())
		{
			// Draw everything relative to the banner centre.
			g.ModelMatrix = g.ModelMatrix.Translate(Position.X, Position.Y);
			if (_bannerInsideOpacity > 0.0 && _bannerOpacity < 1.0)
			{
				g.Colour = new Colour(_bannerInsideOpacity, 1.0, 1.0, 1.0);
				g.RenderTexture(_bannerInsideTexture, new Vector2(0.0, -114.0));
			}
			if (_bannerOpacity > 0.0)
			{
				g.Colour = new Colour(_bannerOpacity, 1.0, 1.0, 1.0);
				g.RenderTexture(_bannerTexture, new Vector2(0.0, 0.0));
			}
			for (int index = 0; index < 8; ++index)
			{
				if (_preBannerStarSpin[index] != null && _preBannerStarSpin[index].Cycles == 0)
					_preBannerStarSpin[index].Draw(g, (Vector2)SpinningStarOffsets[index]);
			}
			DrawSonicAndTails(renderer);
			if (_sonic2AnimationInstance != null)
				_sonic2AnimationInstance.Draw(g, new Colour(_sonic2Opacity, 1.0, 1.0, 1.0), new Vector2(0.0, 77.0));
			if (_theHedgehogAnimationInstance != null)
				_theHedgehogAnimationInstance.Draw(g, new Vector2(0.0, 153.0));
			if (_hdAnimationInstance != null)
				_hdAnimationInstance.Draw(g, new Vector2(0.0, 266.0));
			if (!ShowStarLensFare)
				return;
			g.BlendMode = BlendMode.Additive;
			g.RenderTexture(_hdStarTexture, new Vector2(0.0, 188.0));
		}
	}

	public void DrawUnfaded(Renderer renderer) => DrawTitleShine(renderer);

	private void DrawSonicAndTails(Renderer renderer)
	{
		I2dRenderer g = renderer.Get2dRenderer();
		if (_sonicAnimationInstance != null)
			_sonicAnimationInstance.Draw(g, new Vector2(89.0, -158.0));
		if (_sonicHandAnimationInstance != null)
			_sonicHandAnimationInstance.Draw(g, new Vector2(230.0, -89.0));
		if (_tailsTailsAnimationInstance != null)
			_tailsTailsAnimationInstance.Draw(g, new Vector2(-264.0, -65.0));
		if (_tailsAnimationInstance == null)
			return;
		_tailsAnimationInstance.Draw(g, new Vector2(-108.0, -121.0));
	}

	/// <summary>
	/// Draws the shine over the title outline. A mask texture moves diagonally over the outline while
	/// _maskOffset goes from -1 to 1.
	/// </summary>
	private void DrawTitleShine(Renderer renderer)
	{
		if (_maskOffset <= -1.0 || _maskOffset >= 1.0)
			return;
		renderer.DeativateRenderer();
		_maskRenderer.Texture = _titleOutlineTexture;
		_maskRenderer.Source = new Rectanglei(0, 0, _titleOutlineTexture.Width, _titleOutlineTexture.Height);
		_maskRenderer.Destination = new Rectanglei(Position.X - _titleOutlineTexture.Width / 2, Position.Y - _titleOutlineTexture.Height / 2, _titleOutlineTexture.Width, _titleOutlineTexture.Height);
		_maskTexture.Wrapping = TextureWrapping.Clamp;
		_maskRenderer.MaskTexture = _maskTexture;
		_maskRenderer.MaskSource = new Rectanglei(0, 0, _maskTexture.Width, _maskTexture.Height);
		_maskRenderer.MaskDestination = new Rectanglei(Position.X + (int)(_maskOffset * 1920.0), Position.Y + (int)(_maskOffset * 1080.0), 1920, 1080);
		_maskRenderer.BlendMode = BlendMode.Additive;
		_maskRenderer.Render(true);
	}
}
