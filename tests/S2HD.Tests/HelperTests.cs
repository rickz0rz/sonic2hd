using System;
using System.IO;
using System.Linq;
using SonicOrca;
using SonicOrca.Core.Collision;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using Xunit;

namespace S2HD.Tests;

/// <summary>Tests the geometry, colour and collection helpers that the audit found wrong.</summary>
public sealed class HelperTests
{
	private const double Tolerance = 1e-9;

	[Fact]
	public void Vector2_LengthSquared_IsTheSquareOfTheLength()
	{
		Assert.Equal(25.0, new Vector2(3.0, 4.0).LengthSquared);
	}

	[Fact]
	public void Vector2_RotateAboutAPoint_KeepsTheDistanceToThePoint()
	{
		Vector2 rotated = new Vector2(2.0, 1.0).Rotate(Math.PI / 2.0, new Vector2(1.0, 1.0));

		Assert.Equal(1.0, rotated.X, Tolerance);
		Assert.Equal(2.0, rotated.Y, Tolerance);
	}

	[Fact]
	public void ScalarDividedByVector_DividesTheScalarByEachComponent()
	{
		Assert.Equal(new Vector2(6.0, 3.0), 12.0 / new Vector2(2.0, 4.0));
		Assert.Equal(new Vector3(6.0, 3.0, 2.0), 12.0 / new Vector3(2.0, 4.0, 6.0));
		Assert.Equal(new Vector4(6.0, 3.0, 2.0, 1.0), 12.0 / new Vector4(2.0, 4.0, 6.0, 12.0));
		// The other order divides the vector by the scalar
		Assert.Equal(new Vector2(1.0, 2.0), new Vector2(2.0, 4.0) / 2.0);
		Assert.Equal(new Vector3(1.0, 2.0, 3.0), new Vector3(2.0, 4.0, 6.0) / 2.0);
		Assert.Equal(new Vector4(1.0, 2.0, 3.0, 4.0), new Vector4(2.0, 4.0, 6.0, 8.0) / 2.0);
	}

	[Fact]
	public void Vector4_Length_SquaresEachComponent()
	{
		Assert.Equal(4.0, new Vector4(2.0, 2.0, 2.0, 2.0).Length, Tolerance);
	}

	[Fact]
	public void Matrix4_RotationX_TurnsYIntoZ()
	{
		Vector3 turned = Matrix4.CreateRotationX(Math.PI / 2.0) * new Vector3(0.0, 1.0, 0.0);

		Assert.Equal(0.0, turned.X, Tolerance);
		Assert.Equal(0.0, turned.Y, Tolerance);
		Assert.Equal(1.0, Math.Abs(turned.Z), Tolerance);
	}

	[Fact]
	public void Colour_HueOfOne_IsRed()
	{
		Colour colour = Colour.FromHSL(1.0, 1.0, 0.5);

		Assert.Equal(255, colour.Red);
		Assert.Equal(0, colour.Green);
		Assert.Equal(0, colour.Blue);
	}

	[Fact]
	public void Colour_EqualsAnotherType_IsFalse()
	{
		Assert.False(Colours.White.Equals("white"));
		Assert.True(Colours.White.Equals((object)Colours.White));
	}

	[Fact]
	public void CsvSheet_AnEmptyFile_HasNoColumns()
	{
		Assert.Equal(0, new CsvSheet(new MemoryStream()).Columns);
	}

	[Fact]
	public void CollisionVector_AConnectionOnANewPath_Fits()
	{
		CollisionVector vector = new CollisionVector(new Vector2i(0, 0), new Vector2i(64, 0));
		CollisionVector other = new CollisionVector(new Vector2i(64, 0), new Vector2i(128, 0));

		vector.SetConnectionB(3, other);

		Assert.Same(other, vector.GetConnectionB(3));
	}

	private sealed class Box : IBounds
	{
		public Box(int x, int y) => Bounds = new Rectanglei(x, y, 1, 1);

		public Rectanglei Bounds { get; }
	}

	[Fact]
	public void QuadTree_ManyItemsInASmallNode_KeepTheirOrder()
	{
		// The tree cannot subdivide a node of 4 x 4, so all the items stay in one node in the order they came
		QuadTree<Box> tree = new QuadTree<Box>(new Rectanglei(0, 0, 4, 4), minNodeSize: 16, subdivideTargetCount: 2);
		Box[] boxes = Enumerable.Range(0, 10).Select(i => new Box(i % 3, i % 2)).ToArray();
		foreach (Box box in boxes)
		{
			tree.Add(box);
		}

		Assert.Equal(boxes, tree.ToArray());
		Assert.Equal(1, tree.GetDepth());
	}
}
