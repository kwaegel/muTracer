
using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

using MuxEngine.LinearAlgebra;

namespace MuxEngine.Movables
{
    // A lightweight model class that holds a single mesh
    public class SingleMeshModel : MovableQuat
    {
        // References the one and only bone, the same object as m_mesh.ParentBone
        Bone m_rootBone;
        Mesh m_mesh;

        internal SingleMeshModel ()
            : base ()
        {
        }

        public Bone RootBone
        {
            get { return (m_rootBone); }
            internal set { m_rootBone = value; }
        }

        public Mesh Mesh
        {
            get { return (m_mesh); }
            internal set { m_mesh = value; }
        }

        public void Draw (GraphicsDevice device, Camera camera)
        {
            m_mesh.Draw (device, camera, this.Transform);
        }

        public new SingleMeshModel Clone ()
        {
            SingleMeshModel clone = (SingleMeshModel) this.MemberwiseClone ();            
            return (clone);
        }
    }

}
