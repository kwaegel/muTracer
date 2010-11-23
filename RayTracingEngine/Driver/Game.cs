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
using Raytracing.CL;

namespace Raytracing.Driver
{
    class Game : GameWindow
    {
		private static Vector3 InitialCameraPosition = new Vector3(2.5f, -2f, 5.5f);

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

		private static float CameraMovementSpeed = 0.05f;	// in units
		private static float CameraRotationSpeed = 2.0f;	// in degrees

		public static readonly Color4 DefaultBackgroundColor = Color4.DarkBlue;

		#endregion

		#region Game properties

		private int _frames = 0;
		double _totalTime = 0;

		private bool _renderCLCamera			= false;
		private bool _renderSoftwareRTCamera	= false;

		private bool _renderGridCamera			= true;
		private bool _renderSoftwareGridCamera	= false;

		private bool _cameraSelectionPressed = true;

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
		ClCameraInCS _softwareGridCamera;
		CLCamera _clCamera;		// test camera using OpenCL
		GridCamera _gridCamera;	// Camera using voxel traversal

		#endregion	

		#region Scene structures
		Scene _scene;
		PointLight _light;

		CLSphereBuffer _clSphereBuffer;

		VoxelGrid _voxelGrid;	// used for the grid camera;

		#endregion

		/// <summary>Creates a window with the specified title.</summary>
        public Game()
            : base(1200, 600, GraphicsMode.Default, "Raytracing tester")
        {
            VSync = VSyncMode.Off;
        }

		protected override void Dispose(bool manual)
		{
			_commandQueue.Finish();
			_clSphereBuffer.Dispose();
			_voxelGrid.Dispose();
			
			if (_clCamera != null)
				_clCamera.Dispose();
			if (_gridCamera != null)
				_gridCamera.Dispose();

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

			// create the camera
			// looking down the Z-axis into the scene
			Vector3 cameraPosition = InitialCameraPosition;
			Quaternion cameraRotation = Quaternion.Identity;
			float nearClip = 0.001f;
			float vFOV = 75.0f;

			int halfWidth = ClientRectangle.Width / 2;

			try
			{
				Rectangle rtDrawBounds = new Rectangle(halfWidth, 0, halfWidth, ClientRectangle.Height);
				_rtCamera = new RayTracingCamera(rtDrawBounds, (-Vector3.UnitZ), Vector3.UnitY, cameraPosition);
				_rtCamera.VerticalFieldOfView = vFOV;
				_rtCamera.NearPlane = nearClip;
				_rtCamera.computeProjection();

				Rectangle clDrawBounds = new Rectangle(0, 0, halfWidth, ClientRectangle.Height);
				_clCamera = new CLCamera(clDrawBounds, _commandQueue, -Vector3.UnitZ, Vector3.UnitY, cameraPosition);
				_clCamera.VerticalFieldOfView = vFOV;
				_clCamera.NearPlane = nearClip;
				_clCamera.computeProjection();

				_gridCamera = new GridCamera(clDrawBounds, _commandQueue, -Vector3.UnitZ, Vector3.UnitY, cameraPosition);
				_gridCamera.VerticalFieldOfView = vFOV;
				_gridCamera.NearPlane = nearClip;
				_gridCamera.computeProjection();

				_softwareGridCamera = new ClCameraInCS(rtDrawBounds, (-Vector3.UnitZ), Vector3.UnitY, cameraPosition);
				_softwareGridCamera.VerticalFieldOfView = vFOV;
				_softwareGridCamera.NearPlane = nearClip;
				_softwareGridCamera.computeProjection();
			}
			catch (Exception)
			{
				this.Exit();
			}

			// create the scene
			_scene = new GridScene(16, 1);
			//_scene = new LinearScene(Color4.CornflowerBlue);
			_clSphereBuffer = new CLSphereBuffer(_commandQueue, 1024);
			_scene.BackgroundColor = Color4.Black;
			//buildBlockScene(_scene, _clSphereBuffer);
			buildEdgeScene(_scene, _clSphereBuffer);
			//buildAxisScene(_scene, _clSphereBuffer);

			// create a voxel grid for testing
			_voxelGrid = createVoxelGrid(16, 10); 

			_scene.BackgroundColor = DefaultBackgroundColor;
        }

		// Create a shared context between OpenGL and OpenCL. 
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

			// Create the command queue from the context and device.
			_commandQueue = new ComputeCommandQueue(_computeContext, device, ComputeCommandQueueFlags.None);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="gridWidth">How wide the grid is in world units.</param>
		/// <param name="gridResolution">How many cells wide the grid is.</param>
		/// <returns></returns>
		private VoxelGrid createVoxelGrid(float gridWidth, int gridResolution)
		{
			VoxelGrid grid = new VoxelGrid(_commandQueue, 16, 16);

			// Add test light
			grid.addPointLight(new Vector3(0,4,0), Color4.White, 20.0f);
			grid.addPointLight(new Vector3(0,0,0), Color4.White, 2.0f);

			// Add test data.

			// Create sphere that crosses voxel bounderies
			grid.addSphere(new Vector3(3f, 0, 0), 1.0f, Color4.Black);

			// Create multiple spheres in the same voxel
			grid.addSphere(new Vector3(0.2f, 0.2f, 0.2f), 0.05f, Color4.Black);
			grid.addSphere(new Vector3(0.2f, 0.2f, -0.2f), 0.05f, Color4.Black);
			grid.addSphere(new Vector3(0.2f, -0.2f, 0.2f), 0.05f, Color4.Black);
			grid.addSphere(new Vector3(0.2f, -0.2f, -0.2f), 0.05f, Color4.Black);
			
			grid.addSphere(new Vector3(-0.2f, 0.2f, 0.2f), 0.05f, Color4.Black);
			grid.addSphere(new Vector3(-0.2f, 0.2f, -0.2f), 0.05f, Color4.Black);
			grid.addSphere(new Vector3(-0.2f, -0.2f, 0.2f), 0.05f, Color4.Black);
			grid.addSphere(new Vector3(-0.2f, -0.2f, -0.2f), 0.05f, Color4.Black);

			// Create spheres along the major axies.
			grid.addSphere(new Vector3(1f, 0, 0), 0.25f, Color4.Red);
			grid.addSphere(new Vector3(0, 1f, 0), 0.25f, Color4.Green);
			grid.addSphere(new Vector3(0, 0, 1f), 0.25f, Color4.Blue);

			grid.addSphere(new Vector3(0, 1.5f, 0), 0.05f, Color4.Red);

			// Create a large number of spheres to stress the memory system.
			int min = 2;
			int max = 3;
			for (int x = min; x <= max; x++)
			{
				for (int y = min; y <= max; y++)
				{
					for (int z = min; z <= max; z++)
					{
						grid.addSphere(new Vector3(x,y,z), 0.1f, Color4.Blue);
					}
				}
			}
			float mid = min+(max - min) / 2.0f;
			grid.addPointLight(new Vector3(mid,mid,mid), Color4.White, 2.0f);

			return grid;
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

		private void buildEdgeScene(Scene scene, CLSphereBuffer buffer)
		{
			_moveLight = true;

			int low = -5;
			int high = 5;

			List<int> cornerList = new List<int>(2);
			cornerList.Add(-5);
			cornerList.Add(5);

			foreach (int x in cornerList)
			{
				foreach (int y in cornerList)
				{
					foreach (int z in cornerList)
					{
							Color4 color = getColor(low, high, x, y, z);
							Material mat = new Material(color, 0);
							Vector3 position = new Vector3(x, y, z);
							Sphere s = new Sphere(position, 0.5f, mat);
							scene.add(s);

							buffer.addSphere(new SphereStruct(position, 0.5f, color));
					}
				}
			}


			_light = new PointLight(Vector3.Zero, 1.0f, Color4.White);
			scene.add(_light);

			buffer.sendDataToDevice();
		}

		private void buildBlockScene(Scene scene, CLSphereBuffer buffer)
		{
			_moveLight = true;

			int low = -5;
			int high = 5;

			//int low = -1;
			//int high = 1;

			for (int x = low; x <= high; x++)
			{
				for (int y = low; y <= high; y++)
				{
					for (int z = low; z <= high; z++)
					{
						if (x != 0 && y != 0 && z != 0)
						{
							Color4 color = getColor(low, high, x, y, z);
							Material mat = new Material(color, 0);
							Vector3 position = new Vector3(x, y, z);
							Sphere s = new Sphere(position, 0.5f, mat);
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

			// Set the client bounds for the CL camera
			Rectangle clDrawBounds = new Rectangle(0, 0, halfWidth, ClientRectangle.Height);
			_clCamera.setClientBounds(clDrawBounds);
			_clCamera.computeProjection();

			// Set the viewport bounds for the RT camera
			Rectangle rtDrawBounds = new Rectangle(halfWidth, 0, halfWidth, ClientRectangle.Height);
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
					//_renderCLCamera = !_renderCLCamera;
					_renderCLCamera = true;
					_renderGridCamera = false;
					System.Console.WriteLine("CL camera enabled =" + _renderCLCamera);
					_cameraSelectionPressed = true;
				}
				else if (Keyboard[Key.Number2])
				{
					_renderGridCamera = true;
					_renderCLCamera = false;
					System.Console.WriteLine("Grid camera enabled =" + _renderSoftwareRTCamera);
					_cameraSelectionPressed = true;
				}

				if (Keyboard[Key.Number3])
				{
					_renderSoftwareRTCamera = !_renderSoftwareRTCamera;
					_renderSoftwareGridCamera = false;
					System.Console.WriteLine("RT camera enabled =" + _renderSoftwareRTCamera);
					_cameraSelectionPressed = true;
				}
				else if (Keyboard[Key.Number4])
				{
					_renderSoftwareGridCamera = true;
					_renderSoftwareRTCamera = false;
					_cameraSelectionPressed = true;
				}
				else if (Keyboard[Key.Number5])
				{
					_renderSoftwareRTCamera = false;
					_renderSoftwareGridCamera = false;
					System.Console.WriteLine("RT camera disabled");
					_cameraSelectionPressed = true;
				}
				
			}
			else if (!Keyboard[Key.Number1] && !Keyboard[Key.Number2])
			{
				_cameraSelectionPressed = false;
			}


			// move both cameras
			processCameraMovement(_rtCamera, (float)e.Time, false);

			// copy position and rotation to other cameras
			_gridCamera.Position = _rtCamera.Position;
			_gridCamera.Rotation = _rtCamera.Rotation;

			_clCamera.Position = _rtCamera.Position;
			_clCamera.Rotation = _rtCamera.Rotation;

			_softwareGridCamera.Rotation = _rtCamera.Rotation;
			_softwareGridCamera.Position = _rtCamera.Position;

			// DEBUG: print the camera location
			//System.Console.WriteLine(_rtCamera.Position);

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

			// Render the scene with which ever cameras are selected.
			if (_renderSoftwareRTCamera)
			{
				_rtCamera.render(_scene);
			}

			if (_renderSoftwareGridCamera)
			{
				_softwareGridCamera.setVoxelGrid(_voxelGrid);
				_softwareGridCamera.render(_scene);
			}

			if (_renderCLCamera)
			{
				_clSphereBuffer.sendDataToDevice();
				_clCamera.render(_clSphereBuffer, (float)_totalTime);
			}

			if (_renderGridCamera)
			{
				_voxelGrid.syncBuffers();
				_gridCamera.render(_voxelGrid, (float)_totalTime);
			}

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
				game.Run(30.0);	// this causes two updates per draw call when draw is slow.
            }
        }
    }
}