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

using IcarusModManager.Controls;
using IcarusModManager.Core;
using System;
using System.IO;
using System.Text;
using System.Windows;

namespace IcarusModManager
{
	/// <summary>
	/// Logger designed to log to the console
	/// </summary>
	internal class MessageBoxLogger : Logger, IDisposable
	{
		private NullTextWriter mWriter;

		public MessageBoxLogger()
		{
			mWriter = new();
			SetAllOutput(mWriter);
		}

		~MessageBoxLogger()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected override void OnPostLog(LogLevel level, string caption, string message)
		{
			MessageBoxImage icon = level switch
			{
				LogLevel.Verbose => MessageBoxImage.None,
				LogLevel.Debug => MessageBoxImage.None,
				LogLevel.Information => MessageBoxImage.Information,
				LogLevel.Important => MessageBoxImage.Information,
				LogLevel.Warning => MessageBoxImage.Warning,
				LogLevel.Error => MessageBoxImage.Error,
				LogLevel.Fatal => MessageBoxImage.Error,
				_ => MessageBoxImage.None
			};

			CustomMessageBox.Show(Application.Current.MainWindow, message, caption, MessageBoxButton.OK, icon);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				mWriter.Dispose();
			}
		}

		private class NullTextWriter : TextWriter
		{
			public override Encoding Encoding => Encoding.UTF8;
		}
	}
}
