using OpenTK;
using OpenTK.Graphics;

namespace Raytracing
{
	public abstract class Light
	{
		public Color4 Color;

		public abstract Vector3 getDirection(Vector3 from);
	}
}
