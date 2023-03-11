// Project:         Daggerfall Tools For Unity
// Copyright:       Copyright (C) 2009-2022 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Podleron (podleron@gmail.com)

using System;
using System.IO;
using DaggerfallConnect;
using DaggerfallWorkshop.Game.Addons.RmbBlockEditor.BuildingPresets;
using DaggerfallWorkshop.Utility.AssetInjection;
using Unity.Plastic.Newtonsoft.Json;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace DaggerfallWorkshop.Game.Addons.RmbBlockEditor.Elements
{
    public class BuildingDataElement : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<BuildingDataElement, UxmlTraits>
        {
        }

        private const string WorldDataFolder = "/StreamingAssets/WorldData/";
        public BuildingReplacementData Data;
        public Action<BuildingReplacementData> Change { get; set; }

        public void SetData(BuildingReplacementData data)
        {
            Data = data;
            Initialize();
        }

        public BuildingDataElement()
        {
            var template =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/Game/Addons/RmbBlockEditor/Editor/Elements/BuildingDataElement/Template.uxml");
            Add(template.CloneTree());

            Initialize();
            RegisterCallbacks();
        }

        public void HideCatalogImport()
        {
            var replaceFromCatalogButton = this.Query<Button>("replace-from-catalog-button").First();
            replaceFromCatalogButton.AddToClassList("hidden");
        }

        private void Initialize()
        {
            // Get field references
            var factionIdField = this.Query<IntegerField>("faction-id").First();
            var buildingTypeField = this.Query<EnumField>("building-type").First();
            var qualitySlider = this.Query<SliderInt>("quality").First();
            var qualityField = this.Query<IntegerField>("quality-input").First();
            buildingTypeField.Init(DFLocation.BuildingTypes.House1);

            factionIdField.value = Data.FactionId;
            buildingTypeField.value = (DFLocation.BuildingTypes)Data.BuildingType;
            qualitySlider.value = Data.Quality;
            qualityField.value = Data.Quality;

            // Show exterior thumbnail
            var exteriorThumb = this.Query<VisualElement>("exterior-thumbnail").First();
            exteriorThumb.Clear();
            exteriorThumb.Add(BuildingPreset.GetExteriorPreview(Data));

            // Show interior thumbnail
            var interiorThumb = this.Query<VisualElement>("interior-thumbnail").First();
            interiorThumb.Clear();
            interiorThumb.Add(BuildingPreset.GetInteriorPreview(Data));

            // Set the containers visibility
            HideReplaceFromFile();
        }

        private void RegisterCallbacks()
        {
            // Get element references
            var factionIdField = this.Query<IntegerField>("faction-id").First();
            var buildingTypeField = this.Query<EnumField>("building-type").First();
            var qualitySlider = this.Query<SliderInt>("quality").First();
            var qualityField = this.Query<IntegerField>("quality-input").First();
            var replaceFromFileButton = this.Query<Button>("replace-from-file-button").First();
            var replaceFromCatalogButton = this.Query<Button>("replace-from-catalog-button").First();
            var importFromFile = this.Query<Button>("import-from-file").First();
            var cancelReplaceFromFile = this.Query<Button>("cancel-replace-from-file").First();

            // Register field callbacks
            factionIdField.RegisterCallback<ChangeEvent<int>>(evt =>
            {
                var factionId = ((IntegerField)evt.currentTarget).value;
                Data.FactionId = (byte)factionId;
                Change?.Invoke(Data);
            });
            buildingTypeField.RegisterCallback<ChangeEvent<EnumField>>(evt =>
            {
                var buildingType = ((EnumField)evt.currentTarget).value;
                Data.BuildingType = Convert.ToInt32(buildingType);
                Change?.Invoke(Data);
            });
            qualitySlider.RegisterCallback<ChangeEvent<int>>(evt =>
            {
                var quality = ((SliderInt)evt.currentTarget).value;
                qualityField.value = quality;
                Data.Quality = (byte)quality;
                Change?.Invoke(Data);
            });
            qualityField.RegisterCallback<ChangeEvent<int>>(evt =>
            {
                var quality = ((IntegerField)evt.currentTarget).value;
                Data.Quality = (byte)quality;
                Change?.Invoke(Data);
            });

            // Register button callbacks
            replaceFromFileButton.clicked += () =>
            {
                ShowReplaceFromFile();
            };

            importFromFile.clicked += () =>
            {
                OnImportBuilding();
            };

            cancelReplaceFromFile.clicked += () =>
            {
                HideReplaceFromFile();
            };
        }

        private void ShowReplaceFromFile()
        {
            var replaceFromFileContainer = this.Query<VisualElement>("replace-from-file-container").First();
            replaceFromFileContainer.RemoveFromClassList("hidden");
        }

        private void HideReplaceFromFile()
        {
            var replaceFromFileContainer = this.Query<VisualElement>("replace-from-file-container").First();
            replaceFromFileContainer.AddToClassList("hidden");
        }

        private void OnImportBuilding()
        {
            var importProps = this.Query<Toggle>("import-props").First();
            var importExterior = this.Query<Toggle>("import-exterior").First();
            var importInterior = this.Query<Toggle>("import-interior").First();

            var loadedData = new BuildingReplacementData();
            var success = LoadBuildingFile(ref loadedData);
            if (!success) return;

            if (importProps.value)
            {
                Data.FactionId = loadedData.FactionId;
                Data.BuildingType = loadedData.BuildingType;
                Data.Quality = loadedData.Quality;
            }

            if (importExterior.value)
            {
                Data.RmbSubRecord.Exterior = loadedData.RmbSubRecord.Exterior;
            }

            if (importInterior.value)
            {
                Data.RmbSubRecord.Interior = loadedData.RmbSubRecord.Interior;
            }

            Change?.Invoke(Data);
            Initialize();
        }

        private Boolean LoadBuildingFile(ref BuildingReplacementData buildingData)
        {
            var path = EditorUtility.OpenFilePanel("Import buildings", WorldDataFolder, "json");

            if (String.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return false;
            }

            try
            {
                var buildingJson = File.ReadAllText(path);
                buildingData = JsonConvert.DeserializeObject<BuildingReplacementData>(buildingJson);
                return true;
            }
            catch (ArgumentException e)
            {
                return false;
            }
        }
    }
}