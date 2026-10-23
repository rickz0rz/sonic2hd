// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.CPZPLATFORM.CPZPlatformType
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using SonicOrca.Core;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;

#nullable disable
namespace SONICORCA.OBJECTS.CPZPLATFORM;

/// <summary>The object type for <see cref="CPZPlatformInstance"/>, a floating platform.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Floating platform")]
[Description("Floating platform from Chemical Plant Zone, Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(CPZPlatformInstance))]
public class CPZPlatformType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>Called by <see cref="ObjectType.GetLifeRadius(IActiveObject)"/> through dynamic dispatch.</summary>
	public Vector2 GetLifeRadius(Platform state)
	{
		return state.MovementRadius;
	}
}
