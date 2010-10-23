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
			// Raytrace the scene and render to a texture
			renderSceneToTexture(grid, time);

			// Draw the texture to the screen.
			drawTextureToScreen();
		}

		private void renderSceneToTexture(VoxelGrid voxelGrid, float time)
		{
			// Aquire lock on OpenGL objects.
			GL.Finish();
			_commandQueue.AcquireGLObjects(_sharedObjects, null);

			// Convert the camera position to homogeneous coordinates.
			Vector4 homogeneousPosition = new Vector4(Position, 1);

			float cellSize = voxelGrid.CellSize;

			// Set kernel arguments.
			_renderKernel.SetValueArgument<Vector4>(0, homogeneousPosition);
			_renderKernel.SetValueArgument<Matrix4>(1, _screenToWorldMatrix);
			_renderKernel.SetValueArgument<Color4>(2, Color4.White);
			_renderKernel.SetMemoryArgument(3, _renderTarget);
			_renderKernel.SetMemoryArgument(4, voxelGrid._voxelGrid, false);
			_renderKernel.SetValueArgument<float>(5, cellSize);
			_renderKernel.SetMemoryArgument(6, _debugBuffer, false);

			// Print debug information from kernel call.
			_commandQueue.ReadFromBuffer<float4>(_debugBuffer, ref _debugValues, true, null);
			unpackDebugValues(_debugValues);


			// Add render task to the device queue.
			_commandQueue.Execute(_renderKernel, null, new long[] { ClientBounds.Width, ClientBounds.Height }, null, null);

			// Release OpenGL objects.
			_commandQueue.ReleaseGLObjects(_sharedObjects, null);
			_commandQueue.Finish();
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
			int valuesPerSet = 8;
			int debugSets = debugValues.Length % valuesPerSet;

			for (int setBase = 0; setBase < debugValues.Length; setBase += valuesPerSet)
			{
				int debugSet = setBase / valuesPerSet;
				System.Diagnostics.Trace.WriteLine("Debug set " + debugSet);
				System.Diagnostics.Trace.WriteLine("\tRay Origin: " + debugValues[setBase + 0]);
				System.Diagnostics.Trace.WriteLine("\tRay Direction: " + debugValues[setBase + 1]);
				System.Diagnostics.Trace.WriteLine("\tGridSpace coords: " + debugValues[setBase + 2]);
				System.Diagnostics.Trace.WriteLine("\tfrac: " + debugValues[setBase + 3]);
				System.Diagnostics.Trace.WriteLine("\ttMax: " + debugValues[setBase + 4]);
				System.Diagnostics.Trace.WriteLine("\ttDelta: " + debugValues[setBase + 5]);
				System.Diagnostics.Trace.WriteLine("\tcellData: " + debugValues[setBase + 6]);
				System.Diagnostics.Trace.WriteLine("\tindex: " + debugValues[setBase + 7]);
			}
			System.Diagnostics.Trace.WriteLine("");
		}

		private void unpackDebugValues(DebugStruct[] debugValues)
		{
			foreach (DebugStruct ds in debugValues)
			{
				System.Diagnostics.Trace.WriteLine("Ray Origin: " + ds.rayOrigin);
				System.Diagnostics.Trace.WriteLine("Ray Direction: " + ds.rayDirection);
				System.Diagnostics.Trace.WriteLine("GridSpace coords: " + ds.gridSpaceCoordinates);
				System.Diagnostics.Trace.WriteLine("frac: " + ds.frac);
				System.Diagnostics.Trace.WriteLine("tMax: " + ds.tMax);
				System.Diagnostics.Trace.WriteLine("tDelta: " + ds.tDelta);
				System.Diagnostics.Trace.WriteLine("cellData: " + ds.cellData);
				System.Diagnostics.Trace.WriteLine("");
			}
			System.Diagnostics.Trace.WriteLine("");
		}

	}
}
