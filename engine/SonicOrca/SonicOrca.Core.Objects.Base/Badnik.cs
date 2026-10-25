using System;
using SonicOrca.Core.Collision;
using SonicOrca.Geometry;

namespace SonicOrca.Core.Objects.Base;

/// <summary>
/// The base class for a badnik. A deadly character destroys it, and other characters get hurt.
/// </summary>
public class Badnik : ActiveObject
{
	public bool CanBeDestroyed { get; set; } = true;

	protected override void OnStart()
	{
		Priority = 128;
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.ActiveObject.Type.Classification == ObjectClassification.Character)
		{
			ICharacter character = (ICharacter)e.ActiveObject;
			if (character.IsDeadly && CanBeDestroyed)
			{
				Destroy(character);
			}
			else
			{
				character.Hurt(Math.Sign(e.ActiveObject.Position.X - Position.X));
			}
		}
	}

	protected void Destroy(ICharacter character)
	{
		if (character.IsAirborne)
		{
			Rebound(character);
		}
		FinishForever();
		int points = character.Player.AwardChainedScore();
		CreateExplosionObject();
		Level.CreateRandomAnimalObject(Level.Map.Layers.IndexOf(Layer), Position);
		Level.CreateScoreObject(points, Position);
	}

	protected virtual void Rebound(ICharacter character)
	{
		Vector2 velocity = character.Velocity;
		if (velocity.Y < 0.0)
		{
			velocity.Y += 4.0;
		}
		else if (character.Position.Y >= Position.Y)
		{
			velocity.Y -= 4.0;
		}
		else
		{
			velocity.Y *= -1.0;
		}
		character.Velocity = velocity;
	}

	protected void CreateExplosionObject()
	{
		Level.ObjectManager.AddObject(new ObjectPlacement(Level.CommonResources.GetResourcePath("badnikexplosionobject"), Level.Map.Layers.IndexOf(Layer), Position));
	}
}
