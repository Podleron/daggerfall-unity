// Project:         Daggerfall Tools For Unity
// Copyright:       Copyright (C) 2009-2022 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Podleron (podleron@gmail.com)

using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace DaggerfallWorkshop.Game.Addons.RmbBlockEditor
{
    public static class MiscFlatEditor
    {
        public static VisualElement Render(GameObject go, Action<string> modifyFlat)
        {
            // Get serialized properties:
            var miscFlat = go.GetComponent<MiscFlat>();
            var so = new SerializedObject(miscFlat);
            var positionProperty = so.FindProperty("Position");
            var archiveProperty = so.FindProperty("TextureArchive");
            var recordProperty = so.FindProperty("TextureRecord");
            var factionIdProperty = so.FindProperty("FactionID");
            var flagsProperty = so.FindProperty("Flags");
            var worldPositionProperty = so.FindProperty("WorldPosition");

            // Render the template:
            var rmbMiscFlatPropsElement = new VisualElement();
            var rmbMiscFlatPropsTree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/Game/Addons/RmbBlockEditor/Editor/MiscFlatEditor/Template.uxml");
            rmbMiscFlatPropsElement.Add(rmbMiscFlatPropsTree.CloneTree());

            // Select the bindable fields:
            var positionField = rmbMiscFlatPropsElement.Query<Label>("position").First();
            var archiveField = rmbMiscFlatPropsElement.Query<Label>("archive").First();
            var recordField = rmbMiscFlatPropsElement.Query<Label>("record").First();
            var factionIdField = rmbMiscFlatPropsElement.Query<IntegerField>("faction").First();
            var flagsField = rmbMiscFlatPropsElement.Query<IntegerField>("flags").First();
            var worldPosition = rmbMiscFlatPropsElement.Query<Vector3Field>("world-position").First();

            var flatIdInCategoryField = rmbMiscFlatPropsElement.Query<Label>("flat-id-in-subcategory").First();
            var previous = rmbMiscFlatPropsElement.Query<Button>("previous-flat").First();
            var next = rmbMiscFlatPropsElement.Query<Button>("next-flat").First();
            var modify = rmbMiscFlatPropsElement.Query<Button>("modify-flat").First();

            // Bind the fields to the properties:
            positionField.BindProperty(positionProperty);
            archiveField.BindProperty(archiveProperty);
            recordField.BindProperty(recordProperty);
            factionIdField.BindProperty(factionIdProperty);
            flagsField.BindProperty(flagsProperty);
            worldPosition.BindProperty(worldPositionProperty);

            // Find the subcategory of the item
            var itemById = PersistedFlatsCatalog.ItemsDictionary();
            var subcategories = PersistedFlatsCatalog.SubcatalogDictionary();
            var id = $"{miscFlat.TextureArchive}.{miscFlat.TextureRecord}";
            flatIdInCategoryField.text = id;
            if (itemById.ContainsKey(id))
            {
                var item = itemById[id];
                var subcategory = subcategories[item.Subcategory].ToList();
                var indexInCategory = subcategory.FindIndex(s => s == id);

                // Show subcategory label
                var subcategoryLabel = rmbMiscFlatPropsElement.Query<Label>("flat-subcategory").First();
                var isRoot = item.Subcategory.Contains("_root");
                var labelToShow = isRoot ? item.Category : $"{item.Category}/{item.Subcategory}";
                subcategoryLabel.text = labelToShow;

                previous.RegisterCallback<MouseUpEvent>(evt =>
                {
                    var previousIndex = indexInCategory != 0 ? indexInCategory - 1 : 0;
                    var previousItemId = subcategory[previousIndex];
                    ChangeId(previousItemId, go);
                });

                next.RegisterCallback<MouseUpEvent>(evt =>
                {
                    var nextIndex = indexInCategory != subcategory.Count - 1 ? indexInCategory + 1 : subcategory.Count - 1;
                    var nextItemId = subcategory[nextIndex];
                    ChangeId(nextItemId, go);
                });
            }

            modify.RegisterCallback<MouseUpEvent>(evt =>
            {
                var currentFlat = go.GetComponent<MiscFlat>();
                modifyFlat(currentFlat.TextureArchive + "." + currentFlat.TextureRecord);
            });

            return rmbMiscFlatPropsElement;
        }

        private static void ChangeId(string flatId, GameObject oldGo)
        {
            var currentMiscFlat = oldGo.GetComponent<MiscFlat>();
            var record = currentMiscFlat.GetRecord();
            var parts = flatId.Split('.');
            record.TextureArchive = int.Parse(parts[0]);
            record.TextureRecord = int.Parse(parts[1]);

            try
            {
                var subRecordRotation = Quaternion.AngleAxis(0, Vector3.up);
                var newGo = RmbBlockHelper.AddFlatObject(record, subRecordRotation);
                newGo.transform.parent = oldGo.transform.parent;
                newGo.AddComponent<MiscFlat>().CreateObject(record);

                Object.DestroyImmediate(oldGo);
                Selection.SetActiveObjectWithContext(newGo, null);
            }
            catch (Exception error)
            {
                Debug.LogError(error);
            }
        }
    }
}