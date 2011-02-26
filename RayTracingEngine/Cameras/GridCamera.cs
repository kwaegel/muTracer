﻿using System;
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

		[StructLayout(LayoutKind.Sequential)]
		public struct Pixel
		{
			public int x;
			public int y;

			public Pixel(int x, int y)
			{
				this.x = x;
				this.y = y;
			}
		}

        private VoxelGrid _voxelGrid;


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

        public void setScene(VoxelGrid grid)
        {
            _voxelGrid = grid;
        }

		protected override void buildOpenCLProgram()
		{
			// Load the OpenCL clSource code
			StreamReader sourceReader = new StreamReader("Cameras/clScripts/VoxelTraversal.cl");
			String clSource = sourceReader.ReadToEnd();

			// Build and compile the OpenCL program
			_renderKernel = null;
			_renderProgram = new ComputeProgram(_commandQueue.Context, clSource);
			try
			{
				// build the program
                _renderProgram.Build(null, "-cl-nv-verbose", null, IntPtr.Zero);

                //printBuildLog();

				// create a reference a kernel function
				_renderKernel = _renderProgram.CreateKernel("render");
			}
			catch (BuildProgramFailureComputeException e)
			{
                printBuildLog();

				throw e;
			}
			catch (InvalidBuildOptionsComputeException e)
			{
                printBuildLog();

				throw e;
			}
			catch (InvalidBinaryComputeException e)
			{
                printBuildLog();
				throw e;
			}
		}

		protected override void renderSceneToTexture()
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

			float cellSize = _voxelGrid.CellSize;

			// Set kernel arguments.
			int argi = 0;
			_renderKernel.SetValueArgument<Vector4>(argi++, homogeneousPosition);
			_renderKernel.SetValueArgument<Matrix4>(argi++, _screenToWorldMatrix);

			_renderKernel.SetValueArgument<Color4>(argi++, Color4.CornflowerBlue);  // Frame background color.
			_renderKernel.SetMemoryArgument(argi++, _renderTarget);                 // Image to render to

			// Voxel grid arguments
			_renderKernel.SetMemoryArgument(argi++, _voxelGrid._voxelGrid, false);
			_renderKernel.SetValueArgument<float>(argi++, cellSize);

			// Pass in array of primitives
			_renderKernel.SetMemoryArgument(argi++, _voxelGrid.Geometry);
			_renderKernel.SetValueArgument<int>(argi++, _voxelGrid.VectorsPerVoxel);

			// Pass in lights
			_renderKernel.SetMemoryArgument(argi++, _voxelGrid.PointLights);
			_renderKernel.SetValueArgument<int>(argi++, _voxelGrid.PointLightCount);

			// Add render task to the device queue.
			_commandQueue.Execute(_renderKernel, null, globalWorkSize, localWorkSize, null);

			// Enqueue releasing OpenGL objects and block until calls are finished.
			_commandQueue.ReleaseGLObjects(_sharedObjects, null);
            _commandQueue.Finish();
		}

	}
}
