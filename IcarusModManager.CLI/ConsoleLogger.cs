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
	/// Logger designed to log to the console
	/// </summary>
	internal class ConsoleLogger : Logger
	{
		private readonly ConsoleColor mOriginalColor;

		public ConsoleLogger() : base(Console.Out)
		{
			mOriginalColor = Console.ForegroundColor;

			SetOutput(LogLevel.Error, Console.Error);
			SetOutput(LogLevel.Fatal, Console.Error);
		}

		protected override void OnPreLog(LogLevel level, string caption, string message)
		{
			switch (level)
			{
				case LogLevel.Verbose:
				case LogLevel.Debug:
					Console.ForegroundColor = ConsoleColor.DarkGray;
					break;
				case LogLevel.Information:
					Console.ForegroundColor = ConsoleColor.Gray;
					break;
				case LogLevel.Important:
					Console.ForegroundColor = ConsoleColor.White;
					break;
				case LogLevel.Warning:
					Console.ForegroundColor = ConsoleColor.Yellow;
					break;
				case LogLevel.Error:
				case LogLevel.Fatal:
					Console.ForegroundColor = ConsoleColor.Red;
					break;
			}
		}

		protected override void OnPostLog(LogLevel level, string caption, string message)
		{
			Console.ForegroundColor = mOriginalColor;
		}
	}
}
