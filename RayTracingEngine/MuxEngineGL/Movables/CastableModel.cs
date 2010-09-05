
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using MuxEngine.LinearAlgebra;
using MuxEnginePipelineShared;

namespace MuxEngine.Movables
{
    public class CastableModel : MovableModel
    {
        public CastableModel (Model model)
            : base (model)
        {
        }

        public CastableModel (Model model, Matrix4 transform)
            : base (model, transform)
        {
        }

        public Boolean rayCast (ref Ray ray, Single distanceBound, out Vector3 collisionPoint, out Vector3 collisionNormal)
        {
            Model model = this.Model;
            BoundingBox modelBoundingBox = (BoundingBox)model.Tag;
            Matrix toWorld = this.Transform;
            Matrix toModel;
            Matrix.Invert (ref toWorld, out toModel);
            // Convert ray to model space
            Ray modelRay;
            Vector3.Transform (ref ray.Position, ref toModel, out modelRay.Position);
            Vector3.TransformNormal (ref ray.Direction, ref toModel, out modelRay.Direction);
            modelRay.Direction.Normalize ();

            float? distanceToModelBox;
            modelRay.Intersects (ref modelBoundingBox, out distanceToModelBox);
            if (distanceToModelBox.HasValue)
            {
                // Hit the model box
                if (distanceToModelBox.Value <= distanceBound)
                {
                    // Hit is within distance bound
                    float minDistance = Single.MaxValue;
                    collisionNormal = Vector3.Zero;
                    foreach (ModelMesh mesh in model.Meshes)
                    {
                        float? distanceToMesh = rayCastModelMesh (ref modelRay, distanceBound, mesh, out collisionNormal);
                        if (distanceToMesh.HasValue)
                            minDistance = Math.Min (minDistance, distanceToMesh.Value);
                    }
                    if (minDistance < Single.MaxValue)
                    {
                        // Hit a triangle in at least one mesh
                        collisionPoint = modelRay.Position + minDistance * modelRay.Direction;
                        Vector3.Transform (ref collisionPoint, ref toWorld, out collisionPoint);
                        Vector3.TransformNormal (ref collisionNormal, ref toWorld, out collisionNormal);
                        return (true);
                    }
                }
            }
            // Missed the box or all meshes bounded by it
            collisionPoint = new Vector3 (Single.MaxValue);
            collisionNormal = Vector3.Zero;
            return (false);
        }

        Single? rayCastModelMesh (ref Ray modelRay, Single distanceBound,
                                  ModelMesh mesh, out Vector3 collisionNormal)
        {
            IndexedTriangleList triangleList = (IndexedTriangleList)mesh.Tag;
            BoundingBox box = triangleList.BoundingBox;
            float? rayToBoxDistance;
            modelRay.Intersects (ref box, out rayToBoxDistance);
            if (rayToBoxDistance.HasValue)
            {
                // Intersected the bounding box, so check bound
                if (rayToBoxDistance > distanceBound)
                {
                    // Not within distance bound
                    collisionNormal = Vector3.Zero;
                    return (null);
                }
                // Within the distance bound
                float rayToTriangleDistance;
                int triangleIndex;
                // Check triangle list
                bool hit = rayCastMeshTriangleList (ref modelRay, distanceBound,
                                                    triangleList,
                                                    out rayToTriangleDistance,
                                                    out triangleIndex);
                if (hit)
                {
                    Triangle triangle = triangleList[triangleIndex];
                    Vector3 edge1;
                    Vector3.Subtract (ref triangle.Point3, ref triangle.Point1, out edge1);
                    Vector3 edge2;
                    Vector3.Subtract (ref triangle.Point2, ref triangle.Point1, out edge2);
                    Vector3.Cross (ref edge1, ref edge2, out collisionNormal);
                    collisionNormal.Normalize ();
                    return (rayToTriangleDistance);
                }
            }
            // Did not intersect the bounding box
            collisionNormal = Vector3.Zero;
            
            return (rayToBoxDistance);
        }

        Boolean rayCastMeshTriangleList (ref Ray modelRay, Single distanceBound,
                                         IndexedTriangleList triangleList, 
                                         out Single rayToTriangleDistance,
                                         out Int32 triangleIndex)
        {
            bool hit = false;
            rayToTriangleDistance = Single.MaxValue;
            int i = 0;
            for ( ; i < triangleList.Count; ++i)
            {
                Triangle triangle = triangleList[i];
                hit = rayCastTriangle (ref modelRay, ref triangle, out rayToTriangleDistance);
                if (hit && rayToTriangleDistance <= distanceBound)
                    break;
                hit = false;
            }
            triangleIndex = i;
            return (hit);
        }

        // Uses algorithm by Moller and Trumbore
        //   from Journal of Graphics Tools, Volume 2, 
        //   "Fast, Minimum Storage Ray/Triangle Intersection"
        // Intersections with back-facing triangles will NOT be detected
        Boolean rayCastTriangle (ref Ray modelRay, ref Triangle triangle,
                                         out Single rayToTriangleDistance)
        {
            Vector3 edge1;
            // GMZ: Check if pass-by-reference is truly faster 
            Vector3.Subtract (ref triangle.Point3, ref triangle.Point1, out edge1);
            Vector3 edge2;
            Vector3.Subtract (ref triangle.Point2, ref triangle.Point1, out edge2);

            // Compute determinant
            Vector3 directionCrossEdge2;
            Vector3.Cross (ref modelRay.Direction, ref edge2, out directionCrossEdge2);
            float determinant;
            Vector3.Dot (ref edge1, ref directionCrossEdge2, out determinant);

            // Is ray parallel to triangle plane
            //   or is ray origin behind the triangle?
            if (determinant < Single.Epsilon)
            {
                rayToTriangleDistance = Single.MaxValue;
                return (false);
            }

            // Calculate barycentric coordinates
            // Calculate the U parameter of the intersection point
            Vector3 distanceVector;
            Vector3.Subtract (ref modelRay.Position, ref triangle.Point1, out distanceVector);

            float triangleU;
            Vector3.Dot (ref distanceVector, ref directionCrossEdge2, out triangleU);
            // Is it inside the triangle?
            if (triangleU < 0.0f || triangleU > determinant)
            {
                rayToTriangleDistance = Single.MaxValue;
                return (false);
            }

            // Calculate the V parameter of the intersection point
            Vector3 distanceCrossEdge1;
            Vector3.Cross (ref distanceVector, ref edge1, out distanceCrossEdge1);
            float triangleV;
            Vector3.Dot (ref modelRay.Direction, ref distanceCrossEdge1, out triangleV);
            // Is it inside the triangle?
            if (triangleV < 0.0f || triangleU + triangleV > determinant)
            {
                rayToTriangleDistance = Single.MaxValue;
                return (false);
            }

            // Compute distance along ray to triangle
            Vector3.Dot (ref edge2, ref distanceCrossEdge1, out rayToTriangleDistance);
            float inverseDeterminant = 1.0f / determinant;
            rayToTriangleDistance *= inverseDeterminant;

            return (true);
        }
    }
}
