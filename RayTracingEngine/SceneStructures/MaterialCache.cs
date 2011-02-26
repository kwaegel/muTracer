using System;
using System.Collections.Generic;

using Cloo;

using Raytracing.Primitives;

namespace Raytracing.SceneStructures
{
    public class MaterialCache
    {
        private static int CacheSize;

        ComputeContext _context;
        private ComputeImageFormat _imageFormat = new ComputeImageFormat(ComputeImageChannelOrder.Rgba, ComputeImageChannelType.UnsignedInt32);

        Material[] _materialArray;
        private int _nextOpening = 0;

        public ComputeBuffer<Material> Buffer { get; private set; }

        public MaterialCache(ComputeContext context)
        {
            _context = context;

            _materialArray = new Material[10];
            Buffer = new ComputeBuffer<Material>(_context, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.UseHostPointer, _materialArray);
        }

        /// <summary>
        /// Copy the material data to the GPU.
        /// </summary>
        /// <param name="commandQueue"></param>
        public void syncBuffer(ComputeCommandQueue commandQueue)
        {
            commandQueue.WriteToBuffer<Material>(_materialArray, Buffer, false, null);
        }

        // Add a material and return the index of the new material.
        public int addMaterial(Material mat)
        {
            if (_nextOpening < _materialArray.Length)
            {
                _materialArray[_nextOpening] = mat;
                return _nextOpening++;
            }
            else
            {
                throw new Exception("Material cache full. Can't add: " + mat.ToString());
            }
        }

        // Get the index of the given material.
        public int getMaterialIndex(Material mat)
        {
            return -1;
        }

    }
}
