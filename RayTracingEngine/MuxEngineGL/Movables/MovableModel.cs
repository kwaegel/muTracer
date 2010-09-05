
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using MuxEngine.LinearAlgebra;
using MuxEngine.Bsp;
using MuxEngine.Utility;

namespace MuxEngine.Movables
{
    public class MovableModel : MovableQuat
    {
        Model m_model;
        Matrix[] m_transforms;

        public MovableModel (Model model)
            : base ()
        {
            m_model = model;
            m_transforms = new Matrix[m_model.Bones.Count];
            m_model.CopyAbsoluteBoneTransformsTo (m_transforms);
        }

        public MovableModel (Model model, Matrix4 transform)
            : base (transform)
        {
            m_model = model;
            m_transforms = new Matrix[m_model.Bones.Count];
            m_model.CopyAbsoluteBoneTransformsTo (m_transforms);
        }

        public Model Model
        {
            get { return (m_model); }
            set { m_model = value; }
        }

        /****************************************************************************/
        /****************************************************************************/
        /*                      Begin Remove                                        */
        /****************************************************************************/
        /****************************************************************************/
        public void DrawBspWithoutFrustumCulling (Camera camera)
        {
            // invoke "makeOrthonormalCross" if derived from "Movable"
            // otherwise, use "makeOrthonormal"
            this.makeOrthonormal ();

            // a Model has multiple meshes, each with a parent ModelBone
            //   containing a transform for that mesh
            m_model.CopyAbsoluteBoneTransformsTo (m_transforms);

            // Grab the Bsp tree from the model's tag
            ITree tree = (ITree)m_model.Tag;

            // Compute the current visibility and get the list of visible ModelMeshes
            tree.ComputeVisibility (camera.Position);
            List<String> visibleModelMeshes = tree.VisibleMeshNames ();

            // Run through the list of visible model meshes and draw them
            for (int currentModelMesh = 0; currentModelMesh < visibleModelMeshes.Count; ++currentModelMesh)
            {
                // Grab the current mesh
                ModelMesh mesh = m_model.Meshes[visibleModelMeshes[currentModelMesh]];

                // Save the current AlphaBlendEnable value
                bool oldBlend = mesh.Effects[0].GraphicsDevice.RenderState.AlphaBlendEnable;

                // Set the AlphaBlendEnable to the value stored in the shader
                mesh.Effects[0].GraphicsDevice.RenderState.AlphaBlendEnable = mesh.Effects[0].Parameters["translucent"].GetValueBoolean ();

                foreach (Effect effect in mesh.Effects)
                {
                    effect.Parameters["World"].SetValue (m_transforms[mesh.ParentBone.Index] * this.Transform);
                    effect.Parameters["View"].SetValue (camera.View);
                    effect.Parameters["Projection"].SetValue (camera.Projection);
                }
                mesh.Draw ();

                // Restore AlphaBlendEnable to what is was before DrawBsp was called
                mesh.Effects[0].GraphicsDevice.RenderState.AlphaBlendEnable = oldBlend;
            }
        }
        public void DrawWithoutFrustumCulling (Camera camera)
        {
            // Invoke "makeOrthonormalCross" if derived from "Movable"
            // otherwise, use "makeOrthonormal"
            this.makeOrthonormal ();

            // A Model has multiple meshes, each with a parent ModelBone
            //   containing a transform for that mesh
            m_model.CopyAbsoluteBoneTransformsTo (m_transforms);
            foreach (ModelMesh mesh in m_model.Meshes)
            {
                EffectParameter translucent = mesh.Effects[0].Parameters.GetParameterBySemantic ("MuxEngineTranslucent");
                bool oldBlend = mesh.Effects[0].GraphicsDevice.RenderState.AlphaBlendEnable;
                if (translucent != null)
                {
                    mesh.Effects[0].GraphicsDevice.RenderState.AlphaBlendEnable = translucent.GetValueBoolean ();
                }
                foreach (Effect effect in mesh.Effects)
                {
                    EffectParameter world = effect.Parameters["World"];

                    if (world != null)
                    {
                        world.SetValue (m_transforms[mesh.ParentBone.Index] * this.Transform);
                        effect.Parameters["View"].SetValue (camera.View);
                        effect.Parameters["Projection"].SetValue (camera.Projection);
                    }
                }
                mesh.Draw ();

                mesh.Effects[0].GraphicsDevice.RenderState.AlphaBlendEnable = oldBlend;
            }
        }
        public void DrawBspObserver (Camera observer, Camera actor)
        {
            // invoke "makeOrthonormalCross" if derived from "Movable"
            // otherwise, use "makeOrthonormal"
            this.makeOrthonormal ();

            // a Model has multiple meshes, each with a parent ModelBone
            //   containing a transform for that mesh
            m_model.CopyAbsoluteBoneTransformsTo (m_transforms);

            // Grab the Bsp tree from the model's tag
            ITree tree = (ITree)m_model.Tag;

            // Get the camera's BoundingFrustum
            BoundingFrustum frustum = actor.computeBoundingFrustum ();

            // Compute the current visibility and get the list of visible ModelMeshes
            tree.ComputeVisibility (actor.Position);
            List<String> visibleModelMeshes = tree.VisibleMeshNames (frustum);

            // Run through the list of visible model meshes and draw them
            for (int currentModelMesh = 0; currentModelMesh < visibleModelMeshes.Count; ++currentModelMesh)
            {
                // Grab the current mesh
                ModelMesh mesh = m_model.Meshes[visibleModelMeshes[currentModelMesh]];

                // Save the current AlphaBlendEnable value
                bool oldBlend = mesh.Effects[0].GraphicsDevice.RenderState.AlphaBlendEnable;

                // Set the AlphaBlendEnable to the value stored in the shader
                mesh.Effects[0].GraphicsDevice.RenderState.AlphaBlendEnable = mesh.Effects[0].Parameters["translucent"].GetValueBoolean ();

                foreach (Effect effect in mesh.Effects)
                {
                    effect.Parameters["World"].SetValue (m_transforms[mesh.ParentBone.Index] * this.Transform);
                    effect.Parameters["View"].SetValue (observer.View);
                    effect.Parameters["Projection"].SetValue (observer.Projection);
                }
                mesh.Draw ();

                // Restore AlphaBlendEnable to what is was before DrawBsp was called
                mesh.Effects[0].GraphicsDevice.RenderState.AlphaBlendEnable = oldBlend;
            }
        }
        public void DrawBspObserverWithoutFrustumCulling (Camera observer, Camera actor)
        {
            // invoke "makeOrthonormalCross" if derived from "Movable"
            // otherwise, use "makeOrthonormal"
            this.makeOrthonormal ();

            // a Model has multiple meshes, each with a parent ModelBone
            //   containing a transform for that mesh
            m_model.CopyAbsoluteBoneTransformsTo (m_transforms);

            // Grab the Bsp tree from the model's tag
            ITree tree = (ITree)m_model.Tag;

            // Compute the current visibility and get the list of visible ModelMeshes
            tree.ComputeVisibility (actor.Position);
            List<String> visibleModelMeshes = tree.VisibleMeshNames ();

            // Run through the list of visible model meshes and draw them
            for (int currentModelMesh = 0; currentModelMesh < visibleModelMeshes.Count; ++currentModelMesh)
            {
                // Grab the current mesh
                ModelMesh mesh = m_model.Meshes[visibleModelMeshes[currentModelMesh]];

                // Save the current AlphaBlendEnable value
                bool oldBlend = mesh.Effects[0].GraphicsDevice.RenderState.AlphaBlendEnable;

                // Set the AlphaBlendEnable to the value stored in the shader
                mesh.Effects[0].GraphicsDevice.RenderState.AlphaBlendEnable = mesh.Effects[0].Parameters["translucent"].GetValueBoolean ();

                foreach (Effect effect in mesh.Effects)
                {
                    effect.Parameters["World"].SetValue (m_transforms[mesh.ParentBone.Index] * this.Transform);
                    effect.Parameters["View"].SetValue (observer.View);
                    effect.Parameters["Projection"].SetValue (observer.Projection);
                }
                mesh.Draw ();

                // Restore AlphaBlendEnable to what is was before DrawBsp was called
                mesh.Effects[0].GraphicsDevice.RenderState.AlphaBlendEnable = oldBlend;
            }
        }
        public void DrawObserver (Camera observer, Camera actor)
        {
            // invoke "makeOrthonormalCross" if derived from "Movable"
            // otherwise, use "makeOrthonormal"
            this.makeOrthonormal ();

            // a Model has multiple meshes, each with a parent ModelBone
            //   containing a transform for that mesh
            m_model.CopyAbsoluteBoneTransformsTo (m_transforms);

            BoundingFrustum frustum = actor.computeBoundingFrustum ();
            foreach (ModelMesh mesh in m_model.Meshes)
            {
                // frustum cull meshes that aren't intersecting or contained within the view volume
                BoundingSphere sphere = mesh.BoundingSphere;
                sphere.Center = Vector3.Transform (sphere.Center, this.getRotation ());
                ContainmentType type = frustum.Contains (sphere);
                if (type == ContainmentType.Disjoint)
                    continue;

                EffectParameter translucent = mesh.Effects[0].Parameters.GetParameterBySemantic ("MuxEngineTranslucent");
                bool oldBlend = mesh.Effects[0].GraphicsDevice.RenderState.AlphaBlendEnable;
                if (translucent != null)
                {
                    mesh.Effects[0].GraphicsDevice.RenderState.AlphaBlendEnable = translucent.GetValueBoolean ();
                }
                foreach (Effect effect in mesh.Effects)
                {
                    EffectParameter world = effect.Parameters["World"];

                    if (world != null)
                    {
                        world.SetValue (m_transforms[mesh.ParentBone.Index] * this.Transform);
                        effect.Parameters["View"].SetValue (observer.View);
                        effect.Parameters["Projection"].SetValue (observer.Projection);
                    }
                }
                mesh.Draw ();

                mesh.Effects[0].GraphicsDevice.RenderState.AlphaBlendEnable = oldBlend;
            }
        }
        /****************************************************************************/
        /****************************************************************************/
        /*                      End Remove                                          */
        /****************************************************************************/
        /****************************************************************************/

        public void DrawBsp (Camera camera)
        {
            // invoke "makeOrthonormalCross" if derived from "Movable"
            // otherwise, use "makeOrthonormal"
            this.makeOrthonormal ();

            // a Model has multiple meshes, each with a parent ModelBone
            //   containing a transform for that mesh
            m_model.CopyAbsoluteBoneTransformsTo (m_transforms);

            // Grab the Bsp tree from the model's tag
            ITree tree = (ITree)m_model.Tag;

            // Get the camera's BoundingFrustum
            BoundingFrustum frustum = camera.computeBoundingFrustum ();

            // Compute the current visibility and get the list of visible ModelMeshes
            tree.ComputeVisibility (camera.Position);
            List<String> visibleModelMeshes = tree.VisibleMeshNames (frustum);

            // Run through the list of visible model meshes and draw them
            for (int currentModelMesh = 0; currentModelMesh < visibleModelMeshes.Count; ++currentModelMesh)
            {
                // Grab the current mesh
                ModelMesh mesh = m_model.Meshes[visibleModelMeshes[currentModelMesh]];

                // Save the current AlphaBlendEnable value
                bool oldBlend = mesh.Effects[0].GraphicsDevice.RenderState.AlphaBlendEnable;

                // Set the AlphaBlendEnable to the value stored in the shader
                mesh.Effects[0].GraphicsDevice.RenderState.AlphaBlendEnable = mesh.Effects[0].Parameters["translucent"].GetValueBoolean ();

                foreach (Effect effect in mesh.Effects)
                {
                    effect.Parameters["World"].SetValue (m_transforms[mesh.ParentBone.Index] * this.Transform);
                    effect.Parameters["View"].SetValue (camera.View);
                    effect.Parameters["Projection"].SetValue (camera.Projection);
                }
                mesh.Draw ();

                // Restore AlphaBlendEnable to what is was before DrawBsp was called
                mesh.Effects[0].GraphicsDevice.RenderState.AlphaBlendEnable = oldBlend;
            }
        }

        public void Draw (Camera camera)
        {
            // Invoke "makeOrthonormalCross" if derived from "Movable"
            // otherwise, use "makeOrthonormal"
            this.makeOrthonormal ();

            // A Model has multiple meshes, each with a parent ModelBone
            //   containing a transform for that mesh
            m_model.CopyAbsoluteBoneTransformsTo (m_transforms);

            // GMZ: Disable frustum culling until fully tested
            //BoundingFrustum frustum = camera.computeBoundingFrustum ();
            foreach (ModelMesh mesh in m_model.Meshes)
            {
                // Frustum cull meshes that aren't intersecting or contained within the view volume
                //BoundingSphere sphere = mesh.BoundingSphere;
                //sphere.Center = this.Position;
                //ContainmentType type = frustum.Contains (sphere);
                //if (type == ContainmentType.Disjoint)
                //    continue;

                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.EnableDefaultLighting ();

                    Matrix meshToModel = m_transforms[mesh.ParentBone.Index];
                    Matrix world = meshToModel * this.Transform;
                    effect.Parameters["World"].SetValue (world);
                    effect.Parameters["View"].SetValue (camera.View);
                    effect.Parameters["Projection"].SetValue (camera.Projection);
                }
                mesh.Draw ();
            }
        }

        // Draw the model assuming the bone transforms haven't changed
        //   The model can be transformed in the world, but the 
        //   mesh transforms must be static.
        // For large models this method may be substantially faster than the standard Draw.
        public void DrawWithoutRecomputingTransforms (Camera camera)
        {
            // Invoke "makeOrthonormalCross" if derived from "Movable"
            // otherwise, use "makeOrthonormal"
            this.makeOrthonormal ();

            // GMZ: Disable frustum culling until fully tested
            //BoundingFrustum frustum = camera.computeBoundingFrustum ();
            foreach (ModelMesh mesh in m_model.Meshes)
            {
                // Frustum cull meshes that aren't intersecting or contained within the view volume
                //BoundingSphere sphere = mesh.BoundingSphere;
                //sphere.Center = this.Position;
                //ContainmentType type = frustum.Contains (sphere);
                //if (type == ContainmentType.Disjoint)
                //    continue;

                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.EnableDefaultLighting ();

                    Matrix meshToModel = m_transforms[mesh.ParentBone.Index];
                    Matrix world = meshToModel * this.Transform;
                    effect.Parameters["World"].SetValue (world);
                    effect.Parameters["View"].SetValue (camera.View);
                    effect.Parameters["Projection"].SetValue (camera.Projection);
                }
                mesh.Draw ();
            }
        }

        public void DrawUsingGeneralShader (Camera camera)
        {
            // Invoke "makeOrthonormalCross" if derived from "Movable"
            // otherwise, use "makeOrthonormal"
            this.makeOrthonormal ();

            // A Model has multiple meshes, each with a parent ModelBone
            //   containing a transform for that mesh
            m_model.CopyAbsoluteBoneTransformsTo (m_transforms);

            foreach (ModelMesh mesh in m_model.Meshes)
            {
                foreach (Effect effect in mesh.Effects)
                {
                    Matrix meshToModel = m_transforms[mesh.ParentBone.Index];
                    Matrix world = meshToModel * this.Transform;
                    effect.Parameters["World"].SetValue (world);
                    Matrix worldViewProjection = world * camera.View * camera.Projection;
                    effect.Parameters["WorldViewProjection"].SetValue (worldViewProjection);
                    Matrix worldInverseTranspose = world;
                    Matrix.Invert (ref worldInverseTranspose, out worldInverseTranspose);
                    effect.Parameters["WorldInverseTranspose"].SetValueTranspose (worldInverseTranspose);
                    effect.Parameters["EyePosition"].SetValue (camera.Position);
                }
                mesh.Draw ();
            }
        }

        public int getVertexCount ()
        {
            return (MuxGraphics.getVertexCount (m_model));
        }

        public int getPrimitiveCount ()
        {
            return (MuxGraphics.getPrimitiveCount (m_model));
        }

    }
}
