using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Core;

public class LevelLayer : ILevelLayerTreeNode
{
	private readonly LevelMap _map;

	private readonly List<LayerRowDefinition> _layerRowDefinitions = new List<LayerRowDefinition>();

	private readonly List<LevelLayerShadow> _shadows = new List<LevelLayerShadow>();

	public Level Level => _map.Level;

	public LevelMap Map => _map;

	public int Index { get; set; }

	public string Name { get; set; }

	public bool Editing { get; set; }

	public bool Visible { get; set; }

	public IList<ILevelLayerTreeNode> Children => null;

	public IList<LayerRowDefinition> LayerRowDefinitions => _layerRowDefinitions;

	public int Columns { get; private set; }

	public int Rows { get; private set; }

	public int[,] Tiles { get; private set; }

	public int OffsetY { get; set; }

	public double ParallaxY { get; set; }

	public bool AutomaticYParallax { get; set; }

	public bool WrapX { get; set; }

	public bool WrapY { get; set; }

	public IList<LevelLayerShadow> Shadows => _shadows;

	public LevelLayerLighting Lighting { get; set; }

	public IEnumerable<Rectanglei> WaterfallEffects { get; set; }

	public Colour MiniMapColour { get; set; }

	public LevelLayer(LevelMap map)
	{
		_map = map;
		Visible = true;
		Columns = 0;
		Rows = 0;
		Tiles = new int[0, 0];
		Name = string.Empty;
		ParallaxY = 1.0;
		Lighting = new LevelLayerLighting();
	}

	public override string ToString()
	{
		if (!string.IsNullOrEmpty(Name))
		{
			return Name;
		}
		return base.ToString();
	}

	public void Resize(int columns, int rows)
	{
		int num = Math.Min(rows, Rows);
		int num2 = Math.Min(columns, Columns);
		int[,] array = new int[columns, rows];
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num2; j++)
			{
				array[j, i] = Tiles[j, i];
			}
		}
		Columns = columns;
		Rows = rows;
		Tiles = array;
	}

	public void Update()
	{
	}

	public void Animate()
	{
		foreach (LayerRowDefinition layerRowDefinition in _layerRowDefinitions)
		{
			layerRowDefinition.Animate();
		}
	}

	public void Draw(Renderer renderer, Viewport viewport, LayerViewOptions viewOptions, LevelLayerShadow shadow)
	{
		if (!Visible)
		{
			return;
		}
		if (Editing)
		{
			DrawNonLayerTiles(renderer, viewport, viewOptions);
		}
		using (viewport.ApplyRendererState(renderer))
		{
			if (viewOptions.ShowObjects)
			{
				Level.ObjectManager.Draw(renderer, viewport, this, viewOptions, priority: false);
			}
			if (viewOptions.ShowLandscape)
			{
				Matrix4 modelMatrix = Matrix4.Identity;
				if (viewOptions.Shadows)
				{
					modelMatrix = Matrix4.CreateTranslation((Vector2i)new Vector2((double)shadow.Displacement.X * viewport.Scale.X, (double)shadow.Displacement.Y * viewport.Scale.Y));
				}
				ITileRenderer tileRenderer = renderer.GetTileRenderer();
				tileRenderer.ClipRectangle = viewport.Destination;
				tileRenderer.ModelMatrix = modelMatrix;
				tileRenderer.Textures = _map.Level.TileSet.Textures.ToArray();
				tileRenderer.Filter = viewOptions.Filter;
				tileRenderer.FilterAmount = viewOptions.FilterAmount;
				tileRenderer.BeginRender();
				DrawTilesVertical(renderer, viewport, viewOptions);
				tileRenderer.EndRender();
			}
			if (viewOptions.ShowObjects)
			{
				Level.ObjectManager.Draw(renderer, viewport, this, viewOptions, priority: true);
			}
			renderer.DeativateRenderer();
		}
	}

	public void DrawNonLayerTiles(Renderer renderer, Viewport viewport, LayerViewOptions viewOptions)
	{
		int num = Math.Max(0, Columns * 64 - viewport.Bounds.X);
		int num2 = Math.Max(0, Rows * 64 - viewport.Bounds.Y);
		num = (int)((double)num * viewport.Scale.X);
		num2 = (int)((double)num2 * viewport.Scale.Y);
		List<Rectanglei> list = new List<Rectanglei>(2);
		if (num < viewport.Destination.Width)
		{
			Rectanglei item = new Rectanglei(num, 0, viewport.Destination.Width - num, viewport.Destination.Height);
			list.Add(item);
		}
		if (num2 < viewport.Destination.Height && num > 0)
		{
			Rectanglei item2 = new Rectanglei(0, num2, Math.Min(viewport.Destination.Width, num), viewport.Destination.Height - num2);
			list.Add(item2);
		}
		if (list.Count > 0)
		{
			renderer.GetNonLayerRenderer().Render(list);
		}
	}

	private void DrawTilesVertical(Renderer renderer, Viewport viewport, LayerViewOptions viewOptions)
	{
		if (Rows == 0 || Columns == 0)
		{
			return;
		}
		bool flag = !Editing && WrapY;
		int num = ((!Editing) ? OffsetY : 0);
		double num2 = (Editing ? 1.0 : ParallaxY);
		int num3 = (int)((double)(Rows * 64) * viewport.Scale.Y);
		int num4 = (int)((double)viewport.Bounds.Y * viewport.Scale.Y * num2);
		if (!flag && num4 >= num3)
		{
			return;
		}
		int num5 = num4 % num3;
		int num6 = viewport.Destination.Y + num;
		while (num6 < viewport.Destination.Bottom)
		{
			int val = num3 - num5;
			int val2 = viewport.Destination.Bottom - num6;
			int height = Math.Min(val, val2);
			DrawTilesHorizontal(renderer, viewport, viewOptions, num5, num6, ref height);
			if (height > 0)
			{
				num5 += height;
				num6 += height;
				if (flag)
				{
					num5 %= num3;
				}
				else if (num5 > num3)
				{
					break;
				}
				continue;
			}
			break;
		}
	}

	private void DrawTilesHorizontal(Renderer renderer, Viewport viewport, LayerViewOptions viewOptions, int sourceY, int destinationY, ref int height)
	{
		int num = (int)(64.0 * viewport.Scale.X);
		int num2 = Columns * num;
		int num3 = Rows * num;
		bool flag = !Editing && WrapX;
		double num4 = (Editing ? 1.0 : ParallaxY);
		int num5 = 0;
		LayerRowDefinition rowDefinitionAt = GetRowDefinitionAt((int)((double)sourceY / viewport.Scale.Y), out var top);
		if (rowDefinitionAt != null && !Editing)
		{
			num4 = rowDefinitionAt.Parallax;
			num5 = (int)rowDefinitionAt.CurrentOffset;
			if (rowDefinitionAt.Width != 0)
			{
				num2 = (int)((double)rowDefinitionAt.Width * viewport.Scale.X);
			}
			top = (int)((double)top * viewport.Scale.Y);
			height = Math.Min(height, top + (int)((double)rowDefinitionAt.Height * viewport.Scale.Y) - sourceY);
			height = Math.Min(height, 64 - sourceY % 64);
			if (height <= 0)
			{
				height = 1;
				return;
			}
		}
		if (height > num)
		{
			height = num - sourceY % num;
		}
		if (destinationY + height < viewport.Destination.Top)
		{
			return;
		}
		int i = (int)((double)viewport.Bounds.X * viewport.Scale.X * num4) - num5;
		if (flag)
		{
			for (; i < 0; i += num2)
			{
			}
		}
		if (!flag && i > num2)
		{
			return;
		}
		if (flag)
		{
			i %= num2;
		}
		int num6 = i;
		int num7 = viewport.Destination.X;
		while (num7 < viewport.Destination.Right)
		{
			int index = 0;
			if (num6 >= 0 && sourceY >= 0 && num6 < num2 && sourceY < num3)
			{
				index = Tiles[num6 / num, sourceY / num];
			}
			int num8 = -(num6 % num);
			int num9 = num - num6 % num;
			if (num9 != 0)
			{
				Rectanglei source = new Rectanglei(0, (int)((double)(sourceY % num) / viewport.Scale.Y), 64, (int)((double)height / viewport.Scale.Y));
				Rectanglei destination = new Rectanglei(num7 + num8, destinationY, num, height);
				Level.TileSet.DrawTile(renderer, index, source, destination);
				num6 += num9;
				num7 += num9;
				if (flag)
				{
					num6 %= num2;
				}
				else if (num6 > num2)
				{
					break;
				}
				continue;
			}
			break;
		}
	}

	public LayerRowDefinition GetRowDefinitionAt(int y, out int top)
	{
		int num = 0;
		foreach (LayerRowDefinition layerRowDefinition in _layerRowDefinitions)
		{
			if (y >= num && y < num + layerRowDefinition.Height)
			{
				top = num;
				return layerRowDefinition;
			}
			num += layerRowDefinition.Height;
		}
		top = 0;
		return null;
	}

	public void Merge(LevelLayer layer)
	{
		int num = Math.Max(Columns, layer.Columns);
		int num2 = Math.Max(Rows, layer.Rows);
		int[,] array = new int[num, num2];
		for (int i = 0; i < num2; i++)
		{
			for (int j = 0; j < num; j++)
			{
				if (Columns > j && Rows > i)
				{
					array[j, i] = Tiles[j, i];
				}
				if (layer.Columns > j && layer.Rows > i && layer.Tiles[j, i] != 0)
				{
					array[j, i] = layer.Tiles[j, i];
				}
			}
		}
		Columns = num;
		Rows = num2;
		Tiles = array;
	}
}
