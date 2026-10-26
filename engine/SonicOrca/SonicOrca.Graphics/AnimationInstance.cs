using System.Collections.Generic;
using SonicOrca.Geometry;
using SonicOrca.Resources;

namespace SonicOrca.Graphics;

/// <summary>
/// Plays one animation of an animation group and draws its current frame.
/// </summary>
public class AnimationInstance
{
	private readonly AnimationGroup _animationGroup;

	private int _animationIndex;

	private int _frameTime;

	public int CurrentFrameIndex { get; set; }

	public int Cycles { get; set; }

	public int? OverrideDelay { get; set; }

	public int? OverrideTextureIndex { get; set; }

	public bool AdditiveBlending { get; set; }

	public Animation Animation => _animationGroup[_animationIndex];

	public AnimationGroup AnimationGroup => _animationGroup;

	/// <summary>
	/// The frame that the instance shows. An editor can give the animation fewer frames (see
	/// <see cref="AnimationGroup.Replace"/>), so a frame index after the last frame goes back to the first frame.
	/// </summary>
	public Animation.Frame CurrentFrame
	{
		get
		{
			IReadOnlyList<Animation.Frame> frames = _animationGroup[_animationIndex].Frames;
			if (CurrentFrameIndex >= frames.Count)
			{
				CurrentFrameIndex = 0;
			}
			return frames[CurrentFrameIndex];
		}
	}

	public ITexture CurrentTexture => _animationGroup.Textures[OverrideTextureIndex.GetValueOrDefault(CurrentFrame.TextureIndex)];

	public int Index
	{
		get
		{
			return _animationIndex;
		}
		set
		{
			if (_animationIndex != value)
			{
				_animationIndex = value;
				CurrentFrameIndex = 0;
				_frameTime = 0;
			}
		}
	}

	public AnimationInstance(ResourceTree resourceTree, string resourceKey, int index = 0)
	{
		_animationGroup = resourceTree.GetLoadedResource<AnimationGroup>(resourceKey);
		_animationIndex = index;
	}

	public AnimationInstance(AnimationGroup animationGroup, int index = 0)
	{
		_animationGroup = animationGroup;
		_animationIndex = index;
	}

	public void ResetFrame()
	{
		CurrentFrameIndex = 0;
		_frameTime = 0;
	}

	public void Animate()
	{
		Animation animation = _animationGroup[_animationIndex];
		Animation.Frame frame = CurrentFrame;
		int delay = OverrideDelay.GetValueOrDefault(frame.Delay);
		if (_frameTime >= delay)
		{
			CurrentFrameIndex++;
			if (CurrentFrameIndex >= animation.Frames.Count)
			{
				if (animation.LoopFrameIndex.HasValue)
				{
					CurrentFrameIndex = animation.LoopFrameIndex.Value;
				}
				else if (animation.NextFrameIndex.HasValue)
				{
					// When the index changes, the Index setter also resets the frame index and the frame time.
					Index = animation.NextFrameIndex.Value;
				}
				else
				{
					CurrentFrameIndex = 0;
				}
				Cycles++;
			}
			_frameTime = 0;
		}
		else
		{
			_frameTime++;
		}
	}

	public void Seek(int ticks)
	{
		for (int i = 0; i < ticks; i++)
		{
			Animate();
		}
	}

	public void Draw(I2dRenderer renderer, Vector2 position = default, bool flipX = false, bool flipY = false)
	{
		Draw(renderer, Colours.White, position, flipX, flipY);
	}

	public void Draw(I2dRenderer renderer, Colour colour, Vector2 position = default, bool flipX = false, bool flipY = false)
	{
		Animation.Frame currentFrame = CurrentFrame;
		Rectangle destination = new Rectangle(position.X - currentFrame.Source.Width / 2, position.Y - currentFrame.Source.Height / 2, currentFrame.Source.Width, currentFrame.Source.Height);
		Draw(renderer, colour, destination, flipX, flipY);
	}

	public void Draw(I2dRenderer renderer, Colour colour, Rectangle destination, bool flipX = false, bool flipY = false)
	{
		Draw(renderer, colour, CurrentFrame.Source, destination, flipX, flipY);
	}

	public void Draw(I2dRenderer renderer, Colour colour, Rectangle source, Rectangle destination, bool flipX = false, bool flipY = false)
	{
		Animation.Frame currentFrame = CurrentFrame;
		ITexture texture = _animationGroup.Textures[OverrideTextureIndex.GetValueOrDefault(currentFrame.TextureIndex)];
		destination = destination.OffsetBy(currentFrame.Offset);
		renderer.Colour = colour;
		renderer.BlendMode = AdditiveBlending ? BlendMode.Additive : BlendMode.Alpha;
		renderer.RenderTexture(texture, source, destination, flipX, flipY);
		renderer.BlendMode = BlendMode.Alpha;
	}
}
