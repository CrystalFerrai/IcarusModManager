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

namespace IcarusModManager
{
	/// <summary>
	/// The main application window
	/// </summary>
	internal partial class MainWindow : Window
	{
		/// <summary>
		/// Creates a new instance of the MainWindow class
		/// </summary>
		public MainWindow(MainWindowVM viewModel)
		{
			DataContext = viewModel;
			InitializeComponent();

			Loaded += MainWindow_Loaded;
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			((MainWindowVM)DataContext).WindowLoaded();
		}

		protected override void OnDragOver(DragEventArgs e)
		{
			if (e.Data.GetData(DataFormats.FileDrop) != null) e.Effects = DragDropEffects.Copy;
			else e.Effects = DragDropEffects.None;
		}

		protected override void OnPreviewDrop(DragEventArgs e)
		{
			string[]? files = e.Data.GetData(DataFormats.FileDrop) as string[];
			if (files == null) return;

			((MainWindowVM)DataContext).AddMods(files);
		}
	}
}
