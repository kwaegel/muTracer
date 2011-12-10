using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Raytracing.CL
{
	class GpuBvhCamera : ClTextureCamera
	{
		private readonly string[] _sourcePaths = { "gpuScripts/clDataStructs.cl", "gpuScripts/clMathHelper.cl", "gpuScripts/clIntersectionTests.cl", "gpuScripts/VoxelTraversalTris.cl" };


	}
}
