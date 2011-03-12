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
	[StructLayout(LayoutKind.Sequential, Pack = 16)]
	unsafe public struct DebugStruct
	{
		public float4 rayOrigin;
		public float4 rayDirection;
		public float4 gridSpaceCoordinates;
		public float4 frac;
		public float4 tMax;
		public float4 tDelta;
		public Color4 cellData;
	}

	public abstract class ClTextureCamera : MuxEngine.Movables.Camera, IDisposable
	{

#region Fields

		protected Matrix4 _screenToWorldMatrix;

		// list of shared objects. Needed for OpenCL, OpenGL interop.
		protected List<ComputeMemory> _sharedObjects = new System.Collections.Generic.List<ComputeMemory>();

		// Queue for OpenCL commands
		protected ComputeCommandQueue _commandQueue;

		// OpenGL texture to render on
		protected int _renderTextureID;		

		// OpenCL shared texture handle
		protected ComputeImage2D _renderTarget;

		protected ComputeProgram _renderProgram;
		protected ComputeKernel _renderKernel;

#endregion

#region Initialization

		public ClTextureCamera(Rectangle clientBounds, ComputeCommandQueue commandQueue)
			: this(clientBounds, commandQueue, MuxEngine.LinearAlgebra.Matrix4.Identity)
		{
		}

		public ClTextureCamera(Rectangle clientBounds, ComputeCommandQueue commandQueue, MuxEngine.LinearAlgebra.Matrix4 transform)
			: base(clientBounds, transform)
		{
			rayTracingInit(commandQueue);
		}

		public ClTextureCamera(Rectangle clientBounds, ComputeCommandQueue commandQueue, 
			Vector3 forward, Vector3 up, Vector3 position)
			: base (clientBounds, forward, up, position)
		{
			rayTracingInit(commandQueue);
		}

		private void rayTracingInit(ComputeCommandQueue commandQueue)
		{
			_commandQueue = commandQueue;

			createSharedTexture();

			buildOpenCLProgram();
		}

		private void createSharedTexture()
		{
			// create a texture to render to.
			_renderTextureID = createBlankTexture(ClientBounds);

			// Create OpenCL image for kernel to write to
			_renderTarget = ComputeImage2D.CreateFromGLTexture2D(_commandQueue.Context, ComputeMemoryFlags.ReadWrite, (int)TextureTarget.Texture2D, 0, _renderTextureID);
			_sharedObjects.Add(_renderTarget);
		}

		protected virtual void buildOpenCLProgram()
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
			catch (BuildProgramFailureComputeException e)
			{
				String buildLog = _renderProgram.GetBuildLog(_commandQueue.Device);
				System.Diagnostics.Trace.WriteLine(buildLog);
				throw e;
			}
			catch (InvalidBuildOptionsComputeException e)
			{
				String buildLog = _renderProgram.GetBuildLog(_commandQueue.Device);
				System.Diagnostics.Trace.WriteLine(buildLog);
				throw e;
			}
            catch (InvalidBinaryComputeException e)
            {
                String buildLog = _renderProgram.GetBuildLog(_commandQueue.Device);
                System.Diagnostics.Trace.WriteLine(buildLog);
				throw e;
            }
		}


		/// <summary>
		/// Create a blank texture with a specified size.
		/// </summary>
		/// <returns></returns>
		private int createBlankTexture(Rectangle size)
		{
			int width = size.Width;
			int height = size.Height;

			// create a test texture from the bitmap
			int textureID = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, textureID);

			// Allocate space for texture with undefined resultVector.
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, width, height, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);

			// These are needed to disable mipmapping.
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

			return textureID;
		}

#endregion

		public virtual void Dispose()
		{
			_renderTarget.Dispose();
			_renderKernel.Dispose();
			_renderProgram.Dispose();
		}

        /// <summary>
        /// Print the device build log to the Trace stream.
        /// </summary>
        protected void printBuildLog()
        {
            String buildLog = _renderProgram.GetBuildLog(_commandQueue.Device);
            System.Diagnostics.Trace.WriteLine("\n********** Build Log **********\n" + buildLog + "\n*************************");
        }

#region Render

		private Vector2 computeWorldWindowSize(float verticalfieldOfView)
		{
			float vFOV = MathHelper.DegreesToRadians(verticalfieldOfView);

			// calculate horzFOV from vertFOV
			float hFOV = GetHorizontalFov(vFOV, AspectRatio);

			float width = (float) (2 * NearPlane * System.Math.Tan(hFOV / 2.0));
			float height = (float) (2 * NearPlane * System.Math.Tan(vFOV / 2.0));

			return new Vector2(width, height);
		}

		// this code from http://forums.xna.com/forums/p/48859/293285.aspx#293285
		private float GetHorizontalFov(float verticalFov, float aspectRatio)
		{
			return (float)System.Math.Atan(aspectRatio * (float)System.Math.Tan(verticalFov / 2)) * 2;
		}

		/// <summary>
		/// This must be called before drawing if the camera has been moved or rotated.
		/// </summary>
		public override void computeView()
		{
			base.computeView();

			calculateScreenToWorldMatrix();
		}


		// Only computes if FOV, aspect ratio, or near/far planes have changed
		public new void computeProjection()
		{
			base.computeProjection();
		}


		// this recalculates the screen to world matrix required for creating viweing rays.
		private void calculateScreenToWorldMatrix()
		{
			// Calculace the matrix needed to unproject pixels.
			// View matrix will change for every camera movement.
			_screenToWorldMatrix = Matrix4.Invert(View * Projection);
		}


		public Matrix4 getScreenToWorldMatrix()
		{
			return _screenToWorldMatrix;
		}

		public void render()
		{
			this.computeView();

			// Raytrace the scene and render it to a textured quad.
			renderSceneToTexture();

			// Draw the texture to the screen using OpenGL calls.
			drawTextureToScreen();
		}

		protected abstract void renderSceneToTexture();

		/// <summary>
		/// Draw the texture that OpenCL renders into using a full-viewport quad. Drawn 
		/// at z=1 so it is behind all other elements.
		/// </summary>
		internal void drawTextureToScreen()
		{
			GL.Color4(Color4.Transparent);		// No blend Color.

			GL.MatrixMode(MatrixMode.Modelview);
			GL.PushMatrix();
			GL.LoadIdentity();
			GL.MatrixMode(MatrixMode.Projection);
			GL.PushMatrix();
			GL.LoadIdentity();

			GL.Enable(EnableCap.Texture2D);
			GL.BindTexture(TextureTarget.Texture2D, _renderTextureID);

            // TODO: this is absurd. Need a way to write directly to the back buffer.
			GL.Begin(BeginMode.Quads);

				GL.TexCoord2(0, 0);
				GL.Vertex3(-1, -1, 0.9999f);

				GL.TexCoord2(1, 0);
				GL.Vertex3(1, -1, 0.9999f);

				GL.TexCoord2(1, 1);
				GL.Vertex3(1, 1, 0.9999f);


				GL.TexCoord2(0, 1);
				GL.Vertex3(-1, 1, 0.9999f);

			GL.End();
			GL.Disable(EnableCap.Texture2D);

			GL.PopMatrix();
			GL.MatrixMode(MatrixMode.Modelview);
			GL.PopMatrix();
		}

#endregion

	}
}
