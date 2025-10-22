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

using Microsoft.Win32;
using System.IO;
using System.Runtime.InteropServices;

namespace IcarusModManager.Core.Utils
{
	/// <summary>
	/// Utility for automatically locating the Icarus game installation directory
	/// </summary>
	public static class GameLocator
	{
		private const string IcarusAppId = "1149460";

		/// <summary>
		/// Attempts to locate the game's install directory using Steam installer data
		/// </summary>
		/// <param name="path">If successful, outputs the game installation directory path</param>
		/// <returns>Whether the game installation could be located</returns>
		public static bool TryLocateGamePath(out string? path)
		{
			path = null;
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return false;

			RegistryKey? baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
			if (baseKey == null) return false;

			RegistryKey? steamKey = baseKey.OpenSubKey("SOFTWARE\\Valve\\Steam", RegistryKeyPermissionCheck.ReadSubTree);
			if (steamKey == null) return false;

			string? steamPath = steamKey.GetValue("InstallPath") as string;
			if (steamPath == null) return false;

			string libraryPath = Path.Combine(steamPath, "steamapps\\libraryfolders.vdf");
			if (!File.Exists(libraryPath)) return false;

			string? appsPath = null;
			try
			{
				SteamMetaFile library = SteamMetaFile.Load(libraryPath);

				foreach (SteamMetaObject obj in library.RootObject!)
				{
					SteamMetaObject apps = (SteamMetaObject)obj["apps"];

					foreach (SteamMetaValue app in apps)
					{
						if (app.Name == IcarusAppId)
						{
							appsPath = ((SteamMetaValue)obj["path"]).Value;
							break;
						}
					}
					if (appsPath != null) break;
				}
			}
			catch
			{
				return false;
			}
			if (appsPath == null) return false;
			appsPath = appsPath.Replace("\\\\", "\\");

			string appManifestPath = Path.Combine(appsPath, $"steamapps\\appmanifest_{IcarusAppId}.acf");
			if (!File.Exists(appManifestPath)) return false;

			string installDir;
			try
			{
				SteamMetaFile appManifest = SteamMetaFile.Load(appManifestPath);
				installDir = ((SteamMetaValue)appManifest.RootObject!["installdir"]).Value.Replace("\\\\", "\\");
			}
			catch
			{
				return false;
			}

			string gameDir = Path.Combine(appsPath, "steamapps\\common", installDir);
			if (!Directory.Exists(gameDir)) return false;

			path = gameDir;
			return true;
		}
	}
}
