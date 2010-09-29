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
			for (int x = 4; x < 6; x++)
			{
				for (int y = 04; y < 6; y++)
				{
					for (int z = 4; z < 6; z++)
					{
						colorArray[x, y, z] = Color4.Red;
					}
				}
			}

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

	}
}
