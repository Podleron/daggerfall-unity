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
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace DaggerfallWorkshop.Game.Addons.RmbBlockEditor
{
    public class FlatsCatalogEditor
    {
        private const string WorldDataFolder = "/StreamingAssets/WorldData/";
        private const string exportFile = "FlatsCatalogExport";
        private VisualElement visualElement;
        private ObjectPicker2 picker;
        private List<CatalogItem> catalog;
        private string objectId;
        private int selectedIndex;

        public FlatsCatalogEditor()
        {
            visualElement = new VisualElement();
            catalog = PersistedFlatsCatalog.List();
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
                    "Assets/Game/Addons/RmbBlockEditor/Editor/Catalogs/FlatsCatalogEditor/Template.uxml");
            visualElement.Add(tree.CloneTree());
        }

        private void BindInfoButton()
        {
            var infoButton = visualElement.Query<Button>("catalog-info").First();
            infoButton.RegisterCallback<MouseUpEvent>(evt => EditorUtility.DisplayDialog("Flats Catalog Editor",
                "Here you can see the catalog of flats that you can use in the RMB block editor, when adding or modifying flats in the scene. " +
                "\n\nThis screen lets you change what flats are included by importing catalog files or by editing the items by hand. " +
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

            picker = new ObjectPicker2(catalog, OnItemSelected, GetPreview);
            pickerElement.Add(picker.visualElement);
        }

        private void FillCategory(ref List<CatalogItem> catalog, Dictionary<string, string> dictionary,
            string categoryName)
        {
            var allFlats = WorldDataEditorObjectData.flatGroups;
            foreach (var pair in dictionary)
            {
                var firstId = pair.Key;
                var subCategory = pair.Value;
                var isFound = false;
                foreach (var flatsCategory in allFlats)
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
                catalog = new List<CatalogItem>();

                var people = WorldDataEditorObjectData.NPCs;
                var interior = WorldDataEditorObjectData.billboards_interior;
                var nature = WorldDataEditorObjectData.billboards_nature;
                var lights = WorldDataEditorObjectData.billboards_lights;
                var treasure = WorldDataEditorObjectData.billboards_treasure;
                var markers = WorldDataEditorObjectData.billboards_markers;

                FillCategory(ref catalog, people, "People");
                FillCategory(ref catalog, interior, "Interior");
                FillCategory(ref catalog, nature, "Nature");
                FillCategory(ref catalog, lights, "Lights");
                FillCategory(ref catalog, treasure, "Treasure");
                FillCategory(ref catalog, markers, "Markers");

                PersistedFlatsCatalog.Set(catalog);
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

        private VisualElement GetPreview(string flatId)
        {
            var previewObject = RmbBlockHelper.AddFlatObject(flatId);
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
                PersistedFlatsCatalog.Set(catalog);
            }

            if (importType == 1 && success) // Merge
            {
                catalog = MergeCatalogs(catalog, newCatalog);
                PersistedFlatsCatalog.Set(catalog);
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
            PersistedFlatsCatalog.Set(catalog);
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
                       "/Assets/Game/Addons/RmbBlockEditor/Editor/Catalogs/FlatsCatalogEditor/DefaultFlatsCatalog.json";
            try
            {
                var catalogJson = File.ReadAllText(path);
                catalog = JsonConvert.DeserializeObject<List<CatalogItem>>(catalogJson);
                PersistedFlatsCatalog.Set(catalog);
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
                "Scan for custom flats",
                "You are about to scan for custom flats from mods. " +
                    "Would you like it to replace the existing catalog, or to be merged with it?",
                "Replace",
                "Merge",
                "Cancel");

            if (importType == 2) return; // Cancel

            var newCatalog = RMBModManager.GetCustomCatalogFlats();

            if (importType == 0) // Replace
            {
                catalog = newCatalog;
                PersistedFlatsCatalog.Set(catalog);
            }

            if (importType == 1) // Merge
            {
                catalog = MergeCatalogs(catalog, newCatalog);
                PersistedFlatsCatalog.Set(catalog);
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
            PersistedFlatsCatalog.Set(catalog);
            RenderPicker();
        }

        private void OnRemoveItem(MouseUpEvent evt)
        {
            var confirmed = EditorUtility.DisplayDialog("Remove Item?",
                "You are about to remove this item from the catalog! Are you sure?", "Yes",
                "No");
            if (!confirmed) return;

            catalog.RemoveAt(selectedIndex);
            PersistedFlatsCatalog.Set(catalog);
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
            var path = EditorUtility.OpenFilePanel("Import flats catalog", WorldDataFolder, "json");

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