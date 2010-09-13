using System;
using System.Collections;
using System.Threading;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using Raytracing.Primitives;
using Raytracing.SceneStructures;

namespace Raytracing
{
	public class RayTracingCamera : MuxEngine.Movables.Camera
	{
		//public static readonly Vector3 Forward = -Vector3.UnitZ;

		Vector2[,] _normilizedScreenPoints;
		Color4[] _pixelBuffer;
		Color4[,] _pixelBufferRec;

		Matrix4 _oldView;

		// having more threads then CPU cores seems to work better then equal numbers...
#if DEBUG
		static int numberOfCPUWorkerThreads = 0;
#else
		static int numberOfCPUWorkerThreads = 3;	// total threads == worker threads + main thread
#endif

		// worker syncronisation.
		int _nextRowToProcess;
		private static Mutex _rayToProcessLock = new Mutex();

		// synchronize between worker thread
		ManualResetEvent[] _resetEvents;

		//SimpleScene _scene;
		Scene _gridScene;

		Matrix4 _screenToWorldMatrix;

		// OpenGL texture to render on
		int _renderTextureID;

		#region Initialization

		public RayTracingCamera(System.Drawing.Rectangle clientBounds)
			: this(clientBounds, MuxEngine.LinearAlgebra.Matrix4.Identity)
		{}

		public RayTracingCamera(System.Drawing.Rectangle clientBounds, MuxEngine.LinearAlgebra.Matrix4 transform)
			: base(clientBounds, transform)
		{
			rayTracingInit();
		}

		public RayTracingCamera(System.Drawing.Rectangle clientBounds, Vector3 forward,
                       Vector3 up, Vector3 position)
            : base (clientBounds, forward, up, position)
        {
			rayTracingInit();
		}

		private void rayTracingInit()
		{
			_renderTextureID = GL.GenTexture();
			_oldView = new Matrix4();
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

			rayTracingInit();

			calculateScreenPoints();
		}


		// this recalculates the screen to world matrix required for creating 
		// viweing rays.
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

		private void calculateScreenPoints()
		{
			int rows = ClientBounds.Height;
			int columns = ClientBounds.Width;
			int pixelCount = rows * columns;

			if (_normilizedScreenPoints == null || _normilizedScreenPoints.Length != pixelCount)
			{
				_normilizedScreenPoints = new Vector2[columns, rows];
			}

			// cache view constants (constant between projection changes...)
			Vector3 cachedPosition = Position;

			// can I just create a simple arary of Point structs in place of nested loops?
			// might help with the transition to OpenCL code
			for (int row = 0; row < rows; row++)
			{
				for (int col = 0; col < columns; col++)
				{
					// convert to normilized screen coordinates [-1,1]
					float x = 2.0f * col / columns - 1.0f;
					float y = 2.0f * row / rows - 1.0f;

					// why did I need to transpose the x and y coordinates here?
					_normilizedScreenPoints[col, row] = new Vector2(x,y);
				}
			}

		}

		private Ray unprojectPointIntoWorld(Vector2 point)
		{
			Vector3 screenPoint = new Vector3(point);
			screenPoint.Z = -1;

			Vector3 windowPointInWorld = Vector3.Transform(screenPoint, _screenToWorldMatrix);

			Vector3 direction = Vector3.Subtract(windowPointInWorld, Position);
			direction.Normalize();

			return new Ray(windowPointInWorld, direction);
		}


		/// <summary>
		/// for each pixel:
		///		* find direction from eye point to pixel
		///		* cast a ray into the scene from pixel in the calculated direction
		///		* copy resulting color into the surfaceColor buffer
		///		* render buffer to screen as a texture
		/// </summary>
		/// <param name="scene"></param>
		public void render(Scene scene)
		{
			// hold reference to the scene
			_gridScene = scene;

			int width = ClientBounds.Width;
			int height = ClientBounds.Height;

			int pixelCount = _normilizedScreenPoints.Length;
			if (_pixelBuffer == null || _pixelBuffer.Length != pixelCount)
			{
				_pixelBuffer = new Color4[pixelCount];
				
			}
			if (_pixelBufferRec == null || _pixelBufferRec.Length != pixelCount)
			{
				_pixelBufferRec = new Color4[width, height];
			}

			// initilize the ray process index for threading
			_rayToProcessLock.WaitOne();
			_nextRowToProcess = 0;
			_rayToProcessLock.ReleaseMutex();

			// create worker threads to cast rays
			_resetEvents = new ManualResetEvent[numberOfCPUWorkerThreads + 1];
			for (int i = 0; i < numberOfCPUWorkerThreads; i++)
			{
				_resetEvents[i] = new ManualResetEvent(false);
				ThreadPool.QueueUserWorkItem(new WaitCallback(gridWorker), (Object)i);
			}
			_resetEvents[numberOfCPUWorkerThreads] = new ManualResetEvent(false);

			// run main thread with remaining valid worker index
			gridWorker(numberOfCPUWorkerThreads);

			// block until all the worker threads have finished
			WaitHandle.WaitAll(_resetEvents);

			// draw pixels to the screen using a textured quad
			copyPixelsToTexture(_renderTextureID, _pixelBuffer);
			drawTextureToScreen();
		}

		private void copyPixelsToTexture(int textureID, Color4[] pixels)
		{
			// Copy pixel data to texture
			GL.BindTexture(TextureTarget.Texture2D, textureID);
			GL.TexImage2D<Color4>(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, ClientBounds.Width, ClientBounds.Height, 0, PixelFormat.Rgba, PixelType.Float, pixels);

			// These are needed to disable mipmapping.
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
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


		// process rays by row
		private void gridWorker(Object o)
		{
			int workerIndex = (int)o;

			int rows = ClientBounds.Height;
			int columns = ClientBounds.Width;

			// get a lock on the _nextRayToProcess lock
			_rayToProcessLock.WaitOne();

			int y;

			// while there are more rays to process
			while (_nextRowToProcess < rows)
			{
				// get a block of rays to process in form [start, end)
				y = _nextRowToProcess;

				// increment row to process
				_nextRowToProcess++;

				// release the mutex for other threads to use
				_rayToProcessLock.ReleaseMutex();

				// process rays
				Vector3 collisionPoint = new Vector3();
				Vector3 surfaceNormal = new Vector3();
				Raytracing.Primitives.Material mat = new Raytracing.Primitives.Material();
				for (int x = 0; x < columns; x++)
				{
					Ray r = unprojectPointIntoWorld(_normilizedScreenPoints[x,y]);

					float intersectionDistance = _gridScene.getNearestIntersection(ref r, ref collisionPoint, ref surfaceNormal, ref mat);

					int pixelFlatIndex = y*columns + x;

					if (intersectionDistance >= 0)
					{
						Vector3 intersectionPoint = r.Position + r.Direction * intersectionDistance;
						//_pixelBufferRec[x, y] = mat.color;

						Color4 color = Color4.Black;
						System.Collections.Generic.List<PointLight> lights = _gridScene.getLights();

						foreach (PointLight pl in lights)
						{
							Vector3 lightDirection = pl.Position - intersectionPoint;
							lightDirection.Normalize();
							float shade = Vector3.Dot(surfaceNormal, lightDirection);

							Ray shadowRay = new Ray(collisionPoint, lightDirection);
							shadowRay.Position += surfaceNormal * 0.001f;

							if (shade > 0 && _gridScene.getNearestIntersection(ref shadowRay, ref collisionPoint, ref surfaceNormal, ref mat) < 0)
							{
								color.R += (mat.color.R + pl.Color.R) * shade;
								color.G += (mat.color.G + pl.Color.G) * shade;
								color.B += (mat.color.B + pl.Color.B) * shade;

								//color.R += mat.color.R * shade;
								//color.G += mat.color.G * shade;
								//color.B += mat.color.B * shade;
							}
						}

						_pixelBuffer[pixelFlatIndex] = color;
					}
					else
					{
						_pixelBuffer[pixelFlatIndex] = _gridScene.BackgroundColor;
					}

				}

				// aquire the mutex for the next loop check
				_rayToProcessLock.WaitOne();
			}

			// release the mutex before terminating
			_rayToProcessLock.ReleaseMutex();

			// notify the main thread that there are no more rays for this thread to process
			_resetEvents[workerIndex].Set();
		}
	}
}
