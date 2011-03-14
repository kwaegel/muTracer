using System;
using System.Collections.Generic;
using System.Diagnostics;

using Cloo;

using Raytracing.Primitives;

namespace Raytracing.SceneStructures
{
    internal class MaterialCache : IDisposable
    {
        private const int DefaultCacheSize = 10;

        ComputeCommandQueue _commandQueue;
        private ComputeImageFormat _imageFormat = new ComputeImageFormat(ComputeImageChannelOrder.Rgba, ComputeImageChannelType.UnsignedInt32);

        Material[] _materialArray;
        private int _nextOpening = 0;

        public ComputeBuffer<Material> Buffer { get; private set; }

        public MaterialCache(ComputeCommandQueue commandQueue)
        {
            _commandQueue = commandQueue;

            _materialArray = new Material[DefaultCacheSize];
            Buffer = new ComputeBuffer<Material>(commandQueue.Context, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.UseHostPointer, _materialArray);
        }

        public void Dispose()
        {
            Buffer.Dispose();
        }

        /// <summary>
        /// Copy the material data to the GPU.
        /// </summary>
        /// <param name="commandQueue"></param>
        public void syncBuffer(ComputeCommandQueue commandQueue)
        {
            // TODO: try non-blocking sends.
            commandQueue.WriteToBuffer<Material>(_materialArray, Buffer, true, null);
        }

        public int getMaterialIndex(Material material)
        {
            Debug.Assert(material.Reflectivity >= 0 && material.Reflectivity <= 1.0f);
            Debug.Assert(material.Transparency >= 0 && material.Transparency <= 1.0f);

            int materialIndex = _nextOpening;

            for (int i=0; i< _nextOpening; i++)
            {
                Material mat = _materialArray[i];

                if (mat == material)
                {
                    materialIndex = i;
                    break;
                }
            }

            System.Diagnostics.Debug.Assert(materialIndex < _materialArray.Length, "Maxium number of materials exceeded.");

            if (materialIndex == _nextOpening)
            {
                _materialArray[materialIndex] = material;
                _nextOpening++;
            }

            return materialIndex;
        }
    }
}
