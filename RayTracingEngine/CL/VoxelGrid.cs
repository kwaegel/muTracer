using System;

using OpenTK.Graphics;

using Cloo;

namespace Raytracing.CL
{
	class VoxelGrid
	{
		internal ComputeImage3D _voxelGrid;
		private float _gridWidth;
		private int _gridResolution;
		public float CellSize
		{
			get;
			private set;
		}

		public VoxelGrid(ComputeCommandQueue commandQueue, float gridWidth, int gridResolution)
		{
			_gridWidth = gridWidth;
			_gridResolution = gridResolution;
			CellSize = gridWidth / gridResolution;

			// create test data. gridResolution^3 cells
			int cellCount = gridResolution* gridResolution* gridResolution;
			Color4[,,] colorArray = new Color4[gridResolution, gridResolution, gridResolution];

			colorArray[4, 4, 4] = Color4.Red;
			colorArray[4, 4, 5] = Color4.Green;
			colorArray[4, 5, 4] = Color4.Blue;
			colorArray[4, 5, 5] = Color4.Yellow;
			colorArray[5, 4, 4] = Color4.DarkRed;
			colorArray[5, 4, 5] = Color4.DarkGreen;
			colorArray[5, 5, 4] = Color4.DarkBlue;
			colorArray[5, 5, 5] = Color4.DarkGoldenrod;

			ComputeImageFormat cif = new ComputeImageFormat(ComputeImageChannelOrder.Rgba, ComputeImageChannelType.Float);

			unsafe
			{
				fixed (Color4* gridData = colorArray)
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
