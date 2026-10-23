// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.REXON.RexonType
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

#nullable disable
namespace SONICORCA.OBJECTS.REXON;

/// <summary>The object type for Rexon, a badnik that lifts a long neck out of the lava and fires projectiles.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Rexon")]
[Description("Rexon from Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Badnik)]
[ObjectInstance(typeof(RexonInstance))]
public class RexonType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
