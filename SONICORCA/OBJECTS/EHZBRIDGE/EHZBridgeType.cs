// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.EHZBRIDGE.EHZBridgeType
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;

#nullable disable
namespace SONICORCA.OBJECTS.EHZBRIDGE;

/// <summary>The object type for <see cref="EHZBridgeInstance"/>, a log bridge that bends when a character stands on it.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Bridge")]
[Description("Bridge from Emerald Hill Zone, Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(EHZBridgeInstance))]
public class EHZBridgeType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>Called by <see cref="ObjectType.GetLifeRadius(IActiveObject)"/> through dynamic dispatch.</summary>
	public Vector2 GetLifeRadius(EHZBridgeInstance state)
	{
		return (Vector2)new Vector2i(state.Logs * 64 / 2, 0);
	}
}
