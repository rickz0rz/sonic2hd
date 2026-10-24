using SonicOrca.Core.Collision;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Core.Objects.Base;

/// <summary>
/// The base class for a boss. It has health, flashes after a hit and makes explosions when it is defeated.
/// </summary>
public class BossObject : Enemy
{
	public static readonly Colour FlashAdditiveColour = new Colour(64, 64, 64);

	/// <summary>
	/// The number of ticks that the boss cannot be hit after a hit.
	/// </summary>
	private const int HitInvincibilityTicks = 32;

	/// <summary>
	/// The number of ticks between two explosions.
	/// </summary>
	private const int ExplosionInterval = 8;

	private int _explosionTimer;

	protected string ExplosionResourceKey { get; set; }

	protected string HitSoundResourceKey { get; set; }

	protected int Health { get; set; }

	protected int InvincibilityTimer { get; set; }

	public bool Defeated { get; set; }

	public bool Fleeing { get; set; }

	protected bool IsInvincibleFlashing
	{
		get
		{
			if (InvincibilityTimer > 0)
			{
				return InvincibilityTimer % 2 == 0;
			}
			return false;
		}
	}

	public BossObject()
	{
		ExplosionResourceKey = "SONICORCA/OBJECTS/EXPLOSION/BOSS";
		HitSoundResourceKey = "SONICORCA/SOUND/BOSSHIT";
		Health = 8;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		Level.BossFight = true;
		if (InvincibilityTimer > 0)
		{
			InvincibilityTimer--;
		}
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.ActiveObject.Type.Classification == ObjectClassification.Character)
		{
			ICharacter character = (ICharacter)e.ActiveObject;
			if (character.IsDeadly)
			{
				if (InvincibilityTimer <= 0)
				{
					Hit(character);
				}
				return;
			}
		}
		base.OnCollision(e);
	}

	/// <summary>
	/// The boss takes a hit. The character bounces back. In Original physics, Touch_Enemy_Part2 reverses the velocity
	/// and keeps the ground speed, also on the ground. Thus a character on the ground continues in the next tick. HD
	/// physics reverses the ground speed of a character on the ground.
	/// </summary>
	protected virtual void Hit(ICharacter character)
	{
		if (character.IsAirborne || Level.OriginalPhysics)
		{
			character.Velocity *= -1.0;
		}
		else
		{
			character.GroundVelocity *= -1.0;
		}
		Health--;
		InvincibilityTimer = HitInvincibilityTicks;
		if (Health > 0)
		{
			Level.SoundManager.PlaySound(this, HitSoundResourceKey);
		}
		else
		{
			Defeat();
		}
	}

	protected virtual void Defeat()
	{
	}

	protected void UpdateExplosions(int radius)
	{
		if (_explosionTimer-- <= 0)
		{
			_explosionTimer = ExplosionInterval;
			Vector2i offset = new Vector2i(Level.Random.Next(-radius, radius), Level.Random.Next(-radius, radius));
			ExplodeAt(Position + offset);
		}
	}

	protected void ExplodeAt(Vector2i position)
	{
		Level.ObjectManager.AddObject(new ObjectPlacement(ExplosionResourceKey, Level.Map.Layers.IndexOf(Layer), position));
	}
}
