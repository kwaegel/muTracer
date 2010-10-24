using System;
using System.Collections.Generic;
using System.Text;

using OpenTK;

namespace Raytracing.BoundingVolumes
{
	public class AxisAlignedBoundingBox
	{
		public Vector3 Min;
		public Vector3 Max;

		public AxisAlignedBoundingBox(Vector3 min, Vector3 max)
		{
			Min = min;
			Max = max;
		}


	}
}
