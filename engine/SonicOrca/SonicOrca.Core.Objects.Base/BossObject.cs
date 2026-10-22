using SonicOrca.Core.Collision;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Core.Objects.Base;

public class BossObject : Enemy
{
	public static readonly Colour FlashAdditiveColour = new Colour(64, 64, 64);

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
		ExplosionResourceKey = "SONICORCA/OBJECTS/BOSS/EXPLOSION";
		HitSoundResourceKey = "SONICORCA/SOUND/BOSSHIT";
		Health = 8;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
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

	protected virtual void Hit(ICharacter character)
	{
		if (character.IsAirborne)
		{
			character.Velocity *= -1.0;
		}
		else
		{
			character.GroundVelocity *= -1.0;
		}
		Health--;
		InvincibilityTimer = 32;
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
			_explosionTimer = 8;
			Vector2i vector2i = new Vector2i(Level.Random.Next(-radius, radius), Level.Random.Next(-radius, radius));
			ExplodeAt(Position + vector2i);
		}
	}

	protected void ExplodeAt(Vector2i position)
	{
		Level.ObjectManager.AddObject(new ObjectPlacement(ExplosionResourceKey, Level.Map.Layers.IndexOf(Layer), position));
	}
}
