// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.WOCKY.WockyInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using SonicOrca.Core.Objects.Base;
using SonicOrca.Geometry;

#nullable disable
namespace SONICORCA.OBJECTS.WOCKY;

/// <summary>Wocky, an animal that jumps away after a destroyed badnik sets it free.</summary>
public class WockyInstance : Animal
{
	public WockyInstance()
		: base(WockyType.AnimationGroupResourceKey)
	{
		JumpVelocity = new Vector2(-10.0, -5.0);
	}
}
