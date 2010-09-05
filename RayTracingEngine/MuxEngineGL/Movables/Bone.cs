
using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace MuxEngine.Movables
{
    public class Bone : MovableQuat, IComparer<Bone>, IComparable<Bone>
    {
        String m_name;
        int m_index;

        Bone m_parent;
        List<Bone> m_children;

        public Bone (Bone parent, Matrix xform, int boneIndex, String name)
            : base (xform)
        {
            m_name = name;
            m_index = boneIndex; 
            m_parent = parent;
            m_children = new List<Bone> ();
        }

        internal Bone ()
        {
            // handled by reader
        }

        public String Name
        {
            get { return (m_name); }
            internal set { m_name = value; }
        }

        public int Index
        {
            get { return (m_index); }
            internal set { m_index = value; }
        }

        public Bone Parent
        {
            get { return (m_parent); }
            internal set { m_parent = value; }
        }

        public List<Bone> Children
        {
            get { return (m_children); }
            internal set { m_children = value; }
        }

        public int CompareTo (Bone rhs)
        {
            return (m_name.CompareTo (rhs.Name));
        }

        public int Compare (Bone bone1, Bone bone2)
        {
            return (StringComparer.CurrentCultureIgnoreCase.Compare (bone1.Name, bone2.Name));
        }

        // shallow clone
        internal new Bone Clone ()
        {
            Bone clone = (Bone)this.MemberwiseClone ();
            return (clone);
        }

        internal List<Bone> CloneHierarchy ()
        {
            List<Bone> clonedHierarchy = new List<Bone> ();
            cloneHierarchy (clonedHierarchy);
            return (clonedHierarchy);
        }

        Bone cloneHierarchy (List<Bone> bones)
        {
            Bone clone = this.Clone ();
            bones.Add (clone);
            clone.m_children = new List<Bone> (Children.Count);
            foreach (Bone child in Children)
            {
                Bone childClone = child.cloneHierarchy (bones);
                childClone.m_parent = clone;
                clone.Children.Add (childClone);
            }
            return (clone);
        }

    }
}
