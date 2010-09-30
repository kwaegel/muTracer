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
	class CLCamera : MuxEngine.Movables.Camera
	{

#region Fields

		Matrix4 _oldView;
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

		public CLCamera(Rectangle clientBounds, ComputeCommandQueue commandQueue)
			: this(clientBounds, commandQueue, MuxEngine.LinearAlgebra.Matrix4.Identity)
		{
		}

		public CLCamera(Rectangle clientBounds, ComputeCommandQueue commandQueue, MuxEngine.LinearAlgebra.Matrix4 transform)
			: base(clientBounds, transform)
		{
			rayTracingInit(commandQueue);
		}

		public CLCamera(Rectangle clientBounds, ComputeCommandQueue commandQueue, 
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

		public void Dispose()
		{
			_renderTarget.Dispose();
			_renderKernel.Dispose();
			_renderProgram.Dispose();
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

			// only calculate new rays if the view has changed
			if (_oldView == null || !_oldView.Equals(View))
			{
				_oldView = View;

				calculateScreenToWorldMatrix();
			}
		}


		// Only computes if FOV, aspect ratio, or near/far planes have changed
		public new void computeProjection()
		{
			base.computeProjection();

			_oldView = new Matrix4();
		}


		// this recalculates the screen to world matrix required for creating viweing rays.
		private void calculateScreenToWorldMatrix()
		{
			// calculace the matrix needed to unproject pixels
			// view matrix will change for every camera movement
			Matrix4 vp = View * Projection;
			_screenToWorldMatrix = Matrix4.Invert(vp);
		}


		public Matrix4 getScreenToWorldMatrix()
		{
			return _screenToWorldMatrix;
		}

		public void render(CLSphereBuffer sphereBuffer, float time)
		{
			// Raytrace the scene and render to a texture
			renderSceneToTexture(sphereBuffer, time);

			// Draw the texture to the screen.
			drawTextureToScreen();
		}

		protected virtual void renderSceneToTexture(CLSphereBuffer sphereBuffer, float time)
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

		/// <summary>
		/// Draw the texture that OpenCL renders into using a full-viewport quad. Drawn 
		/// at z=1 so it is behind all other elements.
		/// </summary>
		protected void drawTextureToScreen()
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
