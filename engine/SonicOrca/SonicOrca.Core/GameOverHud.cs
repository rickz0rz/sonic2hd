using System;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca.Audio;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

namespace SonicOrca.Core;

public class GameOverHud : IDisposable
{
	public enum Reason
	{
		GameOver,
		TimeOver
	}

	private const string FontResourceKey = "SONICORCA/FONTS/METALIC";

	private const string JingleResourceKey = "SONICORCA/MUSIC/JINGLE/GAMEOVER/S1";

	private const int TotalTime = 900;

	private const int FlyTime = 15;

	private static readonly EaseTimeline LeftFly = new EaseTimeline(new EaseTimeline.Entry(0, 0.0), new EaseTimeline.Entry(15, 960.0), new EaseTimeline.Entry(885, 960.0), new EaseTimeline.Entry(900, 0.0));

	private static readonly EaseTimeline RightFly = new EaseTimeline(new EaseTimeline.Entry(0, 1920.0), new EaseTimeline.Entry(15, 960.0), new EaseTimeline.Entry(885, 960.0), new EaseTimeline.Entry(900, 1920.0));

	private readonly Level _level;

	private readonly ResourceSession _resourceSession;

	private Font _font;

	private Sample _jingle;

	private Reason _reason;

	private int _ticks;

	public bool Finished { get; private set; }

	public GameOverHud(Level level)
	{
		_level = level;
		_resourceSession = new ResourceSession(level.GameContext.ResourceTree);
	}

	public async Task LoadAsync(CancellationToken ct = default(CancellationToken))
	{
		ResourceTree resourceTree = _resourceSession.ResourceTree;
		_resourceSession.PushDependencies("SONICORCA/FONTS/METALIC", "SONICORCA/MUSIC/JINGLE/GAMEOVER/S1");
		await _resourceSession.LoadAsync(ct);
		_font = resourceTree.GetLoadedResource<Font>("SONICORCA/FONTS/METALIC");
		_jingle = resourceTree.GetLoadedResource<Sample>("SONICORCA/MUSIC/JINGLE/GAMEOVER/S1");
	}

	public void Dispose()
	{
		_resourceSession.Dispose();
	}

	public void Start(Reason reason)
	{
		_ticks = 0;
		_reason = reason;
		Finished = false;
		_level.SoundManager.StopAll();
		_level.SoundManager.PlayJingleOnce(_jingle);
	}

	public void Update()
	{
		_ticks++;
		if (_ticks >= 900)
		{
			Finished = true;
		}
	}

	public void Draw(Renderer renderer)
	{
		IFontRenderer fontRenderer = renderer.GetFontRenderer();
		int num = _font.DefaultWidth / 2;
		fontRenderer.RenderStringWithShadow((_reason == Reason.GameOver) ? "GAME" : "TIME", new Rectangle(LeftFly.GetValueAt(_ticks) - (double)num, 0.0, 0.0, 1080.0), (FontAlignment)6, _font, Colours.White);
		fontRenderer.RenderStringWithShadow("OVER", new Rectangle(RightFly.GetValueAt(_ticks) + (double)num, 0.0, 0.0, 1080.0), FontAlignment.MiddleY, _font, Colours.White);
	}
}
