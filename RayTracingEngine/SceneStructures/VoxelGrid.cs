using System;
using System.Runtime.InteropServices;

using OpenTK;
using OpenTK.Graphics;

using Cloo;

using Raytracing.Math;
using Raytracing.Primitives;

namespace Raytracing.SceneStructures
{
	[StructLayout(LayoutKind.Sequential)]
	public struct Voxel
	{
		public uint PrimitiveCount;
		public uint y;
		public uint z;
		public uint w;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SimplePointLight
	{
		public Vector4 position;
		public Color4 colorAndIntensity;
	}

	internal class VoxelGrid
	{
		// Compute queue
		ComputeCommandQueue _commandQueue;

		// Grid buffer.
		private ComputeImageFormat _imageFormat = new ComputeImageFormat(ComputeImageChannelOrder.Rgba, ComputeImageChannelType.UnsignedInt32);
		private Voxel[] _voxelArray;
		internal ComputeImage3D _grid;

		// Primitive buffer
		//private SphereStruct[] _geometryArray;
		private Triangle[] _geometryArray;
		private int numTris = 0;
		public int VectorsPerVoxel { get; private set; }

        internal ComputeBuffer<Triangle> Geometry { get; private set; }

		// Light buffer
		private static int InitialPointLightArraySize = 4;
		public int PointLightCount { get; private set; }
		private SimplePointLight[] _pointLightArray;
		private ComputeBuffer<SimplePointLight> _pointLightBuffer;
		internal ComputeBuffer<SimplePointLight> PointLights
		{
			get { return _pointLightBuffer; }
			private set { }
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

		public Voxel this[int x, int y, int z]
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

			// Create voxel grid. gridResolution^3 cells
			int cellCount = gridResolution * gridResolution * gridResolution;
			_voxelArray = new Voxel[cellCount];

            unsafe
            {
                fixed (Voxel* gridData = _voxelArray)
                {
                    _grid = new ComputeImage3D(_commandQueue.Context,
                        ComputeMemoryFlags.CopyHostPointer | ComputeMemoryFlags.ReadOnly,
                        _imageFormat,
                        GridResolution, GridResolution, GridResolution,
                        0, 0,
                        (IntPtr)gridData);
                }
            }

			// Create array to hold primitives.
			VectorsPerVoxel = 16;	// Low value for testing;
			_geometryArray = new Triangle[cellCount * VectorsPerVoxel];
            Geometry = new ComputeBuffer<Triangle>(_commandQueue.Context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer, _geometryArray);

			// Create array for lights
			_pointLightArray = new SimplePointLight[InitialPointLightArraySize];
			PointLightCount = 0;
			_pointLightBuffer = new ComputeBuffer<SimplePointLight>(_commandQueue.Context, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.UseHostPointer, _pointLightArray);

			syncBuffers();
		}

		public void Dispose()
		{
			_grid.Dispose();
            Geometry.Dispose();
            _pointLightBuffer.Dispose();
		}

		public void addTriangle(Vector3 p0, Vector3 p1, Vector3 p2, int materialIndex)
		{
			// Pack triangle
			Triangle tri = new Triangle(p0, p1, p2, materialIndex);

			_geometryArray[numTris++] = tri;

			/*
			// Translate to grid space.
			p0 -= _gridOrigin;
			p1 -= _gridOrigin;
			p2 -= _gridOrigin;

			int minX = (int)( System.Math.Min(p0.X, System.Math.Min(p1.X, p2.X))  / CellSize);
			int minY = (int)(System.Math.Min(p0.Y, System.Math.Min(p1.Y, p2.Y)) / CellSize);
			int minZ = (int)(System.Math.Min(p0.Z, System.Math.Min(p1.Z, p2.Z)) / CellSize);

			int maxX = (int)(System.Math.Max(p0.X, System.Math.Max(p1.X, p2.X)) / CellSize);
			int maxY = (int)(System.Math.Max(p0.Y, System.Math.Max(p1.Y, p2.Y)) / CellSize);
			int maxZ = (int)(System.Math.Max(p0.Z, System.Math.Max(p1.Z, p2.Z)) / CellSize);

			int cellCount = 0;

			// Add a reference to model to every cell the bounding box intesects
			for (int x = minX; x <= maxX; x++)
			{
				for (int y = minY; y <= maxY; y++)
				{
					for (int z = minZ; z <= maxZ; z++)
					{
						Voxel voxelData = this[x, y, z];

						int geometryIndex = (x * GridResolution * GridResolution + y * GridResolution + z) * VectorsPerVoxel;
						_geometryArray[geometryIndex + voxelData.PrimitiveCount] = tri;

						voxelData.PrimitiveCount += 1;
						this[x, y, z] = voxelData;
						cellCount++;

						//System.Diagnostics.Debug.WriteLine("["+x+","+y+","+z+"]=+1");
					}
				}
			}
			*/
			
		}
		

		//public void addSphere(Vector3 center, float radius, int materialIndex)
		//{
		//    // Pack sphere into a Vector4
		//    //Vector4 packedSphere = new Vector4(center, radius);
		//    SphereStruct packedSphere = new SphereStruct(center, radius, materialIndex);

		//    // Translate to grid space.
		//    Vector3 gridCenter = center - _gridOrigin;

		//    int minX = (int)((gridCenter.X - radius) / CellSize);
		//    int minY = (int)((gridCenter.Y - radius) / CellSize);
		//    int minZ = (int)((gridCenter.Z - radius) / CellSize);
		//    int maxX = (int)((gridCenter.X + radius) / CellSize);
		//    int maxY = (int)((gridCenter.Y + radius) / CellSize);
		//    int maxZ = (int)((gridCenter.Z + radius) / CellSize);

		//    int cellCount = 0;

		//    // Add a reference to model to every cell the bounding box intesects
		//    for (int x = minX; x <= maxX; x++)
		//    {
		//        for (int y = minY; y <= maxY; y++)
		//        {
		//            for (int z = minZ; z <= maxZ; z++)
		//            {
		//                Voxel voxelData = this[x, y, z];

		//                int geometryIndex = (x * GridResolution * GridResolution + y * GridResolution + z) * VectorsPerVoxel;
		//                _geometryArray[geometryIndex + voxelData.PrimitiveCount] = packedSphere;

		//                voxelData.PrimitiveCount += 1;
		//                this[x, y, z] = voxelData;
		//                cellCount++;
		//            }
		//        }
		//    }
		//}


		public void addPointLight(Vector3 position, Color4 color, float intensity)
		{
			if (PointLightCount < _pointLightArray.Length - 1)
			{
				// pack the color and intensity values into a single struct
				Color4 colorAndIntensity = color;
				colorAndIntensity.A = intensity;

				_pointLightArray[PointLightCount].position = new Vector4(position, 1.0f);
				_pointLightArray[PointLightCount].colorAndIntensity = colorAndIntensity;
				PointLightCount++;
			}
		}


		public void syncBuffers()
		{
            // TODO: Only write sections of buffers that have changed.

			// copy voxel texture
			unsafe
			{
				fixed (Voxel* gridData = _voxelArray)
				{
                    _commandQueue.WriteToImage((IntPtr)gridData, _grid, true, null);
				}
			}

			// Copy pinned geometry data to device memory.
            // NOTE: using the unblocking version creates hundreds of ComputeEvents.
			_commandQueue.WriteToBuffer<Triangle>(_geometryArray, Geometry, true, null);

			// Copy pinned light data to device memory.
			_commandQueue.WriteToBuffer<SimplePointLight>(_pointLightArray, _pointLightBuffer, true, null);
			_commandQueue.AddBarrier();
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
