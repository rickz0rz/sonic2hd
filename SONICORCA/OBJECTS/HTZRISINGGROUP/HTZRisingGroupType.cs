// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.HTZRISINGGROUP.HTZRisingGroupType
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;

#nullable disable
namespace SONICORCA.OBJECTS.HTZRISINGGROUP;

/// <summary>The object type of the controller that moves a group of rising blocks during the Hill Top Zone earthquake.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Rising group")]
[Description("Rising group from Hill Top Zone, Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Obstacle)]
[ObjectInstance(typeof(HTZRisingGroupInstance))]
public class HTZRisingGroupType : ObjectType
{
	public Vector2 GetLifeRadius(HTZRisingGroupInstance state)
	{
		return new Vector2(state.Size.X / 2.0, state.Size.Y / 2.0);
	}
}
