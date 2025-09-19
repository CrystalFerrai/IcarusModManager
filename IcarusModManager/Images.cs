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

using IcarusModManager.Utils;
using System;
using System.Linq;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace IcarusModManager
{
	/// <summary>
	/// Specifies image sources for all of the images embedded within this application
	/// </summary>
	internal static class Images
	{
		// Note: Class names must match directory names. Property names must match image file names.

		internal static class ActionIcons
		{
#nullable disable annotations

			public static ImageSource Add { get; private set; }
			public static ImageSource Bottom { get; private set; }
			public static ImageSource Down { get; private set; }
			public static ImageSource Remove { get; private set; }
			public static ImageSource Search { get; private set; }
			public static ImageSource Top { get; private set; }
			public static ImageSource Up { get; private set; }
			public static ImageSource Web { get; private set; }

#nullable enable annotations
			static ActionIcons()
			{
				LoadImages(typeof(ActionIcons));
			}
		}

		/// <summary>
		/// Loads images and sets the image properties for a type
		/// </summary>
		/// <param name="type">The type to set properties on</param>
		private static void LoadImages(Type type)
		{
			foreach (PropertyInfo property in type.GetProperties(BindingFlags.Static | BindingFlags.Public).Where(p => p.PropertyType.IsAssignableFrom(typeof(BitmapImage))))
			{
				property.SetValue(null, new BitmapImage(ResourceHelper.GetResourceUri(string.Format("/Images/{0}/{1}.png", type.Name, property.Name))), null);
			}
		}
	}
}
