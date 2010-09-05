
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

using MuxEngine.LinearAlgebra;

namespace MuxEngine.Movables
{
    public class SimpleModel : MovableQuat
    {
        Bone m_rootBone;
        Mesh m_mesh;

        internal SimpleModel ()
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

        public new SimpleModel Clone ()
        {
            SimpleModel clone = (SimpleModel) this.MemberwiseClone ();            
            return (clone);
        }
    }

}
