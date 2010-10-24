using System;

using OpenTK;
using OpenTK.Graphics;

using Cloo;

namespace Raytracing.CL
{
	class VoxelGrid
	{
		// Compute queue
		ComputeCommandQueue _commandQueue;

		// Grid buffer.
		private Color4[] _voxelArray;
		internal ComputeImage3D _voxelGrid;

		// Grid description.
		private float _gridWidth;
		public int GridResolution { get; private set; }

		private Vector3 _gridOrigin;

		public float CellSize
		{
			get;
			private set;
		}

		
		public Color4 this[int x, int y, int z]
		{
			get
			{
				int index = z * GridResolution * GridResolution + y * GridResolution + x;
				return _voxelArray[index];
			}
			private set
			{
				int index = z * GridResolution * GridResolution + y * GridResolution + x;
				_voxelArray[index] = value;
			}
		}

		public VoxelGrid(ComputeCommandQueue commandQueue, float gridWidth, int gridResolution)
		{
			_commandQueue = commandQueue;

			_gridWidth = gridWidth;
			GridResolution = gridResolution;
			CellSize = gridWidth / gridResolution;


			Vector3 halfGridWidth = new Vector3(gridWidth/2.0f, gridWidth/2.0f, gridWidth/2.0f);
			_gridOrigin = -halfGridWidth;

			// create test data. gridResolution^3 cells
			int cellCount = gridResolution * gridResolution * gridResolution;
			_voxelArray = new Color4[cellCount];

			int gridMax = gridResolution - 1;

			// Corners
			this[0, 0, 0] = Color4.Black;

			this[gridMax, 0, 0] = Color4.Red;
			this[0, gridMax, 0] = Color4.Green;
			this[0, 0, gridMax] = Color4.Blue;

			this[gridMax, gridMax, 0] = Color4.Yellow;
			this[gridMax, 0, gridMax] = Color4.Magenta;
			this[0, gridMax, gridMax] = Color4.Cyan;

			this[gridMax, gridMax, gridMax] = Color4.White;

			// Center block
			//this[7, 7, 7] = Color4.Black;

			this[7, 7, 7] = Color4.White;
			this[8, 7, 7] = Color4.Red;
			this[7, 8, 7] = Color4.Green;
			this[7, 7, 8] = Color4.Blue;

			//this[8, 8, 7] = Color4.Yellow;
			//this[8, 7, 8] = Color4.Magenta;
			//this[7, 8, 8] = Color4.Cyan;

			//this[8, 8, 8] = Color4.White;


			ComputeImageFormat cif = new ComputeImageFormat(ComputeImageChannelOrder.Rgba, ComputeImageChannelType.Float);

			unsafe
			{
				fixed (Color4* gridData = _voxelArray)
				{
					_voxelGrid = new ComputeImage3D(_commandQueue.Context,
						ComputeMemoryFlags.CopyHostPointer | ComputeMemoryFlags.ReadOnly,
						cif,
						gridResolution, gridResolution, gridResolution,
						0, 0,
						(IntPtr)gridData);
				}
			}
		}

		public void Dispose()
		{
			_voxelGrid.Dispose();
		}


		public void addSphere(Vector3 center, float radius, Color4 color)
		{
			// Translate to grid space.
			Vector3 gridCenter = center - _gridOrigin;

			// Find center voxel.
			int x = (int)(gridCenter.X / CellSize);
			int y = (int)(gridCenter.Y / CellSize);
			int z = (int)(gridCenter.Z / CellSize);

			// Only color the center voxel for now.
			this[x, y, z] = color;
		}

		public void syncBuffers()
		{
			
		}

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
			if (x >= GridResolution)
			{
				x = GridResolution - 1;
			}
			if (y >= GridResolution)
			{
				y = GridResolution - 1;
			}
			if (z >= GridResolution)
			{
				z = GridResolution - 1;
			}
		}

	}
}
