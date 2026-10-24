using System.Collections.Generic;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.CPZSPINTUBE;

/// <summary>
/// The entrance of a spin tube in Chemical Plant Zone. It takes control of a character that touches it and moves
/// the character along one of two paths as a spinball. At the end of the path, it releases the character upwards.
/// The object is invisible in the game.
/// </summary>
public class CPZSpinTubeInstance : ActiveObject
{
	/// <summary>The maximum distance that a character moves along the path in one frame.</summary>
	private const double PathSpeed = 32.0;

	private int _previousCharacterLayer;
	private PathElement[][] _path = new PathElement[2][];

	/// <summary>The path that characters follow now. It changes at random while no character is in the tube.</summary>
	private int _pathIndex;

	public CPZSpinTubeInstance()
	{
		DesignBounds = new Rectanglei(-64, -64, 128, 128);
	}

	[StateVariable]
	private string Path0 { get; set; }

	[StateVariable]
	private string Path1 { get; set; }

	protected override void OnStart()
	{
		_path[0] = ReadPathElements(Path0);
		_path[1] = ReadPathElements(Path1);
		if (_path[0] == null || _path[0].Length == 0)
		{
			Finish();
		}
		else
		{
			CollisionRectangles = new CollisionRectangle[1]
			{
				new CollisionRectangle(this, 0, _path[0][0].Position.X - 64, _path[0][0].Position.Y - 56, 128, 128)
			};
		}
	}

	/// <summary>
	/// Reads a path from text such as "0,0|64,128|#128,256". Each point is "x,y", relative to the object.
	/// A point that starts with '#' plays the spin sound. Points that are not valid are ignored.
	/// </summary>
	private static PathElement[] ReadPathElements(string pathDataString)
	{
		if (pathDataString == null)
		{
			pathDataString = string.Empty;
		}
		List<PathElement> pathElements = new List<PathElement>();
		foreach (string elementString in pathDataString.Trim().Split('|'))
		{
			bool spinNoise = false;
			string element = elementString.Trim();
			if (element.Length < 3)
			{
				continue;
			}
			if (element[0] == '#')
			{
				spinNoise = true;
				element = element.Substring(1);
			}
			string[] coordinates = element.Split(',');
			if (coordinates.Length == 2 && int.TryParse(coordinates[0], out int x) && int.TryParse(coordinates[1], out int y))
			{
				pathElements.Add(new PathElement(new Vector2i(x, y), spinNoise));
			}
		}
		return pathElements.ToArray();
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.ActiveObject.Type.Classification != ObjectClassification.Character)
		{
			return;
		}
		ICharacter character = (ICharacter)e.ActiveObject;
		if (character.ObjectLink != this)
		{
			CharacterBegin(character);
		}
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		ICharacter[] linkedCharacters = Level.ObjectManager.Characters.Where(x => x.ObjectLink == this).ToArray();
		if (linkedCharacters.Length == 0)
		{
			_pathIndex = Level.Random.Next(0, 2);
			if (_path[1] == null || _path[1].Length == 0)
			{
				_pathIndex = 0;
			}
		}
		// The object tag of a character holds the index of the next path point.
		foreach (ICharacter character in linkedCharacters)
		{
			if ((int)character.ObjectTag >= _path[_pathIndex].Length || character.IsDebug || character.IsDying || character.IsDead)
			{
				ReleaseCharacter(character);
			}
			else
			{
				CharacterContinue(character);
			}
		}
		LockLifetime = linkedCharacters.Length != 0;
	}

	private void CharacterBegin(ICharacter character)
	{
		character.IsAirborne = true;
		character.IsSpinball = true;
		character.ForceSpinball = true;
		character.Position = Position + _path[_pathIndex][0].Position;
		character.ObjectLink = this;
		character.ObjectTag = 1;
		character.IsObjectControlled = true;
		Level.SoundManager.PlaySound("SONICORCA/SOUND/SPINBALL");
		_previousCharacterLayer = Level.Map.Layers.IndexOf(character.Layer);
		character.Layer = Level.Map.Layers.First(l => l.Name == "pipes bottom");
	}

	/// <summary>Moves the character towards the next path point. When the character reaches the point, it goes to the point after it.</summary>
	private void CharacterContinue(ICharacter character)
	{
		int pathPointIndex = (int)character.ObjectTag;
		Vector2i target = Position + _path[_pathIndex][pathPointIndex].Position;
		Vector2i position = character.Position;
		Vector2 toTarget = (Vector2)(target - position);
		if (toTarget.Length <= PathSpeed)
		{
			character.Position = target;
			character.ObjectTag = pathPointIndex + 1;
		}
		else
		{
			character.Position = (Vector2i)((Vector2)position + toTarget.Normalised * PathSpeed);
		}
		character.IsAirborne = true;
		character.ForceSpinball = false;
		// Set a very small velocity in the direction of movement. The position above already moves the character.
		character.Velocity = (toTarget.X != 0.0 || toTarget.Y != 0.0 ? toTarget.Normalised : character.Velocity.Normalised) * 0.001;
		character.CheckCollision = false;
		if (_path[_pathIndex][pathPointIndex].SpinNoise)
		{
			Level.SoundManager.PlaySound("SONICORCA/SOUND/SPINBALL");
		}
	}

	private void ReleaseCharacter(ICharacter character)
	{
		character.ObjectLink = null;
		character.ObjectTag = null;
		character.ForceSpinball = false;
		character.CheckCollision = true;
		character.IsObjectControlled = false;
		character.IsRollJumping = false;
		if (!character.IsDebug && !character.IsDying && !character.IsDead)
		{
			character.IsAirborne = true;
			character.IsSpinball = true;
			character.Velocity = new Vector2(0.0, -32.0);
			Level.SoundManager.PlaySound("SONICORCA/SOUND/SPINDASH/RELEASE");
		}
		character.Layer = Level.Map.Layers[_previousCharacterLayer];
	}

	/// <summary>Draws the entrance area and the two paths, in the editor or when object collision is visible.</summary>
	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		if (!viewOptions.ShowObjectCollision && !Level.StateFlags.HasFlag(LevelStateFlags.Editing))
		{
			return;
		}
		I2dRenderer renderer2d = renderer.Get2dRenderer();
		Vector2 scale = renderer.GetObjectRenderer().Scale;
		Rectanglei entrance = DesignBounds.OffsetBy(_path[0][0].Position);
		renderer2d.RenderQuad(Colours.White, new Rectangle(entrance.X * scale.X, entrance.Y * scale.Y, entrance.Width * scale.X, entrance.Height * scale.Y));
		for (int index = 0; index < 2; ++index)
		{
			PathElement[] path = _path[index];
			if (path == null || path.Length == 0)
			{
				continue;
			}
			Vector2 previousPoint = (Vector2)path[0].Position;
			foreach (Vector2i pathPoint in path.Skip(1).Select(x => x.Position))
			{
				Vector2 point = (Vector2)pathPoint;
				Vector2 a = new Vector2(previousPoint.X * scale.X, previousPoint.Y * scale.Y);
				Vector2 b = new Vector2(point.X * scale.X, point.Y * scale.Y);
				renderer2d.RenderLine(Colours.Yellow, a, b, 2.0);
				previousPoint = point;
			}
		}
	}

	/// <summary>One point of a spin tube path.</summary>
	private struct PathElement
	{
		public Vector2i Position { get; set; }

		public bool SpinNoise { get; set; }

		public PathElement(Vector2i position, bool spinNoise)
			: this()
		{
			Position = position;
			SpinNoise = spinNoise;
		}

		public override string ToString() => $"{Position}, Spin = {SpinNoise}";
	}
}
