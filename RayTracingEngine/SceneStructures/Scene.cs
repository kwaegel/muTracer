using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Cloo;
using Raytracing.CL;

namespace Raytracing.SceneStructures
{
    class Scene
    {
        VoxelGrid _voxelGrid;
        MaterialCache _materialCache;



        public void render(ComputeCommandQueue commandQueue, GridCamera camera)
        {
            _materialCache.syncBuffer(commandQueue);

            camera.render();
        }

    }
}
