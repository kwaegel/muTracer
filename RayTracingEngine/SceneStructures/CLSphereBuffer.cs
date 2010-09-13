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
			}
		}

		/// <summary>
		/// Not working yet.
		/// </summary>
		unsafe public void sendDataToDevice()
		{
			fillBuffer(_sphereList.ToArray());

			// Modifying an existing buffer is not working yet. Access violation exception.
			// Send data to device and block until finished.
			//SphereStruct[] spheres = _sphereList.ToArray();
			//_commandQueue.WriteToBuffer<SphereStruct>(_sphereList.ToArray(), _sphereBuffer, true, null);
			//_commandQueue.AddBarrier();
		}

		private void fillBuffer(SphereStruct[] spheres)
		{
			_sphereList.Clear();
			_sphereList.AddRange(spheres);
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
