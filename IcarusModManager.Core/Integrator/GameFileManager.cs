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

using NetPak;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace IcarusModManager.Core.Integrator
{
	/// <summary>
	/// Provides access to all of the source (unmodded) asset files for the Icarus game.
	/// </summary>
	public class GameFileManager : IDisposable
	{
		private static readonly Regex sGetPakIndexRegex;

		private readonly PakFile mDataPak;

		private readonly List<PakFile> mContentPaks;

		private readonly Dictionary<string, PakFile> mContentMap;

		static GameFileManager()
		{
			sGetPakIndexRegex = new Regex(@".*\\pakchunk0_s(\d+)\-WindowsNoEditor\.pak", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		}

		private GameFileManager(PakFile dataPak)
		{
			mDataPak = dataPak;
			mContentPaks = new List<PakFile>();
			mContentMap = new Dictionary<string, PakFile>();
		}

		/// <summary>
		/// Create an instance of the manager and load metadata for all game assets.
		/// </summary>
		/// <param name="gameDirectory">The root directory of the game isntallation</param>
		public static GameFileManager Create(string gameDirectory)
		{
			string dataPakPath = Path.Combine(gameDirectory, "Icarus\\Content\\Data\\Data.pak");
			if (!File.Exists(dataPakPath)) throw new ArgumentException($"Could not locate Data.pak within \"{Path.GetDirectoryName(dataPakPath)}\"");

			string contentPaksDirectory = Path.Combine(gameDirectory, "Icarus\\Content\\Paks");
			if (!Directory.Exists(contentPaksDirectory)) throw new ArgumentException($"Could not locate directory\"{contentPaksDirectory}\"");

			List<string> contentPakPaths = new List<string>(Directory.GetFiles(contentPaksDirectory, "*.pak", SearchOption.TopDirectoryOnly));
			contentPakPaths.Sort((a, b) =>
			{
				Match matchA = sGetPakIndexRegex.Match(a);
				if (!matchA.Success) return -1;

				Match matchB = sGetPakIndexRegex.Match(b);
				if (!matchB.Success) return 1;

				int indexA = int.Parse(matchA.Groups[1].Value);
				int indexB = int.Parse(matchB.Groups[1].Value);

				return indexA - indexB;
			});

			GameFileManager instance = new GameFileManager(PakFile.Mount(dataPakPath));

			foreach (string path in contentPakPaths)
			{
				PakFile file = PakFile.Mount(path);
				instance.mContentPaks.Add(file);
				foreach (FString entryPath in file.Entries)
				{
					instance.mContentMap[entryPath] = file;
				}
			}

			return instance;
		}

		/// <summary>
		/// Disposes this instance, unloading data and closing files
		/// </summary>
		public void Dispose()
		{
			GC.SuppressFinalize(this);

			if (mDataPak != null)
			{
				mDataPak.Dispose();
			}
			foreach (PakFile file in mContentPaks)
			{
				file.Dispose();
			}
			mContentPaks.Clear();
		}

		~GameFileManager()
		{
			Dispose();
		}

		/// <summary>
		/// Returns whether an asset exists with the specific path
		/// </summary>
		/// <param name="path">The asset path</param>
		public bool HasFile(string path)
		{
			FString filePath = (FString)path;
			return mDataPak.HasEntry(filePath) || FindAsset(filePath) != null;
		}

		/// <summary>
		/// Reads an asset from the data table package (Data.pak)
		/// </summary>
		/// <param name="path">The asset path to load</param>
		/// <param name="data">If successful, outputs the data that was loaded</param>
		/// <returns>Whether the asset was found and loaded</returns>
		public bool ReadDataFile(string path, out ReadOnlySpan<byte> data)
		{
			return mDataPak.ReadEntryData((FString)path, out data);
		}

		/// <summary>
		/// Reads an asset from the primary game package
		/// </summary>
		/// <param name="path">The asset path to load</param>
		/// <param name="data">If successful, outputs the data that was loaded</param>
		/// <returns>Whether the asset was found and loaded</returns>
		public bool ReadAssetFile(string path, out ReadOnlySpan<byte> data)
		{
			FString assetPath = (FString)path;
			PakFile? file = FindAsset(assetPath);
			if (file == null)
			{
				data = default;
				return false;
			}
			return file.ReadEntryData(assetPath, out data);
		}

		/// <summary>
		/// Reads all files associated with an asset from the primary game package
		/// </summary>
		/// <param name="path">The asset path to load</param>
		/// <param name="data">If successful, outputs the data that was loaded</param>
		/// <param name="exportsPath">Outputs the path to the asset's exports file, if one exists.</param>
		/// <param name="exportsData">Outputs the asset's exports data, if a separate exports file exists.</param>
		/// <param name="bulkPath">Outputs the path to the asset's bulk data file, if one exists.</param>
		/// <param name="bulkData">Outputs the asset's bulk data, if a separate bulk data file exists.</param>
		/// <returns>Whether the asset was found and loaded</returns>
		public bool ReadFullAsset(string path, out ReadOnlySpan<byte> data, out string? exportsPath, out ReadOnlySpan<byte> exportsData, out string? bulkPath, out ReadOnlySpan<byte> bulkData)
		{
			FString assetPath = (FString)path;
			PakFile? file = FindAsset(assetPath);
			if (file == null)
			{
				data = default;
				exportsPath = default;
				exportsData = default;
				bulkPath = default;
				bulkData = default;
				return false;
			}
			FString? exportsPathTemp, bulkPathTemp;
			bool success = file.GetAssetData((FString)path, out data, out exportsPathTemp, out exportsData, out bulkPathTemp, out bulkData);
			exportsPath = exportsPathTemp?.Value;
			bulkPath = bulkPathTemp?.Value;
			return success;
		}

		private PakFile? FindAsset(string path)
		{
			if (mContentMap.TryGetValue(path, out PakFile? file)) return file;
			return null;
		}
	}
}
