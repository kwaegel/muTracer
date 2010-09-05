using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//using Microsoft.Xna.Framework;
using OpenTK;

using Raytracing.Primitives;

using Raytracing.Math;
using Raytracing.BoundingVolumes;

namespace Raytracing.SceneStructures
{
	// implements coherent grid traversal as described in Wald et. al 2006
	public class GridScene : Scene
	{
		List<AbstractPrimitive> _primitives;
		List<PointLight> _lights;

		GridCell[, ,] _grid;

		Vector3i _gridWidth;

		Vector3i _numCells;

		AxisAlignedBoundingBox _gridBounds;

		int _cellSize;

		#region Constructor and init methods

		public GridScene(int gridWidth) 
			: this(gridWidth, 1)
		{}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="_gridWidth">How wide the grid is. Must be evenly 
		/// divisable by the cell width</param>
		/// <param name="cellSize">How many units wide each cell is.</param>
		public GridScene(int gridWidth, int cellSize)
			:base()
		{
			// this is not strictly nessacery, but it helps aviod grid center confusion.
			if (gridWidth % cellSize != 0)
				throw new ArgumentException("GridWidth must be evenly divisable by the cellSize");

			if (cellSize < 1)
				throw new ArgumentException("Cell size must be at least 1");

			_cellSize = cellSize;

			_gridWidth = new Vector3i(gridWidth, gridWidth, gridWidth);

			// create object lists
			_primitives = new List<AbstractPrimitive>();
			_lights = new List<PointLight>();

			// create reference to the grid start for transformation into grid space
			int halfWidth = gridWidth / 2;
			Vector3 gridOrigin = new Vector3(-halfWidth, -halfWidth, -halfWidth);
			Vector3 gridMax = new Vector3(halfWidth, halfWidth, halfWidth);

			_gridBounds = new AxisAlignedBoundingBox(gridOrigin, gridMax);



			// build the grid structure
			_numCells = new Vector3i(gridWidth / cellSize);
			_grid = new GridCell[_numCells.X, _numCells.Y, _numCells.Z];
			for (int x = 0; x < _numCells.X; x++)
			{
				for (int y = 0; y < _numCells.Y; y++)
				{
					for (int z = 0; z < _numCells.Z; z++)
					{
						_grid[x, y, z] = new GridCell();
					}
				}
			}

			System.Console.Write("created grid with " + _grid.Length +" cells. Grid size= " + _gridWidth.X);
			System.Console.WriteLine(". Cell width= " + _cellSize);
		}
		#endregion

		#region Public methods

		public override void add(AbstractPrimitive model)
		{
			// create an aixs-aligned bounding box around the primitive
			AxisAlignedBoundingBox bb = model.getBoundingBox();

			// translate bounding box to grid space
			bb.Min -= _gridBounds.Min;
			bb.Max -= _gridBounds.Min;

			// get cell index bounds
			int minX, minY, minZ, maxX, maxY, maxZ;
			minX = (int)bb.Min.X / _cellSize;
			minY = (int)bb.Min.Y / _cellSize;
			minZ = (int)bb.Min.Z / _cellSize;

			maxX = (int)bb.Max.X / _cellSize;
			maxY = (int)bb.Max.Y / _cellSize;
			maxZ = (int)bb.Max.Z / _cellSize;

			// clamp min and max to the cell grid
			clampToGrid(ref minX, ref minY, ref minZ);
			clampToGrid(ref maxX, ref maxY, ref maxZ);

			int cellCount = 0;

			// add a reference to model to every cell the bounding box intesects
			for (int x = minX; x <= maxX; x += 1)
			{
				for (int y = minY; y <= maxY; y += 1)
				{
					for (int z = minZ; z <= maxZ; z += 1)
					{
						getGridCell(x, y, z).add(model);
						cellCount++;
					}
				}
			}

			//System.Console.WriteLine("added " + model + " to " + cellCount+ " cells.");
		}
		
		public override void add(PointLight light)
		{
			_lights.Add(light);
		}

		public override List<PointLight> getLights()
		{
			return _lights;
		}

		private GridCell getGridCell(int x, int y, int z)
		{
			return _grid[x, y, z];
		}
		

		public override float getNearestIntersection(ref Ray r, ref Vector3 collisionPoint,
			ref Vector3 surfaceNormal, ref Material material)
		{
			// transform ray into into grid coordinates. Use this ray only for traversing the grid
			GridRay gridRay = new GridRay(this, ref r, _gridBounds, _cellSize);

			GridCell cell = gridRay.getFirstNonEmptyCell();

			while (cell != null)	// break if the ray hits a model or we run out of cells.
			{
				float nearestIntersection = float.PositiveInfinity;
				Vector3 cp = new Vector3();
				Vector3 sn = new Vector3();

				List<AbstractPrimitive> cellGeometry = cell.Geometry;
				for (int i = 0; i < cellGeometry.Count; i++)
				{
					AbstractPrimitive prim = cellGeometry[i];

					float? intersection = prim.intersects(ref r, ref cp, ref sn);

					if (intersection.HasValue && intersection < nearestIntersection)
					{
						nearestIntersection = (float)intersection;
						collisionPoint = cp;
						surfaceNormal = sn;
						material = prim.Material;
					}
				}

				// if the ray has collided with somthing, we have no need to check more cells
				if (!float.IsPositiveInfinity(nearestIntersection))
				{
					return nearestIntersection;
				}

				// move to next cell if no intersection was found
				cell = gridRay.getNextNonEmptyCell();
			}

			return -1;
		}


		#endregion


		#region Private methods

		private void clampToGrid(ref int x, ref int y, ref int z)
		{
			// check lower bounds
			if (x < 0)
				x = 0;
			if (y < 0)
				y = 0;
			if (z < 0)
				z = 0;

			// check upper bounds
			if (x >= _numCells.X)
				x = _numCells.X - 1;
			if (y >= _numCells.Y)
				y = _numCells.Y - 1;
			if (z >= _numCells.Z)
				z = _numCells.Z - 1;
		}

		/// <summary>
		/// Update the position of all models in the grid.
		/// </summary>
		private void rebuildGrid()
		{
			clearGrid();

			foreach (AbstractPrimitive primitive in _primitives)
			{
				add(primitive);
			}
		}

		private void clearGrid()
		{
			foreach (GridCell cell in _grid)
			{
				// I am assuming here that clear does nothing if the list is empty.
				cell.Geometry.Clear();
			}
		}

		#endregion

		#region Grid structures

		// a very simple grid cell
		private class GridCell
		{
			public List<AbstractPrimitive> Geometry;

			public bool ContainsGeometry;

			public GridCell()
			{
				Geometry = new List<AbstractPrimitive>();
				ContainsGeometry = false;
			}

			public void add(AbstractPrimitive model)
			{
				ContainsGeometry = true;
				Geometry.Add(model);
			}

		}

		private struct GridRay
		{
			// use a scene functon to get each grid cell
			private GridScene _scene;

			// cell widths
			//private Vector3i step;
			private int stepX, stepY, stepZ;

			// min distances to move before crossing a grid boundary
			//private Vector3 tMax;
			private float tMaxX, tMaxY, tMaxZ;

			// distance (in t) between cell boundaries
			//private Vector3 tDelta;
			private float tDeltaX, tDeltaY, tDeltaZ;

			// voxel indices
			int X, Y, Z;

			// first invalid voxel index for each axis
			int outX, outY, outZ;

			private bool intersectsGrid;

			public GridRay(GridScene gridScene, ref Ray worldRay, AxisAlignedBoundingBox gridWorldBounds, int cellSize)
			{
				_scene = gridScene;

				Vector3 startPositionInWorld = new Vector3();

				// create the starting location in grid space coordinates
				float? worldT = worldRay.Intersects(gridWorldBounds);
				if (worldT.HasValue)
				{
					// the ray started outside the grid bounds, set starting point to nearest intersection point
					if (worldT > 0)
						worldT += 0.002f;	// ensure the start location is inside the grid
					startPositionInWorld = worldRay.Position + worldRay.Direction * (float)worldT;
					intersectsGrid = true;
				}
				else
				{
					// ray does not intersect grid. Don't do any more calculations.
					intersectsGrid = false;
					stepX = 0;
					stepY = 0;
					stepZ = 0;
					tMaxX = 0;
					tMaxY = 0;
					tMaxZ = 0;
					tDeltaX = 0;
					tDeltaY = 0;
					tDeltaZ = 0;
					X = 0;
					Y = 0;
					Z = 0;
					outX = 0;
					outY = 0;
					outZ = 0;
					return;
				}

				// convert to grid space
				Vector3 gridSpaceCoordinates = startPositionInWorld - gridWorldBounds.Min;

				// compute max distance to move and stay in the same cell
				float fracX = gridSpaceCoordinates.X % cellSize;
				float fracY = gridSpaceCoordinates.Y % cellSize;
				float fracZ = gridSpaceCoordinates.Z % cellSize;

				// initilize cell coordinates
				X = (int)(gridSpaceCoordinates.X/cellSize);
				Y = (int)(gridSpaceCoordinates.Y/cellSize);
				Z = (int)(gridSpaceCoordinates.Z/cellSize);

				// make sure we take the fraction in the direction of the ray
				// also calculate the step here to reduce needed compairisons
				outX = -1;
				outY = -1;
				outZ = -1;
				stepX = -1;
				stepY = -1;
				stepZ = -1;
				if (worldRay.Direction.X >= 0)
				{
					outX = _scene._numCells.X;
					stepX = 1;
					fracX = cellSize - fracX;
				}
				if (worldRay.Direction.Y >= 0)
				{
					outY = _scene._numCells.Y;
					stepY = 1;
					fracY = cellSize - fracY;
				}
				if (worldRay.Direction.Z >= 0)
				{
					outZ = _scene._numCells.Z;
					stepZ = 1;
					fracZ = cellSize - fracZ;
				}

				// Reduce to one instruction with SIMD vector? (Mono.Simd.Vector4f)
				tMaxX = fracX / worldRay.Direction.X;
				tMaxY = fracY / worldRay.Direction.Y;
				tMaxZ = fracZ / worldRay.Direction.Z;
				tMaxX *= stepX;
				tMaxY *= stepY;
				tMaxZ *= stepZ;

				// compute projections onto the coordinate axes
				tDeltaX = cellSize / worldRay.Direction.X;
				tDeltaY = cellSize / worldRay.Direction.Y;
				tDeltaZ = cellSize / worldRay.Direction.Z;

				// multiply by step to ensure all deltas are positive.
				tDeltaX *= stepX;
				tDeltaY *= stepY;
				tDeltaZ *= stepZ;
			}

			public GridCell getFirstNonEmptyCell()
			{
				if (!intersectsGrid)
					return null;

				GridCell cell = null;

				if (X < 0 || Y < 0 || Z < 0 
					|| X >= _scene._numCells.X || Y >= _scene._numCells.Y || Z >= _scene._numCells.Z)
				{
					return null;
				}
				else
				{

					// get the current cell
					cell = _scene.getGridCell(X, Y, Z);

					if (cell.ContainsGeometry)
						return cell;
					else
						return getNextNonEmptyCell();
				}
			}

			public GridCell getNextNonEmptyCell()
			{
				
				GridCell cell = null;

				do
				{
					if (tMaxX < tMaxY)
					{
						if (tMaxX < tMaxZ)
						{
							X += stepX;	// step to next voxel along this axis
							if (X == outX)	// outside grid
								return (null); 
							tMaxX = tMaxX + tDeltaX;	// increment max distence to next voxel
						}
						else
						{
							Z += stepZ;
							if (Z == outZ)
								return (null);
							tMaxZ = tMaxZ + tDeltaZ;
						}
					}
					else
					{
						if (tMaxY < tMaxZ)
						{
							Y += stepY;
							if (Y == outY)
								return (null);
							tMaxY = tMaxY + tDeltaY;
						}
						else
						{
							Z += stepZ;
							if (Z == outZ)
								return (null);
							tMaxZ = tMaxZ + tDeltaZ;
						}
					}

					// convert position to grid indices
					//cell = _scene.getGridCell(gridSpaceCoordinates);
					cell = _scene.getGridCell(X,Y,Z);

				} while (cell != null && !cell.ContainsGeometry);

				return (cell);
			}

		}

		private bool isPowerOfTwo(int n)
		{
			return n!=0 && ((n & (n-1)) ==0);
		}

		#endregion
	}
}
