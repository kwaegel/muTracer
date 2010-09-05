using System;
using System.Collections.Generic;
using System.Drawing;

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
		#region OpenCL program code

		static string _GPUraytracingSource = @"
kernel void
cycleColors(	const		float		time,
				const		float4		backgroundColor,
				write_only	image2d_t	outputImage)

{
	int2 coord = (int2)(get_global_id(0), get_global_id(1));

	int2 size = get_image_dim(outputImage);

	float4 color;

	color.x = (((float)coord.x) / size.x) * native_sin(((float)coord.x)/20.0f + time*10.0f);
	color.y = ((float)coord.y) / size.y * native_sin(time*2.0f);
	color.z = backgroundColor.z;
	color.w = 0.0f;
	
	float factor = 1.5f-(color.x + color.y) /2.0f;

	color.x *= copysign(native_sin(time), 1.0f) * factor;
	color.y *= copysign(native_sin(time+3.141592635f), 1.0f) * factor;

	write_imagef(outputImage, coord, color);
}";
//
//kernel void render (	const		float4		cameraPosition,
//						const		float16		unprojectionMatrix,
//						write_only	image2d_t	outputImage)
//{
//	int2 coord = (int2)(get_global_id(0), get_global_id(1));
//	int2 size = get_image_dim(outputImage);
//
//	// convert to normilized device coordinates
//	float2 screenPoint2d = (float2)(2.0f, 2.0f) * convert_float2(coord) / convert_float2(size) - (float2)(1.0f, 1.0f);
//
//	// unproject screen point to world
//	float4 screenPoint = (float4)(screenPoint2d.x, screenPoint2d.y, 0.0f, 1.0f);
//	float4 rayOrigin;// = 
//	transformVector(unprojectionMatrix, screenPoint);
//	rayOrigin.w = 1;
//	float4 rayDirection = native_normalize(rayOrigin, cameraPosition);
//
//	// create test sphere
//	float radius = 1;
//	float4 spherePosition = (float4)(0.0f, 0.0f, 0.0f, 1.0f);
//
//	// cast ray
//	float4 color;
//	float t = raySphereIntersect(rayOrigin, rayDirection, spherePosition, radius);
//
//	if (t > 0)
//	{
//		float4 collisionPoint = rayOrigin + t * rayDirection;
//		float4 surfaceNormal = collisionPoint - spherePosition;
//		surfaceNormal = normalize(surfaceNormal);
//		
//		color = (float4)(1.0f, 0.0f, 0.0f, 0.0f);
//	}
//
//	write_imagef(outputImage, coord, color);
//}
//
//// transfrom a vector by a row-major matrix
//float4 transformVector(	__private	float4*		transform, 
//					__private	float4		vector)
//{
//	float4 result;
//	result.x = dot(vector, transform[0]);
//	result.y = dot(vector, transform[2]);
//	result.z = dot(vector, transform[3]);
//	result.w = dot(vector, transform[4]);
//
//	// homogeneous divide to normalize scale
//	result /= result.w;
//
//	return result;
//}
//
//
//private float raySphereIntersect(	private float4	origin, 
//							private float4	direction, 
//							private float4	center, 
//							private float	radius)
//{
//	float4 originSubCenter = origin - center;
//
//	float b = dot(direction, originSubCenter);
//	float c = dot(originSubCenter, originSubCenter) - radius * radius;
//
//	float sqrtBC = native_sqrt(b * b - c);
//	// if sqrtBC < 0, ray misses sphere
//
//	float tPos = -b + sqrtBC;
//	float tNeg = -b - sqrtBC;
//
//	float minT = min(tPos, tNeg);
//
//	return minT;
//}
//";

		#endregion

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
			//build and compile an OpenCL program to change screen colors
			_renderKernel = null;
			_clProgram = new ComputeProgram(_commandQueue.Context, _GPUraytracingSource);
			try
			{
				// build the program
				_clProgram.Build(null, null, null, IntPtr.Zero);

				// create a reference a kernel function
				_renderKernel = _clProgram.CreateKernel("cycleColors");
				//_renderKernel = _clProgram.CreateKernel("render");
			}
			catch (BuildProgramFailureComputeException ex)
			{
				String buildLog = _clProgram.GetBuildLog(_commandQueue.Device);
				System.Diagnostics.Trace.WriteLine(buildLog);

				// Unable to handle error. Terminate application.
				Environment.Exit(-1);
			}
			catch (InvalidBuildOptionsComputeException ex)
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

			// Allocate space for texture with undefined data.
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

			_renderKernel.SetValueArgument<float>(0, time);	// test value
			_renderKernel.SetValueArgument<Color4>(1, Color4.DarkBlue);
			_renderKernel.SetMemoryArgument(2, _renderTarget);
			
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
