using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Raytracing
{
	class Timer
	{
		static System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

		public static void start()
		{
			stopwatch.Start();
		}

		// returns the delta time
		public static void stop()
		{
			stopwatch.Stop();

			System.Console.WriteLine("delta T: " + stopwatch.ElapsedMilliseconds + " ms");

			stopwatch.Reset();
		}
	}
}
