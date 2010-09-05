using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.IO;

using OpenTK;
using OpenTK.Graphics;

using MuxEngine.Movables;
using MuxEngine.LinearAlgebra;

using Raytracing;
using Raytracing.Primitives;
using Raytracing.SceneStructures;

namespace Raytracing.Driver
{
	/// <summary>
	/// This is the main type for your game
	/// </summary>
	public class RayTracingDriver : GameWindow
	{

		private static float CameraMovementSpeed = 0.1f;	// in units
		private static float CameraRotationSpeed = 0.5f;	// in degrees

#if DEBUG
		private static bool limitFrames = true;
#else
		private static bool limitFrames = false;
#endif

		private static int profileFrameLimit = 1;
		private int frameCount = 0;

		GraphicsDeviceManager graphics;
		SpriteBatch spriteBatch;
		RayTracingCamera _rtCamera;
		GridScene _gridScene;
		SpriteFont _font;

		Sphere _movingSphere;

		public RayTracingDriver()
		{
			graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";

			graphics.IsFullScreen = false;

			graphics.PreferredBackBufferWidth = 800;
			graphics.PreferredBackBufferHeight = 600;
		}

		/// <summary>
		/// Allows the game to perform any initialization it needs to before starting to run.
		/// This is where it can query for any required services and load any non-graphic
		/// related content.  Calling base.Initialize will enumerate through any components
		/// and initialize them as well.
		/// </summary>
		protected override void Initialize()
		{

			//base.Initialize();
		}

		/// <summary>
		/// LoadContent will be called once per game and is the place to load
		/// all of your content.
		/// </summary>
		protected override void LoadContent()
		{

			// load font
			_font = Content.Load<SpriteFont>("genericFont");

			// Create a new SpriteBatch, which can be used to renderAsSprite textures.
			spriteBatch = new SpriteBatch(GraphicsDevice);

			Box2 screenBounds = this.Window.ClientBounds;

			/* Create the scene */
			_gridScene = new GridScene(16, 1);
			_gridScene.BackgroundColor = Color4.Gray;

			/* Add objects to the scene*/
			buildBlockScene(_gridScene);
			//buildFlatScene(_gridScene);
			//_gridScene = buildXYZScene(_gridScene);
			//_gridScene = buildDemoScene(_gridScene);

			// create the camera
			// looking down the Z-axis into the scene
			Vector3 cameraPosition = new Vector3(0,0,40f);	
			Quaternion cameraRotation = Quaternion.Identity;

			_rtCamera = new RayTracingCamera(Window.ClientBounds, -Vector3.UnitZ, Vector3.UnitY, cameraPosition);
			_rtCamera.VerticalFieldOfView = 90.0f;
			_rtCamera.computeProjection();
			_rtCamera.Position = cameraPosition;
			_rtCamera.setRotation(cameraRotation);
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
						Color c = getColor(low, high, x, y, z);
						Material mat = new Material(c, 0);
						Sphere s = new Sphere(new Vector3(x, y, z), 0.25f, mat);

						scene.add(s);
					}
				}
			}

			int width = high - low;
			int objects = (int)Math.Pow(width, 3);
			System.Console.WriteLine("added " + objects + " spheres.");
		}

		private Color4 getColor(int start, int end, int x, int y, int z)
		{
			float percentX = (float)(x - start) / (float)(end - start);
			float percentY = (float)(y - start) / (float)(end - start);
			float percentZ = (float)(z - start) / (float)(end - start);

			return new Color(percentX, percentY, percentZ);
		}

		private void buildFlatScene(Scene scene)
		{

			bool lightColored = false;
			bool colorRed = false;
			Color currentColor;
			for (int y = -5; y <= 5; y++)
			{
				for (int x = -5; x <= 5; x++)
				{
					if (colorRed && lightColored)
						currentColor = Color.LightSalmon;
					else if (colorRed && !lightColored)
						currentColor = Color.Red;
					else if (!colorRed && lightColored)
						currentColor = Color.LightGreen;
					else
						currentColor = Color.Green;


					scene.add(new Sphere(new Vector3(x, y, 0), 0.5f, new Material(currentColor)));
					lightColored = !lightColored;
				}
				colorRed = !colorRed;
			}
		}

		private void buildXYZScene(Scene scene)
		{
			// XYZ => RGB
			scene.add(new Sphere(Vector3.UnitX, 0.5f, new Material(Color.Red)));
			scene.add(new Sphere(Vector3.UnitY, 0.5f, new Material(Color.Green)));
			scene.add(new Sphere(Vector3.UnitZ, 0.5f, new Material(Color.Blue)));

			scene.add(new Sphere(-Vector3.UnitX, 0.5f, new Material(Color.DarkRed)));
			scene.add(new Sphere(-Vector3.UnitY, 0.5f, new Material(Color.DarkGreen)));
			scene.add(new Sphere(-Vector3.UnitZ, 0.5f, new Material(Color.DarkBlue)));
		}

		private void buildDemoScene(Scene scene)
		{
			Material basicSphereMaterial = new Material(Color.Red);
			Material basicBoxMaterial = new Material(Color.Green);

			// create some light
			Vector3 lightPos1 = 25 * Vector3.UnitY;
			PointLight pl = new PointLight(lightPos1, 5, Color.White);
			//sceneList.add(pl);
			scene.add(pl);

			// create a plane for things to sit on
			Vector3 planeNormal = new Vector3(0, 1, 0);
			float planeDistance = 10;
			Vector4 planeDef = new Vector4(planeNormal, planeDistance);
			ScenePlane sp = new ScenePlane(planeDef, new Material(Color.BurlyWood));
			//scene.add(sp);

			// add single sphre to move along the x-axis
			_movingSphere = new Sphere(Vector3.Zero, 1.5f, new Material(Color.Yellow));
			//scene.add(_movingSphere);

			// create a single sphere for shadow demonstration
			Vector3 spherePos3 = 10 * Vector3.UnitY;
			Sphere shadowSphere = new Sphere(spherePos3, 0.5f, new Material(Color.RoyalBlue, 0.5f));
			scene.add(shadowSphere);

			// create row of spheres
			Vector3 rowPosition = 8 * Vector3.Forward + 12 * Vector3.Left;

			Sphere rs1 = new Sphere(rowPosition, 5, new Material(Color.Purple, 1.0f));
			scene.add(rs1);

			rowPosition.X += 12;
			Sphere rs2 = new Sphere(rowPosition, 5, new Material(Color.Green, 0.7f));
			scene.add(rs2);

			rowPosition.X += 12;
			Sphere rs3 = new Sphere(rowPosition, 5, new Material(Color.IndianRed));
			scene.add(rs3);

			rowPosition = 8 * Vector3.Backward+ 12 * Vector3.Left;

			Sphere rs4 = new Sphere(rowPosition, 5, new Material(Color.Purple, 1.0f));
			scene.add(rs4);

			rowPosition.X += 12;
			Sphere rs5 = new Sphere(rowPosition, 5, new Material(Color.Green, 0, 0.7f, 1.2f));
			scene.add(rs5);

			rowPosition.X += 12;
			Sphere rs6 = new Sphere(rowPosition, 5, new Material(Color.IndianRed, 0.2f));
			scene.add(rs6);

			//_monkey = Content.Load<Model>("Models/monkey");
			//_movingMonkey = new MovableModel(_monkey);
		}

		/// <summary>
		/// UnloadContent will be called once per game and is the place to unload
		/// all content.
		/// </summary>
		protected override void UnloadContent()
		{
			Content.Unload();
		}

		/// <summary>
		/// Allows the game to run logic such as updating the world,
		/// checking for collisions, gathering input, and playing audio.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Update(GameTime gameTime)
		{
			// constant time for preformance testing
			if (limitFrames && frameCount++ > profileFrameLimit)
			{
				this.Exit();
			}

			// move objects
			//Vector3 movePos = _movingSphere.Position;
			//movePos.X = (float)(15 * Math.Sin((float)frameCount/100.0f));
			//_movingSphere.Position = movePos;

			_rtCamera.rotateAboutPoint(Vector3.Zero, Vector3.UnitY, 0.1f);
			_rtCamera.rotateAboutPoint(Vector3.Zero, Vector3.UnitX, 0.05f);
			_rtCamera.rotateAboutPoint(Vector3.Zero, Vector3.UnitZ, 0.05f);


			KeyboardState kbd = Keyboard.GetState();

			// Allows the game to exit
			if (kbd.IsKeyDown(Keys.Escape))
			{
				this.Exit();
			}

			if (kbd.IsKeyDown(Keys.A))
			{
				_rtCamera.moveLocal(Vector3.Left, CameraMovementSpeed);
			}
			else if (kbd.IsKeyDown(Keys.E))
			{
				_rtCamera.moveLocal(Vector3.Right, CameraMovementSpeed);
			}

			if (kbd.IsKeyDown(Keys.OemComma))
			{
				_rtCamera.moveLocal(Vector3.Forward, CameraMovementSpeed);
			}
			else if (kbd.IsKeyDown(Keys.O))
			{
				_rtCamera.moveLocal(Vector3.Backward, CameraMovementSpeed);
			}

			if (kbd.IsKeyDown(Keys.OemPeriod))
			{
				_rtCamera.moveLocal(Vector3.Up, CameraMovementSpeed);
			}
			else if (kbd.IsKeyDown(Keys.J))
			{
				_rtCamera.moveLocal(Vector3.Down, CameraMovementSpeed);
			}

			if (kbd.IsKeyDown(Keys.NumPad4))
			{
				_rtCamera.yaw(CameraRotationSpeed);
			}
			else if (kbd.IsKeyDown(Keys.NumPad6))
			{
				_rtCamera.yaw(-CameraRotationSpeed);
			}

			if (kbd.IsKeyDown(Keys.NumPad8))
			{
				_rtCamera.pitch(CameraRotationSpeed);
			}
			else if (kbd.IsKeyDown(Keys.NumPad2))
			{
				_rtCamera.pitch(-CameraRotationSpeed);
			}

			if (kbd.IsKeyDown(Keys.NumPad7))
			{
				_rtCamera.roll(-CameraRotationSpeed);
			}
			else if (kbd.IsKeyDown(Keys.NumPad9))
			{
				_rtCamera.roll(CameraRotationSpeed);
			}

			base.Update(gameTime);
		}

		/// <summary>
		/// This is called when the game should renderAsSprite itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Draw(GameTime gameTime)
		{
			// might not even need this call with the ray tracer...
			GraphicsDevice.Clear(Color.CornflowerBlue);

			// NOTE: MUST CALL THIS IF THE CAMERA HAS MOVED!
			_rtCamera.computeView();

			//_cam.computeView();

			//_movingMonkey.Draw(_cam);

			// renderAsSprite ray traced image as a sprite texture
			spriteBatch.Begin();

				//_rtCamera.renderAsSprite(this.GraphicsDevice, spriteBatch, _scene1);

				_rtCamera.renderAsSprite(this.GraphicsDevice, spriteBatch, _gridScene);

				int stringDrawHeight = 1;
				String screenSize = GraphicsDevice.Viewport.Width + " x " + GraphicsDevice.Viewport.Height;
				spriteBatch.DrawString(_font, screenSize, new Vector2(5, stringDrawHeight), Color.DarkGreen);
				stringDrawHeight += _font.LineSpacing;

				//String position = _rtCamera.Position.ToString();
				//spriteBatch.DrawString(_font, position, new Vector2(5, stringDrawHeight), Color.DarkGreen);
				//stringDrawHeight += _font.LineSpacing;

				//String rotation = _rtCamera.getRotation().ToString();
				//spriteBatch.DrawString(_font, rotation, new Vector2(5, stringDrawHeight), Color.DarkGreen);
				//stringDrawHeight += _font.LineSpacing;

				double fps = 1.0f / gameTime.ElapsedRealTime.TotalSeconds;
				String fpsString = "fps="+String.Format("{0:0.##}", fps);
				spriteBatch.DrawString(_font, fpsString, new Vector2(5, stringDrawHeight), Color.DarkRed);

			spriteBatch.End();
			

			base.Draw(gameTime);
		}

	}
}
