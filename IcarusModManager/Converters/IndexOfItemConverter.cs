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

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace IcarusModManager.Converters
{
	/// <summary>
	/// Binding converter which gets the index of an item with an ItemsControl
	/// </summary>
	internal class IndexOfItemConverter : IMultiValueConverter
	{
		public static IndexOfItemConverter Instance { get; }

		static IndexOfItemConverter()
		{
			Instance = new IndexOfItemConverter();
		}

		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if (values.Length < 2) return DependencyProperty.UnsetValue;

			ItemsControl? itemsControl = values[0] as ItemsControl;
			if (itemsControl == null) return DependencyProperty.UnsetValue;

			object item = values[1];

			DependencyObject container = itemsControl.ItemContainerGenerator.ContainerFromItem(item);
			if (container == null) return Binding.DoNothing; // Container not generated

			return itemsControl.ItemContainerGenerator.IndexFromContainer(container);
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
