using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.STARPOST;

/// <summary>
/// A starpost (checkpoint). When the protagonist touches it, it sets the restart position of the player. The star on
/// top spins two turns and then glows.
/// </summary>
public class StarpostInstance : ActiveObject
{
	public const int AnimationStand = 0;
	public const int AnimationStarInactive = 1;
	public const int AnimationStarActive = 2;
	public const int AnimationStarActiveGlow = 3;

	/// <summary>The centre of the star circle (for the special stage) above the starpost.</summary>
	private static readonly Vector2i BonusStarOffset = new Vector2i(0, -220);

	/// <summary>The distance from the star to the axle that it turns around.</summary>
	private const double StarAxleRadius = 44.0;
	private static readonly Vector2 StarAxleMidpoint = new Vector2(0.0, -52.0);

	private AnimationInstance _standAnimation;
	private AnimationInstance _starAnimation;
	private AnimationInstance _starGlowAnimation;
	private Vector2 _starPosition;
	private int _index;
	private bool _activated;
	private bool _glowing;
	private bool _starSpinning;
	private double _starAngle;

	[StateVariable]
	private int Index
	{
		get => _index;
		set => _index = value;
	}

	public StarpostInstance() => DesignBounds = new Rectanglei(-50, -150, 100, 300);

	protected override void OnStart()
	{
		AnimationGroup animationGroup = Level.GameContext.ResourceTree.GetLoadedResource<AnimationGroup>(Type, StarpostType.AnimationGroupResourceKey);
		_standAnimation = new AnimationInstance(animationGroup);
		_starAnimation = new AnimationInstance(animationGroup);
		_starGlowAnimation = new AnimationInstance(animationGroup);
		_standAnimation.Index = AnimationStand;
		_starAnimation.Index = AnimationStarInactive;
		_starGlowAnimation.Index = AnimationStarActiveGlow;
		CollisionRectangles = new CollisionRectangle[]
		{
			new CollisionRectangle(this, 0, -32, -128, 64, 256)
		};
		_activated = Level.Player.IsStarpostActivated(_index);
		_glowing = _activated;
		UpdateStarPosition();
	}

	protected override void OnUpdate()
	{
		// A starpost with a higher index also activates this starpost.
		if (!_activated && Level.Player.IsStarpostActivated(_index))
		{
			_activated = true;
			_glowing = true;
		}
		if (_starSpinning)
		{
			// Spin two full turns in 30 ticks, then glow.
			_starAngle += 2.0 * Math.PI / 15.0;
			if (_starAngle >= 4.0 * Math.PI)
			{
				_starAngle = 0.0;
				_starSpinning = false;
				_glowing = true;
			}
		}
		UpdateStarPosition();
	}

	private void UpdateStarPosition()
	{
		_starPosition = new Vector2(StarAxleMidpoint.X + Math.Sin(_starAngle) * StarAxleRadius, StarAxleMidpoint.Y - Math.Cos(_starAngle) * StarAxleRadius);
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (_activated || e.ActiveObject.Type.Classification != ObjectClassification.Character)
			return;
		ICharacter character = (ICharacter)e.ActiveObject;
		if (character.IsSidekick)
			return;
		character.Player.ActivateStarpost(_index, Position + new Vector2i(0, 64));
		_activated = true;
		_starSpinning = true;
		Level.SoundManager.PlaySound(this, Type.GetAbsolutePath(StarpostType.SoundResourceKey));
		// With 50 rings and fewer than 7 emeralds, a star circle opens the special stage (Sonic 2)
		Player player = character.Player;
		if (player.CurrentRings >= SONICORCA.OBJECTS.STARPOSTBONUSSTAR.StarpostBonusStarInstance.RingsNeeded
			&& player.Emeralds <= SONICORCA.OBJECTS.STARPOSTBONUSSTAR.StarpostBonusStarInstance.MaxEmeralds)
		{
			Level.ObjectManager.AddObject(new ObjectPlacement(Type.GetAbsolutePath(StarpostType.BonusStarResourceKey),
				Level.Map.Layers.IndexOf(Layer), Position + BonusStarOffset));
		}
	}

	protected override void OnAnimate()
	{
		if (_glowing)
		{
			_starAnimation.Index = AnimationStarActive;
			_starGlowAnimation.CurrentFrameIndex = _starAnimation.CurrentFrameIndex;
		}
		_standAnimation.Animate();
		_starAnimation.Animate();
		_starGlowAnimation.Animate();
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		objectRenderer.Render(_standAnimation);
		objectRenderer.Render(_starAnimation, _starPosition);
		if (!_glowing)
			return;
		objectRenderer.BlendMode = BlendMode.Additive;
		objectRenderer.EmitsLight = true;
		objectRenderer.Render(_starGlowAnimation, _starPosition);
	}
}
