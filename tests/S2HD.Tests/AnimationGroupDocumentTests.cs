using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SonicOrca.Core.Editor;
using SonicOrca.Resources;
using Xunit;

namespace S2HD.Tests;

/// <summary>Tests the document of the animation editor: reading, writing and the defaults of the animations.</summary>
public sealed class AnimationGroupDocumentTests
{
	private const string Sample = """
		<?xml version="1.0"?>
		<anigroup>
		  <textures>
		    <texture>//BODY</texture>
		    <texture>//SKIN</texture>
		  </textures>
		  <animations>

		    <!-- a note -->
		    <!-- wait -->
		    <animation delay="4" loop="1" offset_y="-7" w="10" h="20">
		      <frame x="0" y="0" />
		      <frame x="10" y="0" delay="8" />
		      <frame x="20" y="0" w="12" texture="1" offset_x="3" />
		    </animation>
		    <animation next="0">
		      <frame x="1" y="2" w="3" h="4" />
		    </animation>
		  </animations>
		</anigroup>
		""";

	[Fact]
	public void Parse_GivesEachFrameTheValuesOfItsAnimation()
	{
		AnimationGroupDocument document = AnimationGroupDocument.Parse(Sample);

		Assert.Equal(new[] { "//BODY", "//SKIN" }, document.Textures);
		Assert.Equal(2, document.Animations.Count);
		AnimationDocument wait = document.Animations[0];
		Assert.Equal("wait", wait.Name);
		Assert.Equal(new[] { "a note", "wait" }, wait.Comments);
		Assert.Equal(1, wait.Loop);
		Assert.Null(wait.Next);
		AnimationFrameDocument second = wait.Frames[1];
		Assert.Equal((10, 0, 10, 20, -7, 8), (second.X, second.Y, second.Width, second.Height, second.OffsetY, second.Delay));
		AnimationFrameDocument third = wait.Frames[2];
		Assert.Equal((12, 1, 3, 4), (third.Width, third.Texture, third.OffsetX, third.Delay));
		Assert.Null(document.Animations[1].Name);
		Assert.Equal(0, document.Animations[1].Next);
	}

	[Fact]
	public void ToXml_ThenParse_KeepsEverything()
	{
		AnimationGroupDocument document = AnimationGroupDocument.Parse(Sample);

		AnimationGroupDocument copy = AnimationGroupDocument.Parse(document.ToXml());

		AssertSame(document, copy, "sample");
	}

	[Fact]
	public void ToXml_PutsTheMostCommonValuesOnTheAnimation()
	{
		string xml = AnimationGroupDocument.Parse(Sample).ToXml();

		Assert.Contains("<animation w=\"10\" h=\"20\" offset_y=\"-7\" delay=\"4\" loop=\"1\">", xml);
		Assert.Contains("<frame x=\"20\" y=\"0\" w=\"12\" texture=\"1\" offset_x=\"3\" />", xml);
		Assert.Contains("<!-- wait -->", xml);
	}

	[Fact]
	public void Name_ReplacesTheLastComment_AndCannotBreakTheComment()
	{
		AnimationGroupDocument document = AnimationGroupDocument.Parse(Sample);

		document.Animations[0].Name = "idle -- new";
		document.Animations[1].Name = "run";

		Assert.Equal(new[] { "a note", "idle - new" }, document.Animations[0].Comments);
		Assert.Equal("run", AnimationGroupDocument.Parse(document.ToXml()).Animations[1].Name);
	}

	[Fact]
	public void Clone_SharesNoFrame()
	{
		AnimationGroupDocument document = AnimationGroupDocument.Parse(Sample);

		AnimationGroupDocument copy = document.Clone();
		copy.Animations[0].Frames[0].X = 99;
		copy.Animations[0].Frames.RemoveAt(1);

		Assert.Equal(0, document.Animations[0].Frames[0].X);
		Assert.Equal(3, document.Animations[0].Frames.Count);
	}

	[Fact]
	public void GameData_EveryAnimationGroup_WritesAndReadsTheSameFrames()
	{
		string path = TestPaths.GameDataFile;
		if (path == null)
		{
			Assert.Skip("Game files not found. Set S2HD_GAME_DIR to the S2HD_2.0.1012-rc2 folder.");
		}
		IEnumerable<Resource> groups = new ResourceFile(path).Scan().GetResourceListing().Values
			.Where(r => r.Identifier == ResourceTypeIdentifier.AnimationGroup);
		int count = 0;
		foreach (Resource group in groups)
		{
			string xml = Encoding.UTF8.GetString(ResourceFileTests.ReadAll(group.Source.ReadUncompressed()));
			AnimationGroupDocument document = AnimationGroupDocument.Parse(xml);
			AssertSame(document, AnimationGroupDocument.Parse(document.ToXml()), group.FullKeyPath);
			count++;
		}
		Assert.True(count > 50, $"Only {count} animation groups.");
	}

	private static void AssertSame(AnimationGroupDocument expected, AnimationGroupDocument actual, string name)
	{
		Assert.Equal(expected.Textures, actual.Textures);
		Assert.Equal(expected.Animations.Count, actual.Animations.Count);
		for (int i = 0; i < expected.Animations.Count; i++)
		{
			AnimationDocument a = expected.Animations[i];
			AnimationDocument b = actual.Animations[i];
			Assert.True(a.Loop == b.Loop && a.Next == b.Next, $"{name} animation {i}: loop or next changed.");
			Assert.Equal(a.Comments, b.Comments);
			Assert.Equal(a.Frames.Count, b.Frames.Count);
			for (int f = 0; f < a.Frames.Count; f++)
			{
				AnimationFrameDocument x = a.Frames[f];
				AnimationFrameDocument y = b.Frames[f];
				Assert.True((x.Texture, x.X, x.Y, x.Width, x.Height, x.OffsetX, x.OffsetY, x.Delay) == (y.Texture, y.X, y.Y, y.Width, y.Height, y.OffsetX, y.OffsetY, y.Delay),
					$"{name} animation {i} frame {f} changed.");
			}
		}
	}
}

/// <summary>Tests the changes that the animation editor makes to a document.</summary>
public sealed class AnimationDocumentEditTests
{
	private static AnimationDocument Animation(int frames, int? loop)
	{
		AnimationDocument animation = new AnimationDocument { Loop = loop };
		for (int i = 0; i < frames; i++)
		{
			animation.Frames.Add(new AnimationFrameDocument { X = i });
		}
		return animation;
	}

	[Fact]
	public void InsertAndRemoveFrame_KeepTheLoopOnTheSameFrame()
	{
		AnimationDocument animation = Animation(4, loop: 2);

		animation.InsertFrame(0, new AnimationFrameDocument { X = 9 });
		Assert.Equal(3, animation.Loop);
		Assert.Equal(2, animation.Frames[animation.Loop.Value].X);

		animation.RemoveFrame(1);
		Assert.Equal(2, animation.Loop);
		Assert.Equal(2, animation.Frames[animation.Loop.Value].X);

		animation.InsertFrame(4, new AnimationFrameDocument { X = 7 });
		Assert.Equal(2, animation.Loop);
	}

	[Fact]
	public void RemoveFrame_OfTheLoopFrame_LoopsToTheNextFrame_AndNoFramesMeansNoLoop()
	{
		AnimationDocument animation = Animation(3, loop: 2);

		animation.RemoveFrame(2);
		Assert.Equal(1, animation.Loop);
		animation.RemoveFrame(0);
		animation.RemoveFrame(0);
		Assert.Null(animation.Loop);
	}

	[Fact]
	public void MoveFrame_KeepsTheLoopOnTheSameFrame()
	{
		AnimationDocument animation = Animation(4, loop: 1);

		animation.MoveFrame(1, 3);

		Assert.Equal(new[] { 0, 2, 3, 1 }, animation.Frames.Select(f => f.X));
		Assert.Equal(3, animation.Loop);
	}

	[Fact]
	public void RemoveLastAnimation_RemovesTheNextThatPointedToIt()
	{
		AnimationGroupDocument document = new AnimationGroupDocument();
		document.AddAnimation(new AnimationDocument { Next = 1 });
		document.AddAnimation(new AnimationDocument { Next = 0 });

		document.RemoveLastAnimation();

		Assert.Single(document.Animations);
		Assert.Null(document.Animations[0].Next);
	}
}
