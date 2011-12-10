using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using OpenTK;

using Raytracing.Primitives;

namespace Raytracing
{
	public class BvhTree // : Accelerator
	{
#if DEBUG
		string indent = "";
#endif

		protected int _maxPrimsInNode;
		protected LinearBVHNode[] _nodes;
		protected List<Triangle> _primitives;

		#region Private Structs/Classes

		[StructLayout(LayoutKind.Sequential)]
		public struct LinearBVHNode
		{
			public BBox bounds;
			public int primitivesOffset;
			public int secondChildOffset;
			public int nPrimitives;	// 0-> interior node
			public int axis;
		}

		private struct PointComparator : IComparer<BVHPrimitiveInfo>
		{
			int dim;

			public PointComparator(int dim)
			{
				this.dim = dim;
			}

			public int Compare(BVHPrimitiveInfo a, BVHPrimitiveInfo b)
			{
				return ((int)Math.Ceiling(b.centroid.axisValue(dim))) - (int)a.centroid.axisValue(dim);
			}
		}

		private struct CompareToBucket
		{
			public CompareToBucket(int split, int num, int d, BBox b)
			{
				splitBucket = split;
				nBuckets = num;
				dim = d;
				centroidBounds = b;
			}

			public bool match(BVHPrimitiveInfo p)
			{
				int b = (int)(nBuckets * ((p.centroid.axisValue(dim) - centroidBounds.pMin.axisValue(dim)) /
							(centroidBounds.pMax.axisValue(dim) - centroidBounds.pMin.axisValue(dim))));
				if (b == nBuckets) b = nBuckets - 1;
				return b <= splitBucket;
			}

			int splitBucket, nBuckets, dim;
			BBox centroidBounds;
		}

		private struct BVHPrimitiveInfo
		{
			public BVHPrimitiveInfo(int pn, BBox box)
			{
				primitiveNumber = pn;
				bounds = box;
				centroid = 0.5f * box.pMin.Xyz + 0.5f * box.pMax.Xyz;
			}

			public int primitiveNumber;
			public Vector3 centroid;
			public BBox bounds;
		}

		private class BVHBuildNode
		{
			public BVHBuildNode()
			{
				children[0] = children[1] = null;
			}

			public void initLeaf(int first, int n, BBox b)
			{
				firstPrimOffset = first;
				nPrimitives = n;
				bounds = b;
			}

			public void initInterior(int axis, BVHBuildNode c0, BVHBuildNode c1)
			{
				children[0] = c0;
				children[1] = c1;
				bounds = BBox.union(c0.bounds, c1.bounds);
				splitAxis = axis;
				nPrimitives = 0;
			}

			public BBox bounds;
			public BVHBuildNode[] children = new BVHBuildNode[2];
			public int splitAxis, firstPrimOffset, nPrimitives;
		}

		struct BucketInfo
		{
			public BucketInfo(bool value)
			{
				count = 0;
				bounds = new BBox(true);
			}
			public int count;
			public BBox bounds;
		}

		#endregion

		#region Construction

		public BvhTree(List<Triangle> prims, int maxPrimsPerNode)
		{
			_maxPrimsInNode = Math.Min(8, maxPrimsPerNode);
			_primitives = prims;

			// Construction data
			List<BVHPrimitiveInfo> buildData = new List<BVHPrimitiveInfo>(prims.Count);
			for (int i = 0; i < prims.Count; i++)
			{
				BBox bbox = prims[i].getBounds();
				buildData.Add(new BVHPrimitiveInfo(i, bbox));
			}

			Console.WriteLine("Building Tree with " + _primitives.Count + " primitives...\n");

			// Recursively build BVH tree for all primitives
			int totalNodes = 0;
			List<Triangle> orderedPrims = new List<Triangle>(prims.Count);
			BVHBuildNode root = recursiveBuild(buildData, 0, _primitives.Count, ref totalNodes, orderedPrims);

			Console.WriteLine("Finished building tree with " + totalNodes + " nodes");

			// Copy the ordered primitives to the new list
			_primitives = orderedPrims;

			// Convert to a linear tree
			_nodes = new LinearBVHNode[totalNodes];
			int offset = 0;
			flattenBVHTree(root, ref offset);
		}

		BVHBuildNode recursiveBuild(List<BVHPrimitiveInfo> buildData, int start, int end, ref int totalNodes, List<Triangle> orderedPrims)
		{
#if DEBUG
			indent += "  ";
#endif

			totalNodes++;

			BVHBuildNode node = new BVHBuildNode();
			BBox bbox = new BBox(true);
			for (int i=start; i<end; ++i)
			{
				bbox.union(buildData[i].bounds);
			}

			int nPrimitives = end - start;
			if (nPrimitives == 1)
			{
				// Create leaf node
				int firstPrimOffset = orderedPrims.Count;
				for (int i = start; i < end; ++i)
				{
					int primNum = buildData[i].primitiveNumber;
					orderedPrims.Add(_primitives[primNum]);
				}
				node.initLeaf(firstPrimOffset, nPrimitives, bbox);
			}
			else
			{
				//split
				BBox centroidBounds = new BBox(true);
				for (int i = start; i < end; i++)
				{
					centroidBounds.union(buildData[i].centroid);
				}
				int dim = (int) centroidBounds.longestAxis();

				int mid = (start + end) / 2;
				if ( centroidBounds.pMax.axisValue(dim) == centroidBounds.pMin.axisValue(dim) )
				{
					// Degenerate shapes with identical centroids
					// Create leaf node
					int firstPrimOffset = orderedPrims.Count;
					for (int i = start; i < end; ++i)
					{
						int primNum = buildData[i].primitiveNumber;
						orderedPrims.Add(_primitives[primNum]);
					}
					node.initLeaf(firstPrimOffset, nPrimitives, bbox);
					return node;
				}

				// partition primitives
				if (nPrimitives <= 4)
				{
					// SAH is not worth it here
					mid = (start + end) / 2;
					PointComparator midComp = new PointComparator(dim);
					int sortCount = end - start;
					buildData.Sort(start, sortCount, midComp);
				}
				else
				{
					// use SAH to pick node split position
					// Split primitives into buckets
					const int nBuckets = 12;
					BucketInfo[] buckets = new BucketInfo[nBuckets];
					for (int i = 0; i < buckets.Length; i++) buckets[i] = new BucketInfo(true);
					for (int i = start; i < end; i++)
					{
						int b = (int) (nBuckets * ( (buildData[i].centroid.axisValue(dim) - centroidBounds.pMin.axisValue(dim)) /
							(centroidBounds.pMax.axisValue(dim) - centroidBounds.pMin.axisValue(dim))));
						if (b == nBuckets) b = nBuckets - 1;
						buckets[b].count++;
						buckets[b].bounds.union(buildData[i].bounds);
					}
					// Compute costs for splitting after each bucket
					float[] cost = new float[nBuckets - 1];
					for (int i = 0; i < nBuckets - 1; i++)
					{
						BBox b0 = new BBox(true), b1=new BBox(true);
						int count0 = 0, count1 = 0;
						for (int j = 0; j <= i; j++)
						{
							b0.union(buckets[j].bounds);
							count0 += buckets[j].count;
						}
						for (int j = i + 1; j < nBuckets; j++)
						{
							b1.union(buckets[j].bounds);
							count1 += buckets[j].count;
						}
						cost[i] = 0.125f + (count0 * b0.surfaceArea() + count1 * b1.surfaceArea()) / bbox.surfaceArea();
					}

					// Find bucket to split at that minimizes SAH
					float minCost = cost[0];
					int minCostSplit = 0;
					for (int i = 1; i < nBuckets-1; i++)
					{
						if (cost[i] < minCost)
						{
							minCost = cost[i];
							minCostSplit = i;
						}
					}

					// Create leaf or split primitives
					if (nPrimitives > _maxPrimsInNode || minCost < nPrimitives)
					{
						// use replacement for std::partition to split at selected SAH bucket
						CompareToBucket comp = new CompareToBucket(minCostSplit, nBuckets, dim, centroidBounds);
						// mid is the first element where comp.match(element) is false
						mid = buildData.Partition(start, end, comp.match);
					}
					else
					{
						// Create leaf node
						int firstPrimOffset = orderedPrims.Count;
						for (int i = start; i < end; ++i)
						{
							int primNum = buildData[i].primitiveNumber;
							orderedPrims.Add(_primitives[primNum]);
						}
						node.initLeaf(firstPrimOffset, nPrimitives, bbox);
						return node;
					}
				}

				node.initInterior(dim,
					recursiveBuild(buildData, start, mid, ref totalNodes, orderedPrims),
					recursiveBuild(buildData, mid, end, ref totalNodes, orderedPrims));
			}

#if DEBUG
			String boundsString = "{pMin=" + node.bounds.pMin + ", " + node.bounds.pMax + "}";
			if (node.nPrimitives == 0)
			{
				Console.WriteLine(indent + "Internel node " + boundsString);
			}
			else
			{
				int[] nodePrims = new int[node.nPrimitives];
				for (int i = 0; i < node.nPrimitives; i++)
				{
					nodePrims[i] = node.firstPrimOffset + i;
				}
				Console.WriteLine(indent + "Leaf node " + boundsString);
				Console.WriteLine(indent + " Prims: { " + String.Join(",", nodePrims) + " }");
			}

			indent = indent.Substring(0, indent.Length - 2);
#endif
			return node;
		} // end recursive build


		int flattenBVHTree(BVHBuildNode node, ref int offset)
		{
			LinearBVHNode linearNode = new LinearBVHNode();
			linearNode.bounds = node.bounds;
			int myOffset = offset++;
			if (node.nPrimitives > 0)
			{
				linearNode.primitivesOffset = node.firstPrimOffset;
				linearNode.nPrimitives = node.nPrimitives;
			}
			else
			{
				linearNode.axis = node.splitAxis;
				linearNode.nPrimitives = 0;	// Denotes an interior node
				flattenBVHTree(node.children[0], ref offset);
				linearNode.secondChildOffset = flattenBVHTree(node.children[1], ref offset);
			}
			_nodes[myOffset] = linearNode;
			return myOffset;
		}

		#endregion



		#region Intersection

		public float getNearestIntersection(ref Ray ray, out Triangle? primHit)
		{
			float minT = float.PositiveInfinity;
			primHit = null;
			if (_nodes == null || _nodes.Length <= 0) { return float.PositiveInfinity; }

			bool hit = false;
			//Vector3 origin = ray.tMin
			Vector3 invDir = new Vector3(1/ray.Direction.X, 1/ray.Direction.Y, 1/ray.Direction.Z);
			int[] dirIsNeg = { Convert.ToInt32(invDir.X < 0), Convert.ToInt32(invDir.Y < 0), Convert.ToInt32(invDir.Z < 0) };

			int todoOffset = 0;
			int nodeNum = 0;
			int[] todo = new int[64];
			while (true)
			{
				LinearBVHNode node = _nodes[nodeNum];
				if ( intersectP(ref node.bounds, ref ray, invDir, dirIsNeg) )
				//if (true)
				{
					if (node.nPrimitives > 0)
					{
						for (int i = 0; i < node.nPrimitives; i++)
						{
							// Intersect primitives
							Triangle tri = _primitives[node.primitivesOffset + i];
							float t = ray.intersects(tri);
							if( t < minT )
							{
								hit = true;
								minT = t;
								primHit = _primitives[node.primitivesOffset + i];
							}
						}
						if (todoOffset == 0) break;
						nodeNum = todo[--todoOffset];
					}
					else
					{
						if (dirIsNeg[node.axis] != 0)
						{
							todo[todoOffset++] = nodeNum + 1;
							nodeNum = node.secondChildOffset;
						}
						else
						{
							todo[todoOffset++] = node.secondChildOffset;
							nodeNum++;
						}
					}
				}
				else
				{
					if (todoOffset == 0) break;
					nodeNum = todo[--todoOffset];
				}
			}

			if (hit)
				return minT;
			return float.PositiveInfinity;
		}

		// This is taking up 50% of CPU time. Should not be a problem on the GPU :^)
		private bool intersectP(ref BBox bounds, ref Ray ray, Vector3 invDir, int[] dirIsNeg)
		{
			float  tMin = (bounds[    dirIsNeg[0]].X - ray.Origin.X) * invDir.X;
			float  tMax = (bounds[1 - dirIsNeg[0]].X - ray.Origin.X) * invDir.X;
			float tyMin = (bounds[    dirIsNeg[1]].Y - ray.Origin.Y) * invDir.Y;
			float tyMax = (bounds[1 - dirIsNeg[1]].Y - ray.Origin.Y) * invDir.Y;
			if ((tMin > tyMax) || (tyMin > tMax))
				return false;
			if (tyMin > tMin) tMin = tyMin;
			if (tyMax < tMax) tMax = tyMax;

			float tzMin = (bounds[    dirIsNeg[2]].Z - ray.Origin.Z) * invDir.Z;
			float tzMax = (bounds[1 - dirIsNeg[2]].Z - ray.Origin.Z) * invDir.Z;
			if ((tMin > tzMax) || (tzMin > tMax))
				return false;
			if (tzMin > tMin) tMin = tzMin;
			if (tzMax < tMax) tMax = tzMax;

			return (tMin < ray.tMax) && (tMax > ray.tMin);
		}

		#endregion
	}
}
