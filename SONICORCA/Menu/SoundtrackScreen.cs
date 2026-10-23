// Decompiled with JetBrains decompiler
// Type: SonicOrca.Menu.SoundtrackScreen
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

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

internal class SoundtrackScreen : Screen
{
	private const string BackgroundResourceKey = "SONICORCA/MENU/LEVELSELECT/BACKGROUND";
	private const string FontResourceKey = "SONICORCA/S2/HUD/FONT";
	private const string TrackBarResourceKey = "SONICORCA/S2/HUD/LIFE/SONIC";
	private const string SelectionChangeResourceKey = "SONICORCA/SOUND/TALLY/SWITCH";
	private const string ListingResourceKey = "SONICORCA/MENU/SOUNDTRACK/LISTING";
	private const int CursorMinDelay = 10;
	private const int ListSize = 10;
	private const int ListHeight = 74;
	private const int ListGap = 8;
	private readonly SonicOrcaGameContext _gameContext;
	private ResourceSession _resourceSession;
	private ITexture _backgroundTexture;
	private ITexture _trackBarTexture;
	private Font _font;
	private Sample _selectionChangeSample;
	private readonly FadeTransition _fadeTransition = new FadeTransition(30);
	private SoundtrackScreen.InternalState _state;
	private SoundtrackScreen.TrackNode _listingHead;
	private SoundtrackScreen.TrackNode _currentTrackNode;
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
		_resourceSession.PushDependencies("SONICORCA/MENU/LEVELSELECT/BACKGROUND", "SONICORCA/S2/HUD/FONT", "SONICORCA/S2/HUD/LIFE/SONIC", "SONICORCA/SOUND/TALLY/SWITCH", "SONICORCA/MENU/SOUNDTRACK/LISTING");
		await _resourceSession.LoadAsync(ct);
		_backgroundTexture = resourceTree.GetLoadedResource<ITexture>("SONICORCA/MENU/LEVELSELECT/BACKGROUND");
		_trackBarTexture = resourceTree.GetLoadedResource<ITexture>("SONICORCA/S2/HUD/LIFE/SONIC");
		_font = resourceTree.GetLoadedResource<Font>("SONICORCA/S2/HUD/FONT");
		_selectionChangeSample = resourceTree.GetLoadedResource<Sample>("SONICORCA/SOUND/TALLY/SWITCH");
		ParseListingXml(resourceTree.GetLoadedResource<XmlLoadedResource>("SONICORCA/MENU/SOUNDTRACK/LISTING"));
	}

	public override void Unload()
	{
		if (_resourceSession == null)
			return;
		_resourceSession.Unload();
	}

	private void ParseListingXml(XmlLoadedResource xmlResource)
	{
		_currentTrackNode = _listingHead = new SoundtrackScreen.TrackNode(null, ParseListingXml(xmlResource.XmlDocument.SelectSingleNode("soundtrack")));
	}

	private IEnumerable<SoundtrackScreen.TrackNode> ParseListingXml(XmlNode xmlNode)
	{
		foreach (XmlNode child in xmlNode.ChildNodes)
		{
			if (child.Name == "track")
				yield return new SoundtrackScreen.TrackNode(child.Attributes["name"].Value, child.InnerText);
			else if (child.Name == "group")
			{
				string str;
				child.TryGetAttributeValue("type", out str);
				yield return new SoundtrackScreen.TrackNode(child.Attributes["name"].Value, ParseListingXml(child), str == "mini");
			}
		}
	}

	public override void Update()
	{
		switch (_state)
		{
			case SoundtrackScreen.InternalState.BeginFadeIn:
				_fadeTransition.BeginFadeIn();
				_state = SoundtrackScreen.InternalState.FadingIn;
				break;
			case SoundtrackScreen.InternalState.FadingIn:
				_fadeTransition.Update();
				if (!_fadeTransition.Finished)
					break;
				_state = SoundtrackScreen.InternalState.Normal;
				break;
			case SoundtrackScreen.InternalState.Normal:
				HandleInput();
				break;
			case SoundtrackScreen.InternalState.BeginFadeOut:
				_fadeTransition.BeginFadeOut();
				_state = SoundtrackScreen.InternalState.FadingOut;
				break;
			case SoundtrackScreen.InternalState.FadingOut:
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
			if (controller.DirectionLeft.X != 0.0 || controller.DirectionLeft.Y != 0.0)
				_cursorDelay = (int)(10.0 * (2.0 * controller.DirectionLeft.Y));
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
			if (_gameContext.Input.CurrentState.Keyboard[18])
				factor = -1.0;
			if (_gameContext.Input.CurrentState.Keyboard[19])
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
		SoundtrackScreen.TrackNode child = _currentTrackNode.Children[_currentTrackNode.SelectedIndex];
		if (child.Type != SoundtrackScreen.ListingType.MiniGroup || child.SelectedIndex <= 0)
			return;
		--child.SelectedIndex;
		PlayChangeListingSound();
	}

	private void ListingRight()
	{
		if (_currentTrackNode.Children.Count == 0)
			return;
		SoundtrackScreen.TrackNode child = _currentTrackNode.Children[_currentTrackNode.SelectedIndex];
		if (child.Type != SoundtrackScreen.ListingType.MiniGroup || child.SelectedIndex >= child.Children.Count - 1)
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
		SoundtrackScreen.TrackNode child1 = _currentTrackNode.Children[_currentTrackNode.SelectedIndex];
		if (child1.Type == SoundtrackScreen.ListingType.Track)
			SetAndPlayTrack(child1);
		else if (child1.Type == SoundtrackScreen.ListingType.MiniGroup)
		{
			if (child1.Children.Count == 0)
				return;
			SoundtrackScreen.TrackNode child2 = child1.Children[child1.SelectedIndex];
			if (child2.Type != SoundtrackScreen.ListingType.Track)
				return;
			SetAndPlayTrack(child2);
		}
		else
		{
			_currentTrackNode = child1;
			PlayChangeListingSound();
		}
	}

	private void PlayChangeListingSound()
	{
		_gameContext.Audio.PlaySound(_selectionChangeSample);
	}

	private void SetAndPlayTrack(SoundtrackScreen.TrackNode node)
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
				_loadedTrackSampleInstance = _loadedTrackResourceSession.ResourceTree[resourceKey].Resource.Identifier != ResourceTypeIdentifier.SampleInfo ? new SampleInstance(_gameContext.Audio, _loadedTrackResourceSession.ResourceTree.GetLoadedResource<Sample>(resourceKey)) : new SampleInstance(_gameContext.Audio, _loadedTrackResourceSession.ResourceTree.GetLoadedResource<SampleInfo>(resourceKey));
		}
		catch (Exception ex)
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

	private void SeekTrack(double factor)
	{
		lock (_loadTrackSync)
		{
			if (_loadedTrackSampleInstance == null || factor == 0.0)
				return;
			int sampleIndex = _loadedTrackSampleInstance.SampleIndex;
			int num1 = _loadedTrackSampleInstance.Sample.SampleRate / 2;
			int sampleCount = _loadedTrackSampleInstance.Sample.SampleCount;
			factor = Math.Sign(factor) * factor * factor;
			int num2 = (int)(num1 * factor);
			_loadedTrackSampleInstance.SeekTo(MathX.Clamp(0, sampleIndex + num2, sampleCount));
		}
	}

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
		I2dRenderer obj = renderer.Get2dRenderer();
		IFontRenderer fontRenderer = renderer.GetFontRenderer();
		obj.Colour = Colours.White;
		obj.RenderTexture(_backgroundTexture, new Rectangle(0.0, 0.0, 1920.0, 1080.0));
		using (obj.BeginMatixState())
		{
			obj.ModelMatrix = obj.ModelMatrix.Scale(2.5, 2.5);
			fontRenderer.RenderString("SOUNDTRACK", new Rectangle(32.0, 32.0, 0.0, 0.0), FontAlignment.Left, _font, Colours.Black);
		}
		DrawList(renderer, new Rectangle(32.0, 228.0, 896.0, 820.0));
		lock (_loadTrackSync)
			DrawTrackbar(renderer, new Rectangle(1080.0, 244.0, 804.0, 804.0));
		_fadeTransition.Draw(renderer);
	}

	private void DrawList(Renderer renderer, Rectangle bounds)
	{
		I2dRenderer obj = renderer.Get2dRenderer();
		using (obj.BeginMatixState())
		{
			obj.ModelMatrix = obj.ModelMatrix.Translate(bounds.X, bounds.Y);
			int num1 = 0;
			int num2 = 0;
			int num3 = MathX.Clamp(0, _currentTrackNode.SelectedIndex - 5, _currentTrackNode.Children.Count - 1 - 9);
			double y = 0.0;
			foreach (SoundtrackScreen.TrackNode child in (IEnumerable<SoundtrackScreen.TrackNode>)_currentTrackNode.Children)
			{
				bool selected = num1 == _currentTrackNode.SelectedIndex;
				if (num1++ >= num3)
				{
					y += DrawListItem(renderer, child, new Rectangle(0.0, y, bounds.Width, 74.0), selected) + 8.0;
					if (child.Type == SoundtrackScreen.ListingType.MiniGroup & selected)
						++num2;
					if (++num2 >= 10)
						break;
				}
			}
		}
	}

	private double DrawListItem(
	  Renderer renderer,
	  SoundtrackScreen.TrackNode node,
	  Rectangle bounds,
	  bool selected)
	{
		bool flag = false;
		if (node.Type == SoundtrackScreen.ListingType.MiniGroup & selected)
			flag = true;
		if (flag)
			bounds.Height = bounds.Height * 2.0 + 8.0;
		I2dRenderer obj = renderer.Get2dRenderer();
		IFontRenderer fontRenderer = renderer.GetFontRenderer();
		Colour colour1 = new Colour(128 /*0x80*/, 0, 0, 0);
		Rectangle destination = bounds;
		obj.RenderQuad(colour1, destination);
		if (flag)
			bounds.Height = (bounds.Height - 8.0) / 2.0;
		bounds.X += 12.0;
		Colour colour2 = new Colour(128 /*0x80*/, 128 /*0x80*/, 128 /*0x80*/);
		Colour colour3 = Colours.White;
		if (_loadedTrackResourceKey == node.ResourceKey && _loadTrackTask != null)
			colour3 = colour2;
		fontRenderer.RenderStringWithShadow(node.Name.ToUpper(), bounds, FontAlignment.MiddleY, _font, colour3, new int?(selected ? 1 : 0));
		if (flag)
		{
			bounds.Y += bounds.Height + 8.0;
			for (int index = 0; index < node.Children.Count; ++index)
			{
				SoundtrackScreen.TrackNode child = node.Children[index];
				Colour colour4 = Colours.White;
				if (_loadedTrackResourceKey == child.ResourceKey && _loadTrackTask != null)
					colour4 = colour2;
				fontRenderer.RenderStringWithShadow(child.Name.ToUpper(), bounds, FontAlignment.MiddleY, _font, colour4, new int?(node.SelectedIndex == index ? 1 : 0));
				bounds.X += _font.MeasureString(child.Name.ToUpper(), bounds, FontAlignment.MiddleY).Width + bounds.Height;
			}
			bounds.Height = bounds.Height * 2.0 + 8.0;
		}
		return bounds.Height;
	}

	private void DrawTrackbar(Renderer renderer, Rectangle bounds)
	{
		I2dRenderer obj = renderer.Get2dRenderer();
		IFontRenderer fontRenderer = renderer.GetFontRenderer();
		using (obj.BeginMatixState())
		{
			obj.ModelMatrix = obj.ModelMatrix.Translate(bounds.CentreX, bounds.CentreY);
			double outerRadius = bounds.Width / 2.0;
			double innerRadius = outerRadius - 32.0;
			double num1 = innerRadius + (outerRadius - innerRadius) / 2.0;
			obj.RenderEllipse(new Colour(128 /*0x80*/, 0, 0, 0), new Vector2(), innerRadius, outerRadius, 64 /*0x40*/);
			if (_loadedTrackSampleInstance == null)
				return;
			double num2 = _loadedTrackSampleInstance.SampleIndex / (double)_loadedTrackSampleInstance.Sample.SampleCount * (2.0 * Math.PI);
			obj.Colour = Colours.White;
			obj.RenderTexture(_trackBarTexture, new Vector2(Math.Sin(num2) * num1, -Math.Cos(num2) * num1));
			fontRenderer.RenderStringWithShadow(_loadedTrackSampleInstance.SampleIndex.ToString(), new Rectangle(), FontAlignment.Centre, _font, 0);
			TimeSpan time1 = GetTime(_loadedTrackSampleInstance.SampleIndex);
			TimeSpan time2 = GetTime(_loadedTrackSampleInstance.Sample.SampleCount);
			fontRenderer.RenderStringWithShadow(string.Format("{0:m\\:ss} / {1:m\\:ss}", time1, time2), new Rectangle(0.0, innerRadius * 0.75, 0.0, 0.0), FontAlignment.Centre, _font, 0);
		}
	}

	private enum ListingType
	{
		Group,
		MiniGroup,
		Track,
	}

	private class TrackNode
	{
		private readonly SoundtrackScreen.TrackNode[] _children;
		private readonly SoundtrackScreen.ListingType _type;
		private readonly string _name;
		private readonly string _resourceKey;
		private SoundtrackScreen.TrackNode _parent;

		public int SelectedIndex { get; set; }

		public SoundtrackScreen.TrackNode Parent => _parent;

		public SoundtrackScreen.ListingType Type => _type;

		public IReadOnlyList<SoundtrackScreen.TrackNode> Children
		{
			get => _children;
		}

		public string Name => _name;

		public string ResourceKey => _resourceKey;

		public TrackNode(string name, IEnumerable<SoundtrackScreen.TrackNode> children, bool mini = false)
		{
			_children = children.ToArray<SoundtrackScreen.TrackNode>();
			_type = mini ? SoundtrackScreen.ListingType.MiniGroup : SoundtrackScreen.ListingType.Group;
			_name = name;
			foreach (SoundtrackScreen.TrackNode child in _children)
				child._parent = this;
		}

		public TrackNode(string name, string resourceKey)
		{
			_type = SoundtrackScreen.ListingType.Track;
			_name = name;
			_resourceKey = resourceKey;
		}

		public override string ToString() => Name;
	}

	private enum InternalState
	{
		BeginFadeIn,
		FadingIn,
		Normal,
		BeginFadeOut,
		FadingOut,
	}
}
