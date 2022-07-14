﻿// Copyright 2022 Crystal Ferrai
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

using System.Collections.Generic;

namespace IcarusModManager.Collections
{
	/// <summary>
	/// Represents a collection that is both enumerable and observable
	/// </summary>
	/// <typeparam name="T">The type of the items in the collection</typeparam>
	public interface IObservableCollection<T> : IObservableEnumerable<T>, IReadOnlyCollection<T>
	{
	}
}
