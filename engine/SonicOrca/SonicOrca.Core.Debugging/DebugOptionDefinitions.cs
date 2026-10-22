using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Core.Collision;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Core.Debugging;

internal static class DebugOptionDefinitions
{
	public class PlatformStats : InformationDebugOption
	{
		public PlatformStats(DebugContext context)
			: base(context, "PLATFORM", "")
		{
		}

		protected override IEnumerable<IEnumerable<KeyValuePair<string, object>>> GetInformation()
		{
			IPlatform platform = Context.Level.GameContext.Platform;
			IGraphicsContext graphicsContext = platform.Window.GraphicsContext;
			double num = platform.TotalMemory;
			double num2 = (double)Environment.WorkingSet / 1048576.0;
			int num3 = (int)Math.Ceiling(num2 / num * 100.0);
			return new KeyValuePair<string, object>[10][]
			{
				new KeyValuePair<string, object>[2]
				{
					new KeyValuePair<string, object>("NAME", "SONICORCA"),
					new KeyValuePair<string, object>("VERSION", SonicOrcaInfo.Version)
				},
				new KeyValuePair<string, object>[2]
				{
					new KeyValuePair<string, object>("ARCHITECTURE", Environment.Is64BitOperatingSystem ? "x64" : "x86"),
					new KeyValuePair<string, object>("OS ARCHITECTURE", Environment.Is64BitOperatingSystem ? "x64" : "x86")
				},
				new KeyValuePair<string, object>[2]
				{
					new KeyValuePair<string, object>("OS", Environment.OSVersion),
					new KeyValuePair<string, object>("MACHINE NAME", Environment.MachineName)
				},
				new KeyValuePair<string, object>[2]
				{
					new KeyValuePair<string, object>("LOGICAL PROCESSORS", Environment.ProcessorCount),
					new KeyValuePair<string, object>("TOTAL MEMORY", $"{num / 1024.0:0.0} GB")
				},
				new KeyValuePair<string, object>[2]
				{
					new KeyValuePair<string, object>("USER", Environment.UserName),
					new KeyValuePair<string, object>("MEMORY USAGE", $"{num2:0.0} MB ({num3}%)")
				},
				new KeyValuePair<string, object>[0],
				new KeyValuePair<string, object>[2]
				{
					new KeyValuePair<string, object>("WINDOW SIZE", $"{platform.Window.ClientSize.X} X {platform.Window.ClientSize.Y}"),
					new KeyValuePair<string, object>("FULLSCREEN", platform.Window.FullScreen)
				},
				new KeyValuePair<string, object>[2]
				{
					new KeyValuePair<string, object>("GRAPHICS API", platform.GraphicsAPI),
					new KeyValuePair<string, object>("VENDOR", platform.GraphicsVendor)
				},
				new KeyValuePair<string, object>[2]
				{
					new KeyValuePair<string, object>("TEXTURES", graphicsContext.Textures.Count),
					new KeyValuePair<string, object>("SHADERS", graphicsContext.ShaderPrograms.Count)
				},
				new KeyValuePair<string, object>[2]
				{
					new KeyValuePair<string, object>("RENDER TARGETS", graphicsContext.RenderTargets.Count),
					new KeyValuePair<string, object>("VERTEX BUFFERS", graphicsContext.VertexBuffers.Count)
				}
			};
		}
	}

	public class LevelInfo : InformationDebugOption
	{
		public LevelInfo(DebugContext context)
			: base(context, "LEVEL", "")
		{
		}

		protected override IEnumerable<IEnumerable<KeyValuePair<string, object>>> GetInformation()
		{
			Level level = Context.Level;
			KeyValuePair<string, object>[] array = level.Area.StateVariables.ToArray();
			KeyValuePair<string, object>[] array2 = new KeyValuePair<string, object>[(array.Length + 1) / 2];
			KeyValuePair<string, object>[] array3 = new KeyValuePair<string, object>[array.Length - array2.Length];
			if (array2.Length != 0)
			{
				Array.Copy(array, 0, array2, 0, array2.Length);
			}
			if (array3.Length != 0)
			{
				Array.Copy(array, array2.Length, array3, 0, array3.Length);
			}
			List<IEnumerable<KeyValuePair<string, object>>> list = new List<IEnumerable<KeyValuePair<string, object>>>();
			for (int i = 0; i < array2.Length; i++)
			{
				list.Add(new KeyValuePair<string, object>[2]
				{
					array2[i],
					(array3.Length > i) ? array3[i] : new KeyValuePair<string, object>("", "")
				});
			}
			return new KeyValuePair<string, object>[5][]
			{
				new KeyValuePair<string, object>[2]
				{
					new KeyValuePair<string, object>("NAME", level.Name + (level.ShowAsAct ? " Zone" : string.Empty)),
					new KeyValuePair<string, object>("ACT", level.CurrentAct)
				},
				new KeyValuePair<string, object>[2]
				{
					new KeyValuePair<string, object>("TICKS", level.Ticks),
					new KeyValuePair<string, object>("TIME", $"{level.Time}, {TimeSpan.FromSeconds((double)level.Time / 60.0):mm':'ss':'fff}")
				},
				new KeyValuePair<string, object>[2]
				{
					new KeyValuePair<string, object>("BOUNDS", $"{level.Bounds.Left}, {level.Bounds.Top} - {level.Bounds.Right}, {level.Bounds.Bottom}"),
					new KeyValuePair<string, object>("BOUNDS SIZE", $"{level.Bounds.Width} X {level.Bounds.Height}")
				},
				new KeyValuePair<string, object>[2]
				{
					new KeyValuePair<string, object>("LAYERS", level.Map.Layers.Count),
					new KeyValuePair<string, object>("", "")
				},
				new KeyValuePair<string, object>[0]
			}.Concat(list);
		}
	}

	public class PlayerInfo : InformationDebugOption
	{
		public PlayerInfo(DebugContext context)
			: base(context, "LEVEL", "PLAYER")
		{
		}

		protected override IEnumerable<IEnumerable<KeyValuePair<string, object>>> GetInformation()
		{
			Level level = Context.Level;
			Player player = Context.Level.Player;
			return new KeyValuePair<string, object>[7][]
			{
				new KeyValuePair<string, object>[2]
				{
					new KeyValuePair<string, object>("PROTAGONIST", "SONIC"),
					new KeyValuePair<string, object>("SIDEKICK", "TAILS")
				},
				new KeyValuePair<string, object>[2]
				{
					new KeyValuePair<string, object>("SCORE", player.Score),
					new KeyValuePair<string, object>("LIFE TARGET SCORE", player.TargetScoreForNextLife)
				},
				new KeyValuePair<string, object>[2]
				{
					new KeyValuePair<string, object>("CURRENT RINGS", player.CurrentRings),
					new KeyValuePair<string, object>("LIFE TARGET RINGS", player.TargetRingCountForNextLife)
				},
				new KeyValuePair<string, object>[2]
				{
					new KeyValuePair<string, object>("TOTAL COLLECTED RINGS", player.TotalRings),
					new KeyValuePair<string, object>("PERFECT BONUS", player.TotalRings + level.ObjectManager.ObjectEntryTable.GetRingCount())
				},
				new KeyValuePair<string, object>[2]
				{
					new KeyValuePair<string, object>("LIVES", player.Lives),
					new KeyValuePair<string, object>("", "")
				},
				new KeyValuePair<string, object>[2]
				{
					new KeyValuePair<string, object>("STARPOST ID", player.StarpostIndex),
					new KeyValuePair<string, object>("STARPOST TIME", $"{player.StarpostTime}, {TimeSpan.FromSeconds((double)player.StarpostTime / 60.0):mm':'ss':'fff}")
				},
				new KeyValuePair<string, object>[2]
				{
					new KeyValuePair<string, object>("INVINCIBILITY, TICKS LEFT", player.InvincibillityTicks),
					new KeyValuePair<string, object>("SPEED SHOES, TICKS LEFT", player.SpeedShoesTicks)
				}
			};
		}
	}

	public class ShowHud : DiscreteDebugOption<bool>
	{
		public ShowHud(DebugContext context)
			: base(context, "LEVEL", "", "SHOW HUD", (IEnumerable<KeyValuePair<string, bool>>)new KeyValuePair<string, bool>[2]
			{
				new KeyValuePair<string, bool>("YES", value: true),
				new KeyValuePair<string, bool>("NO", value: false)
			})
		{
			SelectedValue = true;
		}

		public override void OnChange()
		{
			Context.Level.ShowHUD = SelectedValue;
		}
	}

	public class ClassicDebugMode : DiscreteDebugOption<bool>
	{
		public ClassicDebugMode(DebugContext context)
			: base(context, "LEVEL", "", "CLASSIC DEBUG MODE", (IEnumerable<KeyValuePair<string, bool>>)new KeyValuePair<string, bool>[2]
			{
				new KeyValuePair<string, bool>("YES", value: true),
				new KeyValuePair<string, bool>("NO", value: false)
			})
		{
			SelectedValue = false;
		}

		public override void OnChange()
		{
			Context.Level.ClassicDebugMode = SelectedValue;
		}
	}

	public class CameraInfo : InformationDebugOption
	{
		public CameraInfo(DebugContext context)
			: base(context, "CAMERA", "")
		{
		}

		protected override IEnumerable<IEnumerable<KeyValuePair<string, object>>> GetInformation()
		{
			Camera camera = Context.Level.Camera;
			return new KeyValuePair<string, object>[2][]
			{
				new KeyValuePair<string, object>[2]
				{
					new KeyValuePair<string, object>("BOUNDS", $"{camera.Bounds.Left}, {camera.Bounds.Top} - {camera.Bounds.Right}, {camera.Bounds.Bottom}"),
					new KeyValuePair<string, object>("SIZE", $"{camera.Bounds.Width} X {camera.Bounds.Height}")
				},
				new KeyValuePair<string, object>[2]
				{
					new KeyValuePair<string, object>("VELOCITY", camera.Velocity),
					new KeyValuePair<string, object>("ACCELERATION", camera.Acceleration)
				}
			};
		}
	}

	public class CameraShowInfo : DiscreteDebugOption<bool>
	{
		public CameraShowInfo(DebugContext context)
			: base(context, "CAMERA", "", "SHOW INFO", (IEnumerable<KeyValuePair<string, bool>>)new KeyValuePair<string, bool>[2]
			{
				new KeyValuePair<string, bool>("YES", value: true),
				new KeyValuePair<string, bool>("NO", value: false)
			})
		{
		}

		public override void OnChange()
		{
			Context.Level.Camera.ShowDebugInformation = SelectedValue;
		}
	}

	public class CameraMode : DiscreteDebugOption<int>
	{
		public CameraMode(DebugContext context)
			: base(context, "CAMERA", "", "MODE", (IEnumerable<KeyValuePair<string, int>>)new KeyValuePair<string, int>[2]
			{
				new KeyValuePair<string, int>("TRACK", 0),
				new KeyValuePair<string, int>("FREECAM", 1)
			})
		{
		}

		public override void OnChange()
		{
			Context.Level.Camera.SpyMode = SelectedValue == 1;
		}
	}

	public class CameraZoom : DiscreteDebugOption<double>
	{
		public CameraZoom(DebugContext context)
			: base(context, "CAMERA", "", "ZOOM", (IEnumerable<KeyValuePair<string, double>>)new KeyValuePair<string, double>[5]
			{
				new KeyValuePair<string, double>("0.25", 0.25),
				new KeyValuePair<string, double>("0.50", 0.5),
				new KeyValuePair<string, double>("1.00", 1.0),
				new KeyValuePair<string, double>("2.00", 2.0),
				new KeyValuePair<string, double>("4.00", 4.0)
			})
		{
			SelectedValue = 1.0;
		}

		public override void OnChange()
		{
			Context.Level.Camera.SetScale(SelectedValue);
		}
	}

	public class LandscapeInfo : InformationDebugOption
	{
		public LandscapeInfo(DebugContext context)
			: base(context, "LANDSCAPE", "")
		{
		}

		protected override IEnumerable<IEnumerable<KeyValuePair<string, object>>> GetInformation()
		{
			TileSet tileSet = Context.Level.TileSet;
			return new KeyValuePair<string, object>[2][]
			{
				new KeyValuePair<string, object>[2]
				{
					new KeyValuePair<string, object>("TOTAL TILES", tileSet.Count),
					new KeyValuePair<string, object>("ANIMATED TILES", tileSet.Values.Count((ITile x) => x.Animated))
				},
				new KeyValuePair<string, object>[2]
				{
					new KeyValuePair<string, object>("TILESET TEXTURES", tileSet.Textures.Count),
					new KeyValuePair<string, object>("", "")
				}
			};
		}
	}

	public class LandscapeVisible : DiscreteDebugOption<bool>
	{
		public LandscapeVisible(DebugContext context)
			: base(context, "LANDSCAPE", "", "VISIBLE", (IEnumerable<KeyValuePair<string, bool>>)new KeyValuePair<string, bool>[2]
			{
				new KeyValuePair<string, bool>("YES", value: true),
				new KeyValuePair<string, bool>("NO", value: false)
			})
		{
		}

		public override void OnChange()
		{
			Context.Level.LayerViewOptions.ShowLandscape = SelectedValue;
		}
	}

	public class LandscapeAnimate : DiscreteDebugOption<bool>
	{
		public LandscapeAnimate(DebugContext context)
			: base(context, "LANDSCAPE", "", "ANIMATE", (IEnumerable<KeyValuePair<string, bool>>)new KeyValuePair<string, bool>[2]
			{
				new KeyValuePair<string, bool>("YES", value: true),
				new KeyValuePair<string, bool>("NO", value: false)
			})
		{
		}

		public override void OnChange()
		{
			Context.Level.LandscapeAnimating = SelectedValue;
		}
	}

	public class CollisionStats : InformationDebugOption
	{
		public CollisionStats(DebugContext context)
			: base(context, "COLLISION", "STATISTICS")
		{
		}

		protected override IEnumerable<IEnumerable<KeyValuePair<string, object>>> GetInformation()
		{
			_ = Context.Level;
			CollisionTable collisionTable = Context.Level.CollisionTable;
			QuadTree<CollisionVector> internalTree = collisionTable.InternalTree;
			return new KeyValuePair<string, object>[1][] { new KeyValuePair<string, object>[2]
			{
				new KeyValuePair<string, object>("TOTAL VECTORS", collisionTable.Count),
				new KeyValuePair<string, object>("TOTAL QUADTREE NODES", internalTree.GetDepth())
			} };
		}
	}

	public class CollisionShowLandscape : DiscreteDebugOption<bool>
	{
		public CollisionShowLandscape(DebugContext context)
			: base(context, "COLLISION", "", "SHOW LANDSCAPE", (IEnumerable<KeyValuePair<string, bool>>)new KeyValuePair<string, bool>[2]
			{
				new KeyValuePair<string, bool>("YES", value: true),
				new KeyValuePair<string, bool>("NO", value: false)
			})
		{
			SelectedValue = false;
		}

		public override void OnChange()
		{
			Context.Level.LayerViewOptions.ShowLandscapeCollision = SelectedValue;
		}
	}

	public class CollisionShowObjects : DiscreteDebugOption<bool>
	{
		public CollisionShowObjects(DebugContext context)
			: base(context, "COLLISION", "", "SHOW OBJECTS", (IEnumerable<KeyValuePair<string, bool>>)new KeyValuePair<string, bool>[2]
			{
				new KeyValuePair<string, bool>("YES", value: true),
				new KeyValuePair<string, bool>("NO", value: false)
			})
		{
			SelectedValue = false;
		}

		public override void OnChange()
		{
			Context.Level.LayerViewOptions.ShowObjectCollision = SelectedValue;
		}
	}

	public class ObjectsVisible : DiscreteDebugOption<bool>
	{
		public ObjectsVisible(DebugContext context)
			: base(context, "OBJECTS", "", "VISIBLE", (IEnumerable<KeyValuePair<string, bool>>)new KeyValuePair<string, bool>[2]
			{
				new KeyValuePair<string, bool>("YES", value: true),
				new KeyValuePair<string, bool>("NO", value: false)
			})
		{
		}

		public override void OnChange()
		{
			Context.Level.LayerViewOptions.ShowObjects = SelectedValue;
		}
	}

	public class ObjectsAnimate : DiscreteDebugOption<bool>
	{
		public ObjectsAnimate(DebugContext context)
			: base(context, "OBJECTS", "", "ANIMATE", (IEnumerable<KeyValuePair<string, bool>>)new KeyValuePair<string, bool>[2]
			{
				new KeyValuePair<string, bool>("YES", value: true),
				new KeyValuePair<string, bool>("NO", value: false)
			})
		{
		}

		public override void OnChange()
		{
			Context.Level.ObjectsAnimating = SelectedValue;
		}
	}

	public class ObjectsShowCharacterInfo : DiscreteDebugOption<bool>
	{
		public ObjectsShowCharacterInfo(DebugContext context)
			: base(context, "OBJECTS", "", "SHOW CHARACTER INFO", (IEnumerable<KeyValuePair<string, bool>>)new KeyValuePair<string, bool>[2]
			{
				new KeyValuePair<string, bool>("YES", value: true),
				new KeyValuePair<string, bool>("NO", value: false)
			})
		{
			SelectedValue = false;
		}

		public override void OnChange()
		{
			Context.Level.ShowCharacterInfo = SelectedValue;
		}
	}

	public class ObjectsShowSidekickIntel : DiscreteDebugOption<bool>
	{
		public ObjectsShowSidekickIntel(DebugContext context)
			: base(context, "OBJECTS", "", "SHOW SIDEKICK INTELLIGENCE", (IEnumerable<KeyValuePair<string, bool>>)new KeyValuePair<string, bool>[2]
			{
				new KeyValuePair<string, bool>("YES", value: true),
				new KeyValuePair<string, bool>("NO", value: false)
			})
		{
			SelectedValue = false;
		}

		public override void OnChange()
		{
			Context.Level.ShowSidekickIntelligence = SelectedValue;
		}
	}

	public class ObjectsStats : InformationDebugOption
	{
		public ObjectsStats(DebugContext context)
			: base(context, "OBJECTS", "STATISTICS")
		{
		}

		protected override IEnumerable<IEnumerable<KeyValuePair<string, object>>> GetInformation()
		{
			Level level = Context.Level;
			return new KeyValuePair<string, object>[2][]
			{
				new KeyValuePair<string, object>[2]
				{
					new KeyValuePair<string, object>("TOTAL OBJECTS", level.ObjectManager.ObjectEntryTable.Count),
					new KeyValuePair<string, object>("ACTIVE OBJECTS", level.ObjectManager.ActiveObjects.Count)
				},
				new KeyValuePair<string, object>[2]
				{
					new KeyValuePair<string, object>("SUB OBJECTS", level.ObjectManager.ActiveObjects.Count((ActiveObject x) => x.IsSubObject)),
					new KeyValuePair<string, object>("RESPAWN PREVENTED OBJECTS", "")
				}
			};
		}
	}

	public class WaterInfo : InformationDebugOption
	{
		public WaterInfo(DebugContext context)
			: base(context, "WATER", "")
		{
		}

		protected override IEnumerable<IEnumerable<KeyValuePair<string, object>>> GetInformation()
		{
			WaterManager waterManager = Context.Level.WaterManager;
			return new KeyValuePair<string, object>[4][]
			{
				new KeyValuePair<string, object>[2]
				{
					new KeyValuePair<string, object>("TOTAL AREAS", waterManager.WaterAreas.Count),
					new KeyValuePair<string, object>("", "")
				},
				new KeyValuePair<string, object>[2]
				{
					new KeyValuePair<string, object>("HUE", $"{waterManager.HueTarget * 360.0:0} DEG"),
					new KeyValuePair<string, object>("HUE CONCENTRATION", waterManager.HueAmount)
				},
				new KeyValuePair<string, object>[2]
				{
					new KeyValuePair<string, object>("SATURATION ADJUSTMENT", waterManager.SaturationChange),
					new KeyValuePair<string, object>("LUMINOSITY ADJUSTMENT", waterManager.LuminosityChange)
				},
				new KeyValuePair<string, object>[2]
				{
					new KeyValuePair<string, object>("RIPPLE SIZE", waterManager.WaveSize),
					new KeyValuePair<string, object>("RIPPLE PHASE", $"{MathX.ToDegrees(waterManager.WavePhase):0} DEG")
				}
			};
		}
	}

	public class WaterVisible : DiscreteDebugOption<bool>
	{
		public WaterVisible(DebugContext context)
			: base(context, "WATER", "", "VISIBLE", (IEnumerable<KeyValuePair<string, bool>>)new KeyValuePair<string, bool>[2]
			{
				new KeyValuePair<string, bool>("YES", value: true),
				new KeyValuePair<string, bool>("NO", value: false)
			})
		{
		}

		public override void OnChange()
		{
			Context.Level.LayerViewOptions.ShowWater = SelectedValue;
		}
	}

	public static IEnumerable<DebugOption> CreateOptionsInOrder(DebugContext context)
	{
		return new DebugOption[22]
		{
			new PlatformStats(context),
			new LevelInfo(context),
			new PlayerInfo(context),
			new ShowHud(context),
			new ClassicDebugMode(context),
			new CameraInfo(context),
			new CameraShowInfo(context),
			new CameraMode(context),
			new CameraZoom(context),
			new LandscapeInfo(context),
			new LandscapeVisible(context),
			new LandscapeAnimate(context),
			new CollisionShowLandscape(context),
			new CollisionShowObjects(context),
			new CollisionStats(context),
			new ObjectsVisible(context),
			new ObjectsAnimate(context),
			new ObjectsShowCharacterInfo(context),
			new ObjectsShowSidekickIntel(context),
			new ObjectsStats(context),
			new WaterInfo(context),
			new WaterVisible(context)
		};
	}
}
