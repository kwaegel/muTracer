using System;
using System.Collections.Generic;

using Cloo;

using Raytracing.Primitives;

namespace Raytracing.SceneStructures
{
	class CLSphereBuffer
	{
		private readonly int _maxItems;

		ComputeCommandQueue _commandQueue;

		List<SphereStruct> _sphereList;

		ComputeBuffer<SphereStruct> _sphereBuffer;

		private bool _geometryChanged = false;

		public CLSphereBuffer(ComputeCommandQueue commandQueue, int maxItems)
		{
			_commandQueue = commandQueue;
			_maxItems = maxItems;
			_sphereList = new List<SphereStruct>(_maxItems);
			_sphereBuffer = new ComputeBuffer<SphereStruct>(commandQueue.Context, ComputeMemoryFlags.ReadWrite, _maxItems);
		}

		public void addSphere(SphereStruct newSphere)
		{
			if (_sphereList.Count < _sphereList.Capacity)
			{
				_sphereList.Add(newSphere);
				_geometryChanged = true;
			}
		}

		/// <summary>
		/// Not working correctly yet. Has to create a new buffer every time.
		/// </summary>
		unsafe public void sendDataToDevice()
		{
			// Only send data to the device if the local geometry list has changed.
			if (_geometryChanged)
			{
				fillBuffer(_sphereList.ToArray());
				_geometryChanged = false;
			}

			// Modifying an existing buffer is not working yet. Access violation exception.
			// Send data to device and block until finished.
			//SphereStruct[] spheres = _sphereList.ToArray();
			//CommandQueue.WriteToBuffer<SphereStruct>(_sphereList.ToArray(), _sphereBuffer, true, null);
			//CommandQueue.AddBarrier();
		}

		private void fillBuffer(SphereStruct[] spheres)
		{
			_sphereList.Clear();
			_sphereList.AddRange(spheres);
			_sphereBuffer.Dispose();
			_sphereBuffer = new ComputeBuffer<SphereStruct>(_commandQueue.Context, ComputeMemoryFlags.CopyHostPointer, spheres);
			_commandQueue.AddBarrier();
		}

		public ComputeBuffer<SphereStruct> getBuffer()
		{
			return _sphereBuffer;
		}

		public int getCount()
		{
			return _sphereList.Count;
		}
	}
}
