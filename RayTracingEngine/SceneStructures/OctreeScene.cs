using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//using Microsoft.Xna.Framework;
using OpenTK;

using Raytracing.Primitives;

namespace Raytracing.SceneStructures
{
	class OctreeScene : Scene
	{
		private const int MinCellWidth = 4;	// must be a power of 2

		List<Primitive> _primitives;
		List<PointLight> _lights;

		Vector3 _gridOrigin;
		Vector3 _gridMax;

		Node _root;

		int _cellSize;

		#region Constructor and init methods

		/// <summary>
		/// 
		/// </summary>
		/// <param name="gridWidth">How wide the root of the tree is.</param>
		/// <param name="cellSize">How many units wide each cell is.</param>
		public OctreeScene(int sceneWidth)
		{
			// create list of lights
			_lights = new List<PointLight>();

			// create the octree structure
			float halfWidth = sceneWidth/2;
			Vector3 max = new Vector3(halfWidth, halfWidth, halfWidth);
			Vector3 min = new Vector3(-halfWidth, -halfWidth, -halfWidth);
			_root = new Node(new BoundingBox(min, max));

		}
		#endregion

		#region Public methods

		public override void add(Primitive model)
		{
			// TODO: keep a  model->node map to update model positions as needed for animations.
			_root.add(model);
		}

		public override void addPointLight(PointLight light)
		{
			_lights.Add(light);
		}



		public override float? getNearestIntersection(ref Ray r, ref Vector3 collisionPoint,
			ref Vector3 surfaceNormal, ref Material material)
		{
			

			return null;
		}


		public float? castRayPacket(ref Ray[,] r, ref Vector3 collisionPoint,
			ref Vector3 surfaceNormal, ref Material material)
		{
			// check each subtree for intersections
			// how to pick closest subtree?
			return null;
		}

		#endregion


		private class Node
		{
			public bool Empty
			{
				get;
				protected set;
			}

			public BoundingBox Bounds;

			public Node[] ChildNodes;

			public List<Primitives.Primitive> Geometry;

			public float? getIntersection()
			{
				return null;
			}

			// return a reference to the node the model was inserted into
			public Node add(Primitives.Primitive model)
			{
				if (ChildNodes == null)
				{
					Geometry.Add(model);
					return this;
				}

				// if we have child nodes, check if the model fits inside any of them
				foreach (Node n in ChildNodes)
				{
					if (n.Bounds.Contains(model.Position))
					{
						return n.add(model);
					}
				}

				// if control falls through to here, the model only entirly fits in the current node
				Geometry.Add(model);
				return this;
			}

			// constructor
			public Node(BoundingBox outerBounds)
			{
				Geometry = new List<Raytracing.Primitives.Primitive>();
				Bounds = outerBounds;
				ChildNodes = null;

				// if the node is large enough to split into child nodes
				float width = Bounds.Max.X - Bounds.Min.X;
				if (width > MinCellWidth )
				{
					ChildNodes = new Node[8];
					Vector3 start = new Vector3(OuterBounds.Min);	


					int halfWidth = width / 2;	// the node is a cube, so this is the same for all axis

					Vector3 halfVector = new Vector3(halfWidth, halfWidth, halfWidth);

					Vector3 max;
					Vector3 min = new Vector3(Bounds.Min);
					int octantIndex = 0;
					for (int x = 0; x < 2; ++x)
					{
						min.X = start.X + halfWidth * x;
						for (int y = 0; y < 2; ++y)
						{
							min.Y = start.Y + halfWidth * y;
							for (int z = 0; z < 2; ++z)
							{
								min.Z = start.Z + halfWidth * z;
								max = min + halfVector;
								ChildNodes[octantIndex++] = new Node(new BoundingBox(min, max));
							}
						}
					}
				}

			}


		}

	}
}
