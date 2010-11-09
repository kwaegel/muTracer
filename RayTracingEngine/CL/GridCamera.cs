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

	class GridCamera : CLCamera
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


		// Debugging buffers. Used to get data out of the kernel.
		private static Pixel _debugPixel = new Pixel(150, 250);
		private static readonly int _debugSetLength = 9;
		private static readonly int _debugSetCount = 10;
		private float4[] _debugValues;
		private ComputeBuffer<float4> _debugBuffer;



		public GridCamera(Rectangle clientBounds, ComputeCommandQueue commandQueue)
			: base(clientBounds, commandQueue, MuxEngine.LinearAlgebra.Matrix4.Identity)
		{
			debugInit(commandQueue);
		}

		public GridCamera(Rectangle clientBounds, ComputeCommandQueue commandQueue, MuxEngine.LinearAlgebra.Matrix4 transform)
			: base(clientBounds, commandQueue, transform)
		{
			debugInit(commandQueue);
		}

		public GridCamera(Rectangle clientBounds, ComputeCommandQueue commandQueue, 
			Vector3 forward, Vector3 up, Vector3 position)
			: base (clientBounds, commandQueue, forward, up, position)
		{
			debugInit(commandQueue);
		}

		private void debugInit(ComputeCommandQueue commandQueue)
		{
			_debugValues = new float4[_debugSetLength * _debugSetLength];
			_debugBuffer = new ComputeBuffer<float4>(commandQueue.Context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.CopyHostPointer, _debugValues);
		}

		protected override void buildOpenCLProgram()
		{
			// Load the OpenCL clSource code
			StreamReader sourceReader = new StreamReader("CL/VoxelTraversal.cl");
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

		public void render(VoxelGrid grid, float time)
		{
			// Compute new view matrix.
			computeView();

			// Raytrace the scene and render to a texture
			renderSceneToTexture(grid, time);

			// Draw the texture to the screen.
			drawTextureToScreen();
		}

		private void renderSceneToTexture(VoxelGrid voxelGrid, float time)
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
			long[] localWorkSize = new long[] { 8,8 };

			float cellSize = voxelGrid.CellSize;

			// Set kernel arguments.
			_renderKernel.SetValueArgument<Vector4>(0, homogeneousPosition);
			_renderKernel.SetValueArgument<Matrix4>(1, _screenToWorldMatrix);
			_renderKernel.SetValueArgument<Color4>(2, Color4.CornflowerBlue);
			_renderKernel.SetMemoryArgument(3, _renderTarget);
			// Voxel grid arguments
			_renderKernel.SetMemoryArgument(4, voxelGrid._voxelGrid, false);
			_renderKernel.SetValueArgument<float>(5, cellSize);
			// Pass in array of primitives
			_renderKernel.SetMemoryArgument(6, voxelGrid.Geometry);
			_renderKernel.SetValueArgument<int>(7, voxelGrid.VectorsPerVoxel);
			// Pass in lights
			_renderKernel.SetMemoryArgument(8, voxelGrid.PointLights);
			_renderKernel.SetValueArgument<int>(9, voxelGrid.PointLightCount);
			_renderKernel.SetLocalArgument(10, voxelGrid.PointLightCount * 4);
			// Pass in debug arrays
			_renderKernel.SetMemoryArgument(11, _debugBuffer, false);
			_renderKernel.SetValueArgument<int>(12, _debugSetCount);
			_renderKernel.SetValueArgument<Pixel>(13, _debugPixel);

			// Add render task to the device queue.
			_commandQueue.Execute(_renderKernel, null, globalWorkSize, localWorkSize, null);

			// Release OpenGL objects and block until calls are finished.
			_commandQueue.ReleaseGLObjects(_sharedObjects, null);
			_commandQueue.Finish();

			// Print debug information from kernel call.
			//_commandQueue.ReadFromBuffer<float4>(_debugBuffer, ref _debugValues, true, null);
			//unpackDebugValues(_debugValues);
			//System.Diagnostics.Trace.WriteLine("");
		}

		/// <summary>
		/// float4 rayOrigin;
		/// float4 rayDirection;
		/// float4 gridSpaceCoordinates;
		/// float4 frac;
		/// float4 tMax;
		/// float4 tDelta;
		/// float4 cellData;
		/// </summary>
		/// <param name="debugValues"></param>
		private void unpackDebugValues(float4[] debugValues)
		{
			int debugSets = debugValues.Length % _debugSetLength;

			System.Diagnostics.Trace.WriteLine("Constant data");
			System.Diagnostics.Trace.WriteLine("\tray origin: " + debugValues[0]);
			System.Diagnostics.Trace.WriteLine("\tray direction: " + debugValues[1]);
			System.Diagnostics.Trace.WriteLine("\tGridSpace coords: " + debugValues[2]);
			System.Diagnostics.Trace.WriteLine("\ttDelta: " + debugValues[5]);
			System.Diagnostics.Trace.WriteLine("\tstep direction: " + debugValues[8]);
			System.Diagnostics.Trace.WriteLine("");

			for (int setBase = 0; setBase < debugValues.Length; setBase += _debugSetLength)
			{
				int debugSetIndex = setBase / _debugSetLength;
				System.Diagnostics.Trace.WriteLine("Debug step " + debugSetIndex);
				System.Diagnostics.Trace.WriteLine("\tfrac: " + debugValues[setBase+3]);
				System.Diagnostics.Trace.WriteLine("\ttMax: " + debugValues[setBase+4]);
				System.Diagnostics.Trace.WriteLine("\tcellData: " + debugValues[setBase + 6]);
				System.Diagnostics.Trace.WriteLine("\tindex: " + debugValues[setBase + 7]);
				System.Diagnostics.Trace.WriteLine("");

				if (debugValues[setBase + 6] != Vector4.Zero)
				{
					break;	// assume the ray terminated and there is no more data to print.
				}
			}
			System.Diagnostics.Trace.WriteLine("");
		}

	}
}
