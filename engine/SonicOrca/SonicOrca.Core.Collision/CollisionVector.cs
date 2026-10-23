using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Core.Collision;

public class CollisionVector : IBounds
{
	public const uint AllPaths = uint.MaxValue;

	private CollisionVector[] _connectionA = new CollisionVector[2];

	private CollisionVector[] _connectionB = new CollisionVector[2];

	private int _left;

	private int _top;

	private int _right;

	private int _bottom;

	public ActiveObject Owner { get; set; }

	public Vector2i RelativeA { get; set; }

	public Vector2i RelativeB { get; set; }

	public uint Paths { get; set; }

	public CollisionFlags Flags { get; set; }

	public int Id { get; set; }

	public double Angle { get; private set; }

	public int FlipX { get; private set; }

	public int FlipY { get; private set; }

	public CollisionMode Mode { get; private set; }

	public int Left
	{
		get
		{
			if (Owner != null)
			{
				return _left + Owner.Position.X;
			}
			return _left;
		}
	}

	public int Top
	{
		get
		{
			if (Owner != null)
			{
				return _top + Owner.Position.Y;
			}
			return _top;
		}
	}

	public int Right
	{
		get
		{
			if (Owner != null)
			{
				return _right + Owner.Position.X;
			}
			return _right;
		}
	}

	public int Bottom
	{
		get
		{
			if (Owner != null)
			{
				return _bottom + Owner.Position.Y;
			}
			return _bottom;
		}
	}

	public int Width { get; private set; }

	public int Height { get; private set; }

	public double Ratio { get; private set; }

	public Vector2i AbsoluteA
	{
		get
		{
			if (Owner != null)
			{
				return RelativeA + Owner.Position;
			}
			return RelativeA;
		}
	}

	public Vector2i AbsoluteB
	{
		get
		{
			if (Owner != null)
			{
				return RelativeB + Owner.Position;
			}
			return RelativeB;
		}
	}

	public Vector2i AB => RelativeB - RelativeA;

	public double Length => AB.Length;

	public double CollisionAngle => Angle + Math.PI;

	public Rectanglei Bounds => Rectanglei.FromLTRB(Math.Min(AbsoluteA.X, AbsoluteB.X), Math.Min(AbsoluteA.Y, AbsoluteB.Y), Math.Max(AbsoluteA.X, AbsoluteB.X), Math.Max(AbsoluteA.Y, AbsoluteB.Y));

	public bool IsWall
	{
		get
		{
			if (Mode != CollisionMode.Left)
			{
				return Mode == CollisionMode.Right;
			}
			return true;
		}
	}

	public bool IsFloor => !IsWall;

	public bool IsThereConnectionA => _connectionA.Any((CollisionVector x) => x != null);

	public bool IsThereConnectionB => _connectionB.Any((CollisionVector x) => x != null);

	public CollisionVector()
	{
	}

	public CollisionVector(Vector2i a, Vector2i b, uint paths = uint.MaxValue, CollisionFlags flags = 0)
		: this(null, a, b, paths, flags)
	{
	}

	public CollisionVector(ActiveObject owner, Vector2i a, Vector2i b, uint paths = uint.MaxValue, CollisionFlags flags = 0)
	{
		Owner = owner;
		RelativeA = a;
		RelativeB = b;
		Paths = paths;
		Flags = flags;
	}

	public static CollisionVector[] FromRectangle(Rectanglei rect)
	{
		return FromRectangle(null, rect);
	}

	public static CollisionVector[] FromRectangle(ActiveObject owner, Rectanglei rect, uint paths = uint.MaxValue, CollisionFlags flags = 0)
	{
		return new CollisionVector[4]
		{
			new CollisionVector(owner, new Vector2i(rect.X, rect.Y + rect.Height), new Vector2i(rect.X, rect.Y), paths),
			new CollisionVector(owner, new Vector2i(rect.X, rect.Y), new Vector2i(rect.X + rect.Width, rect.Y), paths),
			new CollisionVector(owner, new Vector2i(rect.X + rect.Width, rect.Y), new Vector2i(rect.X + rect.Width, rect.Y + rect.Height), paths),
			new CollisionVector(owner, new Vector2i(rect.X + rect.Width, rect.Y + rect.Height), new Vector2i(rect.X, rect.Y + rect.Height), paths)
		};
	}

	public static void UpdateRectangle(CollisionVector[] vectors, Rectanglei rect)
	{
		vectors[0].RelativeA = new Vector2i(rect.X, rect.Y + rect.Height);
		vectors[0].RelativeB = new Vector2i(rect.X, rect.Y);
		vectors[1].RelativeA = new Vector2i(rect.X, rect.Y);
		vectors[1].RelativeB = new Vector2i(rect.X + rect.Width, rect.Y);
		vectors[2].RelativeA = new Vector2i(rect.X + rect.Width, rect.Y);
		vectors[2].RelativeB = new Vector2i(rect.X + rect.Width, rect.Y + rect.Height);
		vectors[3].RelativeA = new Vector2i(rect.X + rect.Width, rect.Y + rect.Height);
		vectors[3].RelativeB = new Vector2i(rect.X, rect.Y + rect.Height);
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		bool isThereConnectionA = IsThereConnectionA;
		bool isThereConnectionB = IsThereConnectionB;
		stringBuilder.AppendFormat("A = {0}, B = {1}", AbsoluteA, AbsoluteB);
		if (isThereConnectionA & isThereConnectionB)
		{
			stringBuilder.Append(" Connected (A, B)");
		}
		else if (isThereConnectionA)
		{
			stringBuilder.Append(" Connected (A)");
		}
		else if (isThereConnectionB)
		{
			stringBuilder.Append(" Connected (B)");
		}
		return stringBuilder.ToString();
	}

	public CollisionVector GetConnectionA(int path)
	{
		if (_connectionA.Length <= path)
		{
			return null;
		}
		return _connectionA[path];
	}

	public CollisionVector GetConnectionB(int path)
	{
		if (_connectionB.Length <= path)
		{
			return null;
		}
		return _connectionB[path];
	}

	public void SetConnectionA(int path, CollisionVector v)
	{
		if (_connectionA.Length <= path)
		{
			Array.Resize(ref _connectionA, path);
		}
		_connectionA[path] = v;
	}

	public void SetConnectionB(int path, CollisionVector v)
	{
		if (_connectionB.Length <= path)
		{
			Array.Resize(ref _connectionB, path);
		}
		_connectionB[path] = v;
	}

	public void UpdateDerrivedFields()
	{
		if (RelativeA.X < RelativeB.X)
		{
			_left = RelativeA.X;
			_right = RelativeB.X;
			FlipX = 1;
		}
		else
		{
			_left = RelativeB.X;
			_right = RelativeA.X;
			FlipX = ((_left != _right) ? (-1) : 0);
		}
		if (AbsoluteA.Y < AbsoluteB.Y)
		{
			_top = RelativeA.Y;
			_bottom = RelativeB.Y;
			FlipY = 1;
		}
		else
		{
			_top = RelativeB.Y;
			_bottom = RelativeA.Y;
			FlipY = ((_top != _bottom) ? (-1) : 0);
		}
		Width = Right - Left;
		Height = Bottom - Top;
		if (Width >= Height)
		{
			Mode = ((FlipX <= 0) ? CollisionMode.Bottom : CollisionMode.Top);
		}
		else
		{
			Mode = ((FlipY <= 0) ? CollisionMode.Left : CollisionMode.Right);
		}
		double num = AbsoluteB.X - AbsoluteA.X;
		double num2 = AbsoluteB.Y - AbsoluteA.Y;
		Ratio = ((num2 == 0.0) ? 0.0 : (num / num2));
		Angle = (AbsoluteB - AbsoluteA).Angle;
	}

	public void ResetConnections()
	{
		Array.Clear(_connectionA, 0, _connectionA.Length);
		Array.Clear(_connectionB, 0, _connectionB.Length);
	}

	public bool HasPath(int path)
	{
		return (Paths & (1 << path)) != 0;
	}

	public bool FindFloor(Vector2 sensorPosition, double sensorSize, int path, out CollisionInfo info)
	{
		bool flag = false;
		double num = Angle;
		double num2 = sensorPosition.X - Math.Sin(num) * sensorSize;
		double num3 = AbsoluteA.Y;
		if (GetConnectionB(path) != null && (num2 - AbsoluteB.X) * FlipX > 0.0)
		{
			num2 = AbsoluteB.X;
			flag = true;
		}
		else if (GetConnectionA(path) != null && (num2 - AbsoluteA.X) * FlipX < 0.0)
		{
			num2 = AbsoluteA.X;
			flag = true;
		}
		if (flag)
		{
			num = Math.Asin(MathX.Clamp(-1.0, (num2 - sensorPosition.X) / sensorSize, 1.0)) * FlipX;
			if (FlipX < 0)
			{
				num += Math.PI;
			}
		}
		if (Height > 0)
		{
			num3 += (num2 - AbsoluteA.X) / Ratio;
		}
		double num4 = num3 - Math.Cos(num) * sensorSize - sensorPosition.Y;
		info = new CollisionInfo(this, new Vector2(num2, num3), num4, num);
		return num4 * FlipX < 0.0;
	}

	public bool FindWall(Vector2 sensorPosition, double sensorSize, int path, out CollisionInfo info)
	{
		bool flag = false;
		double num = Angle;
		double num2 = AbsoluteA.X;
		double num3 = sensorPosition.Y + Math.Cos(num) * sensorSize;
		if (GetConnectionB(path) != null && (num3 - AbsoluteB.Y) * FlipY > 0.0)
		{
			num3 = AbsoluteB.Y;
			flag = true;
		}
		else if (GetConnectionA(path) != null && (num3 - AbsoluteA.Y) * FlipY < 0.0)
		{
			num3 = AbsoluteA.Y;
			flag = true;
		}
		if (flag)
		{
			num = Math.Asin(MathX.Clamp(-1.0, (num3 - sensorPosition.Y) / sensorSize, 1.0)) * FlipY;
			if (FlipY > 0)
			{
				num += Math.PI;
			}
			num -= Math.PI / 2.0;
		}
		if (Width > 0)
		{
			num2 += (num3 - AbsoluteA.Y) * Ratio;
		}
		double num4 = num2 + Math.Sin(num) * sensorSize - sensorPosition.X;
		info = new CollisionInfo(this, new Vector2(num2, num3), num4, num);
		return num4 * FlipY > 0.0;
	}

	public static bool TestConnection(CollisionVector a, CollisionVector b)
	{
		if (a.Mode != b.Mode)
		{
			return Math.Abs(MathX.DifferenceRadians(a.Angle, b.Angle)) <= Math.PI / 3.0;
		}
		return true;
	}

	public static bool OnSamePath(CollisionVector a, CollisionVector b, int path)
	{
		if (a == b)
		{
			return true;
		}
		if (a.Mode != b.Mode)
		{
			return false;
		}
		if (a.IsWall ? ((a.AbsoluteA.Y - b.AbsoluteA.Y) * a.FlipY > 0) : ((a.AbsoluteA.X - b.AbsoluteA.X) * a.FlipX > 0))
		{
			for (CollisionVector connectionB = b.GetConnectionB(path); connectionB != null; connectionB = connectionB.GetConnectionB(path))
			{
				if (connectionB == a)
				{
					return true;
				}
				if (connectionB.Mode != b.Mode)
				{
					return false;
				}
			}
		}
		else
		{
			for (CollisionVector connectionA = b.GetConnectionA(path); connectionA != null; connectionA = connectionA.GetConnectionA(path))
			{
				if (connectionA == a)
				{
					return true;
				}
				if (connectionA.Mode != b.Mode)
				{
					return false;
				}
			}
		}
		return false;
	}

	public void Draw(Renderer renderer, Viewport viewport)
	{
		Vector2i absoluteA = AbsoluteA;
		Vector2i absoluteB = AbsoluteB;
		if ((absoluteA.X >= viewport.Bounds.X || absoluteB.X >= viewport.Bounds.X) && (absoluteA.X < viewport.Bounds.Right || absoluteB.X < viewport.Bounds.Right) && (absoluteA.Y >= viewport.Bounds.Y || absoluteB.Y >= viewport.Bounds.Y) && (absoluteA.Y < viewport.Bounds.Bottom || absoluteB.Y < viewport.Bounds.Bottom))
		{
			absoluteA.X -= viewport.Bounds.X;
			absoluteA.Y -= viewport.Bounds.Y;
			absoluteB.X -= viewport.Bounds.X;
			absoluteB.Y -= viewport.Bounds.Y;
			Draw(renderer, absoluteA, absoluteB, viewport.Scale);
		}
	}

	private void Draw(Renderer renderer, Vector2 pa, Vector2 pb, Vector2 scale)
	{
		pa = (Vector2i)(pa * scale);
		pb = (Vector2i)(pb * scale);
		I2dRenderer i2dRenderer = renderer.Get2dRenderer();
		Colour colour = ((Owner == null) ? Colours.White : Colours.Yellow);
		if (Owner == null && Paths == 1)
		{
			colour = Colours.Aqua;
		}
		else if (Owner == null && Paths == 2)
		{
			colour = Colours.Fuchsia;
		}
		i2dRenderer.RenderLine(colour, pa, pb, 1.0);
		double num = 16.0 * scale.X;
		if (num > 2.0)
		{
			double num2 = Angle - Math.PI / 2.0;
			Vector2 vector = new Vector2((pb.X - pa.X) / 2.0 + pa.X, (pb.Y - pa.Y) / 2.0 + pa.Y);
			Vector2 b = vector + new Vector2(Math.Cos(num2) * num, Math.Sin(num2) * num);
			i2dRenderer.RenderLine(colour, vector, b, 1.0);
		}
		double num3 = 8.0 * scale.X;
		int sectors = (int)(8.0 * scale.X);
		if (num3 > 1.0)
		{
			if (IsThereConnectionA)
			{
				i2dRenderer.RenderEllipse(colour, pa, 0.0, num3 + 0.5, sectors);
			}
			else
			{
				i2dRenderer.RenderRectangle(colour, new Rectangle(pa.X - num3, pa.Y - num3, num3 * 2.0, num3 * 2.0), 1.0);
			}
			if (IsThereConnectionB)
			{
				i2dRenderer.RenderEllipse(colour, pb, 0.0, num3 + 0.5, sectors);
			}
			else
			{
				i2dRenderer.RenderRectangle(colour, new Rectangle(pb.X - num3, pb.Y - num3, num3 * 2.0, num3 * 2.0), 1.0);
			}
		}
	}

	public static bool GetIntersection(CollisionVector a, CollisionVector b, out Vector2 intersection)
	{
		Vector2 vector = a.AbsoluteA;
		Vector2 vector2 = a.AbsoluteB;
		Vector2 vector3 = b.AbsoluteA;
		Vector2 vector4 = b.AbsoluteB;
		Vector2 vector5 = new Vector2(vector2.X - vector.X, vector2.Y - vector.Y);
		Vector2 vector6 = new Vector2(vector4.X - vector3.X, vector4.Y - vector3.Y);
		double num = ((0.0 - vector5.Y) * (vector.X - vector3.X) + vector5.X * (vector.Y - vector3.Y)) / ((0.0 - vector6.X) * vector5.Y + vector5.X * vector6.Y);
		double num2 = (vector6.X * (vector.Y - vector3.Y) - vector6.Y * (vector.X - vector3.X)) / ((0.0 - vector6.X) * vector5.Y + vector5.X * vector6.Y);
		if (num >= 0.0 && num <= 1.0 && num2 >= 0.0 && num2 <= 1.0)
		{
			intersection = new Vector2(vector.X + num2 * vector5.X, vector.Y + num2 * vector5.Y);
			return true;
		}
		intersection = default;
		return false;
	}

	public static bool Intersects(CollisionVector a, CollisionVector b)
	{
		Vector2 intersection;
		return GetIntersection(a, b, out intersection);
	}

	public static IEnumerable<CollisionInfo> GetCollisions(ActiveObject obj, int radialSensorSize, uint paths = uint.MaxValue)
	{
		return GetCollisions(obj, radialSensorSize, radialSensorSize, paths);
	}

	public static IEnumerable<CollisionInfo> GetCollisions(ActiveObject obj, int floorSensorSize, int wallSensorSize, uint paths = uint.MaxValue)
	{
		Vector2i vector2i = (Vector2i)obj.PositionPrecise;
		Rectanglei floorRect = Rectanglei.FromLTRB(vector2i.X - wallSensorSize, vector2i.Y - floorSensorSize, vector2i.X + wallSensorSize, vector2i.Y + floorSensorSize);
		Rectanglei wallRect = Rectanglei.FromLTRB(vector2i.X - wallSensorSize, vector2i.Y - wallSensorSize, vector2i.X + wallSensorSize, vector2i.Y + wallSensorSize);
		return GetCollisions(obj, floorSensorSize, wallSensorSize, floorRect, wallRect, paths, CollisionMode.Air, null);
	}

	public static IEnumerable<CollisionInfo> GetCollisions(ActiveObject obj, int floorSensorSize, int wallSensorSize, Rectanglei floorRect, Rectanglei wallRect, uint paths, CollisionMode mode, CollisionVector groundVector)
	{
		int numPaths = obj.Level.Map.CollisionPathLayers.Count;
		Rectanglei bounds = new Rectanglei(obj.Position.X - wallSensorSize * 2, obj.Position.Y - floorSensorSize * 2, wallSensorSize * 4, floorSensorSize * 4);
		foreach (CollisionVector t in obj.Level.CollisionTable.GetPossibleCollisionIntersections(bounds))
		{
			if ((t.Paths & paths) == 0)
			{
				continue;
			}
			for (int i = 0; i < numPaths; i++)
			{
				if ((t.Paths & (1 << i)) == 0L)
				{
					continue;
				}
				CollisionInfo info2;
				if (t.IsWall)
				{
					if (RectangleCheckCollision(wallRect, t.Bounds) && t.FindWall(obj.PositionPrecise, wallSensorSize, i, out var info) && !CheckSolidAngle(t, obj.LastPositionPrecise, info.Touch) && !CheckSolidAngle(t, obj.LastPositionPrecise, obj.PositionPrecise))
					{
						yield return info;
						break;
					}
				}
				else if (RectangleCheckCollision(floorRect, t.Bounds) && t.FindFloor(obj.PositionPrecise, floorSensorSize, i, out info2) && !CheckSolidAngle(t, obj.LastPositionPrecise, info2.Touch) && !CheckSolidAngle(t, obj.LastPositionPrecise, obj.PositionPrecise))
				{
					yield return info2;
					break;
				}
			}
		}
	}

	private static bool CheckSolidAngle(CollisionVector t, Vector2 previousPosition, Vector2 point)
	{
		if (point == previousPosition)
		{
			return true;
		}
		return MathX.DifferenceRadians((point - previousPosition).Angle, t.Angle) > 0.0;
	}

	private static bool RectangleCheckCollision(Rectanglei a, Rectanglei b)
	{
		if (a.X < b.X + b.Width && b.X < a.X + a.Width && a.Y < b.Y + b.Height)
		{
			return b.Y < a.Y + a.Height;
		}
		return false;
	}
}
