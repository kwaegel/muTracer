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

namespace Raytracing
{
	class CLCamera : MuxEngine.Movables.Camera
	{

		#region Fields

		Matrix4 _oldView;
		Matrix4 _screenToWorldMatrix;

		// list of shared objects. Needed for OpenCL, OpenGL interop.
		List<ComputeMemory> _sharedObjects = new System.Collections.Generic.List<ComputeMemory>();

		// Queue for OpenCL commands
		ComputeCommandQueue _commandQueue;

		// OpenGL texture to render on
		int _renderTextureID;		

		// OpenCL shared texture handle
		ComputeImage2D _renderTarget;

		ComputeProgram _clProgram;
		ComputeKernel _renderKernel;
		ComputeKernel _testKernel;

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

		private void buildOpenCLProgram()
		{
			// Load the OpenCL clSource code
			//StreamReader sourceReader = new StreamReader("CycleColors.cl");
			StreamReader sourceReader = new StreamReader("clCameraCode.cl");
			String clSource = sourceReader.ReadToEnd();

			// Build and compile the OpenCL program
			_renderKernel = null;
			_clProgram = new ComputeProgram(_commandQueue.Context, clSource);
			try
			{
				// build the program
				_clProgram.Build(null, null, null, IntPtr.Zero);

				// create a reference a kernel function
				//_renderKernel = _clProgram.CreateKernel("cycleColors");
				_renderKernel = _clProgram.CreateKernel("render");

				_testKernel = _clProgram.CreateKernel("hostTransform");
			}
			catch (BuildProgramFailureComputeException)
			{
				String buildLog = _clProgram.GetBuildLog(_commandQueue.Device);
				System.Diagnostics.Trace.WriteLine(buildLog);

				// Unable to handle error. Terminate application.
				Environment.Exit(-1);
			}
			catch (InvalidBuildOptionsComputeException)
			{
				String buildLog = _clProgram.GetBuildLog(_commandQueue.Device);
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

			// Allocate space for texture with undefined resultData.
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, width, height, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);

			// These are needed to disable mipmapping.
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

			return textureID;
		}

		#endregion

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


		public void render(Scene scene, float time)
		{
			// Raytrace the scene and render to a texture
			renderSceneToTexture(time);

			// Draw the texture to the screen.
			drawTextureToScreen();
		}

		private void renderSceneToTexture(float time)
		{
			// aquire openGL objects
			GL.Finish();
			_commandQueue.AcquireGLObjects(_sharedObjects, null);

			Vector4 homogeneousPosition = new Vector4(Position, 1);


			Vector4 testVector = new Vector4(1, 1, 1, 1);
			Matrix4 translationMatrix = Matrix4.CreateTranslation(new Vector3(1, 1, 1));
			System.Diagnostics.Trace.WriteLine(translationMatrix.ToString());

			float[] resultData = new float[4];
			ComputeBuffer<float> resultBuffer = new ComputeBuffer<float>(_commandQueue.Context, ComputeMemoryFlags.UseHostPointer, resultData);

			_testKernel.SetValueArgument<Matrix4>(0, translationMatrix);
			_testKernel.SetValueArgument<Vector4>(1, testVector);
			_testKernel.SetMemoryArgument(2, resultBuffer);
			_commandQueue.Execute(_testKernel, null, new long[] { 1 }, null, null);
			_commandQueue.ReadFromBuffer<float>(resultBuffer, ref resultData, true, 0, 0, 4, null);

			resultBuffer.Dispose();


			System.Diagnostics.Trace.WriteLine(resultData.ToString());

			System.Diagnostics.Trace.WriteLine(homogeneousPosition);

			_renderKernel.SetValueArgument<Vector4>(0, homogeneousPosition);
			_renderKernel.SetValueArgument<Matrix4>(1, _screenToWorldMatrix);
			_renderKernel.SetMemoryArgument(2, _renderTarget);


			//_renderKernel.SetValueArgument<float>(0, time);	// test value
			//_renderKernel.SetValueArgument<Color4>(1, Color4.DarkBlue);
			//_renderKernel.SetMemoryArgument(2, _renderTarget);
			
			_commandQueue.Execute(_renderKernel, null, new long[] { ClientBounds.Width, ClientBounds.Height }, null, null);

			// release openGL objects
			_commandQueue.ReleaseGLObjects(_sharedObjects, null);
			_commandQueue.Finish();
		}

		/// <summary>
		/// Draw the texture that OpenCL renders into using a full-viewport quad. Drawn 
		/// at z=1 so it is behind all other elements.
		/// </summary>
		private void drawTextureToScreen()
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
			// Texture has (0,0) at upper left
			// OpenGL has (0,0) at lower left.
			// Invert second coordinate to compensate.

			GL.TexCoord2(0, 0);
			GL.Vertex3(-1, 1, 0.999);

			GL.TexCoord2(1, 0);
			GL.Vertex3(1, 1, 0.999);

			GL.TexCoord2(1, 1);
			GL.Vertex3(1, -1, 0.999);


			GL.TexCoord2(0, 1);
			GL.Vertex3(-1, -1, 0.999);
			GL.End();
			GL.Disable(EnableCap.Texture2D);

			GL.PopMatrix();
			GL.MatrixMode(MatrixMode.Modelview);
			GL.PopMatrix();
		}
	}
}
