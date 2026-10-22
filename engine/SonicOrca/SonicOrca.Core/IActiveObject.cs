using SonicOrca.Geometry;

namespace SonicOrca.Core;

public interface IActiveObject
{
	Level Level { get; }

	ObjectType Type { get; }

	ObjectEntry Entry { get; }

	LevelLayer Layer { get; set; }

	int Priority { get; set; }

	Vector2i Position { get; set; }

	Vector2 PositionPrecise { get; set; }

	Vector2i LastPosition { get; }

	Vector2 LastPositionPrecise { get; }

	float Brightness { get; set; }

	void Finish();

	void FinishForever();
}
