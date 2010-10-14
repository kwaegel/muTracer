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
		public Color4[, ,] ColorArray { get; private set; }
		public float CellSize
		{
			get;
			private set;
		}

		public VoxelGrid(ComputeCommandQueue commandQueue, float gridWidth, int gridResolution)
		{
			_gridWidth = gridWidth;
			GridResolution = gridResolution;
			CellSize = gridWidth / gridResolution;

			// create test data. gridResolution^3 cells
			int cellCount = gridResolution * gridResolution * gridResolution;
			ColorArray = new Color4[gridResolution, gridResolution, gridResolution];

			int gridMax = gridResolution - 1;

			// Corners
			ColorArray[0, 0, 0] = Color4.Black;

			ColorArray[gridMax, 0, 0] = Color4.Red;
			ColorArray[0, gridMax, 0] = Color4.Green;
			ColorArray[0, 0, gridMax] = Color4.Blue;

			ColorArray[gridMax, gridMax, 0] = Color4.Yellow;
			ColorArray[gridMax, 0, gridMax] = Color4.Magenta;
			ColorArray[0, gridMax, gridMax] = Color4.Cyan;

			ColorArray[gridMax, gridMax, gridMax] = Color4.White;

			// Center block
			//ColorArray[7, 7, 7] = Color4.Black;

			ColorArray[8, 7, 7] = Color4.Red;
			ColorArray[7, 8, 7] = Color4.Green;
			ColorArray[7, 7, 8] = Color4.Blue;

			//ColorArray[8, 8, 7] = Color4.Yellow;
			//ColorArray[8, 7, 8] = Color4.Magenta;
			//ColorArray[7, 8, 8] = Color4.Cyan;

			//ColorArray[8, 8, 8] = Color4.White;


			ComputeImageFormat cif = new ComputeImageFormat(ComputeImageChannelOrder.Rgba, ComputeImageChannelType.Float);

			unsafe
			{
				fixed (Color4* gridData = ColorArray)
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
