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
    public static class Misc3dEditor
    {
        public static VisualElement Render(GameObject go, Action<uint> modify3d)
        {
            // Get serialized properties:
            var misc3d = go.GetComponent<Misc3d>();
            var so = new SerializedObject(misc3d);
            var modelIdProperty = so.FindProperty("ModelId");
            var objectTypeProperty = so.FindProperty("ObjectType");
            var posProperty = so.FindProperty("pos");
            var rotProperty = so.FindProperty("rotation");
            var scaleProperty = so.FindProperty("scale");

            // Render the template:
            var misc3dPropsElement = new VisualElement();
            var rmbMisc3dPropsTree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/Game/Addons/RmbBlockEditor/Editor/Editors/Misc3dEditor/Template.uxml");
            misc3dPropsElement.Add(rmbMisc3dPropsTree.CloneTree());

            // Select the bindable fields:
            var modelIdField = misc3dPropsElement.Query<Label>("model-id").First();
            var objectTypeField = misc3dPropsElement.Query<Label>("object-type").First();
            var posField = misc3dPropsElement.Query<Vector3Field>("pos").First();
            var rotField = misc3dPropsElement.Query<Vector3Field>("rot").First();
            var scaleField = misc3dPropsElement.Query<FloatField>("scale").First();

            var modelIdInCategoryField = misc3dPropsElement.Query<Label>("model-id-in-subcategory").First();
            var previous = misc3dPropsElement.Query<Button>("previous-model").First();
            var next = misc3dPropsElement.Query<Button>("next-model").First();
            var modify = misc3dPropsElement.Query<Button>("modify-model").First();

            // Bind the fields to the properties:
            modelIdField.BindProperty(modelIdProperty);
            modelIdInCategoryField.BindProperty(modelIdProperty);
            objectTypeField.BindProperty(objectTypeProperty);
            posField.BindProperty(posProperty);
            rotField.BindProperty(rotProperty);
            scaleField.BindProperty(scaleProperty);

            // Find the subcategory of the item
            var itemById = PersistedModelsCatalog.ItemsDictionary();
            var subcategories = PersistedModelsCatalog.SubcatalogDictionary();
            var id = misc3d.ModelId.ToString();
            if (itemById.ContainsKey(id))
            {
                var item = itemById[id];
                var subcategory = subcategories[item.Subcategory].ToList();
                var indexInCategory = subcategory.FindIndex(s => s == id);

                // Show subcategory label
                var subcategoryLabel = misc3dPropsElement.Query<Label>("model-subcategory").First();
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
                var currentMisc3d = go.GetComponent<Misc3d>();
                modify3d(currentMisc3d.ModelId);
            });

            return misc3dPropsElement;
        }

        private static void ChangeId(string modelId, GameObject oldGo)
        {
            var currentMisc3d = oldGo.GetComponent<Misc3d>();
            var record = currentMisc3d.GetRecord();
            record.ModelIdNum = uint.Parse(modelId);
            record.ModelId = modelId;

            try
            {
                var newGo = RmbBlockHelper.Add3dObject(record);
                newGo.transform.parent = oldGo.transform.parent;
                newGo.AddComponent<Misc3d>().CreateObject(record);

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