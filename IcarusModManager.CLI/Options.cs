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
using System.Diagnostics.CodeAnalysis;

namespace IcarusModManager.CLI
{
	/// <summary>
	/// Program options read from the command line
	/// </summary>
	internal class Options
	{
		/// <summary>
		/// The action to perform
		/// </summary>
		public ProgramAction ProgramAction { get; }

		public Options(ProgramAction programAction)
		{
			ProgramAction = programAction;
		}

		/// <summary>
		/// Try to parse command line args to create a new instance
		/// </summary>
		/// <param name="args">The command line args</param>
		/// <param name="logger">For logging issues</param>
		/// <param name="options">If successful, the resulting options</param>
		/// <returns>Whether the args were successfully parsed</returns>
		public static bool TryParse(string[] args, Logger logger, [NotNullWhen(true)] out Options? options)
		{
			options = null;

			ProgramAction action = ProgramAction.None;
			for (int i = 0; i < args.Length; ++i)
			{
				if (args[i].StartsWith("--"))
				{
					string arg = args[i][2..].ToLowerInvariant();
					switch (arg)
					{
						case "install":
							action = ProgramAction.Install;
							break;
						case "uninstall":
							action = ProgramAction.Uninstall;
							break;
						default:
							logger.Error($"Unrecognized parameter: {args[i]}");
							return false;
					}
				}
				else
				{
					logger.Error($"Unrecognized parameter: {args[i]}");
					return false;
				}
			}

			if (action == ProgramAction.None)
			{
				logger.Error("Must specify at least one action to perform");
				return false;
			}

			options = new(action);
			return true;
		}

		/// <summary>
		/// Prints program usage
		/// </summary>
		/// <param name="logger">Usage will be printed to this logger</param>
		public static void PrintUsage(Logger logger)
		{
			logger.Information(
				"This is the command line version of IcarusModManager. It has limited\n" +
				"functionality intended for use from scripts to automate some common tasks.\n" +
				"You must use the GUI version of IcarusModManager to configure your mod\n" +
				"list and game location.\n" +
				"\n" +
				"Usage: IcarusModManagerCLI [action]\n" +
				"\n" +
				"Actions\n" +
				"\n" +
				"  --install    Install configured mods.\n" +
				"\n" +
				"  --uninstall  Uninstall all mods.\n" +
				"\n" +
				"If an error occurs, the program will return a non-zero exit code that can\n" +
				"be checked from a script and acted upon as needed."
			);
		}
	}

	/// <summary>
	/// Identifies which action the program should perform
	/// </summary>
	internal enum ProgramAction
	{
		None,
		Install,
		Uninstall
	}
}
