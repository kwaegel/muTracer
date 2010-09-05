﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Graphics;
using OpenTK;
using OpenTK.Graphics;

using Raytracing.Primitives;

namespace Raytracing.SceneStructures
{
	public abstract class Scene
	{
		public Color4 BackgroundColor
		{
			get;
			set;
		}

		public Scene()
		{
			BackgroundColor = Color4.CornflowerBlue;
		}

		public Scene(Color4 backgroundColor)
		{
			BackgroundColor = backgroundColor;
		}

		public abstract void add(AbstractPrimitive primitive);

		public abstract void add(PointLight light);

		public abstract float getNearestIntersection(ref Ray r, ref Vector3 collisionPoint,
			ref Vector3 surfaceNormal, ref Material material);

		public abstract List<PointLight> getLights();
	};
}