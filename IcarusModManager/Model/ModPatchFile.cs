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

using IcarusModManager.Integrator;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace IcarusModManager.Model
{
	/// <summary>
	/// A file containing data about how to modify an existing game asset. A mod may contain
	/// any number of patch files.
	/// </summary>
	internal class ModPatchFile
	{
		/// <summary>
		/// The path of the asset to patch
		/// </summary>
		public string TargetPath { get; set; }

		/// <summary>
		/// The type of this patch
		/// </summary>
		public ModPatchType Type { get; set; }

		/// <summary>
		/// The data for the patch. Type depends on patch type.
		/// </summary>
		public object PatchData { get; set; }

		/// <summary>
		/// Creates a new patch file
		/// </summary>
		/// <param name="targetPath">The path of the asset to patch</param>
		/// <param name="type">The type of the patch</param>
		/// <param name="patchData">The data for the match. Type must be consistent with the patch type.</param>
		public ModPatchFile(string targetPath, ModPatchType type, object patchData)
		{
			TargetPath = targetPath;
			Type = type;
			PatchData = patchData;
		}

		/// <summary>
		/// Desreializes a patch file from Json
		/// </summary>
		/// <param name="data">The serialized patch file Json</param>
		public static ModPatchFile Parse(string data)
		{
			JObject? dataObject = JsonConvert.DeserializeObject(data) as JObject;
			if (dataObject == null) throw new FormatException($"Expected root of file to be a json object");

			// Verify schema version
			{
				JProperty? schemaVersionProperty = dataObject.Property("schema_version");
				if (schemaVersionProperty == null) throw new FormatException("Missing required property 'schema_version'");
				int schemaVersion = schemaVersionProperty.Value.Value<int>();
				if (schemaVersion != 1) throw new FormatException($"Unknown schema version: {schemaVersion}. Supported schema version: 1");
			}

			string target = dataObject["target"]?.Value<string>() ?? throw new FormatException("Missing required property 'target'");
			string typeString = dataObject["type"]?.Value<string>() ?? throw new FormatException("Missing required property 'type'");
			if (!Enum.TryParse(typeString, out ModPatchType modType)) throw new FormatException($"'{typeString}' is not a valid value for property 'type'");

			object patchData;
			JObject patchDataObject = dataObject["data"]?.Value<JObject>() ?? throw new FormatException($"'data' property either missing or not valid for patch type '{typeString}'");
			switch (modType)
			{
				case ModPatchType.Json:
					patchData = JsonPatchData.Read(patchDataObject);
					break;
				case ModPatchType.Actor:
					patchData = ActorPatchData.Read(patchDataObject);
					break;
				case ModPatchType.DataTable:
					patchData = DataTablePatchData.Read(patchDataObject);
					break;
				case ModPatchType.AssetCopy:
					patchData = AssetCopyPatchData.Read(patchDataObject);
					break;
				case ModPatchType.Invalid:
				default:
					throw new FormatException($"'{typeString}' is not a valid value for property 'type'");
			}

			return new ModPatchFile(target, modType, patchData);
		}
	}

	/// <summary>
	/// Data associated with the Json ModPatchType
	/// </summary>
	internal class JsonPatchData
	{
		public List<List<Operation>> Patches { get; }

		public JsonPatchData()
		{
			Patches = new List<List<Operation>>();
		}

		/// <summary>
		/// Read patch data from Json
		/// </summary>
		/// <param name="patchObj">A Json object containing patch data</param>
		public static JsonPatchData Read(JObject patchObj)
		{
			JArray patchList = patchObj["patches"]?.Value<JArray>() ?? throw new FormatException("'data' property not valid for patch type 'Json'");

			JsonPatchData data = new JsonPatchData();
			foreach (JToken? patchToken in patchList)
			{
				JArray patch = patchToken as JArray ?? throw new FormatException("'data' property not valid for patch type 'Json'");
				List<Operation> operations = new List<Operation>();
				foreach(var operation in patch)
				{
					operations.Add(operation.ToObject<Operation>() ?? throw new FormatException("'data' property not valid for patch type 'Json'"));
				}
				data.Patches.Add(operations);
			}
			return data;
		}
	}

	/// <summary>
	/// Data associated with the Actor ModPatchType
	/// </summary>
	internal class ActorPatchData
	{
		public List<string> Components { get; }

		public ActorPatchData()
		{
			Components = new List<string>();
		}

		/// <summary>
		/// Read patch data from Json
		/// </summary>
		/// <param name="patchObj">A Json object containing patch data</param>
		public static ActorPatchData Read(JObject patchObj)
		{
			JArray patchList = patchObj["components"]?.Value<JArray>() ?? throw new FormatException("'data' property not valid for patch type 'Actor'");

			ActorPatchData data = new ActorPatchData();
			foreach (JToken? component in patchList)
			{
				data.Components.Add(component?.ToObject<string>() ?? throw new FormatException("'data' property not valid for patch type 'Actor'"));
			}
			return data;
		}
	}

	/// <summary>
	/// Data associated with the DataTable ModPatchType
	/// </summary>
	internal class DataTablePatchData
	{
		public List<DataTableOperation> Patches { get; }

		public DataTablePatchData()
		{
			Patches = new();
		}

		/// <summary>
		/// Read patch data from Json
		/// </summary>
		/// <param name="patchObj">A Json object containing patch data</param>
		public static DataTablePatchData Read(JObject patchObj)
		{
			JArray patchList = patchObj["patches"]?.Value<JArray>() ?? throw new FormatException("'data' property not valid for patch type 'DataTable'");

			DataTablePatchData data = new();
			foreach (JToken? patchToken in patchList)
			{
				JObject patch = patchToken as JObject ?? throw new FormatException("'data' property not valid for patch type 'DataTable'");
				data.Patches.Add(patch.ToObject<DataTableOperation>() ?? throw new FormatException("'data' property not valid for patch type 'DataTable'"));
			}
			return data;
		}
	}

	/// <summary>
	/// Data associated with the AssetCopy ModPatchType
	/// </summary>
	internal class AssetCopyPatchData
	{
		public string NewPath { get; }

		public AssetCopyPatchData(string newPath)
		{
			NewPath = newPath;
		}

		/// <summary>
		/// Read patch data from Json
		/// </summary>
		/// <param name="patchObj">A Json object containing patch data</param>
		public static AssetCopyPatchData Read(JObject patchObj)
		{
			string newPath = patchObj["path"]?.Value<string>() ?? throw new FormatException("'data' property not valid for patch type 'AssetCopy'");
			return new AssetCopyPatchData(newPath);
		}
	}

	/// <summary>
	/// The type of a ModPatchFile
	/// </summary>
	internal enum ModPatchType
	{
		Invalid,
		Json,
		Actor,
		DataTable,
		AssetCopy
	}
}
