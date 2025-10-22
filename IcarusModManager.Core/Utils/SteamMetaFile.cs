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
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace IcarusModManager.Core.Utils
{
	/// <summary>
	/// Steam installer metadata file
	/// </summary>
	public class SteamMetaFile
	{
		/// <summary>
		/// Gets the root object of the file
		/// </summary>
		public SteamMetaObject? RootObject { get; private set; }

		/// <summary>
		/// Loads a Steam metadata file from disk
		/// </summary>
		/// <param name="path">The path to the file to load</param>
		/// <returns>The loaded file</returns>
		public static SteamMetaFile Load(string path)
		{
			using (FileStream file = File.OpenRead(path))
			{
				return LoadFrom(file);
			}
		}

		/// <summary>
		/// Loads a Steam metadata file from a stream
		/// </summary>
		/// <param name="stream">A stream containing the data to load</param>
		/// <returns>The loaded data</returns>
		public static SteamMetaFile LoadFrom(Stream stream)
		{
			SteamMetaFile file = new SteamMetaFile();

			using (StreamReader reader = new StreamReader(stream))
			{
				ParserState state = ParserState.NextToken;

				SteamMetaObject? currentObject = null;
				Stack<SteamMetaObject> parentObjects = new();

				while (!reader.EndOfStream)
				{
					string line = reader.ReadLine()!.Trim();

					switch (state)
					{
						case ParserState.NextToken:
							{
								if (line.Equals("}"))
								{
									currentObject = parentObjects.Count > 0 ? parentObjects.Pop() : null;
									continue;
								}

								string[] split = line.Split("\t\t");
								if (split.Length == 2)
								{
									SteamMetaValue value = new SteamMetaValue(split[0].Trim('"'), split[1].Trim('"'));
									currentObject!.Tokens.Add(value.Name, value);
								}
								else
								{
									if (currentObject != null)
									{
										parentObjects.Push(currentObject);
									}

									SteamMetaObject? parent = currentObject;
									currentObject = new SteamMetaObject(split[0].Trim('"'));

									if (parent != null)
									{
										parent.Tokens.Add(currentObject.Name, currentObject);
									}

									if (file.RootObject == null)
									{
										file.RootObject = currentObject;
									}

									state = ParserState.StartObject;
								}
							}
							break;
						case ParserState.StartObject:
							if (!line.Equals("{")) throw new FormatException($"Error reading Steam meta file. Expected '{{' after \"{currentObject!.Name}\"");
							state = ParserState.NextToken;
							break;
					}
				}
			}

			return file;
		}

		public override string ToString()
		{
			return RootObject?.Name ?? "empty";
		}

		private enum ParserState
		{
			NextToken,
			StartObject
		}
	}

	/// <summary>
	/// A named entry in a Steam installer metadata file
	/// </summary>
	public class SteamMetaToken
	{
		/// <summary>
		/// The name of the token
		/// </summary>
		public string Name { get; }

		protected SteamMetaToken(string name)
		{
			Name = name;
		}

		public override string ToString()
		{
			return Name;
		}
	}

	/// <summary>
	/// An object in a Steam installer metadata file
	/// </summary>
	public class SteamMetaObject : SteamMetaToken, IEnumerable<SteamMetaToken>, IReadOnlyCollection<SteamMetaToken>
	{
		/// <summary>
		/// Gets the tokens within the object
		/// </summary>
		public IDictionary<string, SteamMetaToken> Tokens { get; }

		/// <summary>
		/// Gets the number of tokens within the object
		/// </summary>
		public int Count => Tokens.Count; // Satisfies IReadOnlyCollection interface

		/// <summary>
		/// Returns a token from this object by name
		/// </summary>
		/// <param name="key">the object name</param>
		public SteamMetaToken this[string key] => Tokens[key];

		public SteamMetaObject(string name)
			: base(name)
		{
			Tokens = new Dictionary<string, SteamMetaToken>();
		}

		public override string ToString()
		{
			return $"{base.ToString()} - {Tokens.Count} tokens";
		}

		public IEnumerator<SteamMetaToken> GetEnumerator()
		{
			return Tokens.Values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	/// <summary>
	/// A named entry in a Steam installer metadata file that is associated with a simple value
	/// </summary>
	internal class SteamMetaValue : SteamMetaToken
	{
		/// <summary>
		/// The value of the token
		/// </summary>
		public string Value { get; }

		public SteamMetaValue(string name, string value)
			: base(name)
		{
			Value = value;
		}

		public override string ToString()
		{
			return $"{base.ToString()} - {Value}";
		}
	}
}
