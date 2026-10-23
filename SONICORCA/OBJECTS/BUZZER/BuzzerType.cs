// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.BUZZER.BuzzerType
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;

#nullable disable
namespace SONICORCA.OBJECTS.BUZZER;

/// <summary>The object type for <see cref="BuzzerInstance"/>, the flying wasp badnik.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Buzzer")]
[Description("Buzzer from Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Badnik)]
[ObjectInstance(typeof(BuzzerInstance))]
public class BuzzerType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>Called by <see cref="ObjectType.GetLifeRadius(IActiveObject)"/> through dynamic dispatch.</summary>
	public new Vector2 GetLifeRadius(ActiveObject state)
	{
		return (Vector2)new Vector2i(1024, 0);
	}
}
