﻿using System;
using System.Runtime.InteropServices;

using OpenTK;
using OpenTK.Graphics;

using Cloo;

using Raytracing.Math;

namespace Raytracing.CL
{
	public struct Voxel
	{
		public uint PrimitiveCount;
		public uint y;
		public uint z;
		public uint w;
	}

	public struct SimplePointLight
	{
		public Vector3 position;
		public float intensity;
	}

	class VoxelGrid
	{
		// Compute queue
		ComputeCommandQueue _commandQueue;

		// Grid buffer.
		private ComputeImageFormat _imageFormat = new ComputeImageFormat(ComputeImageChannelOrder.Rgba, ComputeImageChannelType.UnsignedInt32);
		private Voxel[] _voxelArray;
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

		// Light buffer
		private static int InitialPointLightArraySize = 4;
		public int PointLightCount { get; private set; }
		private SimplePointLight[] _pointLightArray;
		private ComputeBuffer<SimplePointLight> _pointLightBuffer;
		private GCHandle _pointLightHandle;
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

			// Create array to hold primitives.
			VectorsPerVoxel = 16;	// Low value for testing;
			_geometryArray = new Vector4[cellCount * VectorsPerVoxel];
			// Array needs to be pinned during copy data to device memory.
			_geometryHandle = GCHandle.Alloc(_geometryArray, GCHandleType.Pinned);
			_geometryBuffer = new ComputeBuffer<Vector4>(_commandQueue.Context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer, _geometryArray.LongLength, _geometryHandle.AddrOfPinnedObject());

			// Create array for lights
			_pointLightArray = new SimplePointLight[InitialPointLightArraySize];
			PointLightCount = 0;
			_pointLightHandle = GCHandle.Alloc(_pointLightArray, GCHandleType.Pinned);
			_pointLightBuffer = new ComputeBuffer<SimplePointLight>(_commandQueue.Context, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.UseHostPointer, _pointLightArray.LongLength, _pointLightHandle.AddrOfPinnedObject());

			syncBuffers();
		}

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

			// Add a reference to model to every cell the bounding box intesects
			for (int x = minX; x <= maxX; x += 1)
			{
				for (int y = minY; y <= maxY; y += 1)
				{
					for (int z = minZ; z <= maxZ; z += 1)
					{
						Voxel voxelData = this[x, y, z];

						int geometryIndex = (x * GridResolution * GridResolution + y * GridResolution + z) * VectorsPerVoxel;
						_geometryArray[geometryIndex + voxelData.PrimitiveCount] = packedSphere;

						voxelData.PrimitiveCount += 1;
						this[x, y, z] = voxelData;
						cellCount++;
					}
				}
			}
		}


		public void addPointLight(Vector3 position, float intensity)
		{
			if (PointLightCount < _pointLightArray.Length - 1)
			{
				_pointLightArray[PointLightCount].position = position;
				_pointLightArray[PointLightCount].intensity = intensity;
				PointLightCount++;
			}
		}


		public void syncBuffers()
		{
			// copy voxel texture
			unsafe
			{
				fixed (Voxel* gridData = _voxelArray)
				{
					_voxelGrid = new ComputeImage3D(_commandQueue.Context,
						ComputeMemoryFlags.CopyHostPointer | ComputeMemoryFlags.ReadOnly,
						_imageFormat,
						GridResolution, GridResolution, GridResolution,
						0, 0,
						(IntPtr)gridData);
				}

				
			}

			// TODO: Only write sections of buffers that have changed.

			// Copy pinned geometry data to device memory.
			_commandQueue.WriteToBuffer<Vector4>(_geometryArray, _geometryBuffer, true, null);

			// Copy pinned light data to device memory.
			_commandQueue.WriteToBuffer<SimplePointLight>(_pointLightArray, _pointLightBuffer, true, null);
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
