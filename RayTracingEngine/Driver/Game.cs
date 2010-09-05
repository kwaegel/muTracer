// Released to the public domain. Use, modify and relicense at will.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Mono.Simd;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using OpenTK.Input;

using Cloo;

using Raytracing.SceneStructures;
using Raytracing.Primitives;

namespace Raytracing.Driver
{
    class Game : GameWindow
    {
		

#if DEBUG
		private static readonly bool limitFrames = true;
#else
		private static readonly bool limitFrames = false;
#endif
		private int _frameLimit = 500;

		#region Constants

		public static readonly Vector3 Forward = -Vector3.UnitZ;
		public static readonly Vector3 Backward = Vector3.UnitZ;
		public static readonly Vector3 Left = -Vector3.UnitX;
		public static readonly Vector3 Right = Vector3.UnitX;
		public static readonly Vector3 Up = Vector3.UnitY;
		public static readonly Vector3 Down = -Vector3.UnitY;

		private static float CameraMovementSpeed = 0.3f;	// in units
		private static float CameraRotationSpeed = 5f;	// in degrees

		#endregion

		#region Game properties

		private int _frames = 0;
		double _totalTime = 0;

		#endregion

		#region OpenCL-OpenGL properties

		// Platform invoke required for OpenCL-OpenGL interop setup.
		[DllImport("opengl32.dll")]
		extern static IntPtr wglGetCurrentDC();

		ComputeContext _context;
		IGraphicsContextInternal _glContext;
		ComputeContext _computeContext;
		ComputeCommandQueue _commandQueue;

		#endregion

		#region Cameras

		RayTracingCamera _rtCamera;
		CLCamera _clCamera;	// test camera using OpenCL

		#endregion	

		#region Scene structures
		Scene _scene;
		PointLight _light;

		#endregion

		/// <summary>Creates a window with the specified title.</summary>
        public Game()
            : base(400, 400, GraphicsMode.Default, "Raytracing tester")
        {
            VSync = VSyncMode.On;

			detectSimdSupport();
        }

		private void detectSimdSupport()
		{
			bool basic_support;
			bool enhanced_support;

			basic_support = SimdRuntime.IsMethodAccelerated (typeof (Vector4f), "op_Addition") &&
				   SimdRuntime.IsMethodAccelerated (typeof (Vector4f), "op_Multiply") &&
				   SimdRuntime.IsMethodAccelerated (typeof (VectorOperations), "Shuffle", typeof (Vector4f), typeof (ShuffleSel));
		
			enhanced_support = SimdRuntime.IsMethodAccelerated (typeof (VectorOperations), "HorizontalAdd", typeof (Vector4f), typeof (Vector4f)) &&
				   SimdRuntime.IsMethodAccelerated (typeof (Vector4f), "op_Multiply");

			System.Console.WriteLine("basic SIMD support = " + basic_support);
			System.Console.WriteLine("enhanced SIMD support = " + enhanced_support);

		}

		#region onLoad

        /// <summary>Load resources here.</summary>
        /// <param name="e">Not used.</param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

			GL.ClearColor(Color4.Black);
            GL.Enable(EnableCap.DepthTest);

			openCLSharedInit();

			// create the camera
			// looking down the Z-axis into the scene
			Vector3 cameraPosition = new Vector3(0, 0, 3f);
			Quaternion cameraRotation = Quaternion.Identity;

			_rtCamera = new RayTracingCamera(ClientRectangle, (-Vector3.UnitZ), Vector3.UnitY, cameraPosition);
			_rtCamera.VerticalFieldOfView = 70.0f;
			_rtCamera.computeProjection();
			_rtCamera.setRotation(cameraRotation);
			//_rtCamera.rotateAboutPoint(Vector3.Zero, Vector3.UnitX, 30f);

			_clCamera = new CLCamera(ClientRectangle, _commandQueue);
			//_clCamera.Position = _rtCamera.Position;
			//_clCamera.Rotation = _rtCamera.Rotation;

			// create the scene
			_scene = new GridScene(16, 1);
			_scene.BackgroundColor = Color4.Black;
			Timer.start();
			buildBlockScene(_scene);
			Timer.stop();
			//buildXYZScene(_scene);
        }

		// Create a sharde context between OpenGL and OpenCL. 
		private void openCLSharedInit()
		{
			// select OpenCL device and platform
			ComputePlatform platform = ComputePlatform.Platforms[0];
			ComputeDevice device = platform.Devices[0];


			IntPtr curDC = wglGetCurrentDC();

			_glContext = (OpenTK.Graphics.IGraphicsContextInternal)OpenTK.Graphics.GraphicsContext.CurrentContext;
			IntPtr raw_context_handle = _glContext.Context.Handle;
			ComputeContextProperty p1 = new ComputeContextProperty(ComputeContextPropertyName.CL_GL_CONTEXT_KHR, raw_context_handle);
			ComputeContextProperty p2 = new ComputeContextProperty(ComputeContextPropertyName.CL_WGL_HDC_KHR, curDC);
			ComputeContextProperty p3 = new ComputeContextProperty(ComputeContextPropertyName.Platform, platform.Handle);
			List<ComputeContextProperty> props = new List<ComputeContextProperty>() { p1, p2, p3 };
			ComputeContextPropertyList Properties = new ComputeContextPropertyList(props);

			_computeContext = new ComputeContext(ComputeDeviceTypes.Gpu, Properties, null, IntPtr.Zero);

			//Create the command queue from the context and device
			_commandQueue = new ComputeCommandQueue(_computeContext, device, ComputeCommandQueueFlags.None);
		}

		private void buildXYZScene(Scene scene)
		{
			_light = new PointLight(Vector3.Zero, 1.0f, Color4.White);
			scene.add(_light);

			// XYZ => RGB
			scene.add(new Sphere(Vector3.UnitX, 0.5f, new Material(Color4.Red)));
			scene.add(new Sphere(Vector3.UnitY, 0.5f, new Material(Color4.Green)));
			scene.add(new Sphere(Vector3.UnitZ, 0.5f, new Material(Color4.Blue)));
		}

		private void buildBlockScene(Scene scene)
		{
			int low = -7;
			int high = 7;

			for (int x = low; x <= high; x++)
			{
				for (int y = low; y <= high; y++)
				{
					for (int z = low; z <= high; z++)
					{
						if (x != 0 || y != 0 || z != 0)
						{
							Color4 c = getColor(low, high, x, y, z);
							Material mat = new Material(c, 0);
							Sphere s = new Sphere(new Vector3(x, y, z), 0.25f, mat);
							scene.add(s);
						}
						else
						{
							_light = new PointLight(Vector3.Zero, 1.0f, Color4.DarkKhaki);
							scene.add(_light);
						}
					}
				}
			}

			int width = high - low;
			int objects = (int)System.Math.Pow(width, 3);
			System.Console.WriteLine("added " + objects + " spheres.");
		}

		private Color4 getColor(int start, int end, int x, int y, int z)
		{
			float percentX = (float)(x - start) / (float)(end - start);
			float percentY = (float)(y - start) / (float)(end - start);
			float percentZ = (float)(z - start) / (float)(end - start);

			return new Color4(percentX, percentY, percentZ,0);
		}

		#endregion

		/// <summary>
        /// Called when your window is resized. Set your viewport here. It is also
        /// a good place to set up your projection matrix (which probably changes
        /// along when the aspect ratio of your window).
        /// </summary>
        /// <param name="e">Not used.</param>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

			_rtCamera.setClientBounds(ClientRectangle);
			_rtCamera.computeProjection();

			GL.MatrixMode(MatrixMode.Projection);
			GL.Viewport(0, 0, ClientRectangle.Width, ClientRectangle.Height);

			// orthographic projection
			GL.LoadIdentity();
			GL.Ortho(0, ClientRectangle.Width, 0, ClientRectangle.Height, -1, 1);

			// perspective projection
			//Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(OpenTK.MathHelper.Pi / 4, Width / (float)Height, 1.0f, 64.0f);
			//GL.LoadMatrix(ref projection);
        }

        /// <summary>
        /// Called when it is time to setup the next frame. Add you game logic here.
        /// </summary>
        /// <param name="e">Contains timing information for framerate independent logic.</param>
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
			_totalTime += e.Time;
			_frames++;
			
			// limit frames for profiling
			if (limitFrames && _frames >= _frameLimit)
			{
				base.Exit();
			}


			float x = (float)System.Math.Cos(_totalTime);
			//float y = (float)System.Math.Sin(_totalTime);
			//float z = (float)System.Math.Cos(_totalTime);

			_light.Position.X = 2 * x;
			//_light.Position.Y = 5 * y;
			//_light.Position.Z = 5 * z;

			// Allows the game to exit
			if (Keyboard[Key.Escape])
			{
				this.Exit();
			}

			if (Keyboard[Key.A])
			{
				_rtCamera.moveLocal(Left, CameraMovementSpeed);
			}
			else if (Keyboard[Key.E])
			{
				_rtCamera.moveLocal(Right, CameraMovementSpeed);
			}
			
			if (Keyboard[Key.Comma])
			{
				_rtCamera.moveLocal(Forward, CameraMovementSpeed);
			}
			else if (Keyboard[Key.O])
			{
				_rtCamera.moveLocal(Backward, CameraMovementSpeed);
			}
			
			if (Keyboard[Key.Period])
			{
				_rtCamera.moveLocal(Up, CameraMovementSpeed);
			}
			else if (Keyboard[Key.J])
			{
				_rtCamera.moveLocal(Down, CameraMovementSpeed);
			}

			if (Keyboard[Key.Keypad4])
			{
				_rtCamera.yaw(CameraRotationSpeed);
			}
			else if (Keyboard[Key.Keypad6])
			{
				_rtCamera.yaw(-CameraRotationSpeed);
			}

			if (Keyboard[Key.Keypad8])
			{
				_rtCamera.pitch(CameraRotationSpeed);
			}
			else if (Keyboard[Key.Keypad2])
			{
				_rtCamera.pitch(-CameraRotationSpeed);
			}

			if (Keyboard[Key.Keypad7])
			{
				_rtCamera.roll(-CameraRotationSpeed);
			}
			else if (Keyboard[Key.Keypad9])
			{
				_rtCamera.roll(CameraRotationSpeed);
			}

			//_rtCamera.rotateAboutPoint(Vector3.Zero, Vector3.UnitY, 2f);
			//_rtCamera.rotateAboutPoint(Vector3.Zero, Vector3.UnitX, 2f);
			//_rtCamera.rotateAboutPoint(Vector3.Zero, Vector3.UnitZ, 2f);

            if (Keyboard[Key.Escape])
                Exit();
        }

        /// <summary>
        /// Called when it is time to render the next frame. Add your rendering code here.
        /// </summary>
        /// <param name="e">Contains timing information.</param>
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

			int pixels = ClientSize.Height * ClientSize.Width;
			String pixelString = String.Format("{0:n0}", pixels);

			float fps = (float)(1.0 / e.Time);
			String fpsString = String.Format("{0:##.#}", fps);
			this.Title = "Raytracing tester (" + fpsString + " FPS, " + pixelString + " pixels)";

			// clear the screen
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			// raytrace the scene
			Timer.start();
			//_rtCamera.computeView();
			//_rtCamera.render(_scene);
			_clCamera.render(_scene, (float)_totalTime);
			Timer.stop();

			// display the new frame
            SwapBuffers();

			//// take a screenshot
			//System.Drawing.Bitmap b = null;
			//System.Drawing.Image image = System.Drawing.Image.FromHbitmap(b.GetHbitmap());
			//image.Save(null, System.Drawing.Imaging.ImageFormat.Png);
			
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            // The 'using' idiom guarantees proper resource cleanup.
            // We request 30 UpdateFrame events per second, and unlimited
            // RenderFrame events (as fast as the computer can handle).
            using (Game game = new Game())
            {
                //game.Run(30.0);	// this causes two updates per draw call
				game.Run();
            }
        }
    }
}