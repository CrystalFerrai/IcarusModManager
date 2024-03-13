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
	}
}
