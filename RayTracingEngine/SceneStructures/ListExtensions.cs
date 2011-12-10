using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KDTreeTracer.RayTracing
{
	static class ListExtensions
	{
		//Swap elements i and j
		public static void Swap<T>(List<T> list, int i, int j)
		{
			T temp = list[j];
			list[j] = list[i];
			list[i] = temp;
		}

		// Partition the range [start, end] inclusive so that match(true) <= match(false)
		public static int Partition<T>(this List<T> list, int start, int end, Predicate<T> pred)
		{
			int i = start - 1;
			int j = end;
			while (i < j)
			{
				while (i <= end && pred(list[++i])) ;
				while (j >= start && !pred(list[--j])) ;
				if (i < j)
					ListExtensions.Swap(list, i, j);
			}
			return i;
		}
	}
}
