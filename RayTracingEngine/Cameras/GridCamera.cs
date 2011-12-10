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
        private readonly string[] _sourcePaths = { "gpuScripts/clDataStructs.cl", "gpuScripts/clMathHelper.cl", "gpuScripts/clIntersectionTests.cl", "gpuScripts/VoxelTraversalTris.cl" };

		public GridCamera(Rectangle clientBounds, ComputeCommandQueue commandQueue)
			: base(clientBounds, commandQueue, MuxEngine.LinearAlgebra.Matrix4.Identity)
		{
		}

		public GridCamera(Rectangle clientBounds, ComputeCommandQueue commandQueue, MuxEngine.LinearAlgebra.Matrix4 transform)
			: base(clientBounds, commandQueue, transform)
		{
		}

		public GridCamera(Rectangle clientBounds, ComputeCommandQueue commandQueue, 
			Vector3 forward, Vector3 up, Vector3 position)
			: base (clientBounds, commandQueue, forward, up, position)
		{
		}

        public override void Dispose()
        {

            base.Dispose();
        }

		protected override void buildOpenCLProgram()
		{
            String[] sourceArray = new String[_sourcePaths.Length];
            try
            {
                
                for (int i = 0; i < _sourcePaths.Length; i++)
                {
                    StreamReader sourceReader = new StreamReader(_sourcePaths[i]);
                    sourceArray[i] = sourceReader.ReadToEnd();
                }
            }
            catch (FileNotFoundException e)
            {
                System.Diagnostics.Trace.Write("Can't find: " + e.FileName + "\n");
                Environment.Exit(-1);
            }

			// Build and compile the OpenCL program
			_renderKernel = null;
			_renderProgram = new ComputeProgram(_commandQueue.Context, sourceArray);
			try
			{
				// build the program
                _renderProgram.Build(null, "-cl-nv-verbose", null, IntPtr.Zero);

				// create a reference a kernel function
				_renderKernel = _renderProgram.CreateKernel("render");
			}
			catch (BuildProgramFailureComputeException)
			{
                printBuildLog();

                Environment.Exit(-1);
			}
			catch (InvalidBuildOptionsComputeException)
			{
                printBuildLog();

                Environment.Exit(-1);
			}
			catch (InvalidBinaryComputeException)
			{
                printBuildLog();

                Environment.Exit(-1);
			}
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
