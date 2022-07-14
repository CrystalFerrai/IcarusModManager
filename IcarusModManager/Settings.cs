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
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;

namespace IcarusModManager
{
	/// <summary>
	/// Application user settings
	/// </summary>
	internal class Settings
	{
		// This class uses reflection to read and write all public properties to/from disk. This means properties are
		// persisted automatically. However, only simple property types that can be converted from a string are supported.

		private static readonly Dictionary<string, PropertyInfo> sPropertyMap;

		private static readonly string sSavePath;

		/// <summary>
		/// The game installation directory
		/// </summary>
		public string GameDirectory { get; set; } = string.Empty;

		static Settings()
		{
			sPropertyMap = new Dictionary<string, PropertyInfo>();
			foreach (PropertyInfo pi in typeof(Settings).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
			{
				sPropertyMap.Add(pi.Name, pi);
			}

			sSavePath = Path.Combine(Constants.LocalUserDirectory, "Settings.txt");
		}

		/// <summary>
		/// Loads settings from disk
		/// </summary>
		public void Load()
		{
			if (File.Exists(sSavePath))
			{
				try
				{
					using (FileStream file = File.OpenRead(sSavePath))
					using (StreamReader reader = new StreamReader(file))
					{
						while (!reader.EndOfStream)
						{
							string line = reader.ReadLine()!.Trim();
							if (string.IsNullOrEmpty(line)) continue;

							string[] parts = line.Split('=');
							if (parts.Length != 2) continue;

							if (!sPropertyMap.TryGetValue(parts[0], out PropertyInfo? pi)) continue;

							pi.SetValue(this, Convert.ChangeType(parts[1], pi.PropertyType));
						}
					}
				}
				catch (Exception ex)
				{
					CustomMessageBox.Show(Application.Current.MainWindow, $"An error occurred while attempting to load application settings. All settings are now at their default values.\n\n[{ex.GetType().FullName}] {ex.Message}", "Unable to Load Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
			}

			if (string.IsNullOrEmpty(GameDirectory))
			{
				if (GameLocator.TryLocateGamePath(out string? path))
				{
					GameDirectory = path!;
				}
			}
		}

		/// <summary>
		/// Saves settings to disk
		/// </summary>
		public void Save()
		{
			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(sSavePath)!);

				using (FileStream file = File.Create(sSavePath))
				using (StreamWriter writer = new StreamWriter(file))
				{
					foreach (PropertyInfo pi in sPropertyMap.Values)
					{
						writer.WriteLine($"{pi.Name}={pi.GetValue(this)}");
					}
				}
			}
			catch (Exception ex)
			{
				CustomMessageBox.Show(Application.Current.MainWindow, $"An error occurred while attempting to save application settings. You can try again fromt he settings dialog.\n\n[{ex.GetType().FullName}] {ex.Message}", "Unable to Save Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}
	}
}
