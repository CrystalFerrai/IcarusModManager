// Copyright 2022 Crystal Ferrai
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

using System.Windows;

namespace IcarusModManager.Utils
{
	/// <summary>
	/// Utility for showing a dialog with arbitrary content
	/// </summary>
	internal static class DialogUtil
	{
		/// <summary>
		/// Shows a modal dialog
		/// </summary>
		/// <param name="title">The dialog title</param>
		/// <param name="content">The content of the dialog</param>
		/// <param name="owner">The owner of the dialog window</param>
		/// <returns>The dialog result invoked by the user</returns>
		public static bool? ShowDialog(string title, object content, Window? owner = null)
		{
			if (owner == null) owner = Application.Current.MainWindow;

			Window window = new Window()
			{
				Title = title,
				Content = content,
				Owner = owner,
				SizeToContent = SizeToContent.WidthAndHeight,
				ResizeMode = ResizeMode.NoResize
			};

			return window.ShowDialog();
		}
	}
}
