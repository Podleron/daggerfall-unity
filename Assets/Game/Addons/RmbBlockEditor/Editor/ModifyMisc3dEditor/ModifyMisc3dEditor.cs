// Project:         Daggerfall Tools For Unity
// Copyright:       Copyright (C) 2009-2022 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Podleron (podleron@gmail.com)

using System;
using System.Collections.Generic;
using DaggerfallWorkshop.Game.Addons.RmbBlockEditor.Elements;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace DaggerfallWorkshop.Game.Addons.RmbBlockEditor
{
    public class ModifyMisc3dEditor
    {
        private VisualElement visualElement = new VisualElement();
        private string objectId;
        private ObjectPicker2 pickerObject;
        private readonly GameObject oldGo;
        private List<CatalogItem> catalog = PersistedModelsCatalog.List();

        public ModifyMisc3dEditor(GameObject oldGo, uint objectId)
        {
            this.objectId = objectId.ToString();
            this.oldGo = oldGo;
            RenderTemplate();
            RenderObjectPicker();
            BindApplyButton();
        }

        public VisualElement Render()
        {
            catalog = PersistedModelsCatalog.List();
            RenderObjectPicker();
            return visualElement;
        }

        private void RenderTemplate()
        {
            var tree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/Game/Addons/RmbBlockEditor/Editor/ModifyMisc3dEditor/Template.uxml");
            visualElement.Add(tree.CloneTree());
        }

        private void RenderObjectPicker()
        {
            var modelPicker = visualElement.Query<VisualElement>("object-picker").First();
            modelPicker.Clear();

            pickerObject =
                new ObjectPicker2(catalog, OnItemSelected, GetPreview, objectId);
            modelPicker.Add(pickerObject.visualElement);
        }

        private void BindApplyButton()
        {
            var button = visualElement.Query<Button>("apply-modification").First();
            button.RegisterCallback<MouseUpEvent>(evt =>
            {
                Modify((uint)int.Parse(objectId));
            });
        }

        private void Modify(uint modelId)
        {
            var currentMisc3d = oldGo.GetComponent<Misc3d>();
            var record = currentMisc3d.GetRecord();
            record.ModelIdNum = modelId;
            record.ModelId = modelId.ToString();

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

        private void OnItemSelected(string objectId)
        {
            this.objectId = objectId;
        }

        private VisualElement GetPreview(string modelId)
        {
            var previewObject = RmbBlockHelper.Add3dObject(modelId);
            return new GoPreview(previewObject);
        }
    }
}