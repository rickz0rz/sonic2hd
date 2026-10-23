// Decompiled with JetBrains decompiler
// Type: SonicOrca.Menu.SegaScreen
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca.Audio;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

#nullable disable
namespace SonicOrca.Menu;

internal class SegaScreen : Screen
{
	private const string LogoResourceKey = "SONICORCA/MENU/SEGA/SCREENLOGO";
	private const string SoundResourceKey = "SONICORCA/MENU/SEGA/SOUND";
	private readonly SonicOrcaGameContext _context;
	private readonly FadeTransition _fadeTransition = new FadeTransition(30);
	private ResourceSession _resourceSession;
	private ITexture _logoTexture;
	private Sample _segaSample;
	private SampleInstance _segaSampleInstance;
	private readonly FiniteStateMachine _stateMachine;
	private bool _cancelled;

	public SegaScreen(SonicOrcaGameContext context)
	{
		_context = context;
		_fadeTransition.Set();
		_stateMachine = CreateFSM();
	}

	public override async Task LoadAsync(ScreenLoadingProgress progress, CancellationToken ct = default(CancellationToken))
	{
		ResourceTree resourceTree = _context.ResourceTree;
		_resourceSession = new ResourceSession(resourceTree);
		_resourceSession.PushDependencies("SONICORCA/MENU/SEGA/SCREENLOGO", "SONICORCA/MENU/SEGA/SOUND");
		await _resourceSession.LoadAsync(ct);
		_logoTexture = resourceTree.GetLoadedResource<ITexture>("SONICORCA/MENU/SEGA/SCREENLOGO");
		_segaSample = resourceTree.GetLoadedResource<Sample>("SONICORCA/MENU/SEGA/SOUND");
	}

	public override void Unload()
	{
		if (_segaSampleInstance != null)
			_segaSampleInstance.Dispose();
		if (_resourceSession == null)
			return;
		_resourceSession.Unload();
	}

	private FiniteStateMachine CreateFSM()
	{
		int cancelWait = 0;
		FiniteStateMachine finiteStateMachine = new FiniteStateMachine();
		finiteStateMachine.Begin().Do(() => cancelWait = 120).While(() => cancelWait-- >= 0 && !_cancelled).Do(() =>
		{
			if (_cancelled)
				finiteStateMachine.Finish();
			else
				_fadeTransition.BeginFadeIn();
		}).While(() => !_fadeTransition.Finished, () =>
		{
			if (_cancelled)
			{
				_fadeTransition.BeginFadeOut();
				finiteStateMachine.Begin().Do(() => _fadeTransition.BeginFadeOut()).While(() => !_fadeTransition.Finished, () => _fadeTransition.Update()).Do(() => finiteStateMachine.Finish());
			}
			else
				_fadeTransition.Update();
		}).Do(() =>
		{
			_segaSampleInstance = new SampleInstance(_context, _segaSample);
			_segaSampleInstance.Play();
		}).While(() => _segaSampleInstance.Playing).Do(() => cancelWait = 60).While(() => cancelWait-- >= 0 && !_cancelled).Do(() => _fadeTransition.BeginFadeOut()).While(() => !_fadeTransition.Finished, () => _fadeTransition.Update());
		return finiteStateMachine;
	}

	public override void Update()
	{
		if (_context.Pressed[0].Start)
			_cancelled = true;
		if (_stateMachine.Update())
			return;
		Finish();
	}

	public override void Draw(Renderer renderer)
	{
		I2dRenderer obj = renderer.Get2dRenderer();
		obj.RenderQuad(Colours.White, new Rectangle(0.0, 0.0, 1920.0, 1080.0));
		obj.RenderTexture(_logoTexture, new Vector2(960.0, 540.0));
		_fadeTransition.Draw(renderer);
	}
}
