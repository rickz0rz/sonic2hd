// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.FLICKY.FlickyInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using SonicOrca.Core.Objects.Base;
using SonicOrca.Geometry;

#nullable disable
namespace SONICORCA.OBJECTS.FLICKY;

/// <summary>The Flicky bird that a destroyed badnik releases. The base class <see cref="Animal"/> moves it.</summary>
public class FlickyInstance : Animal
{
	public FlickyInstance()
	  : base("/ANIGROUP")
	{
		JumpVelocity = new Vector2(-12.0, -16.0);
	}
}
