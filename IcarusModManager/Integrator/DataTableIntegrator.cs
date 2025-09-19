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

using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Exceptions;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;

namespace IcarusModManager.Integrator
{
	/// <summary>
	/// Applies serialized patches to a Json serialized data table
	/// </summary>
	internal class DataTableIntegrator
	{
		/// <summary>
		/// Performs an integration
		/// </summary>
		/// <param name="source">The serialized source Json data table to patch</param>
		/// <param name="patches">The patches to apply</param>
		/// <returns>The Json with the patches applied to it</returns>
		public static string Integrate(string source, IEnumerable<DataTableOperation> patches)
		{
			JObject? sourceObj = JsonConvert.DeserializeObject(source) as JObject;
			if (sourceObj is null) throw new ArgumentException("Unable to parse source.", nameof(source));

			Dictionary<string, JObject> rowMap = new();
			JArray? rows = sourceObj["Rows"] as JArray;
			if (rows is not null)
			{
				try
				{
					foreach (JObject row in rows)
					{
						JValue? rowName = row["Name"] as JValue;
						if (rowName is null || rowName.Type != JTokenType.String) continue;

						rowMap.Add((string)rowName!, row);
					}
				}
				catch
				{
					// Failed to parse rows
					rowMap.Clear();
				}
			}

			DefaultContractResolver contractResolver = new();

			foreach (DataTableOperation patch in patches)
			{
				if (patch.Type == DataTableOperationType.Add)
				{
					// Cannot add null row or null value
					if (patch.Row is null || patch.Value is null) continue;

					// Cannot add duplicate row
					if (rowMap.ContainsKey(patch.Row)) continue;

					if (rows is null)
					{
						rows = new();
						sourceObj.Add("Rows", rows);
					}

					JObject value = new(patch.Value);
					value.AddFirst(new JProperty("Name", patch.Row));

					rows.Add(value);
					rowMap.Add(patch.Row, value);

					continue;
				}

				JObject? target;
				if (patch.Row is null)
				{
					target = sourceObj["Defaults"] as JObject;
				}
				else if (!rowMap.TryGetValue(patch.Row, out target))
				{
					continue;
				}

				if (patch.Type == DataTableOperationType.Remove)
				{
					// Cannot remove null row
					if (patch.Row is null || rows is null) continue;

					JObject? toRemove;
					if (rowMap.TryGetValue(patch.Row, out toRemove))
					{
						rows.Remove(toRemove);
						rowMap.Remove(patch.Row);
					}

					continue;
				}

				if (patch.Type == DataTableOperationType.Alter)
				{
					foreach (List<Operation> ops in patch.Patches)
					{
						JsonPatchDocument document = new(ops, contractResolver);
						try
						{
							document.ApplyTo(target);
						}
						catch (JsonPatchException)
						{
							// It is valid for patches to fail. We should still continue with further patches.
						}
					}
				}
			}

			return JsonConvert.SerializeObject(sourceObj, Formatting.Indented);
		}
	}

	internal class DataTableOperation
	{
		[JsonProperty("op", Order = 0, Required = Required.Always)]
		public DataTableOperationType Type { get; set; }

		[JsonProperty("row", Order = 1, NullValueHandling = NullValueHandling.Include)]
		public string? Row { get; set; }

		[JsonProperty("value", Order = 2, NullValueHandling = NullValueHandling.Ignore)]
		public JObject? Value { get; set; }

		[JsonProperty("patches", Order = 3, NullValueHandling = NullValueHandling.Ignore)]
		public List<List<Operation>> Patches { get; set; }

		public DataTableOperation()
		{
			Patches = new();
		}

		public override string ToString()
		{
			return $"op: {Type}, row: {Row ?? "null"}";
		}
	}

	internal enum DataTableOperationType
	{
		None,
		Add,
		Remove,
		Alter
	}
}
