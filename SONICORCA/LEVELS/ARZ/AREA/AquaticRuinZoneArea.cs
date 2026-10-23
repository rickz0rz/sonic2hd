// Decompiled with JetBrains decompiler
// Type: SONICORCA.LEVELS.ARZ.AREA.AquaticRuinZoneArea
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System.Collections.Generic;
using System.Linq;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Geometry;
using SonicOrca.Resources;

#nullable disable
namespace SONICORCA.LEVELS.ARZ.AREA;

public class AquaticRuinZoneArea : Area
{
	private const string TileSetResourceKey = "//TILESET";
	private const string MapResourceKey = "//MAP";
	private const string BindingResourceKey = "//BINDING";
	private const string LevelMusicResourceKey = "SONICORCA/MUSIC/LEVELS/ARZ";
	public static IReadOnlyList<string> AreaDependencies = (new string[4]
	{
	"//TILESET",
	"//MAP",
	"//BINDING",
	"SONICORCA/MUSIC/LEVELS/ARZ"
	}).ToArray<string>();
	private SonicOrcaGameContext _gameContext;
	private ResourceTree _resourceTree;
	private Level _level;

	public AquaticRuinZoneArea()
	  : base(AreaDependencies)
	{
	}

	public override void Prepare(Level level, LevelPrepareSettings settings)
	{
		_gameContext = level.GameContext;
		_resourceTree = _gameContext.ResourceTree;
		_level = level;
		_level.Name = "Aquatic Ruin";
		_level.ShowAsZone = true;
		_level.ShowAsAct = true;
		_level.Scheme = LevelScheme.S2;
		_level.LevelMusic = "SONICORCA/MUSIC/LEVELS/ARZ";
		if (!settings.Seamless)
		{
			_level.TileSet = _resourceTree.GetLoadedResource<TileSet>(this, "//TILESET");
			_level.LoadMap(_resourceTree.GetLoadedResource<LevelMap>(this, "//MAP"));
			_level.LoadBinding(_resourceTree.GetLoadedResource<LevelBinding>(this, "//BINDING"));
		}
		if (settings.Act == 1)
		{
			PrepareAct1();
		}
		else
		{
			if (settings.Act != 2)
				return;
			if (settings.Seamless)
				SeamlessPrepareAct2();
			else
				PrepareAct2();
		}
	}

	private void PrepareAct1()
	{
		_level.CurrentAct = 1;
		_level.SetStartPosition("startpos_stk_1");
		_level.Bounds = _level.Map.Bounds;
		_level.WaterManager.WaterAreas.Add(new Rectanglei(0, 4096 /*0x1000*/, 65536 /*0x010000*/, 4096 /*0x1000*/));
	}

	private void PrepareAct2()
	{
		_level.CurrentAct = 2;
		_level.SetStartPosition("startpos_stk_2");
		_level.Bounds = _level.Map.Bounds;
	}

	private void SeamlessPrepareAct2()
	{
		_level.CurrentAct = 2;
		ExtendSeamlessLevelBounds(_level, _level.Map.Bounds);
	}
}
