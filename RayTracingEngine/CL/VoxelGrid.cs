using System;

using OpenTK.Graphics;

using Cloo;

namespace Raytracing.CL
{
	class VoxelGrid
	{
		internal ComputeImage3D _voxelGrid;
		private float _gridWidth;
		public int GridResolution { get; private set; }
		
		public float CellSize
		{
			get;
			private set;
		}

		private Color4[] _colorArray;
		public Color4 this[int x, int y, int z]
		{
			get
			{
				int index = z * GridResolution * GridResolution + y * GridResolution + x;
				return _colorArray[index];
			}
			private set
			{
				int index = z * GridResolution * GridResolution + y * GridResolution + x;
				_colorArray[index] = value;
			}
		}

		public VoxelGrid(ComputeCommandQueue commandQueue, float gridWidth, int gridResolution)
		{
			_gridWidth = gridWidth;
			GridResolution = gridResolution;
			CellSize = gridWidth / gridResolution;

			// create test data. gridResolution^3 cells
			int cellCount = gridResolution * gridResolution * gridResolution;
			_colorArray = new Color4[cellCount];

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
				fixed (Color4* gridData = _colorArray)
				{
					_voxelGrid = new ComputeImage3D(commandQueue.Context,
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

	}
}
