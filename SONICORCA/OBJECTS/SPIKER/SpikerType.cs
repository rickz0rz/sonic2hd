// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.SPIKER.SpikerType
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;

#nullable disable
namespace SONICORCA.OBJECTS.SPIKER;

/// <summary>The object type for Spiker, a drill badnik that fires its spike up when a character comes near.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Spiker")]
[Description("Spiker from Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Badnik)]
[ObjectInstance(typeof(SpikerInstance))]
public class SpikerType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	// ObjectType.GetLifeRadius finds this method by dynamic dispatch. A Spiker stays active 1024 units to each side,
	// so that a spike that it fired can finish its flight.
	public new Vector2 GetLifeRadius(ActiveObject state)
	{
		return (Vector2)new Vector2i(1024, 0);
	}
}
