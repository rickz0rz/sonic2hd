using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.STARPOSTBONUSSTAR;

/// <summary>
/// The star circle that appears above a starpost when the player touches it with 50 rings or more and has fewer
/// than 7 Chaos Emeralds. Stars circle around a point, and the circle grows. As in Sonic 2 (s2disasm: Obj79), the
/// player can enter it from frame 128 to frame 511, and it goes away at frame 512. Entering it opens the special stage.
/// </summary>
public class StarpostBonusStarInstance : ActiveObject
{
	/// <summary>The rings that the player needs at the starpost.</summary>
	public const int RingsNeeded = 50;

	/// <summary>The player can have at most this number of emeralds to see the circle.</summary>
	public const int MaxEmeralds = 6;

	private const int StarCount = 4;

	private const int EnterStartFrame = 128;

	private const int EnterEndFrame = 512;

	/// <summary>The frames that the circle takes to grow to its full size.</summary>
	private const int GrowFrames = 64;

	private const double MaxRadius = 96.0;

	/// <summary>The turn of the circle on each frame, in radians.</summary>
	private const double TurnPerFrame = 2.0 * Math.PI / 48.0;

	private AnimationInstance _starAnimation;

	private int _frame;

	private bool _entered;

	public StarpostBonusStarInstance() => DesignBounds = new Rectanglei(-128, -128, 256, 256);

	protected override void OnStart()
	{
		AnimationGroup animationGroup = Level.GameContext.ResourceTree.GetLoadedResource<AnimationGroup>(Type, StarpostBonusStarType.AnimationGroupResourceKey);
		_starAnimation = new AnimationInstance(animationGroup);
		Priority = 1280;
	}

	protected override void OnUpdate()
	{
		_frame++;
		if (_frame >= EnterEndFrame)
		{
			FinishForever();
			return;
		}
		// The circle can be entered only after it has grown and turned for a while
		if (_frame == EnterStartFrame)
		{
			int size = (int)MaxRadius * 2;
			CollisionRectangles = new CollisionRectangle[]
			{
				new CollisionRectangle(this, 0, -size / 2, -size / 2, size, size)
			};
		}
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (_entered || _frame < EnterStartFrame || e.ActiveObject.Type.Classification != ObjectClassification.Character)
			return;
		ICharacter character = (ICharacter)e.ActiveObject;
		// Only the main player can enter
		if (character.IsSidekick)
			return;
		_entered = true;
		Level.SoundManager.PlaySound(this, Type.GetAbsolutePath(StarpostBonusStarType.EnterSoundResourceKey));
		Level.EnterSpecialStage();
	}

	protected override void OnAnimate()
	{
		_starAnimation.Animate();
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		objectRenderer.BlendMode = BlendMode.Additive;
		objectRenderer.EmitsLight = true;
		double radius = MaxRadius * Math.Min(1.0, _frame / (double)GrowFrames);
		for (int i = 0; i < StarCount; i++)
		{
			double angle = _frame * TurnPerFrame + i * 2.0 * Math.PI / StarCount;
			objectRenderer.Render(_starAnimation, new Vector2(Math.Cos(angle) * radius, Math.Sin(angle) * radius * 0.5));
		}
	}
}
