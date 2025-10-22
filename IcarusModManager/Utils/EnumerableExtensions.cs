// Copyright 2025 Crystal Ferrai
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;

namespace IcarusModManager.Utils
{
	/// <summary>
	/// Extension methods for enumerables
	/// </summary>
	public static class EnumerableExtensions
	{
		/// <summary>
		/// Returns the index of the first item in the enumeration that matches the predicate
		/// </summary>
		/// <typeparam name="T">The type of items in the enumeration</typeparam>
		/// <param name="enumerable">The enumerable instance</param>
		/// <param name="predicate">The predicate to match</param>
		public static int IndexOf<T>(this IEnumerable<T> enumerable, Predicate<T> predicate)
		{
			var e = enumerable.GetEnumerator();
			for (int i = 0; e.MoveNext(); ++i)
			{
				if (predicate(e.Current)) return i;
			}
			return -1;
		}

		/// <summary>
		/// Returns whether the enumeration contains exactly one item
		/// </summary>
		/// <param name="enumerable">The enumerable instance</param>
		public static bool CountEqualsOne<T>(this IEnumerable<T> enumerable)
		{
			var e = enumerable.GetEnumerator();
			return e.MoveNext() ? !e.MoveNext() : false;
		}
	}
}
