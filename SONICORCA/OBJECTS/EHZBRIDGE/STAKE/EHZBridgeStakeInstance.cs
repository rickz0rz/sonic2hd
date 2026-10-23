// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.EHZBRIDGE.STAKE.EHZBridgeStakeInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using SonicOrca.Core.Objects.Base;
using SonicOrca.Geometry;

#nullable disable
namespace SONICORCA.OBJECTS.EHZBRIDGE.STAKE;

/// <summary>The post at each end of a bridge in Emerald Hill Zone. It is decoration only.</summary>
public class EHZBridgeStakeInstance : Scenery
{
	public EHZBridgeStakeInstance()
		: base("//ANIGROUP")
	{
		DesignBounds = new Rectanglei(-32, -32, 64, 64);
	}

	protected override void OnStart()
	{
		base.OnStart();
		Priority = 1280;
	}
}
