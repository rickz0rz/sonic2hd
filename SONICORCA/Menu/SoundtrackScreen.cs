using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using SonicOrca.Audio;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

#nullable disable
namespace SonicOrca.Menu;

/// <summary>
/// A sound test screen that lists the music tracks of the game in groups. The player can play a track,
/// pause it, and seek in it with the triggers or with the right stick. No code in the game opens this screen.
/// </summary>
internal class SoundtrackScreen : Screen
{
	// SDL scancodes of the keys that seek in the track.
	private const int SeekBackScancode = 18; // O
	private const int SeekForwardScancode = 19; // P
	private const string BackgroundResourceKey = "SONICORCA/MENU/LEVELSELECT/BACKGROUND";
	private const string FontResourceKey = "SONICORCA/S2/HUD/FONT";
	private const string TrackBarResourceKey = "SONICORCA/S2/HUD/LIFE/SONIC";
	private const string SelectionChangeResourceKey = "SONICORCA/SOUND/TALLY/SWITCH";
	private const string ListingResourceKey = "SONICORCA/MENU/SOUNDTRACK/LISTING";
	private const int CursorMinDelay = 10;
	/// <summary>The number of list items that show at the same time.</summary>
	private const int ListSize = 10;
	/// <summary>The height of one list item, and the gap between two items.</summary>
	private const int ListHeight = 74;
	private const int ListGap = 8;
	private readonly SonicOrcaGameContext _gameContext;
	private ResourceSession _resourceSession;
	private ITexture _backgroundTexture;
	private ITexture _trackBarTexture;
	private Font _font;
	private Sample _selectionChangeSample;
	private readonly FadeTransition _fadeTransition = new FadeTransition(30);
	private InternalState _state;
	private TrackNode _listingHead;
	private TrackNode _currentTrackNode;
	private CancellationTokenSource _loadTrackCancellationTokenSource;
	private Task _loadTrackTask;
	private ResourceSession _loadedTrackResourceSession;
	private string _loadedTrackResourceKey;
	private SampleInstance _loadedTrackSampleInstance;
	private readonly object _loadTrackSync = new object();
	private int _cursorDelay;

	public SoundtrackScreen(SonicOrcaGameContext gameContext) => _gameContext = gameContext;

	public override async Task LoadAsync(ScreenLoadingProgress progress, CancellationToken ct = default(CancellationToken))
	{
		ResourceTree resourceTree = _gameContext.ResourceTree;
		_loadedTrackResourceSession = new ResourceSession(resourceTree);
		_resourceSession = new ResourceSession(resourceTree);
		_resourceSession.PushDependencies(BackgroundResourceKey, FontResourceKey, TrackBarResourceKey, SelectionChangeResourceKey, ListingResourceKey);
		await _resourceSession.LoadAsync(ct);
		_backgroundTexture = resourceTree.GetLoadedResource<ITexture>(BackgroundResourceKey);
		_trackBarTexture = resourceTree.GetLoadedResource<ITexture>(TrackBarResourceKey);
		_font = resourceTree.GetLoadedResource<Font>(FontResourceKey);
		_selectionChangeSample = resourceTree.GetLoadedResource<Sample>(SelectionChangeResourceKey);
		ParseListingXml(resourceTree.GetLoadedResource<XmlLoadedResource>(ListingResourceKey));
	}

	public override void Unload()
	{
		if (_resourceSession == null)
			return;
		_resourceSession.Unload();
	}

	private void ParseListingXml(XmlLoadedResource xmlResource)
	{
		_currentTrackNode = _listingHead = new TrackNode(null, ParseListingXml(xmlResource.XmlDocument.SelectSingleNode("soundtrack")));
	}

	/// <summary>
	/// Reads the children of a listing node. A "track" element holds the resource key of a track, and a
	/// "group" element holds more tracks or groups. A group with type="mini" shows its tracks on one line.
	/// </summary>
	private IEnumerable<TrackNode> ParseListingXml(XmlNode xmlNode)
	{
		foreach (XmlNode child in xmlNode.ChildNodes)
		{
			if (child.Name == "track")
			{
				yield return new TrackNode(child.Attributes["name"].Value, child.InnerText);
			}
			else if (child.Name == "group")
			{
				child.TryGetAttributeValue("type", out string groupType);
				yield return new TrackNode(child.Attributes["name"].Value, ParseListingXml(child), groupType == "mini");
			}
		}
	}

	public override void Update()
	{
		switch (_state)
		{
			case InternalState.BeginFadeIn:
				_fadeTransition.BeginFadeIn();
				_state = InternalState.FadingIn;
				break;
			case InternalState.FadingIn:
				_fadeTransition.Update();
				if (!_fadeTransition.Finished)
					break;
				_state = InternalState.Normal;
				break;
			case InternalState.Normal:
				HandleInput();
				break;
			case InternalState.BeginFadeOut:
				_fadeTransition.BeginFadeOut();
				_state = InternalState.FadingOut;
				break;
			case InternalState.FadingOut:
				_fadeTransition.Update();
				if (!_fadeTransition.Finished)
					break;
				Finish();
				break;
		}
	}

	private void HandleInput()
	{
		Controller controller = _gameContext.Pressed[0];
		if (controller.DirectionLeft.X == 0.0 && controller.DirectionLeft.Y == 0.0)
			_cursorDelay = 0;
		if (_cursorDelay > 0)
		{
			--_cursorDelay;
		}
		else
		{
			if (controller.DirectionLeft.X < 0.0)
				ListingLeft();
			if (controller.DirectionLeft.X > 0.0)
				ListingRight();
			if (controller.DirectionLeft.Y < 0.0)
				ListingUp();
			if (controller.DirectionLeft.Y > 0.0)
				ListingDown();
			// This uses only the Y direction, so the delay is negative (no delay) when the stick is up or level.
			if (controller.DirectionLeft.X != 0.0 || controller.DirectionLeft.Y != 0.0)
				_cursorDelay = (int)(CursorMinDelay * (2.0 * controller.DirectionLeft.Y));
		}
		if (controller.Action2)
			ListingEnter();
		if (controller.Action3)
			ListingExit();
		lock (_loadTrackSync)
		{
			if (_loadedTrackSampleInstance == null)
				return;
			if (controller.Action1)
				TogglePlayTrack();
			double factor = 0.0;
			if (controller.LeftTrigger != 0.0 && controller.RightTrigger == 0.0)
				factor = -controller.LeftTrigger;
			if (controller.LeftTrigger == 0.0 && controller.RightTrigger != 0.0)
				factor = controller.RightTrigger;
			if (_gameContext.Input.CurrentState.Keyboard[SeekBackScancode])
				factor = -1.0;
			if (_gameContext.Input.CurrentState.Keyboard[SeekForwardScancode])
				factor = 1.0;
			SeekTrack(factor);
			SeekTrack(controller.DirectionRight.X, controller.DirectionRight.Y);
		}
	}

	private void ListingUp()
	{
		if (_currentTrackNode.SelectedIndex <= 0)
			return;
		--_currentTrackNode.SelectedIndex;
		PlayChangeListingSound();
	}

	private void ListingDown()
	{
		if (_currentTrackNode.SelectedIndex >= _currentTrackNode.Children.Count - 1)
			return;
		++_currentTrackNode.SelectedIndex;
		PlayChangeListingSound();
	}

	private void ListingLeft()
	{
		if (_currentTrackNode.Children.Count == 0)
			return;
		TrackNode child = _currentTrackNode.Children[_currentTrackNode.SelectedIndex];
		if (child.Type != ListingType.MiniGroup || child.SelectedIndex <= 0)
			return;
		--child.SelectedIndex;
		PlayChangeListingSound();
	}

	private void ListingRight()
	{
		if (_currentTrackNode.Children.Count == 0)
			return;
		TrackNode child = _currentTrackNode.Children[_currentTrackNode.SelectedIndex];
		if (child.Type != ListingType.MiniGroup || child.SelectedIndex >= child.Children.Count - 1)
			return;
		++child.SelectedIndex;
		PlayChangeListingSound();
	}

	private void ListingExit()
	{
		if (_currentTrackNode.Parent == null)
			return;
		_currentTrackNode = _currentTrackNode.Parent;
		PlayChangeListingSound();
	}

	private void ListingEnter()
	{
		if (_currentTrackNode.Children.Count == 0)
			return;
		TrackNode child = _currentTrackNode.Children[_currentTrackNode.SelectedIndex];
		if (child.Type == ListingType.Track)
		{
			SetAndPlayTrack(child);
		}
		else if (child.Type == ListingType.MiniGroup)
		{
			if (child.Children.Count == 0)
				return;
			TrackNode track = child.Children[child.SelectedIndex];
			if (track.Type != ListingType.Track)
				return;
			SetAndPlayTrack(track);
		}
		else
		{
			_currentTrackNode = child;
			PlayChangeListingSound();
		}
	}

	private void PlayChangeListingSound()
	{
		_gameContext.Audio.PlaySound(_selectionChangeSample);
	}

	private void SetAndPlayTrack(TrackNode node)
	{
		if (node.ResourceKey == _loadedTrackResourceKey)
		{
			if (_loadTrackTask != null)
				return;
			RestartTrack();
		}
		else
		{
			if (_loadTrackTask != null)
			{
				// Wait for the loading task to clear _loadTrackTask. The field is not volatile, so this
				// busy wait can, in theory, loop for ever.
				_loadTrackCancellationTokenSource.Cancel();
				while (_loadTrackTask != null)
					;
			}
			_loadTrackCancellationTokenSource = new CancellationTokenSource();
			CancellationToken ct = _loadTrackCancellationTokenSource.Token;
			_loadTrackTask = Task.Run(async () =>
			{
				try
				{
					await SetTrack(node.ResourceKey, ct);
					if (ct.IsCancellationRequested)
						return;
					RestartTrack();
				}
				finally
				{
					_loadTrackTask = null;
				}
			});
		}
	}

	private async Task SetTrack(string resourceKey, CancellationToken ct = default(CancellationToken))
	{
		if (_loadedTrackResourceKey == resourceKey)
			return;
		lock (_loadTrackSync)
		{
			if (_loadedTrackSampleInstance != null)
			{
				_loadedTrackSampleInstance.Dispose();
				_loadedTrackSampleInstance = null;
			}
		}
		_loadedTrackResourceKey = resourceKey;
		_loadedTrackResourceSession.Unload();
		try
		{
			_loadedTrackResourceSession.PushDependency(resourceKey);
			await _loadedTrackResourceSession.LoadAsync(ct);
			lock (_loadTrackSync)
			{
				ResourceTree resourceTree = _loadedTrackResourceSession.ResourceTree;
				// The track resource is a Sample or a SampleInfo.
				if (resourceTree[resourceKey].Resource.Identifier != ResourceTypeIdentifier.SampleInfo)
					_loadedTrackSampleInstance = new SampleInstance(_gameContext.Audio, resourceTree.GetLoadedResource<Sample>(resourceKey));
				else
					_loadedTrackSampleInstance = new SampleInstance(_gameContext.Audio, resourceTree.GetLoadedResource<SampleInfo>(resourceKey));
			}
		}
		catch (Exception)
		{
			_loadedTrackResourceKey = null;
		}
	}

	private void RestartTrack()
	{
		lock (_loadTrackSync)
		{
			if (_loadedTrackSampleInstance == null)
				return;
			_loadedTrackSampleInstance.Stop();
			_loadedTrackSampleInstance.SeekToStart();
			_loadedTrackSampleInstance.Play();
		}
	}

	private void TogglePlayTrack()
	{
		lock (_loadTrackSync)
		{
			if (_loadedTrackSampleInstance == null)
				return;
			if (_loadedTrackSampleInstance.Playing)
				_loadedTrackSampleInstance.Stop();
			else
				_loadedTrackSampleInstance.Play();
		}
	}

	/// <summary>
	/// Moves the track position by up to half a second each frame. The step grows with the square of
	/// <paramref name="factor"/> (from -1 to 1).
	/// </summary>
	private void SeekTrack(double factor)
	{
		lock (_loadTrackSync)
		{
			if (_loadedTrackSampleInstance == null || factor == 0.0)
				return;
			int sampleIndex = _loadedTrackSampleInstance.SampleIndex;
			int halfSecondSamples = _loadedTrackSampleInstance.Sample.SampleRate / 2;
			int sampleCount = _loadedTrackSampleInstance.Sample.SampleCount;
			factor = Math.Sign(factor) * factor * factor;
			int step = (int)(halfSecondSamples * factor);
			_loadedTrackSampleInstance.SeekTo(MathX.Clamp(0, sampleIndex + step, sampleCount));
		}
	}

	/// <summary>
	/// Moves the track position to the angle of the stick: up is the start, and a full turn clockwise is the end.
	/// </summary>
	private void SeekTrack(double x, double y)
	{
		lock (_loadTrackSync)
		{
			if (x == 0.0 && y == 0.0)
				return;
			_loadedTrackSampleInstance.SeekTo(MathX.Clamp(0, (int)((Math.Atan2(y, x) / (2.0 * Math.PI) + 1.25) % 1.0 * _loadedTrackSampleInstance.Sample.SampleCount), _loadedTrackSampleInstance.Sample.SampleCount));
		}
	}

	private TimeSpan GetTime(int sampleOffset)
	{
		return TimeSpan.FromSeconds(sampleOffset / (double)_loadedTrackSampleInstance.Sample.SampleRate);
	}

	public override void Draw(Renderer renderer)
	{
		I2dRenderer g = renderer.Get2dRenderer();
		IFontRenderer fontRenderer = renderer.GetFontRenderer();
		g.Colour = Colours.White;
		g.RenderTexture(_backgroundTexture, new Rectangle(0.0, 0.0, 1920.0, 1080.0));
		using (g.BeginMatixState())
		{
			g.ModelMatrix = g.ModelMatrix.Scale(2.5, 2.5);
			fontRenderer.RenderString("SOUNDTRACK", new Rectangle(32.0, 32.0, 0.0, 0.0), FontAlignment.Left, _font, Colours.Black);
		}
		DrawList(renderer, new Rectangle(32.0, 228.0, 896.0, 820.0));
		lock (_loadTrackSync)
			DrawTrackbar(renderer, new Rectangle(1080.0, 244.0, 804.0, 804.0));
		_fadeTransition.Draw(renderer);
	}

	private void DrawList(Renderer renderer, Rectangle bounds)
	{
		I2dRenderer g = renderer.Get2dRenderer();
		using (g.BeginMatixState())
		{
			g.ModelMatrix = g.ModelMatrix.Translate(bounds.X, bounds.Y);
			int index = 0;
			int rowsDrawn = 0;
			// Scroll the list so that the selected item is near the middle.
			int firstIndex = MathX.Clamp(0, _currentTrackNode.SelectedIndex - ListSize / 2, _currentTrackNode.Children.Count - 1 - (ListSize - 1));
			double y = 0.0;
			foreach (TrackNode child in _currentTrackNode.Children)
			{
				bool selected = index == _currentTrackNode.SelectedIndex;
				if (index++ >= firstIndex)
				{
					y += DrawListItem(renderer, child, new Rectangle(0.0, y, bounds.Width, ListHeight), selected) + ListGap;
					// A selected mini group uses two rows.
					if (child.Type == ListingType.MiniGroup & selected)
						++rowsDrawn;
					if (++rowsDrawn >= ListSize)
						break;
				}
			}
		}
	}

	private double DrawListItem(
	  Renderer renderer,
	  TrackNode node,
	  Rectangle bounds,
	  bool selected)
	{
		// A selected mini group is two rows high: its name, and then its tracks on one line.
		bool expanded = node.Type == ListingType.MiniGroup & selected;
		if (expanded)
			bounds.Height = bounds.Height * 2.0 + ListGap;
		I2dRenderer g = renderer.Get2dRenderer();
		IFontRenderer fontRenderer = renderer.GetFontRenderer();
		Colour backgroundColour = new Colour(128, 0, 0, 0);
		g.RenderQuad(backgroundColour, bounds);
		if (expanded)
			bounds.Height = (bounds.Height - ListGap) / 2.0;
		bounds.X += 12.0;
		// A track that is loading shows in grey.
		Colour loadingColour = new Colour(128, 128, 128);
		Colour nameColour = Colours.White;
		if (_loadedTrackResourceKey == node.ResourceKey && _loadTrackTask != null)
			nameColour = loadingColour;
		fontRenderer.RenderStringWithShadow(node.Name.ToUpper(), bounds, FontAlignment.MiddleY, _font, nameColour, new int?(selected ? 1 : 0));
		if (expanded)
		{
			bounds.Y += bounds.Height + ListGap;
			for (int index = 0; index < node.Children.Count; ++index)
			{
				TrackNode child = node.Children[index];
				Colour childColour = Colours.White;
				if (_loadedTrackResourceKey == child.ResourceKey && _loadTrackTask != null)
					childColour = loadingColour;
				fontRenderer.RenderStringWithShadow(child.Name.ToUpper(), bounds, FontAlignment.MiddleY, _font, childColour, new int?(node.SelectedIndex == index ? 1 : 0));
				bounds.X += _font.MeasureString(child.Name.ToUpper(), bounds, FontAlignment.MiddleY).Width + bounds.Height;
			}
			bounds.Height = bounds.Height * 2.0 + ListGap;
		}
		return bounds.Height;
	}

	private void DrawTrackbar(Renderer renderer, Rectangle bounds)
	{
		I2dRenderer g = renderer.Get2dRenderer();
		IFontRenderer fontRenderer = renderer.GetFontRenderer();
		using (g.BeginMatixState())
		{
			// Draw a ring, and a marker on the ring at the position of the track (clockwise from the top).
			g.ModelMatrix = g.ModelMatrix.Translate(bounds.CentreX, bounds.CentreY);
			double outerRadius = bounds.Width / 2.0;
			double innerRadius = outerRadius - 32.0;
			double markerRadius = innerRadius + (outerRadius - innerRadius) / 2.0;
			g.RenderEllipse(new Colour(128, 0, 0, 0), new Vector2(), innerRadius, outerRadius, 64);
			if (_loadedTrackSampleInstance == null)
				return;
			double angle = _loadedTrackSampleInstance.SampleIndex / (double)_loadedTrackSampleInstance.Sample.SampleCount * (2.0 * Math.PI);
			g.Colour = Colours.White;
			g.RenderTexture(_trackBarTexture, new Vector2(Math.Sin(angle) * markerRadius, -Math.Cos(angle) * markerRadius));
			fontRenderer.RenderStringWithShadow(_loadedTrackSampleInstance.SampleIndex.ToString(), new Rectangle(), FontAlignment.Centre, _font, 0);
			TimeSpan position = GetTime(_loadedTrackSampleInstance.SampleIndex);
			TimeSpan duration = GetTime(_loadedTrackSampleInstance.Sample.SampleCount);
			fontRenderer.RenderStringWithShadow(string.Format("{0:m\\:ss} / {1:m\\:ss}", position, duration), new Rectangle(0.0, innerRadius * 0.75, 0.0, 0.0), FontAlignment.Centre, _font, 0);
		}
	}

	/// <summary>The type of a listing node. A mini group shows its tracks on one line.</summary>
	private enum ListingType
	{
		Group,
		MiniGroup,
		Track,
	}

	/// <summary>A track, or a group of tracks, in the listing.</summary>
	private class TrackNode
	{
		private readonly TrackNode[] _children;
		private readonly ListingType _type;
		private readonly string _name;
		private readonly string _resourceKey;
		private TrackNode _parent;

		public int SelectedIndex { get; set; }

		public TrackNode Parent => _parent;

		public ListingType Type => _type;

		public IReadOnlyList<TrackNode> Children
		{
			get => _children;
		}

		public string Name => _name;

		public string ResourceKey => _resourceKey;

		public TrackNode(string name, IEnumerable<TrackNode> children, bool mini = false)
		{
			_children = children.ToArray();
			_type = mini ? ListingType.MiniGroup : ListingType.Group;
			_name = name;
			foreach (TrackNode child in _children)
				child._parent = this;
		}

		public TrackNode(string name, string resourceKey)
		{
			_type = ListingType.Track;
			_name = name;
			_resourceKey = resourceKey;
		}

		public override string ToString() => Name;
	}

	/// <summary>The steps of the screen: fade in, normal input, and fade out.</summary>
	private enum InternalState
	{
		BeginFadeIn,
		FadingIn,
		Normal,
		BeginFadeOut,
		FadingOut,
	}
}
