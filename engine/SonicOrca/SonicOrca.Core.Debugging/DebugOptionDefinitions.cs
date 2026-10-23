using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Core.Collision;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Core.Debugging;

/// <summary>
/// The options of the debug menu, in the order that the menu shows them.
/// </summary>
internal static class DebugOptionDefinitions
{
	/// <summary>
	/// Shows the version, the operating system, the memory and the graphics resources.
	/// </summary>
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
			double totalMemoryMb = platform.TotalMemory;
			double workingSetMb = Environment.WorkingSet / 1048576.0;
			int memoryUsagePercent = (int)Math.Ceiling(workingSetMb / totalMemoryMb * 100.0);
			return new KeyValuePair<string, object>[10][]
			{
				new KeyValuePair<string, object>[2]
				{
					new KeyValuePair<string, object>("NAME", "SONICORCA"),
					new KeyValuePair<string, object>("VERSION", SonicOrcaInfo.Version)
				},
				new KeyValuePair<string, object>[2]
				{
					new KeyValuePair<string, object>("ARCHITECTURE", Environment.Is64BitProcess ? "x64" : "x86"),
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
					new KeyValuePair<string, object>("TOTAL MEMORY", $"{totalMemoryMb / 1024.0:0.0} GB")
				},
				new KeyValuePair<string, object>[2]
				{
					new KeyValuePair<string, object>("USER", Environment.UserName),
					new KeyValuePair<string, object>("MEMORY USAGE", $"{workingSetMb:0.0} MB ({memoryUsagePercent}%)")
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

	/// <summary>
	/// Shows the level name, the time, the bounds and the state variables of the area.
	/// </summary>
	public class LevelInfo : InformationDebugOption
	{
		public LevelInfo(DebugContext context)
			: base(context, "LEVEL", "")
		{
		}

		protected override IEnumerable<IEnumerable<KeyValuePair<string, object>>> GetInformation()
		{
			Level level = Context.Level;
			// Show the state variables in two columns: the first half on the left, the second half on the right.
			KeyValuePair<string, object>[] stateVariables = level.Area.StateVariables.ToArray();
			KeyValuePair<string, object>[] leftColumn = new KeyValuePair<string, object>[(stateVariables.Length + 1) / 2];
			KeyValuePair<string, object>[] rightColumn = new KeyValuePair<string, object>[stateVariables.Length - leftColumn.Length];
			if (leftColumn.Length != 0)
			{
				Array.Copy(stateVariables, 0, leftColumn, 0, leftColumn.Length);
			}
			if (rightColumn.Length != 0)
			{
				Array.Copy(stateVariables, leftColumn.Length, rightColumn, 0, rightColumn.Length);
			}
			List<IEnumerable<KeyValuePair<string, object>>> stateRows = new List<IEnumerable<KeyValuePair<string, object>>>();
			for (int i = 0; i < leftColumn.Length; i++)
			{
				stateRows.Add(new KeyValuePair<string, object>[2]
				{
					leftColumn[i],
					(rightColumn.Length > i) ? rightColumn[i] : new KeyValuePair<string, object>("", "")
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
					new KeyValuePair<string, object>("TIME", $"{level.Time}, {TimeSpan.FromSeconds(level.Time / 60.0):mm':'ss':'fff}")
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
			}.Concat(stateRows);
		}
	}

	/// <summary>
	/// Shows the score, the rings, the lives and the power-ups of the player.
	/// </summary>
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
					new KeyValuePair<string, object>("STARPOST TIME", $"{player.StarpostTime}, {TimeSpan.FromSeconds(player.StarpostTime / 60.0):mm':'ss':'fff}")
				},
				new KeyValuePair<string, object>[2]
				{
					new KeyValuePair<string, object>("INVINCIBILITY, TICKS LEFT", player.InvincibillityTicks),
					new KeyValuePair<string, object>("SPEED SHOES, TICKS LEFT", player.SpeedShoesTicks)
				}
			};
		}
	}

	/// <summary>
	/// Shows or hides the HUD.
	/// </summary>
	public class ShowHud : DiscreteDebugOption<bool>
	{
		public ShowHud(DebugContext context)
			: base(context, "LEVEL", "", "SHOW HUD", YesNo())
		{
			SelectedValue = true;
		}

		public override void OnChange()
		{
			Context.Level.ShowHUD = SelectedValue;
		}
	}

	/// <summary>
	/// Turns the classic debug mode on or off.
	/// </summary>
	public class ClassicDebugMode : DiscreteDebugOption<bool>
	{
		public ClassicDebugMode(DebugContext context)
			: base(context, "LEVEL", "", "CLASSIC DEBUG MODE", YesNo())
		{
			SelectedValue = false;
		}

		public override void OnChange()
		{
			Context.Level.ClassicDebugMode = SelectedValue;
		}
	}

	/// <summary>
	/// Shows the bounds, the velocity and the acceleration of the camera.
	/// </summary>
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

	/// <summary>
	/// Shows or hides the debug information of the camera.
	/// </summary>
	public class CameraShowInfo : DiscreteDebugOption<bool>
	{
		public CameraShowInfo(DebugContext context)
			: base(context, "CAMERA", "", "SHOW INFO", YesNo())
		{
		}

		public override void OnChange()
		{
			Context.Level.Camera.ShowDebugInformation = SelectedValue;
		}
	}

	/// <summary>
	/// Sets the camera to track the player or to move freely.
	/// </summary>
	public class CameraMode : DiscreteDebugOption<int>
	{
		public CameraMode(DebugContext context)
			: base(context, "CAMERA", "", "MODE", new KeyValuePair<string, int>[2]
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

	/// <summary>
	/// Sets the zoom of the camera.
	/// </summary>
	public class CameraZoom : DiscreteDebugOption<double>
	{
		public CameraZoom(DebugContext context)
			: base(context, "CAMERA", "", "ZOOM", new KeyValuePair<string, double>[5]
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

	/// <summary>
	/// Shows the number of tiles and tile set textures.
	/// </summary>
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
					new KeyValuePair<string, object>("ANIMATED TILES", tileSet.Values.Count(x => x.Animated))
				},
				new KeyValuePair<string, object>[2]
				{
					new KeyValuePair<string, object>("TILESET TEXTURES", tileSet.Textures.Count),
					new KeyValuePair<string, object>("", "")
				}
			};
		}
	}

	/// <summary>
	/// Shows or hides the landscape.
	/// </summary>
	public class LandscapeVisible : DiscreteDebugOption<bool>
	{
		public LandscapeVisible(DebugContext context)
			: base(context, "LANDSCAPE", "", "VISIBLE", YesNo())
		{
		}

		public override void OnChange()
		{
			Context.Level.LayerViewOptions.ShowLandscape = SelectedValue;
		}
	}

	/// <summary>
	/// Turns the landscape animation on or off.
	/// </summary>
	public class LandscapeAnimate : DiscreteDebugOption<bool>
	{
		public LandscapeAnimate(DebugContext context)
			: base(context, "LANDSCAPE", "", "ANIMATE", YesNo())
		{
		}

		public override void OnChange()
		{
			Context.Level.LandscapeAnimating = SelectedValue;
		}
	}

	/// <summary>
	/// Shows the number of collision vectors and the depth of the quad tree.
	/// </summary>
	public class CollisionStats : InformationDebugOption
	{
		public CollisionStats(DebugContext context)
			: base(context, "COLLISION", "STATISTICS")
		{
		}

		protected override IEnumerable<IEnumerable<KeyValuePair<string, object>>> GetInformation()
		{
			CollisionTable collisionTable = Context.Level.CollisionTable;
			QuadTree<CollisionVector> internalTree = collisionTable.InternalTree;
			return new KeyValuePair<string, object>[1][] { new KeyValuePair<string, object>[2]
			{
				new KeyValuePair<string, object>("TOTAL VECTORS", collisionTable.Count),
				new KeyValuePair<string, object>("TOTAL QUADTREE NODES", internalTree.GetDepth())
			} };
		}
	}

	/// <summary>
	/// Shows or hides the landscape collision vectors.
	/// </summary>
	public class CollisionShowLandscape : DiscreteDebugOption<bool>
	{
		public CollisionShowLandscape(DebugContext context)
			: base(context, "COLLISION", "", "SHOW LANDSCAPE", YesNo())
		{
			SelectedValue = false;
		}

		public override void OnChange()
		{
			Context.Level.LayerViewOptions.ShowLandscapeCollision = SelectedValue;
		}
	}

	/// <summary>
	/// Shows or hides the object collision vectors.
	/// </summary>
	public class CollisionShowObjects : DiscreteDebugOption<bool>
	{
		public CollisionShowObjects(DebugContext context)
			: base(context, "COLLISION", "", "SHOW OBJECTS", YesNo())
		{
			SelectedValue = false;
		}

		public override void OnChange()
		{
			Context.Level.LayerViewOptions.ShowObjectCollision = SelectedValue;
		}
	}

	/// <summary>
	/// Shows or hides the objects.
	/// </summary>
	public class ObjectsVisible : DiscreteDebugOption<bool>
	{
		public ObjectsVisible(DebugContext context)
			: base(context, "OBJECTS", "", "VISIBLE", YesNo())
		{
		}

		public override void OnChange()
		{
			Context.Level.LayerViewOptions.ShowObjects = SelectedValue;
		}
	}

	/// <summary>
	/// Turns the object animation on or off.
	/// </summary>
	public class ObjectsAnimate : DiscreteDebugOption<bool>
	{
		public ObjectsAnimate(DebugContext context)
			: base(context, "OBJECTS", "", "ANIMATE", YesNo())
		{
		}

		public override void OnChange()
		{
			Context.Level.ObjectsAnimating = SelectedValue;
		}
	}

	/// <summary>
	/// Shows or hides the debug information of the characters.
	/// </summary>
	public class ObjectsShowCharacterInfo : DiscreteDebugOption<bool>
	{
		public ObjectsShowCharacterInfo(DebugContext context)
			: base(context, "OBJECTS", "", "SHOW CHARACTER INFO", YesNo())
		{
			SelectedValue = false;
		}

		public override void OnChange()
		{
			Context.Level.ShowCharacterInfo = SelectedValue;
		}
	}

	/// <summary>
	/// Shows or hides the debug information of the sidekick AI.
	/// </summary>
	public class ObjectsShowSidekickIntel : DiscreteDebugOption<bool>
	{
		public ObjectsShowSidekickIntel(DebugContext context)
			: base(context, "OBJECTS", "", "SHOW SIDEKICK INTELLIGENCE", YesNo())
		{
			SelectedValue = false;
		}

		public override void OnChange()
		{
			Context.Level.ShowSidekickIntelligence = SelectedValue;
		}
	}

	/// <summary>
	/// Shows the number of objects, active objects and sub-objects.
	/// </summary>
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
					new KeyValuePair<string, object>("SUB OBJECTS", level.ObjectManager.ActiveObjects.Count(x => x.IsSubObject)),
					new KeyValuePair<string, object>("RESPAWN PREVENTED OBJECTS", "")
				}
			};
		}
	}

	/// <summary>
	/// Shows the water areas, the colour adjustments and the ripple values.
	/// </summary>
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

	/// <summary>
	/// Shows or hides the water.
	/// </summary>
	public class WaterVisible : DiscreteDebugOption<bool>
	{
		public WaterVisible(DebugContext context)
			: base(context, "WATER", "", "VISIBLE", YesNo())
		{
		}

		public override void OnChange()
		{
			Context.Level.LayerViewOptions.ShowWater = SelectedValue;
		}
	}

	/// <summary>
	/// Makes the "YES" and "NO" values for a debug option of type bool.
	/// </summary>
	private static KeyValuePair<string, bool>[] YesNo()
	{
		return new KeyValuePair<string, bool>[2]
		{
			new KeyValuePair<string, bool>("YES", value: true),
			new KeyValuePair<string, bool>("NO", value: false)
		};
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
