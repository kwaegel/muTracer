using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

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

		float pixelWidth;

		Matrix4 _oldView;

		//SimpleScene _scene;
		Scene _scene;

		Matrix4 _screenToWorldMatrix;

		public static int maximumRecursiveDepth = 3;

		public AntialiasingModes AntialiasingMode = AntialiasingModes.None;
		public enum AntialiasingModes
		{
			None, Super4x, Jittered4x, Adaptive4x
		}

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


		// NOTE: This appears to be broken.
		public void setUsingHorzintalFov(float hFOV)
		{
			double vFov = System.Math.Atan( System.Math.Tan(hFOV/2.0)/AspectRatio ) * 2.0;
			this.VerticalFieldOfView = (float) vFov;
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

		private void calculateScreenPoints()
		{
			int rows = ClientBounds.Height;
			int columns = ClientBounds.Width;
			int pixelCount = rows * columns;

			if (_normilizedScreenPoints == null || _normilizedScreenPoints.Length != pixelCount)
			{
				_normilizedScreenPoints = new Vector2[columns, rows];
			}
			if (_pixelBuffer == null || _pixelBuffer.Length != pixelCount)
			{
				_pixelBuffer = new Color4[pixelCount];
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

			pixelWidth = _normilizedScreenPoints[1,0].X - _normilizedScreenPoints[0,0].X;

		}

		private Ray unprojectPointIntoWorld(Vector2 point)
		{
			Vector3 screenPoint = new Vector3(point);
			screenPoint.Z = -1;

			Vector3 windowPointInWorld = Vector3.Transform(screenPoint, _screenToWorldMatrix);

			Vector3 direction = Vector3.Subtract(windowPointInWorld, Position);
			direction.Normalize();

			return new Ray(windowPointInWorld, direction, 1);
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
			_scene = scene;

			int width = ClientBounds.Width;
			int height = ClientBounds.Height;

            int rows = ClientBounds.Height;
            int columns = ClientBounds.Width;

            // Loop over all pixels
			#if DEBUG
				for (int y = 0; y < rows; y++)
			#else
				Parallel.For(0, rows, y =>
			#endif
            {
				Vector2[] samplePoints = new Vector2[4];
					for (int x = 0; x < columns; x++)
					{
						Ray r = unprojectPointIntoWorld(_normilizedScreenPoints[x, y]);



						int pixelFlatIndex = y * columns + x;
#if DEBUG
						bool debugPixel = (x==450 && y==305);
						if (debugPixel)
							scene.PrintDebugMessages = true;
#endif
						_pixelBuffer[pixelFlatIndex] = castRay(r, _scene, maximumRecursiveDepth);

#if DEBUG
						if (debugPixel)
							_pixelBuffer[pixelFlatIndex] = Color4.Red;
						scene.PrintDebugMessages = false;
#endif

					} // End y loop
			#if DEBUG
			}
			#else
            }); // end parallel x loop
			#endif

			// draw pixel buffer to the back buffer with z-sorting turned off
            OpenTK.Graphics.OpenGL.GL.DepthMask(false);
            OpenTK.Graphics.OpenGL.GL.DrawPixels<Color4>(width, height, OpenTK.Graphics.OpenGL.PixelFormat.Rgba, OpenTK.Graphics.OpenGL.PixelType.Float, _pixelBuffer);
            OpenTK.Graphics.OpenGL.GL.DepthMask(true);
		}

		private Color4 castRay(Ray r, Scene scene, int maxRayDepth)
		{
			if(maxRayDepth < 0)
			{
				return scene.BackgroundColor;
			}

			Vector3 collisionPoint = new Vector3();
			Vector3 surfaceNormal = new Vector3();
			Material mat = new Material();

			float intersectionDistance = _scene.getNearestIntersection(ref r, ref collisionPoint, ref surfaceNormal, ref mat);

			if (intersectionDistance < float.PositiveInfinity && intersectionDistance >= 0)
			{
				Vector3 intersectionPoint = r.Origin + r.Direction * intersectionDistance;

				Color4 color = Color4.Black;

				float diffuseCoef = mat.kd;	//Math.Max(mat.Color.A-mat.Reflectivity, 0);	// Opacaty - reflectivity = diffuse lighting

				// Add contribution of direct lighting
				// Loop over lights to check for shadowing
				System.Collections.Generic.List<Light> lights = _scene.getLights();
				foreach (Light pl in lights)
				{
					//Vector3 lightDirection = pl.Position - intersectionPoint;
					Vector3 lightDirection = pl.getDirection(intersectionPoint);
					float lightDistance = lightDirection.Length;
					lightDirection.Normalize();

					// Find if this point is in shadow or not
					Ray shadowRay = new Ray(collisionPoint, lightDirection, r.CurrentRefractiveIndex);
					shadowRay.Origin += surfaceNormal * 0.001f;	// Fix for floating point precision error
					Vector3 shadowCollisionPoint = new Vector3();
					Vector3 shadowSurfaceNormal = new Vector3();
					float shadowIntersectDist = _scene.getNearestIntersection(ref shadowRay, ref shadowCollisionPoint, ref shadowSurfaceNormal);

					// If no collision occurs, add light contribution
					if(shadowIntersectDist >= lightDistance)
					{
						// Calculate diffuse value
						float diffuseValue = clampedDot(surfaceNormal, lightDirection) * diffuseCoef;

						// calculate specular value
						Vector3 bisector = lightDirection - r.Direction;	// the ray direction needs to be negated, so just subtract it
						bisector.Normalize();
						float specularValue = clampedDot(surfaceNormal, bisector);

						if (diffuseValue > 0)
						{
							// Add diffuse shading
							color.R += (mat.Color.R * pl.Color.R) * diffuseValue;
							color.G += (mat.Color.G * pl.Color.B) * diffuseValue;
							color.B += (mat.Color.B * pl.Color.G) * diffuseValue;
						}

						if (specularValue > 0)
						{
							specularValue = (float)Math.Pow(specularValue, 1.5f*mat.phongExponent) * mat.Color.A * mat.Reflectivity * 0.75f;
							color.R += mat.Reflectivity * pl.Color.R * specularValue;
							color.G += mat.Reflectivity * pl.Color.G * specularValue;
							color.B += mat.Reflectivity * pl.Color.B * specularValue;
						}
					}

				}// End loop over lights

				// Calculate the cos of theta for both reflecton and refraction
				// TODO: This might need to be dot(normal, -rayDir) for refraction...
				float cosTheta = Vector3.Dot(r.Direction, surfaceNormal);

				// Add recursive reflections
				if (mat.Reflectivity > 0)
				{
					Vector3 reflectiveOrigin = collisionPoint - r.Direction * intersectionDistance * 0.0004f;	// subtract small distance to aviod floating point error
					Vector3 reflectiveDirection = r.Direction - (2 * cosTheta * surfaceNormal);

					Ray reflectionRay = new Ray(reflectiveOrigin, reflectiveDirection, r.CurrentRefractiveIndex);
					Color4 reflectionColor = castRay(reflectionRay, scene, maxRayDepth - 1);

					color.R += reflectionColor.R * mat.Reflectivity * 0.5f;
					color.G += reflectionColor.G * mat.Reflectivity * 0.5f;
					color.B += reflectionColor.B * mat.Reflectivity * 0.5f;
				}


				if (mat.Transparency > 0)
				{
					// if we are moving from a dense medium to a less dense one, reverse the surface normal
					if (cosTheta > 0)	// If we intersected the inside edge of a shape, flip the surface normal.
					{
						surfaceNormal = -surfaceNormal;
						// cosTheta is based on the surface normal and must also be negated
						cosTheta = -cosTheta;
					}

					float n = r.CurrentRefractiveIndex / mat.RefractiveIndex;
					float sinThetaSquared = n * n * (1 - cosTheta * cosTheta);

					// Create refraction ray
					Vector3 refractiveOrigin = collisionPoint + r.Direction * intersectionDistance * 0.0004f;
					Vector3 refractiveDirection = n * r.Direction - (n * cosTheta + (float)Math.Sqrt(1 - sinThetaSquared)) * surfaceNormal;
					Ray refractveRay = new Ray(refractiveOrigin, refractiveDirection, mat.RefractiveIndex);

					Color4 refractionColor = castRay(refractveRay, scene, maxRayDepth - 1);

					color.R += refractionColor.R * mat.Transparency;
					color.G += refractionColor.G * mat.Transparency;
					color.B += refractionColor.B * mat.Transparency;
				}
				
				// Add ambient light
				color.R += mat.Color.R * scene.Ambiant * 0.5f;
				color.G += mat.Color.G * scene.Ambiant * 0.5f;
				color.B += mat.Color.B * scene.Ambiant * 0.5f;

				return color;
			}
			else
			{
				return scene.BackgroundColor;
			}

		}

		// Clamped cos functino
		private static float clampedDot(Vector3 v1, Vector3 v2)
		{
			return Math.Max(Vector3.Dot(v1, v2),0);
		}
	}
}
