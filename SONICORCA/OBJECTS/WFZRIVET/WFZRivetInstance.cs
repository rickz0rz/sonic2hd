using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.WFZRIVET;

/// <summary>
/// The rivet on the deck of the ship at the end of Wing Fortress Zone, as in Sonic 2 (ObjC2). It is a solid block
/// $10 pixels to each side and 8 pixels up and down. When the main player lands on it in a ball, it explodes and
/// opens the deck. The original writes other chunks into the level layout. A map from "s2hd-data import-level wfz"
/// has this change as the layout change "rivet", which the object switches on: a hole in the deck below the rivet,
/// the inside of the ship and the floor of the boss room.
/// </summary>
public class WFZRivetInstance : ActiveObject
{
	private const int Scale = 4;

	private const int HalfWidth = 0x10;

	private const int HalfHeight = 8;

	/// <summary>The layout change of the import that opens the deck.</summary>
	private const string LayoutChangeName = "rivet";

	private AnimationInstance _animation;

	private bool _playerInBall;

	public WFZRivetInstance() => DesignBounds = new Rectanglei(-HalfWidth * Scale, -HalfHeight * Scale, HalfWidth * 2 * Scale, HalfHeight * 2 * Scale);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(WFZRivetType.AnimationGroupResourceKey));
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-HalfWidth * Scale, -HalfHeight * Scale, HalfWidth * 2 * Scale, HalfHeight * 2 * Scale));
		foreach (CollisionVector collisionVector in CollisionVectors)
		{
			collisionVector.Flags |= CollisionFlags.Solid;
		}
	}

	protected override void OnUpdatePrepare()
	{
		// A character in a ball that lands stops being in a ball when it updates. The original also looks at the
		// animation of the player before it lands.
		_playerInBall = Level.Player.Protagonist?.IsSpinball == true;
	}

	protected override void OnUpdate()
	{
		ICharacter player = Level.Player.Protagonist;
		if (player == null || player.ObjectLink != this || !_playerInBall)
		{
			return;
		}
		// ObjC2_Bust
		player.LeaveGround();
		player.IsAirborne = true;
		CollisionVectors = new CollisionVector[0];
		Level.ObjectManager.AddObject(new ObjectPlacement(Type.GetAbsolutePath(WFZRivetType.ExplosionResourceKey), Level.Map.Layers.IndexOf(Layer), Position));
		LayoutChanges.Apply(Level, LayoutChangeName);
		FinishForever();
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		_animation.Index = 0;
		renderer.GetObjectRenderer().Render(_animation);
	}
}
