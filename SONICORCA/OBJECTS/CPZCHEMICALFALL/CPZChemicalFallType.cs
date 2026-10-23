using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;

namespace SONICORCA.OBJECTS.CPZCHEMICALFALL;

/// <summary>The object type for <see cref="CPZChemicalFallInstance"/>, a column of falling chemical liquid.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Chemical Fall")]
[Description("Chemical Fall from Chemical Plant Zone, Sonic 2")]
[ObjectInstance(typeof(CPZChemicalFallInstance))]
public class CPZChemicalFallType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>Called by <see cref="ObjectType.GetLifeRadius(IActiveObject)"/> through dynamic dispatch.</summary>
	public new Vector2 GetLifeRadius(ActiveObject state)
	{
		return (Vector2)new Vector2i(0, Level.Map.Bounds.Height);
	}
}
