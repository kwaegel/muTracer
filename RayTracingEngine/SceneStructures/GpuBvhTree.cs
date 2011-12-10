using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using OpenTK;
using OpenTK.Graphics;
using Cloo;

using Raytracing.Primitives;

namespace Raytracing.SceneStructures
{
	public class GpuBvhTree : BvhTree
	{
		ComputeCommandQueue _commandQueue;

		private static int InitialPointLightArraySize = 4;
		public int PointLightCount = 0;
		private List<SimplePointLight> _lights;
		private SimplePointLight[] _pointLightArray;
		public ComputeBuffer<SimplePointLight> PointLightBuffer;

		private Triangle[] _geometryArray;
		internal ComputeBuffer<Triangle> Geometry;

		public ComputeBuffer<LinearBVHNode> BvhNodeBuffer;

		public GpuBvhTree(ComputeCommandQueue commandQueue,
			List<Triangle> prims, List<SimplePointLight> lights, int maxPrimsPerNode)
			:base(prims, maxPrimsPerNode)
		{
			_commandQueue = commandQueue;
			_lights = lights;
			initBuffers();
		}

		private void initBuffers()
		{
			// Create array for lights
			_pointLightArray = new SimplePointLight[_lights.Count];
			_lights.CopyTo(_pointLightArray, 0);
			PointLightCount = _lights.Count;
			PointLightBuffer = new ComputeBuffer<SimplePointLight>(_commandQueue.Context, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.UseHostPointer, _pointLightArray);

			// Create primitive array
			_geometryArray = new Triangle[_primitives.Count];
			_primitives.CopyTo(_geometryArray, 0);
			Geometry = new ComputeBuffer<Triangle>(_commandQueue.Context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer, _geometryArray);

			// Copy BVH node array to buffer
			BvhNodeBuffer = new ComputeBuffer<LinearBVHNode>(_commandQueue.Context, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.UseHostPointer, _nodes);

			// Don't need to sync buffers here. Will be done before rendering.
		}


		public void syncBuffers()
		{
			_commandQueue.WriteToBuffer<LinearBVHNode>(_nodes, BvhNodeBuffer, true, null);

			// Copy pinned geometry data to device memory.
			_commandQueue.WriteToBuffer<Triangle>(_geometryArray, Geometry, true, null);

			// Copy pinned light data to device memory.
			_commandQueue.WriteToBuffer<SimplePointLight>(_pointLightArray, PointLightBuffer, true, null);
			_commandQueue.AddBarrier();
		}

	}
}
