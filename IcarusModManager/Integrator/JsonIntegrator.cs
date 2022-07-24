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

using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Exceptions;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;

namespace IcarusModManager.Integrator
{
	/// <summary>
	/// Applies serialized patches to a serialized json object or array
	/// </summary>
	/// <remarks>
	/// Uses the JsonPatch spec for applying patches.
	/// Summary: https://jsonpatch.com/
	/// Spec: https://datatracker.ietf.org/doc/html/rfc6902
	/// Library: https://docs.microsoft.com/en-us/aspnet/core/web-api/jsonpatch
	/// </remarks>
	internal static class JsonIntegrator
	{
		/// <summary>
		/// Performs an integration
		/// </summary>
		/// <param name="source">The serialized source Json data to patch</param>
		/// <param name="patches">The patches to apply</param>
		/// <returns>The Json with the patches applied to it</returns>
		public static string Integrate(string source, IEnumerable<List<Operation>> patches)
		{
			object? sourceObj = JsonConvert.DeserializeObject(source);
			if (sourceObj == null) throw new ArgumentException("Unable to parse source.", nameof(source));

			DefaultContractResolver contractResolver = new();

			foreach (List<Operation> patchList in patches)
			{
				JsonPatchDocument document = new JsonPatchDocument(patchList, contractResolver);
				try
				{
					document.ApplyTo(sourceObj);
				}
				catch (JsonPatchException)
				{
					// It is valid for patches to fail. We should still continue with further patches.
				}
			}

			return JsonConvert.SerializeObject(sourceObj, Formatting.Indented);
		}
	}
}
