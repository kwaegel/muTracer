using System;
using System.Drawing;

using Cloo;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using Raytracing.Primitives;
using Raytracing.SceneStructures;

using float4 = OpenTK.Vector4;

namespace Raytracing.CL
{
	public class GpuBvhCamera : ClTextureCamera
	{
		private readonly string[] _bvhSourcePaths = { "gpuScripts/clDataStructs.cl", 
													 "gpuScripts/clMathHelper.cl", 
													 "gpuScripts/clIntersectionTests.cl", 
													 "gpuScripts/rayHelper.cl", 
													 "gpuScripts/BvhTraversal.cl" };

		public GpuBvhCamera(Rectangle clientBounds, ComputeCommandQueue commandQueue)
			: base(clientBounds, commandQueue, MuxEngine.LinearAlgebra.Matrix4.Identity)
		{
			Init();
		}

		public GpuBvhCamera(Rectangle clientBounds, ComputeCommandQueue commandQueue, MuxEngine.LinearAlgebra.Matrix4 transform)
			: base(clientBounds, commandQueue, transform)
		{
			Init();
		}

		public GpuBvhCamera(Rectangle clientBounds, ComputeCommandQueue commandQueue, 
			Vector3 forward, Vector3 up, Vector3 position)
			: base (clientBounds, commandQueue, forward, up, position)
		{
			Init();
		}

		private void Init()
		{
			// Copy our source to the base class to be compiled
			CLSourcePaths = _bvhSourcePaths;
			buildOpenCLProgram();
		}

        public override void Dispose()
        {
            base.Dispose();
        }

		// Version to be used with a scene object.
		internal void renderSceneToTexture(GpuBvhTree scene, MaterialCache matCache, Color4 backgroundColor)
		{
			// Switch viewport to camera client bounds
			GL.Viewport(ClientBounds);

			// Aquire lock on OpenGL objects.
			GL.Finish();
			_commandQueue.AcquireGLObjects(_sharedObjects, null);

			// Convert the camera position to homogeneous coordinates.
			Vector4 homogeneousPosition = new Vector4(Position, 1);

			// pick work group sizes;
			long[] globalWorkSize = new long[] { ClientBounds.Width, ClientBounds.Height };
			long[] localWorkSize = new long[] { 8, 8 };


			// Set kernel arguments.
			int argi = 0;
			_renderKernel.SetValueArgument<Vector4>(argi++, homogeneousPosition);
			_renderKernel.SetValueArgument<Matrix4>(argi++, _screenToWorldMatrix);

			_renderKernel.SetValueArgument<Color4>(argi++, backgroundColor);	// Frame background color.
			_renderKernel.SetMemoryArgument(argi++, _renderTarget);				// Image to render to

			// BVH data
			_renderKernel.SetMemoryArgument(argi++, scene.BvhNodeBuffer);

			// Primitives
			_renderKernel.SetMemoryArgument(argi++, scene.Geometry);

			// Materials
			_renderKernel.SetMemoryArgument(argi++, matCache.Buffer);

			// Lights
			_renderKernel.SetMemoryArgument(argi++, scene.PointLightBuffer);
			_renderKernel.SetValueArgument<int>(argi++, scene.PointLightCount);

			// Add render task to the device queue.
			_commandQueue.Execute(_renderKernel, null, globalWorkSize, localWorkSize, null);

			// Enqueue releasing OpenGL objects and block until calls are finished.
			_commandQueue.ReleaseGLObjects(_sharedObjects, null);
			_commandQueue.Finish();
			
		}
	}
}
