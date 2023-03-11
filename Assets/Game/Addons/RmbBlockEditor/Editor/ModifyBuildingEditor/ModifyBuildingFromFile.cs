using System;
using DaggerfallWorkshop.Game.Addons.RmbBlockEditor.BuildingPresets;
using DaggerfallWorkshop.Game.Addons.RmbBlockEditor.Elements;
using DaggerfallWorkshop.Utility.AssetInjection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DaggerfallWorkshop.Game.Addons.RmbBlockEditor
{
    public class ModifyBuildingFromFile
    {
        private const string WorldDataFolder = "/StreamingAssets/WorldData/";
        private VisualElement visualElement;
        private BuildingReplacementData building;
        private Action<BuildingReplacementData, Boolean, Boolean> onModify;

        public ModifyBuildingFromFile(Action<BuildingReplacementData, Boolean, Boolean> onModify)
        {
            visualElement = new VisualElement();
            this.onModify = onModify;
            RenderTemplate();
            RenderFileLoader();
        }

        public VisualElement Render()
        {
            return visualElement;
        }

        private void RenderTemplate()
        {
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/Game/Addons/RmbBlockEditor/Editor/ModifyBuildingEditor/BuildingFromFile.uxml");
            visualElement.Add(tree.CloneTree());
        }

        private void RenderFileLoader()
        {
            var loadFile = visualElement.Query<Button>("load-file").First();
            loadFile.RegisterCallback<MouseUpEvent>(evt =>
                {
                    LoadFile();
                    RenderApplyBox();
                },
                TrickleDown.TrickleDown);
        }

        private void RenderApplyBox()
        {
            var box = visualElement.Query<Box>("apply-modification-box").First();
            box.RemoveFromClassList("hidden");

            var interior = visualElement.Query<Toggle>("interior").First();
            var exterior = visualElement.Query<Toggle>("exterior").First();

            var button = visualElement.Query<Button>("apply-modification-from-file").First();
            button.RegisterCallback<MouseUpEvent>(evt =>
            {
                onModify(building, interior.value, exterior.value);
            });
        }

        private void LoadFile()
        {
            var path = EditorUtility.OpenFilePanel("Open Building File", WorldDataFolder, "json");
            if (String.IsNullOrEmpty(path))
            {
                return;
            }

            try
            {
                // Load the file
                building = RmbBlockHelper.LoadBuildingFile(path);

                // Show the preview
                var element = visualElement.Query<VisualElement>("file-preview").First();
                var previewGameObject = BuildingPreset.AddBuildingPlaceholder(building.RmbSubRecord);
                var preview = new GoPreview(previewGameObject);
                element.Clear();
                element.Add(preview);
            }
            catch (ArgumentException e)
            {
                Debug.LogError(e.Message);
            }
        }
    }
}