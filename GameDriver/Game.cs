// Released to the public domain. Use, modify and relicense at will.

using System;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using OpenTK.Input;

using Cloo;

using Raytracing.SceneStructures;
using Raytracing.Primitives;
using Raytracing.CL;

namespace Raytracing.Driver
{
    class Game : GameWindow
    {
		private static Vector3 InitialCameraPosition = new Vector3(0, 0, 3.0f);

#if DEBUG
		private static readonly bool limitFrames = false;
#else
		private static readonly bool limitFrames = false;
#endif
		private int _frameLimit = 1;

		#region Constants

		public static readonly Vector3 Forward = -Vector3.UnitZ;
		public static readonly Vector3 Backward = Vector3.UnitZ;
		public static readonly Vector3 Left = -Vector3.UnitX;
		public static readonly Vector3 Right = Vector3.UnitX;
		public static readonly Vector3 Up = Vector3.UnitY;
		public static readonly Vector3 Down = -Vector3.UnitY;

		private static float CameraMovementSpeed = 0.025f;	// in units
		private static float CameraRotationSpeed = 2.0f;	// in degrees

		public static readonly Color4 DefaultBackgroundColor = Color4.DarkBlue;

		#endregion

		#region Game properties

		private int _frames = 0;
		double _totalTime = 0;

		#endregion

		#region OpenCL-OpenGL properties

		// Platform invoke required for OpenCL-OpenGL interop setup.
		[DllImport("opengl32.dll")]
		extern static IntPtr wglGetCurrentDC();

		//ComputeContext _context;
		IGraphicsContextInternal _glContext;
		ComputeContext _computeContext;
		ComputeCommandQueue _commandQueue;

		#endregion

		GridCamera _gridCamera = null;	// Camera using voxel traversal
        Scene _world;                   // The scene to render

		/// <summary>Creates a window with the specified title.</summary>
        public Game()
            : base(512, 512, GraphicsMode.Default, "Raytracing tester")
        {
            VSync = VSyncMode.Off;
            
        }

		protected override void Dispose(bool manual)
		{
			_commandQueue.Finish();
			
			_gridCamera.Dispose();
            _world.Dispose();

            _commandQueue.Dispose();
            _computeContext.Dispose();

			base.Dispose(manual);
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

            _world = new Scene(_commandQueue);
            buildScene(_world);

			// create the camera
			// looking down the Z-axis into the scene
			Vector3 cameraPosition = InitialCameraPosition;
			Quaternion cameraRotation = Quaternion.Identity;
			float nearClip = 0.001f;
			float vFOV = 75.0f;

			int halfWidth = ClientRectangle.Width / 2;

			try
			{
				_gridCamera = new GridCamera(this.ClientRectangle, _commandQueue, -Vector3.UnitZ, Vector3.UnitY, cameraPosition);
				_gridCamera.VerticalFieldOfView = vFOV;
				_gridCamera.NearPlane = nearClip;
				_gridCamera.computeProjection();
				//_gridCamera.rotateWorldY(-90.0f);
			}
			catch (Exception)
			{
				this.Exit();
			}
        }

		// Create a shared context between OpenGL and OpenCL. 
		private void openCLSharedInit()
		{
			// select OpenCL device and platform
            // TODO: need to make this more general.
			ComputePlatform platform = ComputePlatform.Platforms[0];
			ComputeDevice device = platform.Devices[0];
            if (device.Type != ComputeDeviceTypes.Gpu)
            {
                platform = ComputePlatform.Platforms[1];
                device = platform.Devices[0];
            }
#if DEBUG
            Trace.WriteLine("Creating context for " + device.ToString());
#endif

			IntPtr deviceContextHandle = wglGetCurrentDC();

			_glContext = (OpenTK.Graphics.IGraphicsContextInternal)OpenTK.Graphics.GraphicsContext.CurrentContext;
			IntPtr glContextHandle = _glContext.Context.Handle;
			ComputeContextProperty p1 = new ComputeContextProperty(ComputeContextPropertyName.CL_GL_CONTEXT_KHR, glContextHandle);
			ComputeContextProperty p2 = new ComputeContextProperty(ComputeContextPropertyName.CL_WGL_HDC_KHR, deviceContextHandle);
			ComputeContextProperty p3 = new ComputeContextProperty(ComputeContextPropertyName.Platform, platform.Handle.Value);
			List<ComputeContextProperty> rawPropertyList = new List<ComputeContextProperty>() { p1, p2, p3 };
			ComputeContextPropertyList Properties = new ComputeContextPropertyList(rawPropertyList);

			_computeContext = new ComputeContext(ComputeDeviceTypes.Gpu, Properties, null, IntPtr.Zero);

			// Create the command queue from the context and device.
			_commandQueue = new ComputeCommandQueue(_computeContext, device, ComputeCommandQueueFlags.None);
		}


        private void buildScene(Scene s)
        {
			Ray r = new Ray(new Vector3(0.1f,0.1f,3.0f), new Vector3(0,0,-1.0f));

			
			Triangle test = new Triangle(	new Vector3(0,0,0f),
											new Vector3(0,1,0),
											new Vector3(1,0,0),
											0);

			float testDist = test.rayTriIntersect(r);
			

			s.BackgroundColor = Color4.CornflowerBlue;

            Material redGlass = new Material(Color4.Gray, 0, 0.97f, 1.52f);
            Material shinyRed = new Material(Color4.DarkRed, 0.25f);
            Material shinyGreen = new Material(Color4.DarkGreen, 0.50f);
            Material shinyBlue = new Material(Color4.DarkBlue, 0.75f);

			Material matteGray = new Material(Color4.Gray);

            // Add test light
            s.addLight(new Vector3(0, 4, 1), Color4.White, 20.0f);
            //s.addLight(new Vector3(0, 0, 0), Color4.White, 2.0f);

            // Add test data.
			s.addTriangle(	new Vector3(0,0,0f),
							new Vector3(0,1,0),
							new Vector3(1,0,0),
							shinyRed);

			//// Create sphere that crosses voxel bounderies
			//s.addSphere(new Vector3(3f, 0, 0), 1.0f, redGlass);

			//// Create multiple spheres in the same voxel
			//s.addSphere(new Vector3(0.2f, 0.2f, 0.2f), 0.05f, shinyRed);
			//s.addSphere(new Vector3(0.2f, 0.2f, -0.2f), 0.05f, shinyGreen);
			//s.addSphere(new Vector3(0.2f, -0.2f, 0.2f), 0.05f, shinyBlue);
			//s.addSphere(new Vector3(0.2f, -0.2f, -0.2f), 0.05f, matteGray);

			//s.addSphere(new Vector3(-0.2f, 0.2f, 0.2f), 0.05f, redGlass);
			//s.addSphere(new Vector3(-0.2f, 0.2f, -0.2f), 0.05f, redGlass);
			//s.addSphere(new Vector3(-0.2f, -0.2f, 0.2f), 0.05f, redGlass);
			//s.addSphere(new Vector3(-0.2f, -0.2f, -0.2f), 0.05f, redGlass);

			//// Create spheres along the major axies.
			//s.addSphere(new Vector3(1f, 0, 0), 0.25f, shinyRed);
			//s.addSphere(new Vector3(0, 1f, 0), 0.25f, shinyGreen);
			//s.addSphere(new Vector3(0, 0, 1f), 0.25f, shinyBlue);

			//s.addSphere(new Vector3(0, 1.5f, 0), 0.05f, redGlass);

			//// Create a large number of spheres to stress the memory system.
			//int min = 2;
			//int max = 3;
			//for (int x = min; x <= max; x++)
			//{
			//    for (int y = min; y <= max; y++)
			//    {
			//        for (int z = min; z <= max; z++)
			//        {
			//            s.addSphere(new Vector3(x, y, z), 0.1f, shinyGreen);
			//        }
			//    }
			//}

            // Add a light to cast shadows.
            //float mid = min + (max - min) / 2.0f;
           // s.addLight(new Vector3(mid, mid, mid), Color4.White, 2.0f);
        }
		#endregion

		/// <summary>
        /// Called when your window is resized. Set your viewport here. It is also
        /// a good place to set up your projection matrix (which probably changes
        /// along when the aspect ratio of your window).
		/// 
		/// WARNING: resizing the window may invaladate OpenCL command queues!
		/// This has not been accouted for yet.
        /// </summary>
        /// <param name="e">Not used.</param>
        protected override void OnResize(EventArgs e)
        {
			// WARNING: resizing the window may invaladate OpenCL command queues!
			// This has not been accouted for yet.

            base.OnResize(e);

			int halfWidth = ClientRectangle.Width / 2;

			// Set the client bounds for the camera
            _gridCamera.setClientBounds(ClientRectangle);
			_gridCamera.computeProjection();

			// orthographic projection
			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadIdentity();
			GL.Ortho(0, ClientRectangle.Width, 0, ClientRectangle.Height, -1, 1);
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

			if (Keyboard[Key.Space])
			{
				System.Diagnostics.Trace.WriteLine("Breakpoint hit");
			}

			// Allows the game to exit
			if (Keyboard[Key.Escape])
			{
				this.Exit();
			}


			// move the camera
			processCameraMovement(_gridCamera, (float)e.Time, false);

            // Exit if escape is pressed.
            if (Keyboard[Key.Escape])
                Exit();
        }

		private void processCameraMovement(MuxEngine.Movables.Camera currentCamera, float time, bool scaleSpeed)
		{
			
			// adjust movement speed relative to the time between frames
			float adjustedMovementSpeed = CameraMovementSpeed;
			float adjustedRotationSpeed = CameraRotationSpeed;
			if (scaleSpeed)
			{
				float fps = 1.0f / time;
				float scale = 40 / fps;
				adjustedMovementSpeed *= scale;
				adjustedRotationSpeed *= scale;
			}

			if (Keyboard[Key.A])
			{
				currentCamera.moveLocal(Left, adjustedMovementSpeed);
			}
			else if (Keyboard[Key.E] || Keyboard[Key.D])
			{
				currentCamera.moveLocal(Right, adjustedMovementSpeed);
			}

			if (Keyboard[Key.Comma] || Keyboard[Key.W])
			{
				currentCamera.moveLocal(Forward, adjustedMovementSpeed);
			}
			else if (Keyboard[Key.O] || Keyboard[Key.S])
			{
				currentCamera.moveLocal(Backward, adjustedMovementSpeed);
			}

			if (Keyboard[Key.Period])
			{
				currentCamera.moveLocal(Up, adjustedMovementSpeed);
			}
			else if (Keyboard[Key.J])
			{
				currentCamera.moveLocal(Down, adjustedMovementSpeed);
			}

			if (Keyboard[Key.Keypad4])
			{
				currentCamera.yaw(adjustedRotationSpeed);
			}
			else if (Keyboard[Key.Keypad6])
			{
				currentCamera.yaw(-adjustedRotationSpeed);
			}

			if (Keyboard[Key.Keypad8])
			{
				currentCamera.pitch(adjustedRotationSpeed);
			}
			else if (Keyboard[Key.Keypad2])
			{
				currentCamera.pitch(-adjustedRotationSpeed);
			}

			if (Keyboard[Key.Keypad7])
			{
				currentCamera.roll(-adjustedRotationSpeed);
			}
			else if (Keyboard[Key.Keypad9])
			{
				currentCamera.roll(adjustedRotationSpeed);
			}
		}

        /// <summary>
        /// Called when it is time to render the next frame. Add your rendering code here.
        /// </summary>
        /// <param name="e">Contains timing information.</param>
        protected override void OnRenderFrame(FrameEventArgs e)
		{
			base.OnRenderFrame(e);

			float fps = (float)(1.0 / e.Time);
			updateTitle(fps);

			// clear the screen
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            _world.render(_commandQueue, _gridCamera);

			// Display the frame that was just rendered.
			SwapBuffers();
		}

		// Display the FPS and ray count in the title bar.
		private void updateTitle(float fps)
		{
			int pixels = ClientSize.Height * ClientSize.Width;
			String pixelString = String.Format("{0:n0}", pixels);

			String fpsString = null;
			if (fps >= 30)
			{
				fps = (float)System.Math.Round(fps, 2, MidpointRounding.ToEven);
				fpsString = String.Format("{0:##}", fps);
			}
			else
			{
				fpsString = String.Format("{0:##.0}", fps);
			}
			this.Title = "Raytracing tester (" + fpsString + " FPS, " + pixelString + " pixels) @ (" + _gridCamera.Position + ")";
		}

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
			System.Diagnostics.Trace.WriteLine("\nRun at " + DateTime.Now + "\n");

            // The 'using' idiom guarantees proper resource cleanup.
            // We request 30 UpdateFrame events per second, and unlimited
            // RenderFrame events (as fast as the computer can handle).
            using (Game game = new Game())
            {
                game.Run(30.0, 60.0);
            }
        }
    }
}