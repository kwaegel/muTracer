using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK;
using OpenTK.Graphics;

using Cloo;
using Raytracing.CL;
using Raytracing.Math;
using Raytracing.Primitives;

namespace Raytracing.SceneStructures
{
    public class Scene : IDisposable
    {
        private const int DefaultGridWidth = 16;
        private const int DefaultGridResolution = 16;

        ComputeCommandQueue _commandQueue;

        VoxelGrid _voxelGrid;
        MaterialCache _materialCache;

        public Color4 BackgroundColor = Color4.CornflowerBlue;

        public Scene(ComputeCommandQueue commandQueue)
        {
            _commandQueue = commandQueue;

            _voxelGrid = new VoxelGrid(commandQueue, DefaultGridWidth, DefaultGridResolution);
            _materialCache = new MaterialCache(commandQueue);
        }

        public void Dispose()
        {
            _voxelGrid.Dispose();
            _materialCache.Dispose();
        }


        public void addSphere(Vector3 center, float radius, Material mat)
        {
            int materialIndex = _materialCache.getMaterialIndex(mat);

            _voxelGrid.addSphere(center, radius, materialIndex);
        }

        /// <summary>
        /// Adds a new point light.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="color"></param>
        /// <param name="intensity"></param>
        public void addLight(Vector3 position, Color4 color, float intensity)
        {
            _voxelGrid.addPointLight(position, color, intensity);
        }


        public void render(ComputeCommandQueue commandQueue, GridCamera camera)
        {
            _materialCache.syncBuffer(commandQueue);
            _voxelGrid.syncBuffers();

            camera.computeView();
            camera.renderSceneToTexture(_voxelGrid, _materialCache, BackgroundColor);
            camera.drawTextureToScreen();
        }

    }
}
