// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.CPZSPINTUBE.CPZSpinTubeInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System.Collections.Generic;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

#nullable disable
namespace SONICORCA.OBJECTS.CPZSPINTUBE;

public class CPZSpinTubeInstance : ActiveObject
{
	private int _previousCharacterLayer;
	private CPZSpinTubeInstance.PathElement[][] _path = new CPZSpinTubeInstance.PathElement[2][];
	private int _pathIndex;

	public CPZSpinTubeInstance()
	{
		DesignBounds = new Rectanglei(-64, -64, 128 /*0x80*/, 128 /*0x80*/);
	}

	[StateVariable]
	private string Path0 { get; set; }

	[StateVariable]
	private string Path1 { get; set; }

	protected override void OnStart()
	{
		_path[0] = CPZSpinTubeInstance.ReadPathElements(Path0);
		_path[1] = CPZSpinTubeInstance.ReadPathElements(Path1);
		if (_path[0] == null || _path[0].Length == 0)
			Finish();
		else
			CollisionRectangles = new CollisionRectangle[1]
			{
		new CollisionRectangle( this, 0, _path[0][0].Position.X - 64 /*0x40*/, _path[0][0].Position.Y - 56, 128 /*0x80*/, 128 /*0x80*/)
			};
	}

	private static CPZSpinTubeInstance.PathElement[] ReadPathElements(string pathDataString)
	{
		if (pathDataString == null)
			pathDataString = string.Empty;
		List<CPZSpinTubeInstance.PathElement> pathElementList = new List<CPZSpinTubeInstance.PathElement>();
		string str1 = pathDataString.Trim();
		char[] chArray = new char[1] { '|' };
		foreach (string str2 in str1.Split(chArray))
		{
			bool spinNoise = false;
			string str3 = str2.Trim();
			if (str3.Length >= 3)
			{
				if (str3[0] == '#')
				{
					spinNoise = true;
					str3 = str3.Substring(1);
				}
				string[] strArray = str3.Split(',');
				int result1;
				int result2;
				if (strArray.Length == 2 && int.TryParse(strArray[0], out result1) && int.TryParse(strArray[1], out result2))
					pathElementList.Add(new CPZSpinTubeInstance.PathElement(new Vector2i(result1, result2), spinNoise));
			}
		}
		return pathElementList.ToArray();
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.ActiveObject.Type.Classification != ObjectClassification.Character)
			return;
		ICharacter activeObject = (ICharacter)e.ActiveObject;
		if (activeObject.ObjectLink == this)
			return;
		CharacterBegin(activeObject);
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		ICharacter[] array = Level.ObjectManager.Characters.Where<ICharacter>(x => x.ObjectLink == this).ToArray<ICharacter>();
		if (array.Length == 0)
		{
			_pathIndex = Level.Random.Next(0, 2);
			if (_path[1] == null || _path[1].Length == 0)
				_pathIndex = 0;
		}
		foreach (ICharacter character in array)
		{
			if ((int)character.ObjectTag >= _path[_pathIndex].Length || character.IsDebug || character.IsDying || character.IsDead)
				ReleaseCharacter(character);
			else
				CharacterContinue(character);
		}
		LockLifetime = array.Length != 0;
	}

	private void CharacterBegin(ICharacter character)
	{
		character.IsAirborne = true;
		character.ForceSpinball = true;
		character.IsSpinball = true;
		character.Position = Position + _path[_pathIndex][0].Position;
		character.ObjectLink = this;
		character.ObjectTag = 1;
		character.IsObjectControlled = true;
		Level.SoundManager.PlaySound("SONICORCA/SOUND/SPINBALL");
		_previousCharacterLayer = Level.Map.Layers.IndexOf(character.Layer);
		character.Layer = Level.Map.Layers.First<LevelLayer>(l => l.Name == "pipes bottom");
	}

	private void CharacterContinue(ICharacter character)
	{
		int objectTag = (int)character.ObjectTag;
		Vector2i vector2i = Position + _path[_pathIndex][objectTag].Position;
		Vector2i position = character.Position;
		Vector2 vector2 = (Vector2)(vector2i - position);
		if (vector2.Length <= 32.0)
		{
			character.Position = vector2i;
			character.ObjectTag = objectTag + 1;
		}
		else
			character.Position = (Vector2i)((Vector2)position + vector2.Normalised * 32.0);
		character.IsAirborne = true;
		character.ForceSpinball = false;
		character.Velocity = (vector2.X != 0.0 || vector2.Y != 0.0 ? vector2.Normalised : character.Velocity.Normalised) * 0.001;
		character.CheckCollision = false;
		if (!_path[_pathIndex][objectTag].SpinNoise)
			return;
		Level.SoundManager.PlaySound("SONICORCA/SOUND/SPINBALL");
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

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		if (!viewOptions.ShowObjectCollision && !Level.StateFlags.HasFlag(LevelStateFlags.Editing))
			return;
		I2dRenderer obj1 = renderer.Get2dRenderer();
		Rectanglei rectanglei = DesignBounds.OffsetBy(_path[0][0].Position);
		obj1.RenderQuad(Colours.White, new Rectangle(rectanglei.X * renderer.GetObjectRenderer().Scale.X, rectanglei.Y * renderer.GetObjectRenderer().Scale.Y, rectanglei.Width * renderer.GetObjectRenderer().Scale.X, rectanglei.Height * renderer.GetObjectRenderer().Scale.Y));
		for (int index = 0; index < 2; ++index)
		{
			CPZSpinTubeInstance.PathElement[] source = _path[index];
			if (source != null && source.Length != 0)
			{
				Vector2 vector2_1 = (Vector2)source[0].Position;
				foreach (Vector2i vector2i in source.Skip<CPZSpinTubeInstance.PathElement>(1).Select<CPZSpinTubeInstance.PathElement, Vector2i>(x => x.Position))
				{
					Vector2 vector2_2 = (Vector2)vector2i;
					I2dRenderer obj2 = obj1;
					Colour yellow = Colours.Yellow;
					Vector2 a = new Vector2(vector2_1.X * renderer.GetObjectRenderer().Scale.X, vector2_1.Y * renderer.GetObjectRenderer().Scale.Y);
					double x1 = vector2_2.X;
					Vector2 scale = renderer.GetObjectRenderer().Scale;
					double x2 = scale.X;
					double x3 = x1 * x2;
					double y1 = vector2_2.Y;
					scale = renderer.GetObjectRenderer().Scale;
					double y2 = scale.Y;
					double y3 = y1 * y2;
					Vector2 b = new Vector2(x3, y3);
					obj2.RenderLine(yellow, a, b, 2.0);
					vector2_1 = vector2_2;
				}
			}
		}
	}

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
