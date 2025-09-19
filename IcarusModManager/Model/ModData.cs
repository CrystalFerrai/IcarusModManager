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
using IcarusModManager.Integrator;
using IcarusModManager.Utils;
using NetPak;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows;

namespace IcarusModManager.Model
{
	/// <summary>
	/// The data for a game mod
	/// </summary>
	internal class ModData : ObservableObject
	{
		private readonly List<ModPakFile> mPakFiles;

		private readonly List<ModPatchFile> mPatchFiles;

		/// <summary>
		/// The unique ID of the mod. Two instances with the same ID will be considered the same mod.
		/// </summary>
		public string ID { get; }

		/// <summary>
		/// The name of the mod's file on disk
		/// </summary>
		public string? FileName
		{
			get => _fileName;
			set { Set(ref _fileName, value); }
		}
		private string? _fileName;

		/// <summary>
		/// The name of the mod
		/// </summary>
		public string? Name
		{
			get => _Name;
			set { Set(ref _Name, value); }
		}
		private string? _Name;

		/// <summary>
		/// The author of the mod
		/// </summary>
		public string? Author
		{
			get => _author;
			set { Set(ref _author, value); }
		}
		private string? _author;

		/// <summary>
		/// The version of the mod
		/// </summary>
		public string? Version
		{
			get => _version;
			set { Set(ref _version, value); }
		}
		private string? _version;

		/// <summary>
		/// The last time the mod's file on disk was modified
		/// </summary>
		public DateTime LastModifiedTime
		{
			get => _lastModifiedTime;
			set => Set(ref _lastModifiedTime, value);
		}
		private DateTime _lastModifiedTime;

		/// <summary>
		/// A short description of the mod
		/// </summary>
		public string? Description
		{
			get => _description;
			set { Set(ref _description, value); }
		}
		private string? _description;

		/// <summary>
		/// A link to the mod's website
		/// </summary>
		public string? Web
		{
			get => _web;
			set { Set(ref _web, value); }
		}
		private string? _web;

		/// <summary>
		/// Whether the mod is currently enabled
		/// </summary>
		public bool IsEnabled
		{
			get => _isEnabled;
			set { Set(ref _isEnabled, value); }
		}
		private bool _isEnabled = true;

		private ModData(string id)
		{
			ID = id;
			mPakFiles = new List<ModPakFile>();
			mPatchFiles = new List<ModPatchFile>();
		}

		/// <summary>
		/// Loads a mod from disk
		/// </summary>
		/// <param name="path">The path to the mod file. Must be a zip or pak file.</param>
		/// <param name="notifyOnError">Whether to show a message box to the user if an error occurs during load</param>
		public static ModData Load(string path, bool notifyOnError = true)
		{
			FileInfo info = new FileInfo(path);
			using (FileStream file = File.OpenRead(path))
			{
				return LoadFrom(file, Path.GetFileName(path), info.LastWriteTime, notifyOnError);
			}
		}

		private static ModData LoadFrom(Stream stream, string fileName, DateTime lastModified, bool notifyOnError = true)
		{
			string fallbackId = Path.GetFileNameWithoutExtension(fileName);

			string extension = Path.GetExtension(fileName);
			if (extension == ".pak")
			{
				string id = Path.GetFileNameWithoutExtension(fileName);
				ModData mod = new ModData(id)
				{
					FileName = fileName,
					Name = fileName,
					Author = "Unkown",
					Description = "This is a raw pak file mod. No metadata is available.",
					LastModifiedTime = lastModified
				};

				stream.Seek(0, SeekOrigin.Begin);
				mod.mPakFiles.Add(new ModPakFile(fileName, stream.ReadBytes((int)stream.Length)));
				return mod;
			}
			if (extension == ".zip")
			{
				ModData mod;
				using (ZipArchive zip = new(stream, ZipArchiveMode.Read, true))
				{
					ZipArchiveEntry? infoEntry = zip.GetEntry("mod.info");
					if (infoEntry != null)
					{
						byte[] infoBytes = new byte[infoEntry.Length];
						using (Stream infoFile = infoEntry.Open())
						{
							infoFile.ReadAll(infoBytes, 0, infoBytes.Length);
						}
						try
						{
							string infoText = Encoding.UTF8.GetString(infoBytes);
							JObject? infoObject = JsonConvert.DeserializeObject(infoText) as JObject;
							if (infoObject == null) throw new FormatException($"Expected root of file to be a json object");

							// Verify schema version
							{
								JProperty? schemaVersionProperty = infoObject.Property("schema_version");
								if (schemaVersionProperty == null) throw new FormatException("Missing required property 'schema_version'");
								int schemaVersion = schemaVersionProperty.Value.Value<int>();
								if (schemaVersion != 1) throw new FormatException($"Unknown schema version: {schemaVersion}. Supported schema version: 1");
							}

							mod = new ModData(infoObject["id"]?.Value<string>() ?? fallbackId)
							{
								FileName = fileName,
								LastModifiedTime = lastModified,
								Name = infoObject["name"]?.Value<string>() ?? fallbackId,
								Author = infoObject["author"]?.Value<string>(),
								Description = infoObject["description"]?.Value<string>(),
								Version = infoObject["version"]?.Value<string>(),
								Web = infoObject["web"]?.Value<string>()
							};
						}
						catch (Exception ex)
						{
							if (notifyOnError) CustomMessageBox.Show($"An error occurred while attempting to read mod.info from {fileName}. Mod loading will continue, but this mod will be missing meta information.\n\n[{ex.GetType().FullName}] {ex.Message}", "Metadata load failed", MessageBoxButton.OK, MessageBoxImage.Warning);

							mod = new ModData(fallbackId)
							{
								FileName = fileName,
								LastModifiedTime = lastModified,
								Name = fallbackId
							};
						}
					}
					else
					{
						mod = new ModData(fallbackId)
						{
							FileName = fileName,
							LastModifiedTime = lastModified,
							Name = fallbackId
						};
					}

					foreach (ZipArchiveEntry entry in zip.Entries)
					{
						string entryExtension = Path.GetExtension(entry.Name);

						try
						{
							switch (entryExtension)
							{
								case ".patch":
									using (Stream entryStream = entry.Open())
									{
										string patchFileContent = Encoding.UTF8.GetString(entryStream.ReadBytes((int)entry.Length));
										ModPatchFile patchFile = ModPatchFile.Parse(patchFileContent);
										mod.mPatchFiles.Add(patchFile);
									}
									break;
								case ".pak":
									using (Stream entryStream = entry.Open())
									{
										mod.mPakFiles.Add(new ModPakFile(entry.Name, entryStream.ReadBytes((int)entry.Length)));
									}
									break;
							}
						}
						catch (Exception ex)
						{
							if (notifyOnError) CustomMessageBox.Show($"An error occurred while attempting to read {entry} from {fileName}. This mod will likely not work properly.\n\n[{ex.GetType().FullName}] {ex.Message}", "Mod patch load failed", MessageBoxButton.OK, MessageBoxImage.Error);
						}
					}

					return mod;
				}
			}

			throw new ArgumentException($"{fileName} does not have a supported file extension. Supported extensions for mods are .zip and .pak", nameof(fileName));
		}

		public void CollectPatches(Dictionary<string, List<JsonPatchData>> jsonPatches, Dictionary<string, List<ActorPatchData>> actorPatches, Dictionary<string, List<DataTablePatchData>> dataTablePatches, GameFileManager fileManager)
		{
			foreach (ModPatchFile patchFile in mPatchFiles)
			{
				if (!fileManager.HasFile(patchFile.TargetPath))
				{
					MessageBoxResult result = CustomMessageBox.Show($"Mod \"{Name}\" is trying to patch the file {patchFile.TargetPath}, but that file could not be located. Attempt to patch remaining files from this mod?", "Mod patch load failed", MessageBoxButton.YesNo, MessageBoxImage.Warning);
					if (result == MessageBoxResult.Yes) continue;
					break;
				}

				switch (patchFile.Type)
				{
					case ModPatchType.Json:
						{
							List<JsonPatchData>? jsonPatchList;
							if (!jsonPatches.TryGetValue(patchFile.TargetPath, out jsonPatchList))
							{
								jsonPatchList = new List<JsonPatchData>();
								jsonPatches.Add(patchFile.TargetPath, jsonPatchList);
							}
							jsonPatchList.Add((JsonPatchData)patchFile.PatchData);
						}
						break;
					case ModPatchType.Actor:
						{
							List<ActorPatchData>? actorPatchList;
							if (!actorPatches.TryGetValue(patchFile.TargetPath, out actorPatchList))
							{
								actorPatchList = new List<ActorPatchData>();
								actorPatches.Add(patchFile.TargetPath, actorPatchList);
							}
							actorPatchList.Add((ActorPatchData)patchFile.PatchData);
						}
						break;
					case ModPatchType.DataTable:
						{
							List<DataTablePatchData>? dataTablePatchList;
							if (!dataTablePatches.TryGetValue(patchFile.TargetPath, out dataTablePatchList))
							{
								dataTablePatchList = new();
								dataTablePatches.Add(patchFile.TargetPath, dataTablePatchList);
							}
							dataTablePatchList.Add((DataTablePatchData)patchFile.PatchData);
						}
						break;
				}
			}
		}

		public IEnumerable<string> GetFileOverrides()
		{
			foreach (ModPakFile pakFile in mPakFiles)
			{
				using MemoryStream stream = new MemoryStream(pakFile.Data);
				using PakFile file = PakFile.Mount(stream);
				foreach (FString path in file.Entries)
				{
					yield return path;
				}
			}
		}

		public bool GetFileOverrideData(string filePath, out ReadOnlySpan<byte> data)
		{
			FString path = (FString)filePath;
			foreach (ModPakFile pakFile in mPakFiles)
			{
				using MemoryStream stream = new MemoryStream(pakFile.Data);
				using PakFile file = PakFile.Mount(stream);
				if (file.HasEntry(path))
				{
					return file.ReadEntryData(path, out data);
				}
			}
			data = default;
			return false;
		}

		public bool CopyPaks(string modDirectory, int index)
		{
			foreach (ModPakFile pakFile in mPakFiles)
			{
				string filePath = Path.Combine(modDirectory, Path.GetFileNameWithoutExtension($"{(index + 1):000}-{pakFile.Name})"));
				if (!filePath.EndsWith("_P")) filePath += "_P";
				filePath += ".pak";

				using FileStream outFile = File.Create(filePath);
				outFile.Write(pakFile.Data);
			}
			return mPakFiles.Count > 0;
		}

		public override string ToString()
		{
			if (Name == null) return "Unnamed Mod";
			if (Version == null) return Name;
			return $"{Name} {Version}";
		}

		private class ModPakFile
		{
			public string Name { get; set; }

			public byte[] Data { get; set; }

			public ModPakFile(string name, byte[] data)
			{
				Name = name;
				Data = data;
			}
		}
	}
}
