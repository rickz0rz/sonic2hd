using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Resources;

namespace SonicOrca.Graphics;

public class Font : ILoadedResource, IDisposable, IEnumerable<Font.CharacterDefinition>, IEnumerable
{
	public class CharacterDefinition
	{
		private readonly char _key;

		private readonly Rectanglei _sourceRectangle;

		private readonly Vector2i _offset;

		private readonly int _width;

		public char Key => _key;

		public Rectanglei SourceRectangle => _sourceRectangle;

		public Vector2i Offset => _offset;

		public int Width => _width;

		public CharacterDefinition(char key, Rectanglei sourceRectangle, Vector2i offset)
			: this(key, sourceRectangle, offset, sourceRectangle.Width)
		{
		}

		public CharacterDefinition(char key, Rectanglei sourceRectangle, Vector2i offset, int width)
		{
			_key = key;
			_sourceRectangle = sourceRectangle;
			_offset = offset;
			_width = width;
		}

		public override string ToString()
		{
			return _key.ToString();
		}
	}

	private readonly Dictionary<char, CharacterDefinition> _characterDefinitions;

	private readonly ResourceTree _resourceTree;

	private readonly string _shapeResourceKey;

	private readonly IEnumerable<string> _overlayResourceKeys;

	private ITexture _shapeTexture;

	private IReadOnlyList<ITexture> _overlayTextures;

	public Resource Resource { get; set; }

	public int DefaultWidth { get; private set; }

	public int Height { get; private set; }

	public int Tracking { get; private set; }

	public Vector2i? DefaultShadow { get; private set; }

	public ITexture ShapeTexture => _shapeTexture;

	public IReadOnlyList<ITexture> OverlayTextures => _overlayTextures;

	public CharacterDefinition this[char key] => DictionaryExtensions.GetValueOrDefault(_characterDefinitions, key);

	public Font(ResourceTree resourceTree, string shapeResourceKey, IEnumerable<string> overlayResourceKeys, int defaultWidth, int height, int tracking, Vector2i? shadow, IEnumerable<CharacterDefinition> characterDefinitions)
	{
		_resourceTree = resourceTree;
		_shapeResourceKey = shapeResourceKey;
		_overlayResourceKeys = overlayResourceKeys.ToArray();
		DefaultWidth = defaultWidth;
		Height = height;
		Tracking = tracking;
		DefaultShadow = shadow;
		_characterDefinitions = characterDefinitions.ToDictionary((CharacterDefinition x) => x.Key);
	}

	public Font(ITexture shapeTexture, IEnumerable<ITexture> overlayTextures, int defaultWidth, int height, int tracking, Vector2i? shadow, IEnumerable<CharacterDefinition> characterDefinitions)
	{
		_shapeTexture = shapeTexture;
		_overlayTextures = overlayTextures.ToArray();
		DefaultWidth = defaultWidth;
		Height = height;
		Tracking = tracking;
		DefaultShadow = shadow;
		_characterDefinitions = characterDefinitions.ToDictionary((CharacterDefinition x) => x.Key);
	}

	public void OnLoaded()
	{
		if (_resourceTree != null)
		{
			_shapeTexture = _resourceTree.GetLoadedResource<ITexture>(_shapeResourceKey);
			_overlayTextures = _overlayResourceKeys.Select((string x) => _resourceTree.GetLoadedResource<ITexture>(x)).ToArray();
		}
	}

	public void Dispose()
	{
	}

	public Rectangle MeasureString(string text)
	{
		return MeasureString(text, default, FontAlignment.Left);
	}

	public Rectangle MeasureString(string text, Rectangle boundary, FontAlignment alignment)
	{
		double num = 0.0;
		double num2 = Height;
		foreach (char key in text)
		{
			num += (double)(_characterDefinitions.TryGetValue(key, out var value) ? value.Width : DefaultWidth);
			num += (double)Tracking;
		}
		if (text.Length > 0)
		{
			num -= (double)Tracking;
		}
		double x = 0.0;
		double y = 0.0;
		FontAlignment fontAlignment = alignment & FontAlignment.HorizontalMask;
		FontAlignment fontAlignment2 = alignment & FontAlignment.VerticalMask;
		switch (fontAlignment)
		{
			case FontAlignment.Left:
				x = boundary.X;
				break;
			case FontAlignment.MiddleX:
				x = boundary.X + (boundary.Width - num) / 2.0;
				break;
			case FontAlignment.Right:
				x = boundary.Right - num;
				break;
		}
		switch (fontAlignment2)
		{
			case FontAlignment.Left:
				y = boundary.Y;
				break;
			case FontAlignment.MiddleY:
				y = boundary.Y + (boundary.Height - num2) / 2.0;
				break;
			case FontAlignment.Bottom:
				y = boundary.Bottom - num2;
				break;
		}
		return new Rectangle(x, y, num, num2);
	}

	public override string ToString()
	{
		return $"{_characterDefinitions.Count} defined characters";
	}

	public IEnumerator<CharacterDefinition> GetEnumerator()
	{
		return _characterDefinitions.Values.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
