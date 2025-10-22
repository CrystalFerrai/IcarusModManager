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

using IcarusModManager.Core.Collections;
using IcarusModManager.Core.Integrator;
using IcarusModManager.Core.Utils;
using NetPak;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using UAssetAPI;

namespace IcarusModManager.Core
{
	/// <summary>
	/// Handles the management of game mods for the application
	/// </summary>
	public class ModManager
	{
		private static readonly string ModMetaPath;

		private readonly ObservableList<ModData> mModList;

		private readonly Logger mLogger;

		/// <summary>
		/// The list of mods currently being managed
		/// </summary>
		public IObservableCollection<ModData> ModList => mModList;

		static ModManager()
		{
			ModMetaPath = Path.Combine(Constants.LocalUserDirectory, "ModMeta.json");
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="logger">For logging issues that occur while perforing operations</param>
		public ModManager(Logger logger)
		{
			mModList = new ObservableList<ModData>();
			mLogger = logger;
		}

		/// <summary>
		/// Load state from disk
		/// </summary>
		public void Load()
		{
			try
			{
				if (!Directory.Exists(Constants.ModStorageDirectory))
				{
					Directory.CreateDirectory(Constants.ModStorageDirectory);
					return;
				}
			}
			catch
			{
				return;
			}

			Dictionary<string, ModData> modMap = new();
			try
			{
				HashSet<string> supportedExtensions = new()
				{
					".zip",
					".pak"
				};
				foreach (string path in Directory.GetFiles(Constants.ModStorageDirectory, "*.*"))
				{
					if (!supportedExtensions.Contains(Path.GetExtension(path).ToLowerInvariant())) continue;

					try
					{
						ModData mod = ModData.Load(path, mLogger, false);
						modMap.Add(mod.FileName!, mod);
					}
					catch
					{
					}
				}
			}
			catch
			{
				return;
			}

			if (File.Exists(ModMetaPath))
			{
				try
				{
					JArray modMetas;

					using (FileStream file = File.OpenRead(ModMetaPath))
					using (StreamReader stream = new StreamReader(file))
					using (JsonTextReader reader = new JsonTextReader(stream))
					{
						JsonSerializer serializer = new JsonSerializer();
						modMetas = (JArray)serializer.Deserialize(reader)!;
					}

					ModData[] mods = new ModData[modMap.Count];
					foreach (JObject meta in modMetas)
					{
						string modFile = (string)meta["FileName"]!;
						if (modMap.TryGetValue(modFile, out ModData? mod))
						{
							mod.IsEnabled = (bool)(meta["IsEnabled"] ?? true);
							mModList.Add(mod);
							modMap.Remove(modFile);
						}
					}

					foreach (ModData mod in modMap.Values)
					{
						mModList.Add(mod);
					}
				}
				catch (Exception ex)
				{
					mLogger.Warning("Metadata Load Failed", $"An error occured while loading the master mod metadata file. Some information about mod configuration has been lost.\n\n[{ex.GetType().FullName}] {ex.Message}");
					mModList.Clear();
					mModList.AddRange(modMap.Values);
				}
			}
			else
			{
				mModList.AddRange(modMap.Values);
			}

			foreach (ModData mMod in mModList)
			{
				mMod.PropertyChanged += Mod_PropertyChanged;
			}

			mModList.CollectionChanged += ModList_CollectionChanged;
		}

		/// <summary>
		/// Save current state to disk
		/// </summary>
		public void Save()
		{
			try
			{
				// Directory should already exist unless it was externally deleted while the app has been running.
				Directory.CreateDirectory(Constants.LocalUserDirectory);

				using (FileStream file = File.Create(ModMetaPath))
				using (StreamWriter stream = new StreamWriter(file))
				using (JsonTextWriter writer = new JsonTextWriter(stream))
				{
					writer.Formatting = Formatting.Indented;

					writer.WriteStartArray();
					foreach (ModData mod in mModList)
					{
						writer.WriteStartObject();

						writer.WritePropertyName("FileName");
						writer.WriteValue(mod.FileName);

						writer.WritePropertyName("IsEnabled");
						writer.WriteValue(mod.IsEnabled);

						writer.WriteEndObject();
					}
					writer.WriteEndArray();
				}
			}
			catch (Exception ex)
			{
				mLogger.Warning("Metadata Save Failed", $"An error occured while saving the master mod metadata file.\n\n[{ex.GetType().FullName}] {ex.Message}");
			}
		}

		/// <summary>
		/// Installs the currently enabled mods into the game
		/// </summary>
		/// <param name="gameDirectory">The root directory of the game installation</param>
		public void InstallMods(string gameDirectory)
		{
			if (!Directory.Exists(gameDirectory)) return;

			// Ensure any previous files are cleaned up
			UninstallMods(gameDirectory);

			using GameFileManager fileManager = GameFileManager.Create(gameDirectory);

			string modDirectory = Path.Combine(gameDirectory, "Icarus\\Content\\Paks\\Mods");
			Directory.CreateDirectory(modDirectory);

			Dictionary<string, List<PatchWrapper<JsonPatchData>>> jsonPatches = new();
			Dictionary<string, List<PatchWrapper<ActorPatchData>>> actorPatches = new();
			Dictionary<string, List<PatchWrapper<DataTablePatchData>>> dataTablePatches = new();
			Dictionary<string, List<PatchWrapper<AssetCopyPatchData>>> assetCopyPatches = new();

			Dictionary<string, ModData> fileOverrides = new();
			for (int i = 0; i < mModList.Count; ++i)
			{
				ModData mod = mModList[i];
				if (!mod.IsEnabled) continue;

				foreach (string fileOverride in mod.GetFileOverrides())
				{
					// The last mod to override a file wins
					fileOverrides[fileOverride] = mod;

					// Patches to this file from earlier mods should not be applied
					jsonPatches.Remove(fileOverride);
					actorPatches.Remove(fileOverride);
					dataTablePatches.Remove(fileOverride);
					assetCopyPatches.Remove(fileOverride);
				}

				mod.CollectPatches(jsonPatches, actorPatches, dataTablePatches, assetCopyPatches, fileManager, mLogger);

				mod.CopyPaks(modDirectory, i);
			}

			Dictionary<string, List<object>> allDataPatches = new();
			foreach (var dataPatch in jsonPatches)
			{
				allDataPatches.Add(dataPatch.Key, dataPatch.Value.Cast<object>().ToList());
			}
			foreach (var dataTablePatch in dataTablePatches)
			{
				List<object>? list;
				if (!allDataPatches.TryGetValue(dataTablePatch.Key, out list))
				{
					list = new();
					allDataPatches.Add(dataTablePatch.Key, list);
				}
				list.AddRange(dataTablePatch.Value.Cast<object>());
			}

			// The data pak file has a bogus mount point, so everything gets loaded as if there is no mount point
			string dataIntegrationFileName = "998-ModIntegration_Data_P.pak";
			PakFile dataIntegrationFile = PakFile.Create((FString)dataIntegrationFileName, (FString)"../../../Icarus/Content/data/");
			foreach (var pair in allDataPatches)
			{
				IEnumerable<PatchWrapper<JsonPatchData>> fileJsonPatches = pair.Value.OfType<PatchWrapper<JsonPatchData>>();
				IEnumerable<PatchWrapper<DataTablePatchData>> fileDataTablePatches = pair.Value.OfType<PatchWrapper<DataTablePatchData>>();

				ReadOnlySpan<byte> sourceData;
				if (fileOverrides.TryGetValue(pair.Key, out ModData? mod))
				{
					mod.GetFileOverrideData(pair.Key, out sourceData);
				}
				else fileManager.ReadDataFile(pair.Key, out sourceData);

				string modifiedData = Encoding.UTF8.GetString(sourceData);
				if (fileJsonPatches.Any())
				{
					modifiedData = JsonIntegrator.Integrate(modifiedData, fileJsonPatches.SelectMany(p => p.Patch.Patches));
				}
				if (fileDataTablePatches.Any())
				{
					modifiedData = DataTableIntegrator.Integrate(modifiedData, fileDataTablePatches.SelectMany(p => p.Patch.Patches));
				}
				dataIntegrationFile.AddEntry((FString)pair.Key, Encoding.UTF8.GetBytes(modifiedData));
			}
			dataIntegrationFile.Save(Path.Combine(modDirectory, dataIntegrationFileName));

			Dictionary<string, List<object>> allAssetPatches = new();
			foreach (var actorPatch in actorPatches)
			{
				allAssetPatches.Add(actorPatch.Key, actorPatch.Value.Cast<object>().ToList());
			}
			foreach (var assetCopyPatch in assetCopyPatches)
			{
				List<object>? list;
				if (!allAssetPatches.TryGetValue(assetCopyPatch.Key, out list))
				{
					list = new();
					allAssetPatches.Add(assetCopyPatch.Key, list);
				}
				list.AddRange(assetCopyPatch.Value.Cast<object>());
			}

			string assetIntegrationFileName = "999-ModIntegration_Assets_P.pak";
			PakFile assetIntegrationFile = PakFile.Create((FString)assetIntegrationFileName, (FString)"../../../");
			foreach (var pair in allAssetPatches)
			{
				IEnumerable<PatchWrapper<ActorPatchData>> fileActorPatches = pair.Value.OfType<PatchWrapper<ActorPatchData>>();
				IEnumerable<PatchWrapper<AssetCopyPatchData>> fileAssetCopyPatches = pair.Value.OfType<PatchWrapper<AssetCopyPatchData>>();

				string? exportsPath;

				ModData? overrideAssetMod = null;
				ModData? overrideExportsMod = null;
				UAsset originalAsset;
				UAsset? overrideAsset = null;
				if (fileOverrides.TryGetValue(pair.Key, out overrideAssetMod))
				{
					ReadOnlySpan<byte> sourceData;
					ReadOnlySpan<byte> exportsData;
					overrideAssetMod.GetFileOverrideData(pair.Key, out sourceData);
					exportsPath = Path.ChangeExtension(pair.Key, ".uexp");
					if (fileOverrides.TryGetValue(exportsPath, out overrideExportsMod))
					{
						overrideExportsMod.GetFileOverrideData(exportsPath, out exportsData);
					}
					else
					{
						fileManager.ReadAssetFile(exportsPath, out exportsData);
					}
					overrideAsset = IntegratorUtil.ReadAsset(sourceData, exportsData);
				}

				{
					ReadOnlySpan<byte> sourceData;
					ReadOnlySpan<byte> exportsData;
					fileManager.ReadFullAsset(pair.Key, out sourceData, out exportsPath, out exportsData, out string? _, out var _);
					originalAsset = IntegratorUtil.ReadAsset(sourceData, exportsData);
				}

				if (fileActorPatches.Any())
				{
					UAsset asset = overrideAsset ?? originalAsset;

					ActorIntegrator.Integrate(asset, fileActorPatches.SelectMany(ap => ap.Patch.Components).ToArray());
					IntegratorUtil.WriteAsset(asset, pair.Key, exportsPath!, assetIntegrationFile);
				}

				string currentPath = pair.Key;
				foreach(PatchWrapper<AssetCopyPatchData> patch in fileAssetCopyPatches)
				{
					UAsset asset;
					if (overrideAssetMod is not null && overrideAssetMod.ID == patch.ModId)
					{
						// Do not use the asset from the same mod as the source for a copy
						asset = originalAsset;
					}
					else
					{
						asset = overrideAsset ?? originalAsset;
					}

					AssetCopyIntegrator.Integrate(asset, currentPath, patch.Patch.NewPath);
					IntegratorUtil.WriteAsset(asset, patch.Patch.NewPath, Path.ChangeExtension(patch.Patch.NewPath, ".uexp"), assetIntegrationFile);
					currentPath = patch.Patch.NewPath;
				}
			}
			assetIntegrationFile.Save(Path.Combine(modDirectory, assetIntegrationFileName));
		}

		/// <summary>
		/// Uninstalls all mods from the game
		/// </summary>
		/// <param name="gameDirectory">The root directory of the game installation</param>
		public void UninstallMods(string gameDirectory)
		{
			if (!Directory.Exists(gameDirectory)) return;

			string modDirectory = Path.Combine(gameDirectory, "Icarus\\Content\\Paks\\Mods");
			if (Directory.Exists(modDirectory)) Directory.Delete(modDirectory, true);
		}

		/// <summary>
		/// Adds mods to the manager
		/// </summary>
		/// <param name="filePaths">The paths to the mod files to add</param>
		/// <param name="added">Outputs how many files were successfully added</param>
		/// <param name="replaced">Outputs how many files were replaced (due to having the same mod ID or file name)</param>
		/// <param name="failed">Outputs how many mods failed to load. Failures also present a dialog box with more information.</param>
		public void Add(IEnumerable<string> filePaths, out int added, out int replaced, out int failed)
		{
			added = replaced = failed = 0;
			foreach (string path in filePaths)
			{
				string extension = Path.GetExtension(path);
				switch (extension)
				{
					case ".pak":
					case ".zip":
						{
							string fileName = Path.GetFileName(path);
							string newPath = Path.Combine(Constants.ModStorageDirectory, fileName);
							try
							{
								File.Copy(path, newPath, true);

								ModData mod = ModData.Load(newPath, mLogger);
								int existing = mModList.IndexOf(m => mod.ID == m.ID);
								if (existing < 0)
								{
									mModList.Add(mod);
									++added;
								}
								else
								{
									ModData oldMod = mModList[existing];
									if (oldMod.FileName != null && oldMod.FileName != mod.FileName)
									{
										File.Delete(Path.Combine(Constants.ModStorageDirectory, oldMod.FileName));
									}

									mod.IsEnabled = oldMod.IsEnabled;
									mModList[existing] = mod;
									++replaced;
								}
							}
							catch (Exception ex)
							{
								mLogger.Warning("Mod Load Failed", $"An error occured while loading the mod file \"{fileName}\".\n\n[{ex.GetType().FullName}] {ex.Message}");
								++failed;
							}
						}
						break;
				}
			}
		}

		/// <summary>
		/// Can the mod at the specified index more to an earlier position in the mod list?
		/// </summary>
		public bool CanMoveUp(int? index)
		{
			return index > 0;
		}

		/// <summary>
		/// Can the mod at the specified index more to a later position in the mod list?
		/// </summary>
		public bool CanMoveDown(int? index)
		{
			return index < mModList.Count - 1;
		}

		/// <summary>
		/// Move the mod from the specified index to the beginning of the mod list
		/// </summary>
		public void MoveTop(int? index)
		{
			if (index.HasValue) mModList.Move(index.Value, 0);
		}

		/// <summary>
		/// Swap the mod at the specified index with the one before it
		/// </summary>
		public void MoveUp(int? index)
		{
			if (index.HasValue) mModList.Move(index.Value, index.Value - 1);
		}

		/// <summary>
		/// Swap the mod at the specified index with the one after it
		/// </summary>
		public void MoveDown(int? index)
		{
			if (index.HasValue) mModList.Move(index.Value, index.Value + 1);
		}

		/// <summary>
		/// Move the mod from the specified index to the end of the mod list
		/// </summary>
		public void MoveBottom(int? index)
		{
			if (index.HasValue) mModList.Move(index.Value, mModList.Count - 1);
		}

		/// <summary>
		/// Remove the mod at the specified inedex
		/// </summary>
		public void Remove(int? index)
		{
			if (!index.HasValue) return;

			ModData data = mModList[index.Value];
			try
			{
				File.Delete(Path.Combine(Constants.ModStorageDirectory, data.FileName!));
				mModList.RemoveAt(index.Value);
			}
			catch (Exception ex)
			{
				mLogger.Warning("Remove Failed", $"An error occurred while trying to remove the mod \"{data.Name}\".\n\n[{ex.GetType().FullName}] {ex.Message}");
			}
		}

		private void ModList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == NotifyCollectionChangedAction.Reset) throw new NotSupportedException("Unexpected collection reset. This is not supported.");

			Save();

			if (e.Action == NotifyCollectionChangedAction.Move) return;

			if (e.NewItems != null)
			{
				foreach (ModData mod in e.NewItems)
				{
					mod.PropertyChanged += Mod_PropertyChanged;
				}
			}
			if (e.OldItems != null)
			{
				foreach (ModData mod in e.OldItems)
				{
					mod.PropertyChanged -= Mod_PropertyChanged;
				}
			}
		}

		private void Mod_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ModData.IsEnabled))
			{
				Save();
			}
		}
	}
}
