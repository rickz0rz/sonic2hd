using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.MCZSPIKEBALLCHAIN;

/// <summary>
/// A spike ball on a chain, as in Sonic 2 (Obj75 in Mystic Cave Zone). The chain turns around its first link. Each
/// link is $10 pixels from the one before, and the spike ball is at the end. The spike ball hurts a character that
/// touches it. The angle changes by the speed each frame (a word, so 256 units for one angle step). With $F links,
/// the object is a solid brick that does not move.
/// </summary>
public class MCZSpikeBallChainInstance : ActiveObject
{
	private const int Scale = 4;

	/// <summary>The value of the links that makes a brick instead of a chain.</summary>
	private const int BrickLinks = 0xF;

	private const int LinkDistance = 0x10;

	/// <summary>The touch box of the spike ball (collision size $1A): $C pixels from the centre.</summary>
	private const int BallHalfSize = 0xC * Scale;

	/// <summary>The brick is $10 pixels from its centre to each side.</summary>
	private const int BrickHalfSize = 0x10 * Scale;

	private const int BallFrame = 0;

	private const int LinkFrame = 1;

	private const int BrickFrame = 2;

	private AnimationInstance _animation;

	private int _links = 6;

	private int _speed;

	private int _startAngle;

	private Vector2 _pivot;

	/// <summary>The angle in 1/256 angle units (the angle word of the original).</summary>
	private int _angle;

	/// <summary>The number of chain links (subtype bits 0-3). The value $F makes a brick.</summary>
	[StateVariable]
	private int Links
	{
		get => _links;
		set => _links = value;
	}

	/// <summary>The change of the angle each frame, in 1/256 angle units (subtype bits 4-7 as a signed byte, times 8).</summary>
	[StateVariable]
	private int Speed
	{
		get => _speed;
		set => _speed = value;
	}

	/// <summary>The angle at the start: $40 for the x flip and $80 for the y flip of the original.</summary>
	[StateVariable]
	private int StartAngle
	{
		get => _startAngle;
		set => _startAngle = value;
	}

	private bool IsBrick => _links == BrickLinks;

	public MCZSpikeBallChainInstance() => DesignBounds = new Rectanglei(-1024, -1024, 2048, 2048);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(MCZSpikeBallChainType.AnimationGroupResourceKey));
		_pivot = PositionPrecise;
		if (IsBrick)
		{
			DesignBounds = new Rectanglei(-BrickHalfSize, -BrickHalfSize, BrickHalfSize * 2, BrickHalfSize * 2);
			CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-BrickHalfSize, -BrickHalfSize, BrickHalfSize * 2, BrickHalfSize * 2));
			return;
		}
		int radius = (_links * LinkDistance + 0x10) * Scale;
		DesignBounds = new Rectanglei(-radius, -radius, radius * 2, radius * 2);
		_angle = (_startAngle & 0xFF) << 8;
		CollisionRectangles = new CollisionRectangle[]
		{
			new CollisionRectangle(this, 0, -BallHalfSize, -BallHalfSize, BallHalfSize * 2, BallHalfSize * 2)
		};
		PositionPrecise = PointOnChain(_links * LinkDistance);
	}

	protected override void OnUpdate()
	{
		if (IsBrick)
		{
			return;
		}
		_angle = (_angle + _speed) & 0xFFFF;
		PositionPrecise = PointOnChain(_links * LinkDistance);
	}

	/// <summary>A point on the chain at a distance from the pivot, in original pixels.</summary>
	private Vector2 PointOnChain(int distance)
	{
		int angle = _angle >> 8;
		return _pivot + new Vector2(distance * OriginalMath.Cos(angle) / 256.0 * Scale, distance * OriginalMath.Sin(angle) / 256.0 * Scale);
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (!IsBrick && e.ActiveObject.Type.Classification == ObjectClassification.Character)
		{
			var character = (ICharacter)e.ActiveObject;
			character.Hurt(Math.Sign(character.Position.X - Position.X));
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		if (IsBrick)
		{
			_animation.Index = BrickFrame;
			objectRenderer.Render(_animation);
			return;
		}
		_animation.Index = LinkFrame;
		for (int i = 0; i < _links; i++)
		{
			objectRenderer.Render(_animation, PointOnChain(i * LinkDistance) - PositionPrecise);
		}
		_animation.Index = BallFrame;
		objectRenderer.Render(_animation);
	}
}
