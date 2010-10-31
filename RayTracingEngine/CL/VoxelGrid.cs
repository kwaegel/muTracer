using System;
using System.Runtime.InteropServices;

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
		private ComputeImageFormat _imageFormat = new ComputeImageFormat(ComputeImageChannelOrder.Rgba, ComputeImageChannelType.Float);
		private Color4[] _voxelArray;
		internal ComputeImage3D _voxelGrid;

		// Primitive buffer
		private Vector4[] _geometryArray;
		private ComputeBuffer<Vector4> _geometryBuffer;
		private GCHandle _geometryHandle;
		public int VectorsPerVoxel { get; private set; }
		internal ComputeBuffer<Vector4> Geometry {
			get
			{
				return _geometryBuffer;
			}
			private set
			{
				// Only a getter.
			}
		}

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

			VectorsPerVoxel = 16;	// Test value;
			_geometryArray = new Vector4[cellCount * VectorsPerVoxel];
			_geometryHandle = GCHandle.Alloc(_geometryArray, GCHandleType.Pinned);
			_geometryBuffer = new ComputeBuffer<Vector4>(_commandQueue.Context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer, _geometryArray.LongLength, _geometryHandle.AddrOfPinnedObject());

			syncBuffers();
		}

		//private void setTestData()
		//{
		//    int gridMax = GridResolution - 1;

		//    // Corners
		//    this[0, 0, 0] = Color4.Black;

		//    this[gridMax, 0, 0] = Color4.Red;
		//    this[0, gridMax, 0] = Color4.Green;
		//    this[0, 0, gridMax] = Color4.Blue;

		//    this[gridMax, gridMax, 0] = Color4.Yellow;
		//    this[gridMax, 0, gridMax] = Color4.Magenta;
		//    this[0, gridMax, gridMax] = Color4.Cyan;

		//    this[gridMax, gridMax, gridMax] = Color4.White;

		//    this[7, 7, 7] = Color4.White;
		//    this[8, 7, 7] = Color4.Red;
		//    this[7, 8, 7] = Color4.Green;
		//    this[7, 7, 8] = Color4.Blue;
		//}

		public void Dispose()
		{
			_voxelGrid.Dispose();

			_geometryHandle.Free();
			_geometryBuffer.Dispose();
		}


		public void addSphere(Vector3 center, float radius, Color4 color)
		{
			// Pack sphere into a Vector4
			Vector4 packedSphere = new Vector4(center, radius);

			// Translate to grid space.
			Vector3 gridCenter = center - _gridOrigin;

			int minX = (int)((gridCenter.X - radius) / CellSize);
			int minY = (int)((gridCenter.Y - radius) / CellSize);
			int minZ = (int)((gridCenter.Z - radius) / CellSize);
			int maxX = (int)((gridCenter.X + radius) / CellSize);
			int maxY = (int)((gridCenter.Y + radius) / CellSize);
			int maxZ = (int)((gridCenter.Z + radius) / CellSize);

			int cellCount = 0;

			// add a reference to model to every cell the bounding box intesects
			for (int x = minX; x <= maxX; x += 1)
			{
				for (int y = minY; y <= maxY; y += 1)
				{
					for (int z = minZ; z <= maxZ; z += 1)
					{
						int geometryIndex = (x * GridResolution * GridResolution + y * GridResolution + z) * VectorsPerVoxel;
						_geometryArray[geometryIndex] = packedSphere;
						this[x, y, z] = color;
						cellCount++;
					}
				}
			}
		}

		public void syncBuffers()
		{
			// copy voxel texture
			unsafe
			{
				fixed (Color4* gridData = _voxelArray)
				{
					_voxelGrid = new ComputeImage3D(_commandQueue.Context,
						ComputeMemoryFlags.CopyHostPointer | ComputeMemoryFlags.ReadOnly,
						_imageFormat,
						GridResolution, GridResolution, GridResolution,
						0, 0,
						(IntPtr)gridData);
				}
			}

			// Copy geometry data to device.
			// TODO: Only write section of buffer that has changed.
			_commandQueue.WriteToBuffer<Vector4>(_geometryArray, _geometryBuffer, true, null);
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
