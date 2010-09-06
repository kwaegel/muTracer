using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Raytracing
{
	class DebugUtils
	{

		public static void printVector(float[] floats)
		{
			System.Diagnostics.Trace.Write("(");
			int numPrinted = 0;
			foreach (float f in floats)
			{
				if (numPrinted == floats.Length - 1)
					System.Diagnostics.Trace.WriteLine(f + ")");
				else if (++numPrinted % 4 == 0)
					System.Diagnostics.Trace.WriteLine(f);
				else
					System.Diagnostics.Trace.Write(f + ", ");
			}
			//System.Diagnostics.Trace.WriteLine(")");
		}
	}
}
