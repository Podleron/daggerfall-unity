// Project:         Daggerfall Tools For Unity
// Copyright:       Copyright (C) 2009-2022 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Podleron (podleron@gmail.com)

using System;
using System.Collections.Generic;
using DaggerfallWorkshop.Game.Addons.RmbBlockEditor.Elements;
using DaggerfallWorkshop.Game.Utility.WorldDataEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace DaggerfallWorkshop.Game.Addons.RmbBlockEditor
{
    public class ModifyMiscFlatEditor
    {
        private VisualElement visualElement;
        private string objectId;
        private ObjectPicker pickerObject;
        private readonly GameObject oldGo;
        private List<CatalogItem> catalog;

        public ModifyMiscFlatEditor(GameObject oldGo, string objectId)
        {
            visualElement = new VisualElement();
            this.objectId = objectId;
            this.oldGo = oldGo;
            catalog = PersistedFlatsCatalog.List();
            RenderTemplate();
            RenderObjectPicker();
            BindApplyButton();
        }

        public VisualElement Render()
        {
            catalog = PersistedFlatsCatalog.List();
            RenderObjectPicker();
            return visualElement;
        }

        private void RenderTemplate()
        {
            var tree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/Game/Addons/RmbBlockEditor/Editor/ModifyMiscFlatEditor/Template.uxml");
            visualElement.Add(tree.CloneTree());
        }

        private void RenderObjectPicker()
        {
            var modelPicker = visualElement.Query<VisualElement>("object-picker").First();
            modelPicker.Clear();

            pickerObject =
                new ObjectPicker(catalog, OnItemSelected, GetPreview, objectId);
            modelPicker.Add(pickerObject.visualElement);
        }

        private void BindApplyButton()
        {
            var button = visualElement.Query<Button>("apply-modification").First();
            button.RegisterCallback<MouseUpEvent>(evt =>
            {
                Modify(objectId);
            });
        }

        private void Modify(string flatId)
        {
            var currentMiscFlat = oldGo.GetComponent<MiscFlat>();
            var record = currentMiscFlat.GetRecord();

            var dot = Char.Parse(".");
            var splitId = flatId.Split(dot);

            record.TextureArchive = int.Parse(splitId[0]);
            record.TextureRecord = int.Parse(splitId[1]);

            try
            {
                var newGo = RmbBlockHelper.AddFlatObject(flatId);
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

        private void OnItemSelected(string objectId)
        {
            this.objectId = objectId;
        }

        private VisualElement GetPreview(string id)
        {
            var go = RmbBlockHelper.AddFlatObject(id);
            return new GoPreview(go);
        }
    }
}