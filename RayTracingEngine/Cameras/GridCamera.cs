using System;
using System.Drawing;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Cloo;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using Raytracing.Primitives;
using Raytracing.SceneStructures;

using float4 = OpenTK.Vector4;

namespace Raytracing.CL
{
	public class GridCamera : ClTextureCamera
	{
        private readonly string[] _gridSourcePaths = { "gpuScripts/clDataStructs.cl", 
														"gpuScripts/clMathHelper.cl", 
														"gpuScripts/clIntersectionTests.cl",
														"gpuScripts/rayHelper.cl",
														"gpuScripts/VoxelTraversalTris.cl" };

		public GridCamera(Rectangle clientBounds, ComputeCommandQueue commandQueue)
			: base(clientBounds, commandQueue, MuxEngine.LinearAlgebra.Matrix4.Identity)
		{
			Init();
		}

		public GridCamera(Rectangle clientBounds, ComputeCommandQueue commandQueue, MuxEngine.LinearAlgebra.Matrix4 transform)
			: base(clientBounds, commandQueue, transform)
		{
			Init();
		}

		public GridCamera(Rectangle clientBounds, ComputeCommandQueue commandQueue, 
			Vector3 forward, Vector3 up, Vector3 position)
			: base (clientBounds, commandQueue, forward, up, position)
		{
			Init();
		}

		private void Init()
		{
			// Copy our source to the base class to be compiled
			CLSourcePaths = _gridSourcePaths;
			buildOpenCLProgram();
		}

        public override void Dispose()
        {
            base.Dispose();
        }

        // Version to be used with a scene object.
        internal void renderSceneToTexture(VoxelGrid voxelGrid, MaterialCache matCache, Color4 backgroundColor)
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

            float cellSize = voxelGrid.CellSize;

            // Set kernel arguments.
            int argi = 0;
            _renderKernel.SetValueArgument<Vector4>(argi++, homogeneousPosition);
            _renderKernel.SetValueArgument<Matrix4>(argi++, _screenToWorldMatrix);

            _renderKernel.SetValueArgument<Color4>(argi++, backgroundColor);	// Frame background color.
            _renderKernel.SetMemoryArgument(argi++, _renderTarget);				// Image to render to

            // Voxel grid arguments
            _renderKernel.SetMemoryArgument(argi++, voxelGrid._grid, false);
            _renderKernel.SetValueArgument<float>(argi++, cellSize);

            // Pass in array of primitives
            _renderKernel.SetMemoryArgument(argi++, voxelGrid.Geometry);
            _renderKernel.SetValueArgument<int>(argi++, voxelGrid.VectorsPerVoxel);

            // Pass in lights
            _renderKernel.SetMemoryArgument(argi++, voxelGrid.PointLights);
            _renderKernel.SetValueArgument<int>(argi++, voxelGrid.PointLightCount);

            // Pass in materials
            _renderKernel.SetMemoryArgument(argi++, matCache.Buffer);

            // Add render task to the device queue.
            _commandQueue.Execute(_renderKernel, null, globalWorkSize, localWorkSize, null);

            // Enqueue releasing OpenGL objects and block until calls are finished.
            _commandQueue.ReleaseGLObjects(_sharedObjects, null);
            _commandQueue.Finish();
        }
	}
}
