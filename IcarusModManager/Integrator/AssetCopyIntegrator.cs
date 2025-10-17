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

using System;
using System.IO;
using System.Linq;
using UAssetAPI;
using UAssetAPI.UnrealTypes;

namespace IcarusModManager.Integrator
{
	/// <summary>
	/// Updates self references within a UAsset to use a new path
	/// </summary>
	internal class AssetCopyIntegrator
	{
		/// <summary>
		/// Performs an integration
		/// </summary>
		/// <param name="asset">The asset to copy</param>
		/// <param name="oldPath">The original asset path</param>
		/// <param name="newPath">The asset path to copy to</param>
		/// <returns>The actor asset with the components added</returns>
		public static void Integrate(UAsset asset, string oldPath, string newPath)
		{
			const string Game = "/Game";
			const string IcarusContent = "Icarus/Content";

			// Asset reference paths - replace leading 'Icarus/Content' with '/Game' and remove file extensions
			string originalGamePath = oldPath[..oldPath.LastIndexOf('.')];
			if (originalGamePath.StartsWith(IcarusContent, StringComparison.InvariantCultureIgnoreCase))
			{
				originalGamePath = $"{Game}{originalGamePath[IcarusContent.Length..]}";
			}
			string newGamePath = newPath[..newPath.LastIndexOf('.')];
			if (newGamePath.StartsWith(IcarusContent, StringComparison.InvariantCultureIgnoreCase))
			{
				newGamePath = $"{Game}{newGamePath[IcarusContent.Length..]}";
			}

			// Asset names
			string originalAssetName = Path.GetFileName(originalGamePath);
			string newAssetName = Path.GetFileName(newGamePath);

			FString[] nameMap = asset.GetNameMapIndexList().ToArray();

			for (int i = 0; i < nameMap.Length; ++i)
			{
				if (nameMap[i].Value.Contains(originalGamePath, StringComparison.InvariantCultureIgnoreCase))
				{
					asset.SetNameReference(i, new(nameMap[i].Value.Replace(originalGamePath, newGamePath)));
					System.Diagnostics.Debug.WriteLine(i);
				}
				else if (nameMap[i].Value.Contains(originalAssetName, StringComparison.InvariantCultureIgnoreCase))
				{
					asset.SetNameReference(i, new(nameMap[i].Value.Replace(originalAssetName, newAssetName)));
					System.Diagnostics.Debug.WriteLine(i);
				}
			}
		}
	}
}
