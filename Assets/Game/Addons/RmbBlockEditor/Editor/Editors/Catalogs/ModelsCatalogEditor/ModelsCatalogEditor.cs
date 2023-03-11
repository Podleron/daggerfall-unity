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
using DaggerfallWorkshop.Game.Addons.RmbBlockEditor.Elements;
using DaggerfallWorkshop.Game.Utility.WorldDataEditor;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace DaggerfallWorkshop.Game.Addons.RmbBlockEditor
{
    public class ModelsCatalogEditor
    {
        private const string WorldDataFolder = "/StreamingAssets/WorldData/";
        private const string exportFile = "ModelsCatalogExport";
        private VisualElement visualElement;
        private ObjectPicker picker;
        private List<CatalogItem> catalog;
        private string objectId;
        private int selectedIndex;

        public ModelsCatalogEditor()
        {
            visualElement = new VisualElement();
            catalog = PersistedModelsCatalog.List();
        }

        public VisualElement Render()
        {
            RenderTemplate();
            BindInfoButton();
            BindCatalogOperations();
            BindOptions();
            RenderPicker();
            BindScriptButton();
            return visualElement;
        }

        private void RenderTemplate()
        {
            visualElement.Clear();
            var tree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/Game/Addons/RmbBlockEditor/Editor/Editors/Catalogs/ModelsCatalogEditor/Template.uxml");
            visualElement.Add(tree.CloneTree());
        }

        private void BindInfoButton()
        {
            var infoButton = visualElement.Query<Button>("catalog-info").First();
            infoButton.RegisterCallback<MouseUpEvent>(evt => EditorUtility.DisplayDialog("Models Catalog Editor",
                "Here you can see the catalog of models that you can use in the RMB block editor, when adding or modifying models in the scene. " +
                "\n\nThis screen lets you change what models are included by importing catalog files or by editing the items by hand. " +
                "You can also organize the items in categories and subcategories, and you can assign some tags to each item, for easy searching. " +
                "\n\nIn addition, you can export a catalog file, to share with others.", "OK"));
        }

        private void BindCatalogOperations()
        {
            var import = visualElement.Query<Button>("import").First();
            var export = visualElement.Query<Button>("export").First();
            var removeAll = visualElement.Query<Button>("remove-all").First();
            var restoreDefaults = visualElement.Query<Button>("restore-defaults").First();
            var scan = visualElement.Query<Button>("scan").First();

            import.RegisterCallback<MouseUpEvent>(OnImportCatalog, TrickleDown.TrickleDown);
            export.RegisterCallback<MouseUpEvent>(evt => { SaveFile(); }, TrickleDown.TrickleDown);
            removeAll.RegisterCallback<MouseUpEvent>(OnRemoveAllItems, TrickleDown.TrickleDown);
            restoreDefaults.RegisterCallback<MouseUpEvent>(OnRestoreDefault, TrickleDown.TrickleDown);
            scan.RegisterCallback<MouseUpEvent>(OnScan, TrickleDown.TrickleDown);
        }

        private void BindOptions()
        {
            var saveButton = visualElement.Query<Button>("save-item").First();
            var removeButton = visualElement.Query<Button>("remove-item").First();
            saveButton.RegisterCallback<MouseUpEvent>(OnSaveItem, TrickleDown.TrickleDown);
            removeButton.RegisterCallback<MouseUpEvent>(OnRemoveItem, TrickleDown.TrickleDown);
        }

        private void RenderPicker()
        {
            var pickerElement = visualElement.Query<VisualElement>("object-picker").First();
            pickerElement.Clear();
            if (catalog == null)
            {
                return;
            }

            picker =
                new ObjectPicker(catalog, OnItemSelected, GetPreview);
            pickerElement.Add(picker.visualElement);
        }

        private void FillCategory(ref List<CatalogItem> catalog, Dictionary<string, string> dictionary,
            string categoryName)
        {
            var allModels = WorldDataEditorObjectData.modelGroups;
            foreach (var clutterPair in dictionary)
            {
                var firstId = clutterPair.Key;
                var subCategory = clutterPair.Value;
                var isFound = false;
                foreach (var modelsCategory in allModels)
                {
                    if (!isFound && modelsCategory.Value.Contains(firstId))
                    {
                        isFound = true;
                        var allIdsInCategory = modelsCategory.Value;
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
                catalog = new List<CatalogItem>();

                var structure = WorldDataEditorObjectData.models_structure;
                var clutter = WorldDataEditorObjectData.models_clutter;
                var dungeon = WorldDataEditorObjectData.models_dungeon;
                var furniture = WorldDataEditorObjectData.models_furniture;
                var houseParts = WorldDataEditorObjectData.houseParts;
                var dungeonPartsRooms = WorldDataEditorObjectData.dungeonParts_rooms;
                var dungeonPartsCorridors = WorldDataEditorObjectData.dungeonParts_corridors;
                var dungeonPartsMisc = WorldDataEditorObjectData.dungeonParts_misc;
                var dungeonPartsCaves = WorldDataEditorObjectData.dungeonParts_caves;
                var dungeonPartsDoors = WorldDataEditorObjectData.dungeonParts_doors;

                FillCategory(ref catalog, structure, "Structure");
                FillCategory(ref catalog, clutter, "Clutter");
                FillCategory(ref catalog, dungeon, "Dungeon Clutter");
                FillCategory(ref catalog, furniture, "Furniture");
                FillCategory(ref catalog, houseParts, "Building Interior Parts");
                FillCategory(ref catalog, dungeonPartsRooms, "Dungeon Parts - Rooms");
                FillCategory(ref catalog, dungeonPartsCorridors, "Dungeon Parts - Corridors");
                FillCategory(ref catalog, dungeonPartsMisc, "Dungeon Parts - Misc");
                FillCategory(ref catalog, dungeonPartsCaves, "Dungeon Parts - Caves");
                FillCategory(ref catalog, dungeonPartsDoors, "Dungeon Parts - Doors");

                PersistedModelsCatalog.Set(catalog);
                RenderPicker();
            });
        }

        private void OnItemSelected(string objectId)
        {
            this.objectId = objectId;
            selectedIndex = catalog.FindIndex((item) => item.ID == objectId);
            CatalogItem selectedItem;
            if (selectedIndex != -1)
            {
                selectedItem = catalog[selectedIndex];
            }
            else
            {
                // If the selected ID is not in the catalog, create a new Catalog Item to display
                selectedItem = new CatalogItem(objectId);
            }

            var idElement = visualElement.Query<TextField>("id").First();
            var label = visualElement.Query<TextField>("label").First();
            var category = visualElement.Query<TextField>("category").First();
            var subcategory = visualElement.Query<TextField>("subcategory").First();
            var tags = visualElement.Query<TextField>("tags").First();

            idElement.value = objectId;
            label.value = selectedItem.Label;
            category.value = selectedItem.Category;
            subcategory.value = selectedItem.Subcategory;
            tags.value = selectedItem.Tags;
            ShowOptionsBox();
        }

        private void OnItemDeseleted()
        {
            this.objectId = "";
            HideOptionsBox();
        }

        private VisualElement GetPreview(string modelId)
        {
            var previewObject = RmbBlockHelper.Add3dObject(modelId);
            return new GoPreview(previewObject);
        }

        private void OnImportCatalog(MouseUpEvent evt)
        {
            var importType = EditorUtility.DisplayDialogComplex("Import Catalog",
                "You are about to import a catalog from a file. Would you like it to replace the existing catalog, or to be merged with it?",
                "Replace",
                "Merge",
                "Cancel");
            if (importType == 2) return; // Cancel

            var newCatalog = new List<CatalogItem>();
            var success = LoadFile(ref newCatalog);

            if (importType == 0 && success) // Replace
            {
                catalog = newCatalog;
                PersistedModelsCatalog.Set(catalog);
            }

            if (importType == 1 && success) // Merge
            {
                catalog = MergeCatalogs(catalog, newCatalog);
                PersistedModelsCatalog.Set(catalog);
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
            catalog = new List<CatalogItem>();
            PersistedModelsCatalog.Set(catalog);
            RenderPicker();
            HideOptionsBox();
        }

        private void OnRestoreDefault(MouseUpEvent evt)
        {
            var confirmed = EditorUtility.DisplayDialog("Restore Default Catalog?",
                "You are about to restore the default catalog! Are you sure?", "Yes",
                "No");
            if (!confirmed) return;
            var path = Environment.CurrentDirectory +
                       "/Assets/Game/Addons/RmbBlockEditor/Editor/Catalogs/ModelsCatalogEditor/DefaultModelsCatalog.json";
            try
            {
                var catalogJson = File.ReadAllText(path);
                catalog = JsonConvert.DeserializeObject<List<CatalogItem>>(catalogJson);
                PersistedModelsCatalog.Set(catalog);
                RenderPicker();
                HideOptionsBox();
            } catch (Exception e)
            {
                Debug.Log(e);
            }
        }

        private void OnScan(MouseUpEvent evt)
        {
            var importType = EditorUtility.DisplayDialogComplex(
                "Scan for custom models",
                "You are about to scan for custom models from mods. " +
                    "Would you like it to replace the existing catalog, or to be merged with it?",
                "Replace",
                "Merge",
                "Cancel");

            if (importType == 2) return; // Cancel

            var newCatalog = RMBModManager.GetCustomCatalogModels();

            if (importType == 0) // Replace
            {
                catalog = newCatalog;
                PersistedModelsCatalog.Set(catalog);
            }

            if (importType == 1) // Merge
            {
                catalog = MergeCatalogs(catalog, newCatalog);
                PersistedModelsCatalog.Set(catalog);
            }

            RenderPicker();
            HideOptionsBox();
        }

        private void OnSaveItem(MouseUpEvent evt)
        {
            var label = visualElement.Query<TextField>("label").First();
            var category = visualElement.Query<TextField>("category").First();
            var subcategory = visualElement.Query<TextField>("subcategory").First();
            var tags = visualElement.Query<TextField>("tags").First();
            var newLabel = label.text == "" ? null : label.text;
            var newItem = new CatalogItem(objectId, newLabel, category.text, subcategory.text, tags.text);

            var notInCatalog = selectedIndex == -1;
            if (notInCatalog)
            {
                catalog.Add(newItem);
            }
            else
            {
                catalog[selectedIndex] = newItem;
            }

            OnItemDeseleted();
            PersistedModelsCatalog.Set(catalog);
            RenderPicker();
        }

        private void OnRemoveItem(MouseUpEvent evt)
        {
            var confirmed = EditorUtility.DisplayDialog("Remove Item?",
                "You are about to remove this item from the catalog! Are you sure?", "Yes",
                "No");
            if (!confirmed) return;

            catalog.RemoveAt(selectedIndex);
            PersistedModelsCatalog.Set(catalog);
            RenderPicker();
            HideOptionsBox();
        }

        private void ShowOptionsBox()
        {
            var optionsBox = visualElement.Query<Box>("options-box").First();
            optionsBox.RemoveFromClassList("hidden");
        }

        private void HideOptionsBox()
        {
            var optionsBox = visualElement.Query<Box>("options-box").First();
            optionsBox.AddToClassList("hidden");
        }

        private List<CatalogItem> MergeCatalogs(List<CatalogItem> catalog1, List<CatalogItem> catalog2)
        {
            foreach (var item in catalog2)
            {
                var oldIndex = catalog1.FindIndex((i) => i.ID == item.ID);
                if (oldIndex == -1)
                {
                    catalog1.Add(item);
                }
                else
                {
                    catalog1[oldIndex] = item;
                }
            }

            return catalog1;
        }

        private Boolean LoadFile(ref List<CatalogItem> newCatalog)
        {
            var path = EditorUtility.OpenFilePanel("Import models catalog", WorldDataFolder, "json");

            if (String.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return false;
            }

            try
            {
                var catalogJson = File.ReadAllText(path);
                newCatalog = JsonConvert.DeserializeObject<List<CatalogItem>>(catalogJson);
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