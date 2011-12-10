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
        protected List<Light> _lights;

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
		}

		public GpuBVHScene(ComputeCommandQueue commandQueue, List<Triangle> prims, Color4 background, int maxPrimsPerNode) 
		{
			BackgroundColor = background;
			_commandQueue = commandQueue;
			_primitives = prims;
			_materialCache = new MaterialCache(_commandQueue);
			rebuildTree();
		}

		public void Dispose()
		{
			_materialCache.Dispose();
		}

		public void rebuildTree()
		{
			_tree = new GpuBvhTree(_commandQueue, _primitives, _maxPrims);
		}

        /// <summary>
        /// Add a sphere to the scene
        /// </summary>
        /// <param name="s"></param>
        public void add(Triangle s, Material m)
        {
			int index = _materialCache.getMaterialIndex(m);
			s.p2.W = Convert.ToSingle(index);
            _primitives.Add(s);
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

		public void render(ComputeCommandQueue commandQueue, GpuBvhCamera camera)
        {
            _materialCache.syncBuffer(commandQueue);
			_tree.syncBuffers();
            //_voxelGrid.syncBuffers();

            camera.computeView();
            //camera.renderSceneToTexture(_voxelGrid, _materialCache, BackgroundColor);
            camera.drawTextureToScreen();
        }


	}
}
