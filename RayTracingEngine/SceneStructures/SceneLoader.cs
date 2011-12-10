using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;

using Raytracing.Primitives;
using Raytracing.SceneStructures;

namespace Raytracing
{
	public static class SceneLoader
	{
		public static void buildScene(List<Triangle> triangles)
		{
			
			Triangle t0 = new Triangle(Vector4.UnitX, Vector4.UnitY, Vector4.UnitZ, 1);
		}

		public static void loadSceneFromFlatFile(string filename, List<Triangle> triangles)
		{
			StreamReader infile = new StreamReader(filename);

			string line;

			while((line = infile.ReadLine()) != null)
			{
				bool isVertex = line.IndexOf(' ') != -1;	// If there is more then one token, this is a vertex line

				char[] splitMarkers = {' '};
				string[] tokens = line.Split(splitMarkers);

				if (isVertex)
				{

				}
			}
			infile.Close();

			/*
			int itemsToRead = 0;
			bool readingTriangles = false;

			vertex2d vertexBuffer[3];
			int verticesRead = 0;

			while( getline(infile, line) )
			{
				bool isVertex = (line.find_first_of(" ") != string::npos);	// If there is more then one token, this is a vertex line

				stringstream tokens(line);

				if (!isVertex)
				{
					tokens >> itemsToRead;
					readingTriangles = !readingTriangles;
					cout << itemsToRead << endl;
				}
				else // read vertex data
				{
					vertex2d v;

					tokens >> v.x;
					tokens >> v.y;
					tokens >> v.color.r;
					tokens >> v.color.g;
					tokens >> v.color.b;
					tokens >> v.color.a;

					cout << v << endl;

					if(readingTriangles)
					{
						if (verticesRead < 3)
						{
							vertexBuffer[verticesRead++] = v;
						}
				
						if (verticesRead == 3)
						{
							triangle2d tri;
							std::copy(vertexBuffer, &vertexBuffer[3], tri.vertices);	// Copy 3 vertices
							triangles.push_back(tri);
							verticesRead = 0;
						}
					}
					else
					{
						if (verticesRead < 2)
						{
							vertexBuffer[verticesRead++] = v;
						}

						if (verticesRead == 2)
						{
							line2d ln;
							std::copy(vertexBuffer, &vertexBuffer[2], ln.vertices);	// Copy 2 vertices
							lines.push_back(ln);
							verticesRead = 0;
						}
					}
				}
			}
			infile.close();
			 * */
		}

		public static bool loadSceneFromXml(string filename, Scene scene, RayTracingCamera camera)
		{
			XmlTextReader reader = new XmlTextReader(filename);

			XmlDocument doc = new XmlDocument();
			try
			{
				doc.Load(reader);
			}
			catch (DirectoryNotFoundException)
			{
				reader.Close();
				return false;
			}

			

			XmlNode root = doc.FirstChild;

			// Get background color
			XmlNode backgroundColorNode = root.SelectSingleNode("color");
			if(backgroundColorNode != null)
			{
				scene.BackgroundColor = getColor(backgroundColorNode);
			}

			// Invert the z coordinate if the file uses left handed coordinates.
			string handedness = root.Attributes["handedness"] != null ? root.Attributes["handedness"].Value : "left";
			int invertZ = handedness.Equals("right") ? 1 : -1;

			int lightsLoaded=0, primsLoaded=0, dirLightsLoaded=0;
			foreach (XmlNode node in root.ChildNodes)
			{
				//Console.WriteLine(node.Name);
				if (node.Name == "camera")
				{
					// <camera upZ="0.0" upY="1.0" upX="0.0" lookAtZ="0.0" lookAtY="2.0" lookAtX="0.0" fov="90.0" z="-12.0" y="2.0" x="0.0"/>
					// Need to invert x, since forward for the camera is <0,0,-1>

					Vector3 position = getXyzVector(node, invertZ);

					float upX = float.Parse(node.Attributes["upX"].Value);
					float upY = float.Parse(node.Attributes["upY"].Value);
					float upZ = -float.Parse(node.Attributes["upZ"].Value);
					Vector3 up = new Vector3(upX, upY, upZ);

					float lookAtX = float.Parse(node.Attributes["lookAtX"].Value);
					float lookAtY = float.Parse(node.Attributes["lookAtY"].Value);
					float lookAtZ = -float.Parse(node.Attributes["lookAtZ"].Value);
					Vector3 lookAt = new Vector3(lookAtX, lookAtY, lookAtZ);

					float fov = float.Parse(node.Attributes["fov"].Value);
					Vector3 forward = lookAt - position;
					up.Normalize();
					forward.Normalize();

					// Set camera pose
					camera.Forward = forward;
					camera.Up = up;
					camera.Position = position;

					// Convert hFOV into vFOV
					camera.VerticalFieldOfView = fov / camera.AspectRatio;
				}
				else if (node.Name == "light" || node.Name == "PointLight")
				{
					lightsLoaded++;
					Vector3 point = getXyzVector(node, invertZ);
					
					Color4 color = getColor(node.SelectSingleNode("color"));
					PointLight p = new PointLight(point, 1.0f, color);
					scene.add(p);

					float ambientValue = float.Parse(node.Attributes["ambient"].Value);
					scene.Ambiant += ambientValue;
				}
				else if (node.Name == "DirectionalLight")
				{
					dirLightsLoaded++;
					Vector3 direction = getXyzVector(node, invertZ);
					Color4 color = getColor(node.SelectSingleNode("color"));

					scene.add(new DirectionalLight(direction, 1.0f, color));

					float ambientValue = float.Parse(node.Attributes["ambient"].Value);
					scene.Ambiant += ambientValue;
				}
				else if (node.Name == "sphere")
				{
					primsLoaded++;
					Vector3 point = getXyzVector(node, invertZ);

					float radius = float.Parse(node.Attributes["radius"].Value);
					//Console.WriteLine("Radius: " + radius);

					OpenTK.Graphics.Color4 color = getColor(node.SelectSingleNode("color"));

					Material m = new Material(color);
					XmlNode materialNode = node.SelectSingleNode("material");
					m.phongExponent = float.Parse(materialNode.Attributes["phongExponent"].Value);
					m.Reflectivity = float.Parse(materialNode.Attributes["reflectance"].Value);
					m.RefractiveIndex = float.Parse(materialNode.Attributes["refraction"].Value);
					m.Transparency = 1.0f - color.A;	// transparency = 1 - alpha

					Sphere s = new Sphere(point, radius, m);
					scene.add(s);
				}
			}

			Console.WriteLine("\nLoaded: " + primsLoaded + " primitives, " + lightsLoaded + " lights, " 
								+ dirLightsLoaded + " directional lights");
			return true;
		}

		private static Vector3 getXyzVector(XmlNode node, int invertZ)
		{
			float x = float.Parse(node.Attributes["x"].Value);
			float y = float.Parse(node.Attributes["y"].Value);
			float z = float.Parse(node.Attributes["z"].Value) * invertZ;
			return new Vector3(x, y, z);
		}

		private static Color4 getColor(XmlNode colorNode)
		{
			float r = float.Parse(colorNode.Attributes["r"].Value);
			float g = float.Parse(colorNode.Attributes["g"].Value);
			float b = float.Parse(colorNode.Attributes["b"].Value);
			float a = float.Parse(colorNode.Attributes["a"].Value);

			return new OpenTK.Graphics.Color4(r, g, b, a);
		}
	}
}
