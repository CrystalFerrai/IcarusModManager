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
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace IcarusModManager.Converters
{
	/// <summary>
	/// Converts a bool to a bool with the opposite value
	/// </summary>
	internal class InverseBoolConverter : IValueConverter
	{
		/// <summary>
		/// Gets a static instance of the converter
		/// </summary>
		public static InverseBoolConverter Instance { get; }

		/// <summary>
		/// Initializes static members of the InverseBoolConverter class
		/// </summary>
		static InverseBoolConverter()
		{
			Instance = new InverseBoolConverter();
		}

		/// <summary>
		/// Returns a bool with the opposite value of the one passed in
		/// </summary>
		/// <param name="value">The value to convert</param>
		/// <param name="targetType">Unused</param>
		/// <param name="parameter">Unused</param>
		/// <param name="culture">Unused</param>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (value is bool) ? !(bool)value : DependencyProperty.UnsetValue;
		}

		/// <summary>
		/// Returns a bool with the opposite value of the one passed in
		/// </summary>
		/// <param name="value">The value to convert</param>
		/// <param name="targetType">Unused</param>
		/// <param name="parameter">Unused</param>
		/// <param name="culture">Unused</param>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (value is bool) ? !(bool)value : DependencyProperty.UnsetValue;
		}
	}
}
