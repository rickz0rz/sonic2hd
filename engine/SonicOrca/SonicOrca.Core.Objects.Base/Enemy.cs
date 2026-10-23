using System;
using SonicOrca.Core.Collision;

namespace SonicOrca.Core.Objects.Base;

/// <summary>
/// The base class for an object that hurts a character on contact.
/// </summary>
public class Enemy : ActiveObject
{
	protected override void OnCollision(CollisionEvent e)
	{
		if (e.ActiveObject.Type.Classification == ObjectClassification.Character)
		{
			ICharacter character = (ICharacter)e.ActiveObject;
			if (character.CanBeHurt)
			{
				character.Hurt(Math.Sign(character.Position.X - Position.X));
				OnHurtCharacter(character);
			}
		}
	}

	protected virtual void OnHurtCharacter(ICharacter character)
	{
	}
}
