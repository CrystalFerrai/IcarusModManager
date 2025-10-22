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

using IcarusModManager.Controls;
using IcarusModManager.Core;
using IcarusModManager.Core.Collections;
using IcarusModManager.Utils;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace IcarusModManager
{
	/// <summary>
	/// The view model for the main application window
	/// </summary>
	internal class MainWindowVM : ViewModelBase
	{
		private readonly Logger mLogger;

		private readonly Settings mSettings;

		private readonly ModManager mModManager;

		private readonly DispatcherTimer mResetStatusTimer;

		private Thread? mInstallModsThread;

		private readonly DelegateCommand mAddCommand;

		private readonly DelegateCommand mShowSettingsCommand;

		private readonly DelegateCommand mUninstallModsCommand;

		private readonly DelegateCommand mInstallModsCommand;

		private readonly DelegateCommand<int?> mMoveTopCommand;
		private readonly DelegateCommand<int?> mMoveUpCommand;
		private readonly DelegateCommand<int?> mMoveDownCommand;
		private readonly DelegateCommand<int?> mMoveBottomCommand;

		private readonly DelegateCommand<int?> mRemoveCommand;

		/// <summary>
		/// Gets the title for the main window
		/// </summary>
		public string WindowTitle { get; }

		/// <summary>
		/// The list of game mods being managed by the application
		/// </summary>
		public IObservableEnumerable<ModData> ModList => mModManager.ModList;

		/// <summary>
		/// The message to show in the application status bar
		/// </summary>
		public string StatusMessage
		{
			get => _statusMessage;
			private set => Set(ref _statusMessage, value);
		}
		private string _statusMessage = string.Empty;

		// Commands invoked from the UI

		public ICommand AddCommand => mAddCommand;

		public ICommand ShowSettingsCommand => mShowSettingsCommand;

		public ICommand UninstallModsCommand => mUninstallModsCommand;

		public ICommand InstallModsCommand => mInstallModsCommand;

		public ICommand MoveTopCommand => mMoveTopCommand;
		public ICommand MoveUpCommand => mMoveUpCommand;
		public ICommand MoveDownCommand => mMoveDownCommand;
		public ICommand MoveBottomCommand => mMoveBottomCommand;

		public ICommand RemoveCommand => mRemoveCommand;

		// End commands

		/// <summary>
		/// Increments whenever the mod list changes. Used to trigger some command binding updates in the UI.
		/// The actual value is not significant. Changing the value is all that matters.
		/// </summary>
		public int ListVersion
		{
			get => _listVersion;
			set => Set(ref _listVersion, value);
		}
		private int _listVersion;

		/// <summary>
		/// Indicates whether a mod install is currently underway
		/// </summary>
		public bool IsInstalling
		{
			get => _isInstalling;
			set => Set(ref _isInstalling, value);
		}
		private bool _isInstalling;

		public MainWindowVM()
		{
			Version version = Assembly.GetExecutingAssembly().GetName().Version!;
			WindowTitle = $"Icarus Mod Manager {version.ToString(3)}";

			mLogger = new MessageBoxLogger();

			mSettings = new Settings();
			mModManager = new ModManager(mLogger);
			mResetStatusTimer = new DispatcherTimer(TimeSpan.FromSeconds(10), DispatcherPriority.Normal, (s, e) => { StatusMessage = string.Empty; mResetStatusTimer!.Stop(); }, Dispatcher.CurrentDispatcher) { IsEnabled = false };

			mSettings.Load(mLogger);
			mModManager.Load();

			mModManager.ModList.CollectionChanged += ModList_CollectionChanged;

			mAddCommand = new DelegateCommand(AddMod);
			mShowSettingsCommand = new DelegateCommand(ShowSettings);
			mUninstallModsCommand = new DelegateCommand(UninstallMods);
			mInstallModsCommand = new DelegateCommand(InstallMods);

			mMoveTopCommand = new DelegateCommand<int?>(mModManager.MoveTop, mModManager.CanMoveUp);
			mMoveUpCommand = new DelegateCommand<int?>(mModManager.MoveUp, mModManager.CanMoveUp);
			mMoveDownCommand = new DelegateCommand<int?>(mModManager.MoveDown, mModManager.CanMoveDown);
			mMoveBottomCommand = new DelegateCommand<int?>(mModManager.MoveBottom, mModManager.CanMoveDown);

			mRemoveCommand = new DelegateCommand<int?>(mModManager.Remove);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (mInstallModsThread?.IsAlive ?? false)
				{
					mInstallModsThread.Join();
				}
				mResetStatusTimer.Stop();
			}

			base.Dispose(disposing);
		}

		/// <summary>
		/// Called when the main window finishes loading, before it is displayed
		/// </summary>
		public void WindowLoaded()
		{
			ModListUpdated();
		}

		/// <summary>
		/// Adds a list of mods
		/// </summary>
		/// <param name="filePaths">Paths to the mods to add</param>
		public void AddMods(IEnumerable<string> filePaths)
		{
			mModManager.Add(filePaths, out int added, out int replaced, out int failed);

			SetStatus($"{added} mods added. {replaced} replaced. {failed} failed to add.");
		}

		private void SetStatus(string message)
		{
			mResetStatusTimer.Stop();
			StatusMessage = message;
			mResetStatusTimer.Start();
		}

		private void ModList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			ModListUpdated();
		}

		private void ModListUpdated()
		{
			// Queuing this on the dispatcher at a low priority allows time for the ItemsControl bound to the mod list to sync up.
			// This is necessary because the index parameter of the commands depends on the ItemsControl.
			Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
			{
				++ListVersion;

				mMoveTopCommand.RaiseCanExecuteChanged();
				mMoveUpCommand.RaiseCanExecuteChanged();
				mMoveDownCommand.RaiseCanExecuteChanged();
				mMoveBottomCommand.RaiseCanExecuteChanged();
			}), DispatcherPriority.Input);
		}

		private void AddMod()
		{
			CommonOpenFileDialog dialog = new CommonOpenFileDialog("Select Game Folder")
			{
				EnsureFileExists = true,
				EnsurePathExists = true,
				IsFolderPicker = false,
				Multiselect = true,
				NavigateToShortcut = true
			};

			if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
			{
				AddMods(dialog.FileNames);
			}
		}

		private void ShowSettings()
		{
			using (SettingsVM settingsVm = new(mSettings))
			{
				if (DialogUtil.ShowDialog("Settings", settingsVm) == true)
				{
					mSettings.Save(mLogger);
				}
			}
		}

		private void UninstallMods()
		{
			if (!VerifyGameDirectory()) return;

			try
			{
				mModManager.UninstallMods(mSettings.GameDirectory);
				SetStatus("Mods uninstalled from game");
			}
			catch (Exception ex)
			{
				CustomMessageBox.Show($"An error occured attempting to remove the installed mod directory.\n\nThis usually means the game is running.\n\n[{ex.GetType().FullName}] {ex.Message}", "Mod Uninstall Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void InstallMods()
		{
			if (!VerifyGameDirectory()) return;

			IsInstalling = true;
			SetStatus("Installing mods...");
			mInstallModsThread = new Thread((dispatcher) =>
			{
				try
				{
					mModManager.InstallMods(mSettings.GameDirectory);

					((Dispatcher)dispatcher!).BeginInvoke(new Action(() =>
					{
						SetStatus("Mods installed");
						IsInstalling = false;
					}));
				}
				catch (Exception ex)
				{
					((Dispatcher)dispatcher!).BeginInvoke(new Action(() =>
					{
						CustomMessageBox.Show(Application.Current.MainWindow, $"An error occured while attempting to install mods.\n\n[{ex.GetType().FullName}] {ex.Message}", "Mod Install Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
						SetStatus("Failed to install mods");
						IsInstalling = false;
					}));
				}
			});

			mInstallModsThread.Start(Dispatcher.CurrentDispatcher);
		}

		private bool VerifyGameDirectory()
		{
			if (!Directory.Exists(mSettings.GameDirectory))
			{
				CustomMessageBox.Show("Could not locate game directory. Configure a valid game directory in the applicaiton settings.", "Game Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
				return false;
			}
			return true;
		}
	}
}
