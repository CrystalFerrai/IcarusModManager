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

// This class started as a copy of ActorBaker from AstroModIntegrator and
// was then modified to work for Icarus.
// https://github.com/AstroTechies/AstroModIntegrator
// AstroModIntegrator is under the MIT license. A copy can be found at
// https://github.com/AstroTechies/AstroModIntegrator/blob/master/LICENSE

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.FieldTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using UAssetAPI.UnrealTypes;

namespace IcarusModManager.Core.Integrator
{
	/// <summary>
	/// Modifies a cooked actor UAsset, adding a list of components (by name) to it
	/// </summary>
	internal static class ActorIntegrator
	{
		private static Assembly sUAssetAPIAssembly;

		private static readonly Export sRefTemplate;
		private static readonly Export sRefSCSNode;

		static ActorIntegrator()
		{
			// To generate a template file without separate uexp, set these in DefaultEngine.ini
			//
			// [Core.System]
			// UseSeperateBulkDataFiles=False
			// 
			// [/Script/Engine.StreamingSettings]
			// s.EventDrivenLoaderEnabled=False

			sUAssetAPIAssembly = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name!.Equals("UAssetAPI"));

			UAsset asset = new(EngineVersion.VER_UE4_27);

			using (MemoryStream stream = new(Properties.Resources.ActorTemplate))
			using (AssetBinaryReader reader = new(stream, asset))
			{
				asset.Read(reader);
			}

			sRefTemplate = asset.Exports[2];
			sRefSCSNode = asset.Exports[5];
		}

		/// <summary>
		/// Performs an integration
		/// </summary>
		/// <param name="asset">The actor to modify</param>
		/// <param name="propertyOverrides">Override the default value of the given properties</param>
		/// <param name="newComponents">A list of asset paths of actor components to add to the actor</param>
		/// <returns>The actor asset with the components added</returns>
		public static void Integrate(UAsset asset, IEnumerable<ActorProperty>? propertyOverrides, IEnumerable<string>? newComponents)
		{
			if ((propertyOverrides is null || !propertyOverrides.Any()) && (newComponents is null || !newComponents.Any()))
			{
				throw new ArgumentException("Must specify property overrides and/or new components");
			}

			int scsLocation = -1;
			int classLocation = -1;
			int defaultObjectLocation = -1;
			int scsNodeOffset = 0;
			for (int i = 0; i < asset.Exports.Count; i++)
			{
				Export baseUs = asset.Exports[i];
				if (baseUs is NormalExport)
				{
					switch (baseUs.ClassIndex.IsImport() ? baseUs.ClassIndex.ToImport(asset).ObjectName.Value.Value : string.Empty)
					{
						case "SimpleConstructionScript":
							scsLocation = i;
							break;
						case "BlueprintGeneratedClass":
							classLocation = i;
							break;
						case "SCS_Node":
							scsNodeOffset++;
							break;
					}
					if (baseUs.ObjectFlags.HasFlag(EObjectFlags.RF_ClassDefaultObject)) defaultObjectLocation = i;
				}
			}
			if (scsLocation < 0) throw new FormatException("Unable to find SimpleConstructionScript");
			if (classLocation < 0) throw new FormatException("Unable to find BlueprintGeneratedClass");
			if (defaultObjectLocation < 0) throw new FormatException("Unable to find class default object");

			NormalExport defaultObjectExport = (NormalExport)asset.Exports[defaultObjectLocation];

			if (propertyOverrides is not null)
			{
				IntegrateProperties(asset, propertyOverrides, defaultObjectExport);
			}

			if (newComponents is not null)
			{
				IntegrateComponents(asset, newComponents, scsLocation, classLocation, ref scsNodeOffset, defaultObjectExport);
			}
		}

		private static void IntegrateProperties(UAsset asset, IEnumerable<ActorProperty> propertyOverrides, NormalExport defaultObjectExport)
		{
			Dictionary<string, PropertyData> propertyMap = defaultObjectExport.Data.ToDictionary(p => p.Name.Value.Value, p => p);
			foreach (ActorProperty prop in propertyOverrides)
			{
				PropertyData? property;
				if (!propertyMap.TryGetValue(prop.Name, out property))
				{
					Type? dataType = sUAssetAPIAssembly.GetType($"UAssetAPI.PropertyTypes.Objects.{prop.Type}Data");
					if (dataType is null) dataType = sUAssetAPIAssembly.GetType($"UAssetAPI.PropertyTypes.Structs.{prop.Type}Data");
					if (dataType is null) throw new NotSupportedException($"The property type {prop.Type} could not be located.");

					property = (PropertyData)Activator.CreateInstance(dataType, FName.FromString(asset, prop.Name))!;
					defaultObjectExport.Data.Add(property);
				}

				switch (prop.Type)
				{
					case "BoolProperty":
						((BoolPropertyData)property).Value = (bool)Convert.ChangeType(prop.Value, typeof(bool));
						break;
					case "FloatProperty":
						((FloatPropertyData)property).Value = (float)Convert.ChangeType(prop.Value, typeof(float));
						break;
					case "DoubleProperty":
						((DoublePropertyData)property).Value = (double)Convert.ChangeType(prop.Value, typeof(double));
						break;
					case "Int8Property":
						((Int8PropertyData)property).Value = (sbyte)Convert.ChangeType(prop.Value, typeof(sbyte));
						break;
					case "Int16Property":
						((Int16PropertyData)property).Value = (short)Convert.ChangeType(prop.Value, typeof(short));
						break;
					case "IntProperty":
						((IntPropertyData)property).Value = (int)Convert.ChangeType(prop.Value, typeof(int));
						break;
					case "Int64Property":
						((Int64PropertyData)property).Value = (long)Convert.ChangeType(prop.Value, typeof(long));
						break;
					case "UInt16Property":
						((UInt16PropertyData)property).Value = (ushort)Convert.ChangeType(prop.Value, typeof(ushort));
						break;
					case "UInt32Property":
						((UInt32PropertyData)property).Value = (uint)Convert.ChangeType(prop.Value, typeof(uint));
						break;
					case "UInt64Property":
						((UInt64PropertyData)property).Value = (ulong)Convert.ChangeType(prop.Value, typeof(ulong));
						break;
					case "NameProperty":
						((NamePropertyData)property).Value = new FName(asset, (string)Convert.ChangeType(prop.Value, typeof(string)));
						break;
					case "StrProperty":
						((StrPropertyData)property).Value = new FString((string)Convert.ChangeType(prop.Value, typeof(string)));
						break;
					case "InterfaceProperty":
					case "ObjectProperty":
						{
							string assetPath = (string)((JValue)prop.Value).Value!;
							string assetName = Path.GetFileName(assetPath);

							Import firstLink = new Import(new FName(asset, "/Script/CoreUObject"), new FName(asset, "Package"), FPackageIndex.FromRawIndex(0), new FName(asset, assetPath), false);
							FPackageIndex bigFirstLink = asset.AddImport(firstLink);
							Import newLink = new Import(new FName(asset, "/Script/Engine"), new FName(asset, "BlueprintGeneratedClass"), bigFirstLink, new FName(asset, assetName + "_C"), false);
							FPackageIndex bigNewLink = asset.AddImport(newLink);
							Import newLink2 = new Import(new FName(asset, assetPath), new FName(asset, assetName + "_C"), bigFirstLink, new FName(asset, "Default__" + assetName + "_C"), false);
							FPackageIndex bigNewLink2 = asset.AddImport(newLink2);

							((ObjectPropertyData)property).Value = bigNewLink;
						}
						break;

					// Non-trivial to implement. Add as needed.
					case "ArrayProperty":
					case "SetProperty":
					case "MapProperty":
					case "AssetObjectProperty":
					case "SoftObjectProperty":
					case "ByteProperty":
					case "EnumProperty":
					case "TextProperty":
					case "DelegateProperty":
					case "MulticastDelegateProperty":
					case "StructProperty":
						throw new NotImplementedException($"Actor patch property type '{prop.Type}' has not been implemented.");
					default:
						throw new NotSupportedException($"Unrecognized actor patch property type '{prop.Type}'.");
				}
			}
		}

		private static void IntegrateComponents(UAsset asset, IEnumerable<string> newComponents, int scsLocation, int classLocation, ref int scsNodeOffset, NormalExport defaultObjectExport)
		{
			FName coreUObjectName = new(asset, "/Script/CoreUObject");

			int scsNodeLink = asset.SearchForImport(coreUObjectName, new FName(asset, "Class"), new FName(asset, "SCS_Node"));
			int scsNodeLink2 = asset.SearchForImport(new FName(asset, "/Script/Engine"), new FName(asset, "SCS_Node"), new FName(asset, "Default__SCS_Node"));

			foreach (string componentPathRaw in newComponents)
			{
				Export refTemplateExport = (Export)sRefTemplate.Clone();
				Export refSCSNodeExport = (Export)sRefSCSNode.Clone();

				string componentPath = componentPathRaw;
				string component = Path.GetFileNameWithoutExtension(componentPathRaw);
				if (componentPathRaw.Contains('.'))
				{
					string[] tData = componentPathRaw.Split(new char[] { '.' });
					componentPath = tData[0];
					component = tData[1].Remove(tData[1].Length - 2);
				}

				Import firstLink = new Import(new FName(asset, "/Script/CoreUObject"), new FName(asset, "Package"), FPackageIndex.FromRawIndex(0), new FName(asset, componentPath), false);
				FPackageIndex bigFirstLink = asset.AddImport(firstLink);
				Import newLink = new Import(new FName(asset, "/Script/Engine"), new FName(asset, "BlueprintGeneratedClass"), bigFirstLink, new FName(asset, component + "_C"), false);
				FPackageIndex bigNewLink = asset.AddImport(newLink);
				Import newLink2 = new Import(new FName(asset, componentPath), new FName(asset, component + "_C"), bigFirstLink, new FName(asset, "Default__" + component + "_C"), false);
				FPackageIndex bigNewLink2 = asset.AddImport(newLink2);

				refTemplateExport.ClassIndex = bigNewLink;
				refTemplateExport.ObjectName = new FName(asset, component + "_GEN_VARIABLE");
				refTemplateExport.TemplateIndex = bigNewLink2;

				refSCSNodeExport.ClassIndex = FPackageIndex.FromRawIndex(scsNodeLink);
				refSCSNodeExport.ObjectName = new FName(asset, "SCS_Node");
				refSCSNodeExport.TemplateIndex = FPackageIndex.FromRawIndex(scsNodeLink2);

				refTemplateExport.OuterIndex = FPackageIndex.FromRawIndex(classLocation + 1); // BlueprintGeneratedClass
				refSCSNodeExport.OuterIndex = FPackageIndex.FromRawIndex(scsLocation + 1);

				// Note that export links index from 1 instead of 0

				// First we add the template export
				NormalExport templateExport = refTemplateExport.ConvertToChildExport<NormalExport>();
				templateExport.SerializationBeforeSerializationDependencies.Add(FPackageIndex.FromRawIndex(classLocation + 1));
				templateExport.SerializationBeforeCreateDependencies.Add(bigNewLink);
				templateExport.SerializationBeforeCreateDependencies.Add(bigNewLink2);
				templateExport.CreateBeforeCreateDependencies.Add(FPackageIndex.FromRawIndex(classLocation + 1));
				templateExport.Extras = new byte[4] { 0, 0, 0, 0 };
				templateExport.Data = new List<PropertyData>();
				asset.Exports.Add(templateExport);

				defaultObjectExport.SerializationBeforeSerializationDependencies.Add(FPackageIndex.FromRawIndex(asset.Exports.Count));

				// Then the SCS_Node
				NormalExport scsNodeExport = refSCSNodeExport.ConvertToChildExport<NormalExport>();
				scsNodeExport.ObjectName = new FName(asset, "SCS_Node", ++scsNodeOffset);
				scsNodeExport.Extras = new byte[4] { 0, 0, 0, 0 };
				scsNodeExport.CreateBeforeSerializationDependencies.Add(bigNewLink);
				scsNodeExport.CreateBeforeSerializationDependencies.Add(FPackageIndex.FromRawIndex(asset.Exports.Count));
				scsNodeExport.SerializationBeforeCreateDependencies.Add(FPackageIndex.FromRawIndex(scsNodeLink));
				scsNodeExport.SerializationBeforeCreateDependencies.Add(FPackageIndex.FromRawIndex(scsNodeLink2));
				scsNodeExport.CreateBeforeCreateDependencies.Add(FPackageIndex.FromRawIndex(scsLocation + 1));
				scsNodeExport.Data = new List<PropertyData>
					{
						new ObjectPropertyData(new FName(asset, "ComponentClass"))
						{
							Value = bigNewLink
						},
						new ObjectPropertyData(new FName(asset, "ComponentTemplate"))
						{
							Value = FPackageIndex.FromRawIndex(asset.Exports.Count)
						},
						new StructPropertyData(new FName(asset, "VariableGuid"), new FName(asset, "Guid"))
						{
							Value = new List<PropertyData>
							{
								new GuidPropertyData(new FName(asset, "VariableGuid"))
								{
									Value = Guid.NewGuid()
								}
							}
						},
						new NamePropertyData(new FName(asset, "InternalVariableName"))
						{
							Value = new FName(asset, component)
						}
					};
				asset.Exports.Add(scsNodeExport);

				// We update the BlueprintGeneratedClass data to include our new ActorComponent
				FObjectProperty objectProp = new FObjectProperty()
				{
					ArrayDim = EArrayDim.TArray,
					BlueprintReplicationCondition = UAssetAPI.FieldTypes.ELifetimeCondition.COND_None,
					ElementSize = 8,
					Flags = EObjectFlags.RF_Public | EObjectFlags.RF_LoadCompleted,
					Name = new FName(asset, component),
					PropertyClass = bigNewLink,
					PropertyFlags = EPropertyFlags.CPF_BlueprintVisible | EPropertyFlags.CPF_InstancedReference | EPropertyFlags.CPF_NonTransactional,
					RawValue = null,
					RepIndex = 0,
					RepNotifyFunc = new FName(asset, "None"),
					SerializedType = new FName(asset, "ObjectProperty")
				};
				FProperty[] oldData = ((StructExport)asset.Exports[classLocation]).LoadedProperties;
				FProperty[] newData = new FProperty[oldData.Length + 1];
				Array.Copy(oldData, 0, newData, 0, oldData.Length);
				newData[oldData.Length] = objectProp;
				((StructExport)asset.Exports[classLocation]).LoadedProperties = newData;

				// Here we update the SimpleConstructionScript so that the parser constructs our new ActorComponent
				NormalExport scsExport = (NormalExport)asset.Exports[scsLocation];
				scsExport.CreateBeforeSerializationDependencies.Add(FPackageIndex.FromRawIndex(asset.Exports.Count));
				defaultObjectExport.SerializationBeforeSerializationDependencies.Add(FPackageIndex.FromRawIndex(asset.Exports.Count));
				for (int j = 0; j < scsExport.Data.Count; j++)
				{
					PropertyData propData = scsExport.Data[j];
					if (propData is ArrayPropertyData)
					{
						switch (propData.Name.Value.Value)
						{
							case "AllNodes":
							case "RootNodes":
								PropertyData[] ourArr = ((ArrayPropertyData)propData).Value;
								int oldSize = ourArr.Length;
								Array.Resize(ref ourArr, oldSize + 1);
								refSCSNodeExport.ObjectName = new FName(asset, refSCSNodeExport.ObjectName.Value, oldSize + 2);
								ourArr[oldSize] = new ObjectPropertyData(propData.Name)
								{
									Value = FPackageIndex.FromRawIndex(asset.Exports.Count) // the SCS_Node
								};
								((ArrayPropertyData)propData).Value = ourArr;
								break;
						}
					}
				}
			}
		}
	}
}
