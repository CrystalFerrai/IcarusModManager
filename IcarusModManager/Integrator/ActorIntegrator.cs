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

using System;
using System.Collections.Generic;
using System.IO;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.FieldTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using UAssetAPI.UnrealTypes;

namespace IcarusModManager.Integrator
{
	/// <summary>
	/// Modifies a cooked actor UAsset, adding a list of components (by name) to it
	/// </summary>
	internal static class ActorIntegrator
    {
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
		/// <param name="newComponents">A list of asset paths of actor components to add to the actor</param>
		/// <returns>The actor asset with the components added</returns>
		public static void Integrate(UAsset asset, params string[] newComponents)
        {
            if (newComponents == null) throw new ArgumentNullException(nameof(newComponents));
            if (newComponents.Length == 0) throw new ArgumentException("Must pass at least one component", nameof(newComponents));

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

                NormalExport defaultObjectExport = (NormalExport)asset.Exports[defaultObjectLocation];
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
