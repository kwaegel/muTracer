using System;
using System.Drawing;
using System.IO;
using System.Collections.Generic;


using Cloo;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using Raytracing.Primitives;
using Raytracing.SceneStructures;

namespace Raytracing.CL
{
	class GridCamera : CLCamera
	{

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

		protected override void buildOpenCLProgram()
		{
			// Load the OpenCL clSource code
			StreamReader sourceReader = new StreamReader("CL/clCameraCode.cl");
			String clSource = sourceReader.ReadToEnd();

			// Build and compile the OpenCL program
			_renderKernel = null;
			_renderProgram = new ComputeProgram(_commandQueue.Context, clSource);
			try
			{
				// build the program
				_renderProgram.Build(null, null, null, IntPtr.Zero);

				// create a reference a kernel function
				_renderKernel = _renderProgram.CreateKernel("render");
			}
			catch (BuildProgramFailureComputeException)
			{
				String buildLog = _renderProgram.GetBuildLog(_commandQueue.Device);
				System.Diagnostics.Trace.WriteLine(buildLog);

				// Unable to handle error. Terminate application.
				Environment.Exit(-1);
			}
			catch (InvalidBuildOptionsComputeException)
			{
				String buildLog = _renderProgram.GetBuildLog(_commandQueue.Device);
				System.Diagnostics.Trace.WriteLine(buildLog);

				// Unable to handle error. Terminate application.
				Environment.Exit(-1);
			}
		}

		protected override void renderSceneToTexture(CLSphereBuffer sphereBuffer, float time)
		{
			// Aquire lock on OpenGL objects.
			GL.Finish();
			_commandQueue.AcquireGLObjects(_sharedObjects, null);

			// Convert the camera position to homogeneous coordinates.
			Vector4 homogeneousPosition = new Vector4(Position, 1);

			// Set kernel arguments.
			_renderKernel.SetValueArgument<Vector4>(0, homogeneousPosition);
			_renderKernel.SetValueArgument<Matrix4>(1, _screenToWorldMatrix);
			_renderKernel.SetValueArgument<Color4>(2, Color4.DarkBlue);
			_renderKernel.SetMemoryArgument(3, _renderTarget);
			_renderKernel.SetMemoryArgument(4, sphereBuffer.getBuffer());
			_renderKernel.SetValueArgument<int>(5, sphereBuffer.getCount());

			// Add render task to the device queue.
			_commandQueue.Execute(_renderKernel, null, new long[] { ClientBounds.Width, ClientBounds.Height }, null, null);

			// Release OpenGL objects.
			_commandQueue.ReleaseGLObjects(_sharedObjects, null);
			_commandQueue.Finish();
		}
	}
}
