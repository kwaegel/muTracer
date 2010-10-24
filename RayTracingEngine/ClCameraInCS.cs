using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using Raytracing.Primitives;
using Raytracing.SceneStructures;

using Raytracing.CL;
using Raytracing.Math;

namespace Raytracing
{
	class ClCameraInCS : RayTracingCamera
	{
		VoxelGrid _grid;

		#region Initialization

		public ClCameraInCS(System.Drawing.Rectangle clientBounds)
			: this(clientBounds, MuxEngine.LinearAlgebra.Matrix4.Identity)
		{ }

		public ClCameraInCS(System.Drawing.Rectangle clientBounds, MuxEngine.LinearAlgebra.Matrix4 transform)
			: base(clientBounds, transform)
		{
			base.rayTracingInit();
		}

		public ClCameraInCS(System.Drawing.Rectangle clientBounds, Vector3 forward,
					   Vector3 up, Vector3 position)
			: base(clientBounds, forward, up, position)
		{
			base.rayTracingInit();
		}

		public void setVoxelGrid(Raytracing.CL.VoxelGrid grid)
		{
			_grid = grid;
		}

		#endregion

		// process rays by row
		protected override void gridWorker(Object o)
		{
			int workerIndex = (int)o;

			int rows = ClientBounds.Height;
			int columns = ClientBounds.Width;

			// get a lock on the _nextRayToProcess lock
			_rayToProcessLock.WaitOne();

			int y;

			// while there are more rays to process
			while (_nextRowToProcess < rows)
			{
				// get a block of rays to process in form [start, end)
				y = _nextRowToProcess;

				// increment row to process
				_nextRowToProcess++;

				// release the mutex for other threads to use
				_rayToProcessLock.ReleaseMutex();

				// process rays

				// For each pixel in the column
				for (int x = 0; x < columns; x++)
				{
					Ray r = unprojectPointIntoWorld(_normilizedScreenPoints[x, y]);

					bool debug = false;
					if (x == 201 && y == 190)
					{
						debug = true;
					}

					int pixelFlatIndex = y * columns + x;
					_pixelBuffer[pixelFlatIndex] = getPixelColor(r.Position, r.Direction, _grid, Color4.CornflowerBlue, debug);

				}

				// aquire the mutex for the next loop check
				_rayToProcessLock.WaitOne();
			}

			// release the mutex before terminating
			_rayToProcessLock.ReleaseMutex();

			// notify the main thread that there are no more rays for this thread to process
			_resetEvents[workerIndex].Set();
		}





		private Color4 getPixelColor(Vector3 rayOrigin, Vector3 rayDirection, VoxelGrid grid, Color4 backgroundColor, bool debug)
		{
			Color4 color = backgroundColor;

			float cellSize = grid.CellSize;

			/**** Traverse the grid and find the nearest occupied cell ****/

			// setup up traversel variables

			// get grid size from the texture file
			int gridWidth = grid.GridResolution;

			// traversel values


			// Center the grid at 0,0,0
			float halfWidth = (gridWidth * cellSize) / 2.0f;
			Vector3 halfGridWidth = new Vector3(halfWidth, halfWidth, halfWidth);
			Vector3 gridOrigin = -halfGridWidth;

			// convert the ray start position to grid space
			Vector3 gridSpaceCoordinates = rayOrigin - gridOrigin;

			// Get the index of the voxel the ray starts in
			int indexX = (int)(gridSpaceCoordinates.X / cellSize);
			int indexY = (int)(gridSpaceCoordinates.Y / cellSize);
			int indexZ = (int)(gridSpaceCoordinates.Z / cellSize);

			// Don't draw anything if the camera is outside the grid.
			// This prevents indexOutOfBounds exceptions during testing.
			if (indexX < 0 || indexX >= gridWidth ||
				indexY < 0 || indexY >= gridWidth ||
				indexZ < 0 || indexZ >= gridWidth)
			{
				return color;
			}

			// Get the distance to the next voxel boundary
			float fracX = (gridSpaceCoordinates.X % cellSize);
			float fracY = (gridSpaceCoordinates.Y % cellSize);
			float fracZ = (gridSpaceCoordinates.Z % cellSize);

			if (debug)
			{
				bool stop = true;
			}

			fracX = -fracX;
			fracY = -fracY;
			fracZ = -fracZ;

			int stepX = -1;
			int stepY = -1;
			int stepZ = -1;
			int outIndexX = -1;
			int outIndexY = -1;
			int outIndexZ = -1;
			if (rayDirection.X >= 0)
			{
				outIndexX = gridWidth;
				stepX = 1;
				fracX = cellSize + fracX;	// frac is negative
			}
			if (rayDirection.Y >= 0)
			{
				outIndexY = gridWidth;
				stepY = 1;
				fracY = cellSize + fracY;	// frac is negative
			}
			if (rayDirection.Z >= 0)
			{
				outIndexZ = gridWidth;
				stepZ = 1;
				fracZ = cellSize + fracZ;	// frac is negative
			}

			// tMax: min distance to move before crossing a gird boundary
			// This should be positive, but that is not happaning...
			float tMaxX = fracX / rayDirection.X;
			float tMaxY = fracY / rayDirection.Y;
			float tMaxZ = fracZ / rayDirection.Z;

			// tDelta: distance (in t) between cell boundaries
			// Must be positive
			float tDeltaX = System.Math.Abs(cellSize / rayDirection.X);
			float tDeltaY = System.Math.Abs(cellSize / rayDirection.Y);
			float tDeltaZ = System.Math.Abs(cellSize / rayDirection.Z);

			if (debug)
			{
				bool stop = true;
			}

			// begin grid traversel
			/*
			 * Might want to change this to a while() loop to test the current voxel first,
			 * before moving to the next one. I am not sure why this seems to be working in
			 * the C# version.
			 * */
			bool containsGeometry = false;
			Color4 cellData;

			// Check grid data at origional index
			cellData = grid[indexX, indexY, indexZ];
			containsGeometry = (cellData.R > 0 || cellData.G > 0 || cellData.B > 0 || cellData.A > 0);

			while (!containsGeometry)
			{
				if (tMaxX < tMaxY)
				{
					if (tMaxX < tMaxZ)
					{
						indexX += stepX;			// step to next voxel along this axis
						if (indexX == outIndexX)	// outside grid
							break;
						tMaxX = tMaxX + tDeltaX;	// increment max distence to next voxel
					}
					else
					{
						indexZ += stepZ;
						if (indexZ == outIndexZ)
							break;
						tMaxZ = tMaxZ + tDeltaZ;
					}
				}
				else
				{
					if (tMaxY < tMaxZ)
					{
						indexY += stepY;
						if (indexY == outIndexY)
							break;
						tMaxY = tMaxY + tDeltaY;
					}
					else
					{
						indexZ += stepZ;
						if (indexZ == outIndexZ)
							break;
						tMaxZ = tMaxZ + tDeltaZ;
					}
				}

				// get grid data at index
				cellData = grid[indexX, indexY, indexZ];
				containsGeometry = (cellData.R > 0 || cellData.G > 0 || cellData.B > 0 || cellData.A > 0);
			}

			/**** Write output to image ****/
			if (containsGeometry)
			{
				color = cellData;
			}

			return color;	//write_imagef(outputImage, coord, color);
		}





	}
}
