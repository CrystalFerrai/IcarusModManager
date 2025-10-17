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
using System.IO;
using UAssetAPI;
using UAssetAPI.UnrealTypes;

namespace IcarusModManager.Integrator
{
	/// <summary>
	/// Utility for use by asset integrators
	/// </summary>
	internal static class IntegratorUtil
	{
		/// <summary>
		/// Gets the game's Engreal Engine version
		/// </summary>
		/// <remarks>
		/// This should be changed whenever the game upgrades to a new engine version.
		/// </remarks>
		public static EngineVersion EngineVersion => EngineVersion.VER_UE4_27;

		/// <summary>
		/// Deserialize a UAsset from source an export data
		/// </summary>
		public static UAsset ReadAsset(ReadOnlySpan<byte> sourceData, ReadOnlySpan<byte> exportsData)
		{
			byte[] source = new byte[sourceData.Length + exportsData.Length];
			sourceData.CopyTo(source);
			exportsData.CopyTo(source.AsSpan(sourceData.Length));

			using MemoryStream stream = new(source);

			UAsset asset = new(EngineVersion);
			asset.UseSeparateBulkDataFiles = true;

			using (AssetBinaryReader reader = new(stream, asset))
			{
				asset.Read(reader);
			}

			return asset;
		}

		/// <summary>
		/// Serialize a UAsset and add it to a pak file
		/// </summary>
		public static void WriteAsset(UAsset asset, string assetPath, string exportsPath, NetPak.PakFile pakFile)
		{
			using MemoryStream stream = asset.WriteData();
			int exportStart = (int)asset.Exports[0].SerialOffset;

			// Write uasset
			byte[] assetFileData = new byte[exportStart];
			stream.ReadAll(assetFileData, 0, exportStart);
			pakFile.AddEntry(new(assetPath), assetFileData);

			// Write uexp
			byte[] exportsFileData = new byte[stream.Length - exportStart];
			stream.ReadAll(exportsFileData, 0, (int)stream.Length - exportStart);
			pakFile.AddEntry(new(exportsPath), exportsFileData);
		}
	}
}
