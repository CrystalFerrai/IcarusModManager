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

using IcarusModManager.Controls;
using IcarusModManager.Utils;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace IcarusModManager
{
	/// <summary>
	/// View model for the settings dialog
	/// </summary>
	internal class SettingsVM : ViewModelBase
	{
		private readonly Settings mSettings;

		private readonly DelegateCommand mBrowseGamePathCommand;

		private readonly DelegateCommand mLocateGamePathCommand;

		public string GamePath
		{
			get => _gamePath;
			set => Set(ref _gamePath, value);
		}
		private string _gamePath = string.Empty;

		/// <summary>
		/// Whether the automatic game locating feature is available
		/// </summary>
		public bool GameLocatorAvailable => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

		/// <summary>
		/// Browse for the game isntall path
		/// </summary>
		public ICommand BrowseGamePathCommand => mBrowseGamePathCommand;

		/// <summary>
		/// Attempt to automatically locate the game install path
		/// </summary>
		public ICommand LocateGamePathCommand => mLocateGamePathCommand;

		public SettingsVM(Settings settings)
		{
			mSettings = settings;
			GamePath = settings.GameDirectory;

			mBrowseGamePathCommand = new DelegateCommand(BrowseGamePath);
			mLocateGamePathCommand = new DelegateCommand(LocateGamePath);

			ValidateGamePath();
			PropertyChanged += This_PropertyChanged;
		}

		/// <summary>
		/// Save the changes made in the dialog
		/// </summary>
		public void CommitChanges()
		{
			mSettings.GameDirectory = GamePath;
		}

		private void This_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(GamePath))
			{
				ValidateGamePath();
			}
		}

		private void ValidateGamePath()
		{
			if (!Directory.Exists(GamePath)) SetError("Game folder cannot be accessed", nameof(GamePath));
			else if (!Directory.Exists(Path.Combine(GamePath, "Icarus\\Content\\Paks"))) SetError("Specified game folder does not appear to be a valid Icarus installation folder.", nameof(GamePath));
			else ClearError(nameof(GamePath));

			NotifyPropertyChanged(nameof(HasErrors));
		}

		private void BrowseGamePath()
		{
			CommonOpenFileDialog dialog = new CommonOpenFileDialog("Select Game Folder")
			{
				EnsureFileExists = true,
				EnsurePathExists = true,
				IsFolderPicker = true,
				Multiselect = false,
				NavigateToShortcut = true
			};

			if (Directory.Exists(GamePath)) dialog.InitialDirectory = GamePath;

			if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
			{
				GamePath = dialog.FileName;
			}
		}

		private void LocateGamePath()
		{
			string? path;
			if (!GameLocator.TryLocateGamePath(out path))
			{
				CustomMessageBox.Show(Application.Current.MainWindow, "Unable to locate game installation. You will need to manually locate it.", "Unable to Locate Game", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			GamePath = path!;
		}
	}
}
