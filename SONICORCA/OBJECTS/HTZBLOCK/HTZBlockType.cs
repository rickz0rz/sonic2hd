// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.HTZBLOCK.HTZBlockType
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using SonicOrca.Core;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;

#nullable disable
namespace SONICORCA.OBJECTS.HTZBLOCK;

/// <summary>The object type of the floating block from Hill Top Zone.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Floating block")]
[Description("Floating block from Hill Top Zone, Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(HTZBlockInstance))]
public class HTZBlockType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	public Vector2 GetLifeRadius(Platform state)
	{
		return state.MovementRadius;
	}
}
