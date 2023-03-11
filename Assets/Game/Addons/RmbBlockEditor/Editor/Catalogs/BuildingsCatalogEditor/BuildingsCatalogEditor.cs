// Project:         Daggerfall Tools For Unity
// Copyright:       Copyright (C) 2009-2022 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Podleron (podleron@gmail.com)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DaggerfallWorkshop.Game.Addons.RmbBlockEditor.BuildingPresets;
using DaggerfallWorkshop.Game.Addons.RmbBlockEditor.Elements;
using DaggerfallWorkshop.Utility.AssetInjection;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace DaggerfallWorkshop.Game.Addons.RmbBlockEditor
{
    public class BuildingsCatalogEditor
    {
        private const string WorldDataFolder = "/StreamingAssets/WorldData/";
        private const string exportFile = "BuildingsCatalogExport";
        private VisualElement visualElement;
        private ObjectPicker2 picker;
        private PersistedBuildingsCatalog catalog;
        private string objectId;
        private int selectedIndex;
        private CatalogItem selectedItem;
        private BuildingReplacementData selectedBuildingData;

        public BuildingsCatalogEditor()
        {
            visualElement = new VisualElement();
            catalog = PersistedBuildingsCatalog.Get();
        }

        public VisualElement Render()
        {
            RenderTemplate();
            InitializeCatalogItemElement();
            InitializeBuildingDataElement();
            BindInfoButton();
            BindCatalogOperations();
            RenderPicker();
            BindScriptButton();
            return visualElement;
        }

        private void RenderTemplate()
        {
            visualElement.Clear();
            var tree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/Game/Addons/RmbBlockEditor/Editor/Catalogs/BuildingsCatalogEditor/Template.uxml");
            visualElement.Add(tree.CloneTree());
        }

        private void InitializeCatalogItemElement()
        {
            var catalogItemElement = visualElement.Query<CatalogItemElement>("catalog-item-element").First();
            catalogItemElement.SubmitItem = OnSaveItem;
            catalogItemElement.RemoveItem = OnRemoveItem;
        }

        private void InitializeBuildingDataElement()
        {
            var buildingDataElement = visualElement.Query<BuildingDataElement>("building-data-element").First();
            buildingDataElement.Change = OnChangeBuildingData;
            buildingDataElement.HideCatalogImport();
        }

        private void BindInfoButton()
        {
            var infoButton = visualElement.Query<Button>("catalog-info").First();
            infoButton.RegisterCallback<MouseUpEvent>(evt => EditorUtility.DisplayDialog("Buildings Catalog Editor",
                "Here you can see the catalog of buildings that you can use in the RMB block editor, when adding or modifying buildings in the scene. " +
                "\n\nThis screen lets you change what buildings are included by importing catalog files or by editing the items by hand. " +
                "You can also organize the items in categories and subcategories, and you can assign some tags to each item, for easy searching. " +
                "\n\nIn addition, you can export a catalog file, to share with others.", "OK"));
        }

        private void BindCatalogOperations()
        {
            var import = visualElement.Query<Button>("import").First();
            var export = visualElement.Query<Button>("export").First();
            var removeAll = visualElement.Query<Button>("remove-all").First();
            var restoreDefaults = visualElement.Query<Button>("restore-defaults").First();
            var addNew = visualElement.Query<Button>("add-new").First();

            import.RegisterCallback<MouseUpEvent>(OnImportCatalog, TrickleDown.TrickleDown);
            export.RegisterCallback<MouseUpEvent>(evt => { SaveFile(); }, TrickleDown.TrickleDown);
            removeAll.RegisterCallback<MouseUpEvent>(OnRemoveAllItems, TrickleDown.TrickleDown);
            restoreDefaults.RegisterCallback<MouseUpEvent>(OnRestoreDefault, TrickleDown.TrickleDown);
            addNew.RegisterCallback<MouseUpEvent>(OnAddNewItem, TrickleDown.TrickleDown);
        }

        private void RenderPicker()
        {
            var pickerElement = visualElement.Query<VisualElement>("object-picker").First();
            pickerElement.Clear();
            if (catalog == null)
            {
                return;
            }

            picker = new ObjectPicker2(catalog.list, OnItemSelected, GetPreview, objectId);
            pickerElement.Add(picker.visualElement);
        }

        private void FillCategory(ref List<CatalogItem> catalog, Dictionary<string, string> dictionary,
            string categoryName)
        {
            var allBuildings = BuildingPresetData.buildingGroups;
            foreach (var pair in dictionary)
            {
                var firstId = pair.Key;
                var subCategory = pair.Value;
                var isFound = false;
                foreach (var flatsCategory in allBuildings)
                {
                    if (!isFound && flatsCategory.Value.Contains(firstId))
                    {
                        isFound = true;
                        var allIdsInCategory = flatsCategory.Value;
                        foreach (var id in allIdsInCategory)
                        {
                            catalog.Add(new CatalogItem(id, id, categoryName, subCategory));
                        }
                    }
                }

                if (!isFound)
                {
                    catalog.Add(new CatalogItem(firstId, subCategory, categoryName));
                }
            }
        }

        private void BindScriptButton()
        {
            var scriptButton = visualElement.Query<Button>("execute-script").First();
            scriptButton.RegisterCallback<MouseUpEvent>((e) =>
            {
                catalog.list = new List<CatalogItem>();

                var houses = BuildingPresetData.houses;
                var shops = BuildingPresetData.shops;
                var services = BuildingPresetData.services;
                var guilds = BuildingPresetData.guilds;
                var others = BuildingPresetData.others;

                FillCategory(ref catalog.list, houses, "Houses");
                FillCategory(ref catalog.list, shops, "Shops");
                FillCategory(ref catalog.list, services, "Services");
                FillCategory(ref catalog.list, guilds, "Guilds");
                FillCategory(ref catalog.list, others, "Others");

                PersistedBuildingsCatalog.SetList(catalog.list);

                catalog.templates = new Dictionary<string, BuildingReplacementData>();
                foreach (var catalogItem in catalog.list)
                {
                    catalog.templates.Add(catalogItem.ID, BuildingPreset.GetBuildingData(catalogItem.ID));
                }

                PersistedBuildingsCatalog.SetTemplates(catalog.templates);

                RenderPicker();
            });
        }

        private void OnItemSelected(string objectId)
        {
            this.objectId = objectId;
            selectedIndex = catalog.list.FindIndex((item) => item.ID == objectId);

            if (selectedIndex != -1)
            {
                selectedItem = catalog.list[selectedIndex];
                selectedBuildingData = catalog.templates[selectedItem.ID];
            }
            else
            {
                // If the selected ID is not in the catalog, create a new Catalog Item to display
                selectedItem = new CatalogItem(objectId);
                selectedBuildingData = new BuildingReplacementData();
            }

            var catalogItemElement = visualElement.Query<CatalogItemElement>("catalog-item-element").First();
            catalogItemElement.SetItem(selectedItem);

            var buildingDataElement = visualElement.Query<BuildingDataElement>("building-data-element").First();
            buildingDataElement.SetData(selectedBuildingData);

            ShowOptionsBox();
        }

        private void OnChangeBuildingData(BuildingReplacementData data)
        {
            selectedBuildingData = data;
        }

        private void OnItemDeseleted()
        {
            OnItemSelected("");
            HideOptionsBox();
        }

        private VisualElement GetPreview(string buildingId)
        {
            return BuildingPreset.GetPreview(catalog.templates[buildingId]);
        }

        private void OnImportCatalog(MouseUpEvent evt)
        {
            var importType = EditorUtility.DisplayDialogComplex("Import Catalog",
                "You are about to import a catalog from a file. Would you like it to replace the existing catalog, or to be merged with it?",
                "Replace",
                "Merge",
                "Cancel");
            if (importType == 2) return; // Cancel

            var newCatalog = new PersistedBuildingsCatalog();
            var success = LoadCatalogFile(ref newCatalog);

            if (importType == 0 && success) // Replace
            {
                catalog = newCatalog;
                PersistedBuildingsCatalog.Set(catalog);
            }

            if (importType == 1 && success) // Merge
            {
                catalog = MergeCatalogs(catalog, newCatalog);
                PersistedBuildingsCatalog.Set(catalog);
            }

            RenderPicker();
            OnItemDeseleted();
        }

        private void OnRemoveAllItems(MouseUpEvent evt)
        {
            var confirmed = EditorUtility.DisplayDialog("Remove All Items?",
                "You are about to remove all of the items from the catalog! Are you sure?", "Yes",
                "No");
            if (!confirmed) return;
            catalog.list = new List<CatalogItem>();
            catalog.templates = new Dictionary<string, BuildingReplacementData>();
            PersistedBuildingsCatalog.Set(catalog);
            RenderPicker();
            HideOptionsBox();
        }

        private void OnRestoreDefault(MouseUpEvent evt)
        {
            var confirmed = EditorUtility.DisplayDialog("Restore Default Catalog?",
                "You are about to restore the default catalog! Are you sure?", "Yes",
                "No");
            if (!confirmed) return;
            try
            {
                PersistedBuildingsCatalog.RestoreDefault();
                catalog = PersistedBuildingsCatalog.Get();
                RenderPicker();
                HideOptionsBox();
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
        }

        private void OnAddNewItem(MouseUpEvent evt)
        {
            var newItem = new CatalogItem("new item");
            var newBuildingData = new BuildingReplacementData();
            catalog.list.Add(newItem);
            catalog.templates.Add(newItem.ID, newBuildingData);
            OnItemSelected(newItem.ID);
        }

        private void OnSaveItem(CatalogItem newItem)
        {
            var factionId = visualElement.Query<IntegerField>("faction-id").First();
            var buildingType = visualElement.Query<EnumField>("building-type").First();
            var quality = visualElement.Query<SliderInt>("quality").First();
            selectedBuildingData.FactionId = (ushort)factionId.value;
            selectedBuildingData.BuildingType = Convert.ToInt32(buildingType.value);
            selectedBuildingData.Quality = (byte)quality.value;

            newItem.Label = newItem.Label == "" ? null : newItem.Label;
            var idChanged = objectId != newItem.ID;

            var index = catalog.list.FindIndex((item) => item.ID == newItem.ID);
            var inCatalog = catalog.list.FindIndex((item) => item.ID == newItem.ID) != -1;

            if (idChanged && inCatalog)
            {
                var confirmed = EditorUtility.DisplayDialog("Override?",
                    "An item with this ID already exists. Would you like to override it?", "Yes",
                    "No");
                if (!confirmed) return;
            }


            if (!inCatalog)
            {
                catalog.list.RemoveAt(selectedIndex);
                catalog.templates.Remove(selectedItem.ID);
                catalog.list.Add(newItem);
                catalog.templates.Add(newItem.ID, selectedBuildingData);
            }
            else if (idChanged)
            {
                catalog.list[index] = newItem;
                catalog.list.RemoveAt(selectedIndex);
                catalog.templates.Remove(selectedItem.ID);
                catalog.templates[newItem.ID] = selectedBuildingData;
            }
            else
            {
                catalog.list[index] = newItem;
                catalog.templates[newItem.ID] = selectedBuildingData;
            }

            PersistedBuildingsCatalog.Set(catalog);
            RenderPicker();
        }

        private void OnRemoveItem()
        {
            var confirmed = EditorUtility.DisplayDialog("Remove Item?",
                "You are about to remove this item from the catalog! Are you sure?", "Yes",
                "No");
            if (!confirmed) return;

            catalog.list.RemoveAt(selectedIndex);
            PersistedBuildingsCatalog.Set(catalog);
            OnItemDeseleted();
            RenderPicker();
        }

        private void ShowOptionsBox()
        {
            var optionsBox = visualElement.Query<VisualElement>("options-box").First();
            optionsBox.RemoveFromClassList("hidden");
        }

        private void HideOptionsBox()
        {
            var optionsBox = visualElement.Query<VisualElement>("options-box").First();
            optionsBox.AddToClassList("hidden");
        }

        private PersistedBuildingsCatalog MergeCatalogs(PersistedBuildingsCatalog catalog1,
            PersistedBuildingsCatalog catalog2)
        {
            foreach (var item in catalog2.list)
            {
                var oldIndex = catalog1.list.FindIndex((i) => i.ID == item.ID);
                if (oldIndex == -1)
                {
                    catalog1.list.Add(item);
                }
                else
                {
                    catalog1.list[oldIndex] = item;
                }

                catalog1.templates[item.ID] = catalog2.templates[item.ID];
            }

            return catalog1;
        }

        private Boolean LoadCatalogFile(ref PersistedBuildingsCatalog newCatalog)
        {
            var path = EditorUtility.OpenFilePanel("Import buildings catalog", WorldDataFolder, "json");

            if (String.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return false;
            }

            try
            {
                var catalogJson = File.ReadAllText(path);
                newCatalog = JsonConvert.DeserializeObject<PersistedBuildingsCatalog>(catalogJson);
                return true;
            }
            catch (ArgumentException e)
            {
                return false;
            }
        }

        private void SaveFile()
        {
            var fileContent = JsonConvert.SerializeObject(catalog);
            var path = EditorUtility.SaveFilePanel("Save", WorldDataFolder, exportFile, "json");
            File.WriteAllText(path, fileContent);
            OnItemDeseleted();
        }
    }
}