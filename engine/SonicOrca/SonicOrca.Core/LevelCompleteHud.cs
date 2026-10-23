using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca.Audio;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

namespace SonicOrca.Core;

/// <summary>
/// Shows the act clear screen. It counts the time, ring and perfect bonuses into the score.
/// </summary>
internal class LevelCompleteHud : IDisposable
{
	private enum State
	{
		None,
		PreDelay,
		FlyIn,
		Banking,
		PostBankWait,
		FlyOut,
		Finished
	}

	private const string CaptionFontResourceKey = "SONICORCA/FONTS/TITLE/S2/NAME";

	private const string CaptionActFontResourceKey = "SONICORCA/FONTS/TITLE/S2/ACT";

	private const string StatisticsFontResourceKey = "SONICORCA/FONTS/HUD";

	private const string SonicTriangleResourceKey = "SONICORCA/HUD/TRIANGLE";

	private const string TailsTriangleResourceKey = "SONICORCA/HUD/TRIANGLE/TAILS";

	private const string KnucklesTriangleResourceKey = "SONICORCA/HUD/TRIANGLE/KNUCKLES";

	private const string JingleResourceKey = "SONICORCA/MUSIC/JINGLE/STAGECLEAR/S1";

	private const string TallySwitchResourceKey = "SONICORCA/SOUND/TALLY/SWITCH";

	private const string TallyEndResourceKey = "SONICORCA/SOUND/TALLY/END";

	// The time bonus for each 15 seconds of level time. The last value applies to all longer times.
	private static readonly IReadOnlyList<int> TimeBonusScores = new int[21]
	{
		50000, 50000, 10000, 5000, 4000, 4000, 3000, 3000, 2000, 2000,
		2000, 2000, 1000, 1000, 1000, 1000, 500, 500, 500, 500,
		0
	};

	private const int BankAmount = 100;

	private const int TicksPerSecond = 60;

	private const int TimeBonusStepSeconds = 15;

	private const int PointsPerRing = 100;

	private const int PerfectBonusPoints = 50000;

	private const int MusicFadeOutDuration = 60;

	private const int PreDelayTime = 90;

	private const int FlyInTime = 340;

	private const int PostBankWaitTime = 120;

	private const int FlyOutTime = 75;

	private const double ScreenWidth = 1920.0;

	private const double CaptionY = 270.0;

	private readonly Level _level;

	private ResourceSession _resourceSession;

	private Font _captionFont;

	private Font _captionActFont;

	private Font _statisticsFont;

	private ITexture _triangleTextureSonic;

	private ITexture _triangleTextureTails;

	private ITexture _triangleTextureKnuckles;

	private Sample _jingleSample;

	private Sample _tallySwitchSample;

	private Sample _tallyEndSample;

	private State _state;

	private int _ticks;

	private int _flyTicks;

	private int _shineTicks;

	private int _bankPointsDelay;

	private bool _achievedPerfect;

	private int _total;

	private int _tallySwitchDelay;

	private EaseTimeline _captionAFly;

	private EaseTimeline _captionBFly;

	private EaseTimeline _timeBonusFly;

	private EaseTimeline _ringBonusFly;

	private EaseTimeline _perfectBonusFly;

	private EaseTimeline _totalFly;

	private static readonly EaseTimeline CaptionAFlyIn = new EaseTimeline(new EaseTimeline.Entry(0, -1920.0), new EaseTimeline.Entry(40, 0.0));

	private static readonly EaseTimeline CaptionBFlyIn = new EaseTimeline(new EaseTimeline.Entry(0, 1920.0), new EaseTimeline.Entry(40, 0.0));

	private static readonly EaseTimeline TimeBonusFlyIn = new EaseTimeline(new EaseTimeline.Entry(40, 1920.0), new EaseTimeline.Entry(80, 0.0));

	private static readonly EaseTimeline RingBonusFlyIn = new EaseTimeline(new EaseTimeline.Entry(45, 1920.0), new EaseTimeline.Entry(85, 0.0));

	private static readonly EaseTimeline PerfectBonusFlyIn = new EaseTimeline(new EaseTimeline.Entry(50, 1920.0), new EaseTimeline.Entry(90, 0.0));

	private static readonly EaseTimeline TotalFlyIn = new EaseTimeline(new EaseTimeline.Entry(55, 1920.0), new EaseTimeline.Entry(95, 0.0));

	private static readonly EaseTimeline TotalFlyOut = new EaseTimeline(new EaseTimeline.Entry(0, 0.0), new EaseTimeline.Entry(40, 1920.0));

	private static readonly EaseTimeline PerfectBonusFlyOut = new EaseTimeline(new EaseTimeline.Entry(5, 0.0), new EaseTimeline.Entry(45, 1920.0));

	private static readonly EaseTimeline RingBonusFlyOut = new EaseTimeline(new EaseTimeline.Entry(10, 0.0), new EaseTimeline.Entry(50, 1920.0));

	private static readonly EaseTimeline TimeBonusFlyOut = new EaseTimeline(new EaseTimeline.Entry(15, 0.0), new EaseTimeline.Entry(55, 1920.0));

	private static readonly EaseTimeline CaptionBFlyOut = new EaseTimeline(new EaseTimeline.Entry(0, 0.0), new EaseTimeline.Entry(40, 1920.0));

	private static readonly EaseTimeline CaptionAFlyOut = new EaseTimeline(new EaseTimeline.Entry(0, 0.0), new EaseTimeline.Entry(40, -1920.0));

	private static readonly EaseTimeline TotalShine = new EaseTimeline(new EaseTimeline.Entry(0, 0.0), new EaseTimeline.Entry(10, 255.0), new EaseTimeline.Entry(20, 0.0));

	public int TimeBonus { get; set; }

	public int RingBonus { get; set; }

	public int PerfectBonus { get; set; }

	public bool Finished => _state == State.Finished;

	public LevelCompleteHud(Level level)
	{
		_level = level;
	}

	public async Task LoadAsync(CancellationToken ct = default(CancellationToken))
	{
		ResourceTree resourceTree = _level.GameContext.ResourceTree;
		_resourceSession = new ResourceSession(resourceTree);
		_resourceSession.PushDependencies(CaptionFontResourceKey, CaptionActFontResourceKey, StatisticsFontResourceKey, SonicTriangleResourceKey, TailsTriangleResourceKey, KnucklesTriangleResourceKey, JingleResourceKey, TallySwitchResourceKey, TallyEndResourceKey);
		await _resourceSession.LoadAsync(ct);
		_captionFont = resourceTree.GetLoadedResource<Font>(CaptionFontResourceKey);
		_captionActFont = resourceTree.GetLoadedResource<Font>(CaptionActFontResourceKey);
		_statisticsFont = resourceTree.GetLoadedResource<Font>(StatisticsFontResourceKey);
		_triangleTextureSonic = resourceTree.GetLoadedResource<ITexture>(SonicTriangleResourceKey);
		_triangleTextureTails = resourceTree.GetLoadedResource<ITexture>(TailsTriangleResourceKey);
		_triangleTextureKnuckles = resourceTree.GetLoadedResource<ITexture>(KnucklesTriangleResourceKey);
		_jingleSample = resourceTree.GetLoadedResource<Sample>(JingleResourceKey);
		_tallySwitchSample = resourceTree.GetLoadedResource<Sample>(TallySwitchResourceKey);
		_tallyEndSample = resourceTree.GetLoadedResource<Sample>(TallyEndResourceKey);
	}

	public void Dispose()
	{
		if (_resourceSession != null)
		{
			_resourceSession.Dispose();
		}
	}

	public void Start()
	{
		TimeBonus = TimeBonusScores[Math.Min(_level.Time / TicksPerSecond / TimeBonusStepSeconds, TimeBonusScores.Count - 1)];
		RingBonus = _level.Player.CurrentRings * PointsPerRing;
		if (_level.RingsCollected < _level.RingsPerfectTarget)
		{
			_achievedPerfect = false;
			PerfectBonus = 0;
		}
		else
		{
			_achievedPerfect = true;
			PerfectBonus = PerfectBonusPoints;
		}
		_total = 0;
		_state = State.PreDelay;
		_level.SoundManager.FadeOutMusic(MusicFadeOutDuration);
	}

	public void Update()
	{
		switch (_state)
		{
			case State.PreDelay:
				_ticks++;
				if (_ticks >= PreDelayTime)
				{
					_state = State.FlyIn;
					_flyTicks = 0;
					SetFlyInEasings();
					_level.SoundManager.PlayJingleOnce(_jingleSample);
				}
				break;
			case State.FlyIn:
				_flyTicks++;
				if (_flyTicks >= FlyInTime)
				{
					_state = State.Banking;
					_bankPointsDelay = 0;
				}
				_ticks++;
				break;
			case State.Banking:
				_bankPointsDelay--;
				if (_bankPointsDelay <= 0)
				{
					if (BankPoints())
					{
						_bankPointsDelay = 1;
					}
					else
					{
						_state = State.PostBankWait;
						_bankPointsDelay = PostBankWaitTime;
						_shineTicks = 0;
					}
				}
				_ticks++;
				break;
			case State.PostBankWait:
				_bankPointsDelay--;
				_shineTicks++;
				if (_bankPointsDelay <= 0)
				{
					_state = State.FlyOut;
					_flyTicks = 0;
					SetFlyOutEasings();
				}
				_ticks++;
				break;
			case State.FlyOut:
				_flyTicks++;
				if (_flyTicks >= FlyOutTime)
				{
					_level.SoundManager.StopAll();
					_state = State.Finished;
				}
				_ticks++;
				break;
		}
	}

	public void Animate()
	{
	}

	private void SetFlyInEasings()
	{
		_captionAFly = CaptionAFlyIn;
		_captionBFly = CaptionBFlyIn;
		_timeBonusFly = TimeBonusFlyIn;
		_ringBonusFly = RingBonusFlyIn;
		_perfectBonusFly = PerfectBonusFlyIn;
		_totalFly = TotalFlyIn;
	}

	private void SetFlyOutEasings()
	{
		_captionAFly = CaptionAFlyOut;
		_captionBFly = CaptionBFlyOut;
		_timeBonusFly = TimeBonusFlyOut;
		_ringBonusFly = RingBonusFlyOut;
		_perfectBonusFly = PerfectBonusFlyOut;
		_totalFly = TotalFlyOut;
	}

	private bool BankPoints()
	{
		// Move up to BankAmount points from each bonus into the total. Return false when all bonuses are empty.
		int timePoints = Math.Min(TimeBonus, BankAmount);
		int ringPoints = Math.Min(RingBonus, BankAmount);
		int perfectPoints = Math.Min(PerfectBonus, BankAmount);
		int bankedPoints = timePoints + ringPoints + perfectPoints;
		if (bankedPoints == 0)
		{
			_level.SoundManager.PlaySound(_tallyEndSample);
			return false;
		}
		TimeBonus -= timePoints;
		RingBonus -= ringPoints;
		PerfectBonus -= perfectPoints;
		_total += bankedPoints;
		_level.Player.Score += bankedPoints;
		// Play the tally sound on every second bank only.
		_tallySwitchDelay = (_tallySwitchDelay + 1) % 2;
		if (_tallySwitchDelay == 0)
		{
			_level.SoundManager.PlaySound(_tallySwitchSample);
		}
		return true;
	}

	public void Draw(Renderer renderer)
	{
		if (_state != State.PreDelay)
		{
			DrawCaption(renderer);
			DrawScoreLabels(renderer);
		}
	}

	private void DrawCaption(Renderer renderer)
	{
		double captionAX = _captionAFly.GetValueAt(_flyTicks);
		double captionBX = _captionBFly.GetValueAt(_flyTicks);
		string characterName = "SONIC";
		switch (_level.Player.ProtagonistCharacterType)
		{
			case CharacterType.Tails:
				characterName = "TAILS";
				break;
			case CharacterType.Knuckles:
				characterName = "KNUCKLES";
				break;
		}
		IFontRenderer fontRenderer = renderer.GetFontRenderer();
		fontRenderer.RenderStringWithShadow(characterName + " GOT", new Rectangle(captionAX, CaptionY, ScreenWidth, 0.0), FontAlignment.MiddleX, _captionFont, 0);
		// Centre "THROUGH ACT n" as one line. The act number uses a different font.
		double actNumberOffset = _captionFont.MeasureString("THROUGH ACT", default, FontAlignment.Left).Width + _captionFont.DefaultWidth / 2;
		double lineWidth = actNumberOffset + _captionActFont.MeasureString(_level.CurrentAct.ToString(), default, FontAlignment.Left).Width;
		captionBX += (ScreenWidth - lineWidth) / 2.0;
		fontRenderer.RenderStringWithShadow("THROUGH ACT", new Rectangle(captionBX, 270 + _captionFont.Height + 16, ScreenWidth, 0.0), FontAlignment.Left, _captionFont, 0);
		fontRenderer.RenderStringWithShadow(_level.CurrentAct.ToString(), new Rectangle(captionBX + actNumberOffset, 270 + _captionFont.Height + 16, 0.0, 0.0), FontAlignment.Left, _captionActFont, 0);
	}

	private void DrawScoreLabels(Renderer renderer)
	{
		DrawScoreLabel(renderer, "TIME BONUS", TimeBonus, _timeBonusFly.GetValueAt(_flyTicks), 604.0, 0);
		DrawScoreLabel(renderer, "RING BONUS", RingBonus, _ringBonusFly.GetValueAt(_flyTicks), 668.0, 0);
		if (_achievedPerfect)
		{
			DrawScoreLabel(renderer, "PERFECT BONUS", PerfectBonus, _perfectBonusFly.GetValueAt(_flyTicks), 732.0, 0);
		}
		byte highlight = (byte)MathX.Clamp(0.0, TotalShine.GetValueAt(_shineTicks), 255.0);
		DrawScoreLabel(renderer, "TOTAL", _total, _totalFly.GetValueAt(_flyTicks), 796.0, highlight);
	}

	private void DrawScoreLabel(Renderer renderer, string caption, int value, double offsetX, double y, byte highlight = 0)
	{
		DrawCaption(renderer, caption, new Rectangle(480.0 + offsetX, y, 480.0, 0.0), FontAlignment.MiddleX);
		IFontRenderer fontRenderer = renderer.GetFontRenderer();
		FontAlignment valueAlignment = FontAlignment.Right;
		Rectangle boundary = new Rectangle(1380.0 + offsetX, y, 0.0, 0.0);
		if (highlight > 0)
		{
			fontRenderer.RenderStringWithShadow(value.ToString(), boundary, valueAlignment, _statisticsFont, Colours.White, null, _statisticsFont.DefaultShadow, new Colour(highlight, highlight, highlight));
		}
		else
		{
			fontRenderer.RenderStringWithShadow(value.ToString(), boundary, valueAlignment, _statisticsFont, 0);
		}
	}

	private void DrawCaption(Renderer renderer, string text, Rectangle boundary, FontAlignment textAlignment)
	{
		Rectangle textBounds = _statisticsFont.MeasureString(text, boundary, textAlignment);
		ITexture texture = _triangleTextureSonic;
		switch (_level.Player.ProtagonistCharacterType)
		{
			case CharacterType.Tails:
				texture = _triangleTextureTails;
				break;
			case CharacterType.Knuckles:
				texture = _triangleTextureKnuckles;
				break;
		}
		renderer.Get2dRenderer().RenderTexture(texture, new Vector2(textBounds.Right - 8.0, textBounds.Bottom - 7.0));
		renderer.GetFontRenderer().RenderStringWithShadow(text, boundary, textAlignment, _statisticsFont, 1);
	}
}
