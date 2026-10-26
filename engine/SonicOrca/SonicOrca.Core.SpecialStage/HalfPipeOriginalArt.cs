using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

namespace SonicOrca.Core.SpecialStage;

/// <summary>
/// The art of the special stage of the original game, which "s2hd-data import-special-stages" makes from the Sonic 2
/// disassembly: the START banner, the winged hand of the checkpoint messages, and the results screen with its digits.
/// The game data does not have it, so the stage uses it only when a mod adds it. The sprites are at four times the
/// size of the original, and this class draws them at that size, centred on the 1920 x 1080 screen.
/// </summary>
internal sealed class HalfPipeOriginalArt
{
	public const string MessagesKey = "SONICORCA/SPECIALSTAGE/ORIGINAL/MESSAGES/ANIGROUP";
	public const string StartKey = "SONICORCA/SPECIALSTAGE/ORIGINAL/START/ANIGROUP";
	public const string ResultsKey = "SONICORCA/SPECIALSTAGE/ORIGINAL/RESULTS/ANIGROUP";
	public const string DigitsKey = "SONICORCA/SPECIALSTAGE/ORIGINAL/DIGITS/ANIGROUP";

	public static readonly IReadOnlyList<string> ResourceKeys = new[] { MessagesKey, StartKey, ResultsKey, DigitsKey };

	/// <summary>The size of a pixel of the original.</summary>
	public const double Scale = 4.0;

	/// <summary>The special stage is 256 pixels wide (H32): the left of its screen on the 1920 x 1080 screen.</summary>
	public const double StageLeft = (1920.0 - 256.0 * Scale) / 2.0;

	/// <summary>The results screen is 320 pixels wide (H40): the left of its screen.</summary>
	public const double ResultsLeft = (1920.0 - 320.0 * Scale) / 2.0;

	/// <summary>Both screens are 224 pixels high: the top of the screen.</summary>
	public const double ScreenTop = (1080.0 - 224.0 * Scale) / 2.0;

	/// <summary>The wings of the checkpoint messages (Obj5A frame $14), on palette line 1.</summary>
	public const int WingsFrame = 0x14;

	/// <summary>The hand of the checkpoint messages (Obj5A frame $15), on palette line 1.</summary>
	public const int HandFrame = 0x15;

	/// <summary>The banner with the flags (Obj5F frame 0).</summary>
	public const int BannerFrame = 0;

	/// <summary>The pieces of the banner (Obj5F frames 2 to 8): the left flag, S, T, A, R, T and the right flag.</summary>
	public const int FirstBannerPiece = 2;

	public const int BannerPieces = 7;

	public AnimationGroup Messages { get; }

	public AnimationGroup Start { get; }

	public AnimationGroup Results { get; }

	public AnimationGroup Digits { get; }

	public HalfPipeOriginalArt(ResourceTree tree)
	{
		Messages = tree.GetLoadedResource<AnimationGroup>(MessagesKey);
		Start = tree.GetLoadedResource<AnimationGroup>(StartKey);
		Results = tree.GetLoadedResource<AnimationGroup>(ResultsKey);
		Digits = tree.GetLoadedResource<AnimationGroup>(DigitsKey);
		// Sharp pixels, as in the original
		foreach (ITexture texture in new[] { Messages, Start, Results, Digits }.SelectMany(group => group.Textures))
		{
			texture.Filtering = TextureFiltering.NearestNeighbour;
		}
	}

	/// <summary>True when a mod has all the art.</summary>
	public static bool IsAvailable(ResourceTree tree) => ResourceKeys.All(key => tree[key]?.Resource != null);

	/// <summary>Draws a frame with the object at a position on the screen of the original.</summary>
	/// <param name="left">The left of the screen of the original: <see cref="StageLeft"/> or <see cref="ResultsLeft"/>.</param>
	/// <param name="flipY">Draws the frame upside down, about the object position.</param>
	public static void DrawFrame(I2dRenderer g, AnimationGroup group, int animation, double left, double x, double y, bool flipY = false)
	{
		Animation.Frame frame = group[animation].Frames[0];
		Rectanglei source = frame.Source;
		if (source.Width <= 1 && source.Height <= 1)
		{
			return;
		}
		double centreX = left + x * Scale + frame.Offset.X;
		double centreY = ScreenTop + y * Scale + (flipY ? -frame.Offset.Y : frame.Offset.Y);
		g.RenderTexture(group.Textures[frame.TextureIndex], new Rectangle(source.X, source.Y, source.Width, source.Height),
			new Rectangle(centreX - source.Width / 2.0, centreY - source.Height / 2.0, source.Width, source.Height), flipy: flipY);
	}

	/// <summary>Draws a number with its right edge at a position, without leading zeros, as the HUD code of the original does.</summary>
	public void DrawNumber(I2dRenderer g, int value, double left, double right, double y)
	{
		string digits = Math.Max(0, value).ToString(System.Globalization.CultureInfo.InvariantCulture);
		for (int i = 0; i < digits.Length; i++)
		{
			// Each digit is 8 pixels wide, with its left edge at the object position
			double x = right - (digits.Length - i) * 8;
			DrawFrame(g, Digits, digits[i] - '0', left, x, y);
		}
	}
}
