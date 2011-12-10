using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK;

using SimpleTracer.RayTracing;

namespace KDTreeTracer.RayTracing
{
	// Some of the following code was inspired on examples from the 
	// book "Physically Based Rendering", by Pharr and Humphreys.
	class KDTree : Accelerator
	{
		int _maxPrims;
		int _maxDepth;
		List<Sphere> _primitives;
		KdTreeNode[] _nodes;

		BBox _bounds;

		int iCost = 4;				// Relative cost of intersection test
		int tCost = 1;				// Relative cost of node traversel
		float emptyBonus = 1.0f;	// Bonus for empty nodes
		int nextFreeNode = 0;
		int nAllocedNodes = 0;

		public KDTree(List<Sphere> spheres, int maxPrimsPerNode)
		{
			// Store reference
			_primitives = spheres;
			_nodes = new KdTreeNode[0];

			_maxPrims = maxPrimsPerNode;

			// Guess a usefull bound on the maximum tree depth
			_maxDepth = 8 + (int)(1.3*Math.Log(spheres.Count, 2));

			// calculate primitive bounds, plus overall scene bounds
			BBox[] allPrimBounds = new BBox[_primitives.Count];
			for (int i=0; i<_primitives.Count; i++)
			{
				allPrimBounds[i] = _primitives[i].getBounds();
				_bounds.union( allPrimBounds[i] );
			}

			// Allocate temporary working space
			BoundEdge[][] edges = new BoundEdge[3][];
			for (int i = 0; i < 3; i++) { edges[i] = new BoundEdge[2 * _primitives.Count]; }

			// Allocate array to store overlaping primitive indices
			int[] primNums = new int[_primitives.Count];
			int primNumsBase = 0;
			for(int i=0; i<_primitives.Count; i++) { primNums[i]=i; }

			// Arries to hold subarries of primitive indices
			int[] prims0 = new int[_primitives.Count];
			int prims1Base = 0;
			int[] prims1 = new int[ (_maxDepth+1) * _primitives.Count];

			Console.WriteLine("Building Tree with " + _primitives.Count + " primitives...\n");

			// Start recursive construction tree
			buildTree(0, _bounds, allPrimBounds, primNums, primNumsBase, _primitives.Count, _maxDepth, edges, prims0, prims1, prims1Base, 0);

			Console.WriteLine("Finished building tree with " + nextFreeNode + " nodes");
		}

		public override float getNearestIntersection(ref Ray ray, out Sphere primHit)	// ref Vector3 collisionPoint, ref Vector3 surfaceNormal, ref Material mat)
		{
#if DEBUG
			int leafNodesChecked = 0;
			int internalNodesChecked = 0;
#endif

			primHit = null;

			// Store the parametric range for intersection with the current node
			float tMin=0, tMax=float.PositiveInfinity;

			// Break if we are never going to hit the scene, else store tMin and tMax
			if ( float.IsPositiveInfinity( _bounds.intersect(ref ray, ref tMin, ref tMax) ) )
				{ return float.PositiveInfinity; }

			// Convert ray data to an array of floats. Might be faster?
			float[] origin = getArray(ray.Origin);
			float[] direction = getArray(ray.Direction);

			Vector3 invDirVec = new Vector3(1/ray.Direction.X, 1/ray.Direction.Y, 1/ray.Direction.Z);
			float[] invDir = getArray(invDirVec);

			KdToDo[] todo = new KdToDo[_maxDepth];
			int todoPos = 0;

			// Traverse nodes
			bool hit = false;
			int nodeIndex = 0;
			int firstChildIndex = 0, secondChildIndex;
			KdTreeNode node = _nodes[nodeIndex];
			while (nodeIndex >= 0 && nodeIndex < nAllocedNodes)	//(node != null)
			{
				if (ray.tMax < tMin) break;		// Ray will never hit anything in this node

				node = _nodes[nodeIndex];

				if ( node.axis != Axis.none )	// If the node is not a leaf, it has a split axis defined
				{
#if DEBUG
					internalNodesChecked++;
#endif
					// Find dist to split plane
					Axis axis = node.axis;

					// Get distance to split plane in terms of t
					float tplane = (node.splitPos - origin[(int)axis] ) * invDir[(int)axis];

					// Check if we intersect the child above or below the split axis first
					bool belowFirst = ( origin[(int)axis] < node.splitPos ) ||
									 ( origin[(int)axis] == node.splitPos && direction[(int)axis] >= 0);

					if (belowFirst)
					{
						firstChildIndex = nodeIndex+1;
						secondChildIndex = node.aboveChild;
					}
					else
					{
						firstChildIndex = node.aboveChild;
						secondChildIndex = nodeIndex+1;
					}

					// Advance to next child
					if (tplane > tMax || tplane <= 0)	// If the split plane is further then the maximum split distance or only the below child needs to be cheked
					{	
						nodeIndex = firstChildIndex;
					}
					else if (tplane < tMin)				// Will never intersect the first child, since the ray is pointing away from it
					{
						nodeIndex = secondChildIndex;
					}
					else
					{
						// Store the second child
						todo[todoPos].nodeIndex = secondChildIndex;
						todo[todoPos].tMin = tplane;
						todo[todoPos].tMax = tMax;
						todoPos++;

						// Traverse the first child
						nodeIndex = firstChildIndex;
						tMax = tplane;
					}

				}
				else
				{
#if DEBUG
					leafNodesChecked++;
#endif
					// Leaf node
					int nPrimitives = node.nPrimitives;
					if (nPrimitives == 1)
					{
						Sphere prim = _primitives[node.primitiveIndex];
						float t = ray.intersects(prim);
						if ( t < float.PositiveInfinity && t < ray.tMax)
						{
							hit = true;
							primHit = prim;
							ray.tMax = t;
						}
					}
					else
					{
						for (int i=0; i<nPrimitives; i++)
						{
							Sphere prim = _primitives[ node.primitiveIndices[i] ];
							float t = ray.intersects(prim);
							if ( t < float.PositiveInfinity && t < ray.tMax)
							{
								hit = true;
								primHit = prim;
								ray.tMax = t;
							}
						}
					}

					// After checking a leaf node, load the next node to check
					if (todoPos > 0)
					{
						--todoPos;
						nodeIndex = todo[todoPos].nodeIndex;
						node = _nodes[nodeIndex];
						tMin = todo[todoPos].tMin;
						tMax = todo[todoPos].tMax;
					}
					else
						break;
				}
			}

			return (hit) ? ray.tMax : float.PositiveInfinity;
		}

		private void buildTree(	int nodeNum, BBox nodeBounds, BBox[] allPrimBounds, 
								int[] primNums, int primNumsBase, 
								int nPrimitives, int depth, BoundEdge[][] edges,
								int[] prims0, 
								int[] prims1, int prims1base, 
								int badRefines)
		{
			string indent = "".PadLeft(_maxDepth - depth);

			if (nextFreeNode == nAllocedNodes)
			{
				int nAlloc = Math.Max(2*nAllocedNodes, 512);
				KdTreeNode[] n = new KdTreeNode[nAlloc];
				if (nAllocedNodes > 0)
					Array.Copy(_nodes, n, nAllocedNodes);
				_nodes = n;
				nAllocedNodes = nAlloc;
			}
			++nextFreeNode;

			// *** Termination: Stop recursion if we have few enough nodes or the max depth has been reached ***
			if (nPrimitives <= _maxPrims || depth == 0)
			{
				_nodes[nodeNum] = KdTreeNode.makeLeaf(primNums, primNumsBase, nPrimitives);
				#if DEBUG
				Console.WriteLine(indent + _nodes[nodeNum]);
				#endif
				return;
			}

			// *** Not reached termination, so pick split plane and continue recursion ***
			Axis bestAxis = Axis.none;
			int bestOffset = -1;
			float bestCost = float.PositiveInfinity;
			float oldCost = iCost * (float)nPrimitives;
			float totalSA = nodeBounds.surfaceArea();
			float invTotalSA = 1.0f/totalSA;
			Vector3 d = nodeBounds.pMax - nodeBounds.pMin;
			Axis axis = nodeBounds.longestAxis();	// Start by checking the longest axis
			int retries = 0;
			retrySplit:

			// Fill edge lists
			for (int i=0; i<nPrimitives; i++)
			{
				int primIndex = primNums[primNumsBase + i];
				BBox bbox = allPrimBounds[primIndex];
				edges[(int)axis][2*i] = new BoundEdge( getAxisVal(bbox.pMin, axis), primIndex, true);
				edges[(int)axis][2*i+1] = new BoundEdge(getAxisVal(bbox.pMax, axis), primIndex, false);
			}
			Array.Sort(edges[(int)axis], 0, 2*nPrimitives);	// sort the edges of the first n primitives (2*n edges)

			// Find the best split by checking cost of every possible split plane
			int nBelow = 0, nAbove = nPrimitives;
			for (int i=0; i<2*nPrimitives; i++)
			{
				if( edges[(int)axis][i].type == BoundType.End) --nAbove;
				float edgeT = edges[(int)axis][i].t;
				if (edgeT > getAxisVal(nodeBounds.pMin, axis) &&
					edgeT < getAxisVal(nodeBounds.pMax, axis) )
				{
					// Compute split cost for the ith edge
					Axis otherAxis0 = (Axis) (	((int)axis+1)	% 3);
					Axis otherAxis1 = (Axis) (	((int)axis+2)	% 3);
					float belowSA = 2* (getAxisVal(d, otherAxis0) * getAxisVal(d, otherAxis1) +
										(edgeT - getAxisVal(nodeBounds.pMin, axis)) *
										(getAxisVal(d, otherAxis0) + getAxisVal(d, otherAxis1)));
					float aboveSA = 2*(getAxisVal(d, otherAxis0) * getAxisVal(d, otherAxis1) + 
										(getAxisVal(nodeBounds.pMax, axis) - edgeT) *
										(getAxisVal(d, otherAxis0) + getAxisVal(d, otherAxis1)));
					float pBelow = belowSA * invTotalSA;
					float pAbove = aboveSA * invTotalSA;
					float eb = (nAbove == 0 || nBelow == 0) ? emptyBonus : 0;
					float cost = tCost + iCost * (1-eb) * (pBelow * nBelow + pAbove * nAbove);

					// Save this split if it is better the previous split position
					if(cost < bestCost)
					{
						bestCost = cost;
						bestAxis = axis;
						bestOffset = i;
					}
					
				}
				if (edges[(int)axis][i].type == BoundType.Start) ++nBelow;
			}

			// Create a leaf node if no good split was found
			if(bestAxis == Axis.none && retries < 2)
			{
				++retries;
				axis = (Axis) ( ((int)axis+1) % 3);
				goto retrySplit;
			}
			if (bestCost > oldCost) ++badRefines;
			if (( bestCost > 4.0f * oldCost && nPrimitives < 16) || bestAxis == Axis.none || badRefines == 3)
			{
				_nodes[nodeNum] = KdTreeNode.makeLeaf(primNums, primNumsBase, nPrimitives);
				#if DEBUG
				Console.WriteLine(indent + _nodes[nodeNum] + " (split failure)");
				#endif
				return;
			}

			// Classifiy primitives with respect to split
			int n0=0, n1=0;
			for (int i=0; i<bestOffset; i++)
			{
				if(edges[(int)bestAxis][i].type == BoundType.Start)
					prims0[n0++] = edges[(int)bestAxis][i].primNum;
			}
			for (int i=bestOffset+1; i<2*nPrimitives; i++)
			{
				if(edges[(int)bestAxis][i].type == BoundType.End)
					prims1[prims1base + n1++] = edges[(int)bestAxis][i].primNum;
			}

			// Ititalize child nodes
			float tsplit = edges[(int)bestAxis][bestOffset].t;
			BBox bounds0 = nodeBounds, bounds1 = nodeBounds;
			setAxisVal(ref bounds0.pMax, bestAxis, tsplit);
			setAxisVal(ref bounds1.pMin, bestAxis, tsplit);

#if DEBUG
			KdTreeNode testNode = KdTreeNode.makeInternal(bestAxis, nextFreeNode, tsplit);
			int[] aboveIndices = new int[n1];
			int[] belowIndices = new int[n0];
			Array.Copy(prims0, belowIndices, n0);
			Array.Copy(prims1, prims1base, aboveIndices, 0, n1);

			Console.WriteLine(indent + testNode);
			Console.WriteLine(indent + " Above: { " + String.Join(",", aboveIndices) + " }");
			Console.WriteLine(indent + " Below: { " + String.Join(",", belowIndices) + " }");
#endif

			buildTree(nodeNum+1, bounds0, allPrimBounds, prims0, 0, n0,
						depth-1, edges, prims0, prims1, prims1base+nPrimitives-1, badRefines);

			int aboveChild = nextFreeNode;
			_nodes[nodeNum] = KdTreeNode.makeInternal(bestAxis, aboveChild, tsplit);

			buildTree(aboveChild, bounds1, allPrimBounds, prims1, prims1base, n1,
						depth-1, edges, prims0, prims1, prims1base+nPrimitives-1, badRefines);
		}

		// Turn a vector into an array of floats
		private static float[] getArray(Vector3 vec)
		{
			float[] array = new float[3];
			array[0] = vec.X;
			array[1] = vec.Y;
			array[2] = vec.Z;
			return array;
		}

		private float getAxisVal(Vector3 vec, Axis axis)
		{
			switch (axis)
			{
				case Axis.x: return vec.X;
				case Axis.y: return vec.Y;
				case Axis.z: return vec.Z;
				default: return float.NaN;
			}
		}

		private void setAxisVal(ref Vector3 vec, Axis axis, float value)
		{
			switch (axis)
			{
				case Axis.x: vec.X = value; break;
				case Axis.y: vec.Y = value; break;
				case Axis.z: vec.Z = value; break;
			}
		}

		private enum BoundType {End, Start}

		private struct KdToDo
		{
			public int nodeIndex;
			public float tMin, tMax;
		}


		// Pointerless kd tree node
		private struct KdTreeNode
		{
			public float splitPos;
			public int primitiveIndex;
			public int[] primitiveIndices;

			public Axis axis;
			public int nPrimitives;
			public int aboveChild;

			public bool IsLeaf()			{ return axis == Axis.none; }

			public override string ToString()
			{
				if ( IsLeaf() )
				{
					StringBuilder sb = new StringBuilder("*Leaf: { ");
					if (nPrimitives == 1)
					{
						sb.Append(primitiveIndex);
					}
					else if (nPrimitives > 0)
					{
						sb.Append(String.Join(", ", primitiveIndices));
					}
					sb.Append(" }");
					return sb.ToString();
				}
				else
				{
					StringBuilder sb = new StringBuilder(">Internal: split on ");
					sb.Append(axis);
					sb.Append(" = ");
					sb.Append( splitPos );
					return sb.ToString();
				}
			}

			public static KdTreeNode makeLeaf(int[] primIndices, int primIndicesBase, int numPrims)
			{
				KdTreeNode node = new KdTreeNode();
				node.axis = Axis.none;
				node.nPrimitives = numPrims;
				if(numPrims == 0)
				{
					node.primitiveIndex = 0;
				}
				else if (numPrims == 1)
				{
					node.primitiveIndex = primIndices[primIndicesBase];
				}
				else
				{
					node.primitiveIndices = new int[numPrims];
					Array.Copy(primIndices, primIndicesBase, node.primitiveIndices, 0, numPrims);
				}

				return node;
			}

			public static KdTreeNode makeInternal(Axis axis, int aboveChildIndex, float split)
			{
				KdTreeNode node = new KdTreeNode();
				node.axis = axis;
				node.splitPos = split;
				node.aboveChild = aboveChildIndex;

				return node;
			}
		}

		private struct BoundEdge : IComparable
		{
			public BoundEdge(float tt, int pn, bool starting)
			{
				t = tt;
				primNum = pn;
				type =  starting ? BoundType.Start : BoundType.End;
			}

			public int CompareTo(Object obj)
			{
				BoundEdge other = (BoundEdge) obj;
				if (t == other.t)
				{
					return (int)other.type - (int)type;
				}
				return t.CompareTo(other.t);
			}

			public float t;
			public int primNum;
			public BoundType type;
		}
	}
}
