// Decompiled with JetBrains decompiler
// Type: S2HD.DisclaimerGameState
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

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

internal class DisclaimerGameState : IGameState, IDisposable
{
	private const int FadeTime = 60;
	private const int ShowTime = 240 /*0xF0*/;
	[ResourcePath("SONICORCA/DISCLAIMER")]
	private ITexture _disclaimerTexture;
	private S2HDSonicOrcaGameContext _gameContext;
	private ResourceSession _resourceSession;
	private bool _loaded;
	private float _opacity;

	public DisclaimerGameState(S2HDSonicOrcaGameContext gameContext)
	{
		_gameContext = gameContext;
	}

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
		yield return UpdateResult.Wait(60);
		_opacity = 0.0f;
		while (_opacity < 1.0)
		{
			_opacity += 0.0166666675f;
			_opacity = Math.Min(_opacity, 1f);
			yield return UpdateResult.Next();
		}
		int time = 240 /*0xF0*/;
		while (time > 0)
		{
			--time;
			yield return UpdateResult.Next();
		}
		while (_opacity > 0.0)
		{
			_opacity -= 0.0166666675f;
			_opacity = Math.Max(_opacity, 0.0f);
			yield return UpdateResult.Next();
		}
	}

	public void Draw()
	{
		if (!_loaded)
			return;
		I2dRenderer obj = _gameContext.Renderer.Get2dRenderer();
		Rectanglei rectanglei = (Rectanglei)new Rectangle(0.0, 0.0, 1920.0, 1080.0);
		obj.BlendMode = BlendMode.Opaque;
		obj.Colour = Colours.White;
		obj.RenderTexture(_disclaimerTexture, (Vector2)rectanglei.Centre);
		Colour colour = new Colour(1.0 - _opacity, 0.0, 0.0, 0.0);
		obj.BlendMode = BlendMode.Alpha;
		obj.RenderQuad(colour, new Rectangle(0.0, 0.0, 1920.0, 1080.0));
	}
}
