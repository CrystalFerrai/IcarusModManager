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

using IcarusModManager.Core;

namespace IcarusModManager.CLI
{
	/// <summary>
	/// Handles mod automation actions
	/// </summary>
	internal class ModAgent
	{
		private readonly Options mOptions;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="options">Options that will be used when running the agent</param>
		public ModAgent(Options options)
		{
			mOptions = options;
		}

		/// <summary>
		/// Run the agent
		/// </summary>
		/// <param name="logger">For logging issues</param>
		/// <returns>Whether the run was successful</returns>
		/// <exception cref="InvalidOperationException">Something is misconfigured in the options that were given to the agent</exception>
		public bool Run(Logger logger)
		{
			Settings settings = new();
			settings.Load(logger);

			if (string.IsNullOrEmpty(settings.GameDirectory))
			{
				logger.Error("No game directory configured. You can configure the game directory within the IcarusModManager settings dialog.");
				return false;
			}
			if (!Directory.Exists(settings.GameDirectory))
			{
				logger.Error($"The configured game directory does not exist or is not accessible \"{settings.GameDirectory}\". You can configure the game directory within the IcarusModManager settings dialog.");
				return false;
			}

			ModManager manager = new(logger);
			manager.Load();

			switch (mOptions.ProgramAction)
			{
				case ProgramAction.Install:
					logger.Information("Installing mods...");
					try
					{
						manager.InstallMods(settings.GameDirectory);
					}
					catch (Exception ex)
					{
						logger.Error($"An error occurred while attempting to install mods. [{ex.GetType().FullName}] {ex.Message}");
						return false;
					}
					logger.Information("Mods installed");
					break;
				case ProgramAction.Uninstall:
					logger.Information("Uninstalling mods...");
					try
					{
						manager.UninstallMods(settings.GameDirectory);
					}
					catch (Exception ex)
					{
						logger.Error($"An error occurred while attempting to uninstall mods. [{ex.GetType().FullName}] {ex.Message}");
						return false;
					}
					logger.Information("Mods uninstalled");
					break;
				default:
					throw new InvalidOperationException($"Invalid program action: {mOptions.ProgramAction}");
			}

			return true;
		}
	}
}
