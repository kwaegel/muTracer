// Released to the public domain. Use, modify and relicense at will.

using System;
using System.Drawing;
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

namespace Raytracing.Driver
{
    class Game : GameWindow
    {
		

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

		private static float CameraMovementSpeed = 0.1f;	// in units
		private static float CameraRotationSpeed = 2.0f;	// in degrees

		public static readonly Color4 DefaultBackgroundColor = Color4.DarkBlue;

		#endregion

		#region Game properties

		private int _frames = 0;
		double _totalTime = 0;

		private bool _renderCLCamera = true;
		private bool _renderSoftwareRTCamera = true;

		private bool _cameraSelectionPressed = false;

		private bool _moveLight = false;

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

		#region Cameras

		RayTracingCamera _rtCamera;
		CLCamera _clCamera;	// test camera using OpenCL

		#endregion	

		#region Scene structures
		Scene _scene;
		PointLight _light;

		CLSphereBuffer _clSphereBuffer;

		#endregion

		/// <summary>Creates a window with the specified title.</summary>
        public Game()
            : base(800, 400, GraphicsMode.Default, "Raytracing tester")
        {
            VSync = VSyncMode.On;
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
			Vector3 cameraPosition = new Vector3(0, 0, 5f);
			Quaternion cameraRotation = Quaternion.Identity;

			int halfWidth = ClientRectangle.Width / 2;

			Rectangle rtDrawBounds = new Rectangle(halfWidth, 0, halfWidth, ClientRectangle.Height);
			_rtCamera = new RayTracingCamera(rtDrawBounds, (-Vector3.UnitZ), Vector3.UnitY, cameraPosition);
			_rtCamera.VerticalFieldOfView = 60.0f;
			_rtCamera.computeProjection();

			Rectangle clDrawBounds = new Rectangle(0, 0, halfWidth, ClientRectangle.Height);
			_clCamera = new CLCamera(clDrawBounds, _commandQueue, -Vector3.UnitZ, Vector3.UnitY, cameraPosition);
			_clCamera.VerticalFieldOfView = 60.0f;
			_clCamera.computeProjection();

			// create the scene
			_scene = new GridScene(16, 1);
			_clSphereBuffer = new CLSphereBuffer(_commandQueue, 1024);
			_scene.BackgroundColor = Color4.Black;
			//buildBlockScene(_scene, _clSphereBuffer);
			buildAxisScene(_scene, _clSphereBuffer);

			_scene.BackgroundColor = DefaultBackgroundColor;
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

		// build a simple scene with one sphere
		private void buildAxisScene(Scene scene, CLSphereBuffer buffer)
		{
			scene.add(new PointLight(new Vector3(5, 10, 5), 1.0f, Color4.White));

			scene.add(new Sphere(Vector3.Zero, 1.0f, new Material(Color4.White)));
			scene.add(new Sphere(Vector3.UnitX, 0.5f, new Material(Color4.Red)));
			scene.add(new Sphere(Vector3.UnitY, 0.5f, new Material(Color4.Green)));
			scene.add(new Sphere(Vector3.UnitZ, 0.5f, new Material(Color4.Blue)));

			buffer.addSphere(new SphereStruct(Vector3.Zero, 1.0f, Color4.White));
			buffer.addSphere(new SphereStruct(Vector3.UnitX, 0.5f, Color4.Red));
			buffer.addSphere(new SphereStruct(Vector3.UnitY, 0.5f, Color4.Green));
			buffer.addSphere(new SphereStruct(Vector3.UnitZ, 0.5f, Color4.Blue));
			buffer.sendDataToDevice();
		}

		private void buildBlockScene(Scene scene, CLSphereBuffer buffer)
		{
			_moveLight = true;

			//int low = -7;
			//int high = 7;

			int low = -1;
			int high = 1;

			for (int x = low; x <= high; x++)
			{
				for (int y = low; y <= high; y++)
				{
					for (int z = low; z <= high; z++)
					{
						if (x != 0 || y != 0 || z != 0)
						{
							Color4 color = getColor(low, high, x, y, z);
							Material mat = new Material(color, 0);
							Vector3 position = new Vector3(x, y, z);
							Sphere s = new Sphere(position, 0.25f, mat);
							scene.add(s);

							buffer.addSphere(new SphereStruct(position, 0.25f, color));
						}
						else
						{
							_light = new PointLight(Vector3.Zero, 1.0f, Color4.DarkKhaki);
							scene.add(_light);
						}
					}
				}
			}

			buffer.sendDataToDevice();

			int width = high - low;
			int objects = (int)System.Math.Pow(width, 3);
			System.Console.WriteLine("added " + objects + " spheres.");
		}

		private Color4 getColor(int start, int end, int x, int y, int z)
		{
			float percentX = (float)(x - start) / (float)(end - start);
			float percentY = (float)(y - start) / (float)(end - start);
			float percentZ = (float)(z - start) / (float)(end - start);

			return new Color4(percentX, percentY, percentZ, 0);
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
			// WARNING: resizing the window invaladates all OpenCL command queues!
			// This has not been accouted for yet.

            base.OnResize(e);

			int halfWidth = ClientRectangle.Width / 2;

			// Set the client bounds for the CL camera
			Rectangle clDrawBounds = new Rectangle(0, 0, halfWidth, ClientRectangle.Height);
			_clCamera.setClientBounds(clDrawBounds);
			_clCamera.computeProjection();

			// Set the viewport bounds for the RT camera
			Rectangle rtDrawBounds = new Rectangle(0, halfWidth, halfWidth, ClientRectangle.Height);
			_rtCamera.setClientBounds(rtDrawBounds);
			_rtCamera.computeProjection();


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

			if (_moveLight)
			{
				float x = (float)System.Math.Cos(_totalTime / 1f);
				float y = (float)System.Math.Cos(_totalTime / 1f);
				float z = (float)System.Math.Sin(_totalTime / 1f);

				_light.Position.X = 4f * x;
				//_light.Position.Y = 0.15f * y;
				_light.Position.Z = 4f * z;
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

			// toggle either camera on or off
			if (!_cameraSelectionPressed)
			{
				if (Keyboard[Key.Number1])
				{
					_renderCLCamera = !_renderCLCamera;
					System.Console.WriteLine("CL camera enabled =" + _renderCLCamera);
					_cameraSelectionPressed = true;
				}

				if (Keyboard[Key.Number2])
				{
					_renderSoftwareRTCamera = !_renderSoftwareRTCamera;
					System.Console.WriteLine("RT camera enabled =" + _renderSoftwareRTCamera);
					_cameraSelectionPressed = true;
				}
			}
			else if (!Keyboard[Key.Number1] && !Keyboard[Key.Number2])
			{
				_cameraSelectionPressed = false;
			}


			// move both cameras
			processCameraMovement(_rtCamera);
			processCameraMovement(_clCamera);

            if (Keyboard[Key.Escape])
                Exit();
        }

		private void processCameraMovement(MuxEngine.Movables.Camera currentCamera)
		{
			if (Keyboard[Key.A])
			{
				currentCamera.moveLocal(Left, CameraMovementSpeed);
			}
			else if (Keyboard[Key.E] || Keyboard[Key.D])
			{
				currentCamera.moveLocal(Right, CameraMovementSpeed);
			}

			if (Keyboard[Key.Comma] || Keyboard[Key.W])
			{
				currentCamera.moveLocal(Forward, CameraMovementSpeed);
			}
			else if (Keyboard[Key.O] || Keyboard[Key.S])
			{
				currentCamera.moveLocal(Backward, CameraMovementSpeed);
			}

			if (Keyboard[Key.Period])
			{
				currentCamera.moveLocal(Up, CameraMovementSpeed);
			}
			else if (Keyboard[Key.J])
			{
				currentCamera.moveLocal(Down, CameraMovementSpeed);
			}

			if (Keyboard[Key.Keypad4])
			{
				currentCamera.yaw(CameraRotationSpeed);
			}
			else if (Keyboard[Key.Keypad6])
			{
				currentCamera.yaw(-CameraRotationSpeed);
			}

			if (Keyboard[Key.Keypad8])
			{
				currentCamera.pitch(CameraRotationSpeed);
			}
			else if (Keyboard[Key.Keypad2])
			{
				currentCamera.pitch(-CameraRotationSpeed);
			}

			if (Keyboard[Key.Keypad7])
			{
				currentCamera.roll(-CameraRotationSpeed);
			}
			else if (Keyboard[Key.Keypad9])
			{
				currentCamera.roll(CameraRotationSpeed);
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

			int halfWidth = ClientRectangle.Width / 2;

			// Render the scene
			if (_renderSoftwareRTCamera)
			{
				GL.Viewport(halfWidth, 0, halfWidth, ClientRectangle.Height);
				_rtCamera.computeView();
				_rtCamera.render(_scene);
			}

			if (_renderCLCamera)
			{
				_clSphereBuffer.sendDataToDevice();

				GL.Viewport(0, 0, halfWidth, ClientRectangle.Height);
				_clCamera.computeView();
				_clCamera.render(_clSphereBuffer, (float)_totalTime);
			}

			Matrix4 rtMatrix = _rtCamera.getScreenToWorldMatrix();
			Matrix4 clMatrix = _clCamera.getScreenToWorldMatrix();



			// display the new frame
			SwapBuffers();
		}

		// Display the FPS in the title bar.
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
			this.Title = "Raytracing tester (" + fpsString + " FPS, " + pixelString + " pixels)";
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
                //game.Run(30.0);	// this causes two updates per draw call
				game.Run();
            }
        }
    }
}