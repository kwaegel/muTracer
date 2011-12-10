using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;

using Raytracing.Primitives;

namespace Raytracing.SceneStructures
{
    public class Scene : IDisposable
    {
		public bool PrintDebugMessages = false;

        protected List<Sphere> _spheres;
        protected List<Light> _lights;

        public Color4 BackgroundColor = Color4.CornflowerBlue;
		public float Ambiant = 0.0f;

        public Scene(Color4 backgroundColor)
        {
            BackgroundColor = backgroundColor;
            _spheres = new List<Sphere>();
            _lights = new List<Light>();
        }

        public void Dispose()
        {
        }

        /// <summary>
        /// Add a sphere to the scene
        /// </summary>
        /// <param name="s"></param>
        public void add(Sphere s)
        {
            _spheres.Add(s);
        }

        /// <summary>
        /// Adds a new light.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="color"></param>
        /// <param name="intensity"></param>
        public void add(Light light)
        {
            _lights.Add(light);
        }

        public List<Light> getLights()
        {
            return _lights;
        }

        public virtual float getNearestIntersection(ref Ray r, ref Vector3 collisionPoint, ref Vector3 surfaceNormal, ref Material mat)
        {
            float nearestIntersection = float.PositiveInfinity;
            foreach (Sphere s in _spheres)
            {
                float intersection = r.intersects(s, ref collisionPoint, ref surfaceNormal);
                if (intersection < nearestIntersection)
                {
                    nearestIntersection = intersection;
                    mat = s.Material;
                }
            }
            return nearestIntersection;
        }

        public virtual float getNearestIntersection(ref Ray r, ref Vector3 collisionPoint, ref Vector3 surfaceNormal)
        {
            float nearestIntersection = float.PositiveInfinity;
            foreach (Sphere s in _spheres)
            {
                float intersection = r.intersects(s, ref collisionPoint, ref surfaceNormal);
                if (intersection < nearestIntersection)
                {
                    nearestIntersection = intersection;
                }
            }
            return nearestIntersection;
        }
    }
}