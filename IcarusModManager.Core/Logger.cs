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

namespace IcarusModManager.Core
{
	/// <summary>
	/// Utility for logging output messages
	/// </summary>
	public class Logger
	{
		private readonly TextWriter[] mWriters;

		private bool mIsValid;

		/// <summary>
		/// The minimum level of messages to print. Logged messages below this threshold will be discarded
		/// </summary>
		public LogLevel LogLevel { get; set; }

		public Logger()
		{
			mWriters = new TextWriter[Enum.GetNames<LogLevel>().Length];
			mIsValid = false;

#if DEBUG
			LogLevel = LogLevel.Debug;
#else
			LogLevel = LogLevel.Information;
#endif
		}

		public Logger(TextWriter defaultOutput) : this()
		{
			SetAllOutput(defaultOutput);
		}

		/// <summary>
		/// Sets the output device the logger will use for all log levels.
		/// </summary>
		public void SetAllOutput(TextWriter output)
		{
			for (int i = 0; i < mWriters.Length; ++i)
			{
				mWriters[i] = output;
			}
			mIsValid = true;
		}

		/// <summary>
		/// Sets the output device the logger will use for a specific log level.
		/// </summary>
		public void SetOutput(LogLevel level, TextWriter output)
		{
			mWriters[(int)level] = output;
			mIsValid = mWriters.All(w => w is not null);
		}


		/// <summary>
		/// Logs a message at a specific level
		/// </summary>
		public void Log(LogLevel level, string caption, string message)
		{
			if (level < LogLevel) return;

			string formattedCaption = string.Empty;
			if (!string.IsNullOrWhiteSpace(caption))
			{
				formattedCaption = $"[{caption}] ";
			}

			OnPreLog(level, caption, message);

			if (level == LogLevel.Warning)
			{
				mWriters[(int)level].WriteLine($"{formattedCaption}[WARNING] {message}");
			}
			else if (level >= LogLevel.Error)
			{
				mWriters[(int)level].WriteLine($"{formattedCaption}[ERROR] {message}");
			}
			else
			{
				mWriters[(int)level].WriteLine($"{formattedCaption}{message}");
			}

			OnPostLog(level, caption, message);
		}

		/// <summary>
		/// Logs a completely empty line at a specific level
		/// </summary>
		public void LogEmptyLine(LogLevel level)
		{
			if (level < LogLevel) return;

			OnPreLog(level, string.Empty, string.Empty);

			mWriters[(int)level].WriteLine(string.Empty);

			OnPostLog(level, string.Empty, string.Empty);
		}

		/// <summary>
		/// Helper for logging a debug message
		/// </summary>
		public void Debug(string caption, string message)
		{
			Log(LogLevel.Debug, caption, message);
		}

		/// <summary>
		/// Helper for logging a debug message
		/// </summary>
		public void Debug(string message)
		{
			Log(LogLevel.Debug, string.Empty, message);
		}

		/// <summary>
		/// Helper for logging an information message
		/// </summary>
		public void Information(string caption, string message)
		{
			Log(LogLevel.Information, caption, message);
		}

		/// <summary>
		/// Helper for logging an information message
		/// </summary>
		public void Information(string message)
		{
			Log(LogLevel.Information, string.Empty, message);
		}

		/// <summary>
		/// Helper for logging an important message
		/// </summary>
		public void Important(string caption, string message)
		{
			Log(LogLevel.Important, caption, message);
		}

		/// <summary>
		/// Helper for logging an important message
		/// </summary>
		public void Important(string message)
		{
			Log(LogLevel.Important, string.Empty, message);
		}

		/// <summary>
		/// Helper for logging a warning
		/// </summary>
		public void Warning(string caption, string message)
		{
			Log(LogLevel.Warning, caption, message);
		}

		/// <summary>
		/// Helper for logging a warning
		/// </summary>
		public void Warning(string message)
		{
			Log(LogLevel.Warning, string.Empty, message);
		}

		/// <summary>
		/// Helper for logging an error
		/// </summary>
		public void Error(string caption, string message)
		{
			Log(LogLevel.Error, caption, message);
		}

		/// <summary>
		/// Helper for logging an error
		/// </summary>
		public void Error(string message)
		{
			Log(LogLevel.Error, string.Empty, message);
		}

		protected virtual void OnPreLog(LogLevel level, string caption, string message)
		{
		}

		protected virtual void OnPostLog(LogLevel level, string caption, string message)
		{
		}
	}

	/// <summary>
	/// Represents the importance of messages being logged
	/// </summary>
	public enum LogLevel : int
	{
		/// <summary>
		/// For spam messages that will not be printed by default
		/// </summary>
		Verbose,
		/// <summary>
		/// For debugging messages that will only print in a debug build by default
		/// </summary>
		Debug,
		/// <summary>
		/// For informational messages. Will print by default
		/// </summary>
		Information,
		/// <summary>
		/// For important informational messages. Will print by default
		/// </summary>
		Important,
		/// <summary>
		/// For warnings. Will print by default
		/// </summary>
		Warning,
		/// <summary>
		/// For errors. Will print by default
		/// </summary>
		Error,
		/// <summary>
		/// For fatal errors. Will always print
		/// </summary>
		Fatal
	}
}
