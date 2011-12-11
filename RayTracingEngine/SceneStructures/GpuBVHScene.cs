using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;

using Cloo;
using Raytracing.CL;
using Raytracing.Primitives;

namespace Raytracing.SceneStructures
{
	public class GpuBVHScene : IDisposable
	{
		ComputeCommandQueue _commandQueue;

        protected List<Triangle> _primitives;

		private static int InitialPointLightArraySize = 4;
        protected List<SimplePointLight> _lights;

		protected GpuBvhTree _tree;
		protected int _maxPrims;

		internal MaterialCache _materialCache;

        public Color4 BackgroundColor = Color4.CornflowerBlue;
		public float Ambiant = 0.0f;

		public GpuBVHScene(ComputeCommandQueue commandQueue, Color4 background, int maxPrimsPerNode)
		{
			BackgroundColor = background;
			_commandQueue = commandQueue;
			_primitives = new List<Triangle>();
			_materialCache = new MaterialCache(_commandQueue);
			_lights = new List<SimplePointLight>();
			_maxPrims = maxPrimsPerNode;
		}

		public GpuBVHScene(ComputeCommandQueue commandQueue, List<Triangle> prims, Color4 background, int maxPrimsPerNode) 
		{
			BackgroundColor = background;
			_commandQueue = commandQueue;
			_primitives = prims;
			_materialCache = new MaterialCache(_commandQueue);
			rebuildTree();
			_lights = new List<SimplePointLight>();
			_maxPrims = maxPrimsPerNode;
		}

		public void Dispose()
		{
			_materialCache.Dispose();
		}

		public void rebuildTree()
		{
			_tree = new GpuBvhTree(_commandQueue, _primitives, _lights, _maxPrims);
		}

        /// <summary>
        /// Add a sphere to the scene
        /// </summary>
        /// <param name="s"></param>
        public void add(Triangle s, Material m)
        {
			int index = _materialCache.getMaterialIndex(m);
			s.p2.W = Convert.ToSingle(index);	// Pack as a float. Precision issues?
            _primitives.Add(s);
        }

        /// <summary>
        /// Adds a new light.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="color"></param>
        /// <param name="intensity"></param>
        public void add(SimplePointLight light)
        {
            _lights.Add(light);
        }

		public void addPointLight(Vector3 position, Color4 color, float intensity)
		{
			SimplePointLight spl = new SimplePointLight();

			// pack the color and intensity values into a single struct
			Color4 colorAndIntensity = color;
			colorAndIntensity.A = intensity;
			spl.position = new Vector4(position, 1.0f);
			spl.colorAndIntensity = colorAndIntensity;

			_lights.Add(spl);
		}

        public List<SimplePointLight> getLights()
        {
            return _lights;
        }

		public void render(ComputeCommandQueue commandQueue, GpuBvhCamera camera)
        {
            _materialCache.syncBuffer(commandQueue);
			_tree.syncBuffers();

            camera.computeView();
			camera.renderSceneToTexture(_tree, _materialCache, BackgroundColor);
            camera.drawTextureToScreen();
        }


	}
}
