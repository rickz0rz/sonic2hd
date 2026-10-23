using System;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Core.Objects;

internal class GhostCharacterInstance : ActiveObject
{
	private AnimationInstance _animation;

	public double Angle { get; set; }

	public CharacterState State { get; set; }

	protected override void OnStart()
	{
		Priority = 1022;
	}

	public void Initialise(string animationResourceKey)
	{
		Initialise(Level.GameContext.ResourceTree.GetLoadedResource<AnimationGroup>(animationResourceKey));
	}

	public void Initialise(AnimationGroup animationGroup)
	{
		_animation = new AnimationInstance(animationGroup);
	}

	public void Set(PlayRecorder.Entry entry)
	{
		Position = entry.Position;
		if (Level.Map.Layers.Count > entry.LayerIndex)
		{
			Layer = Level.Map.Layers[entry.LayerIndex];
		}
		State = (CharacterState)entry.State;
		if (_animation != null)
		{
			_animation.Index = entry.AnimationIndex;
			_animation.CurrentFrameIndex = entry.AnimationFrameIndex;
		}
		Angle = entry.Angle;
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		if (!viewOptions.Shadows && _animation != null)
		{
			I2dRenderer i2dRenderer = renderer.Get2dRenderer();
			ICharacterRenderer characterRenderer = renderer.GetCharacterRenderer();
			characterRenderer.ModelMatrix = i2dRenderer.ModelMatrix;
			if (Angle != 0.0)
			{
				characterRenderer.ModelMatrix *= Matrix4.CreateRotationZ(Angle * Math.PI);
			}
			Animation.Frame currentFrame = _animation.CurrentFrame;
			Vector2 vector = currentFrame.Offset;
			Rectangle destination = new Rectangle(vector.X - currentFrame.Source.Width / 2, vector.Y - currentFrame.Source.Height / 2, currentFrame.Source.Width, currentFrame.Source.Height);
			characterRenderer.RenderTextureGhost(_animation.AnimationGroup.Textures[1], _animation.AnimationGroup.Textures[0], currentFrame.Source, destination, !State.HasFlag(CharacterState.Left), State.HasFlag(CharacterState.Left) && _animation.Index == 16);
		}
	}
}
