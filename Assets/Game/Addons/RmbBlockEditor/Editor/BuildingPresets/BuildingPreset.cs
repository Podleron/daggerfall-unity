// Project:         Daggerfall Tools For Unity
// Copyright:       Copyright (C) 2009-2022 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Podleron (podleron@gmail.com)

using System;
using System.IO;
using DaggerfallConnect;
using DaggerfallWorkshop.Game.Addons.RmbBlockEditor.Elements;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Utility.AssetInjection;
using UnityEngine;
using UnityEngine.UIElements;

namespace DaggerfallWorkshop.Game.Addons.RmbBlockEditor.BuildingPresets
{
    public class BuildingPreset
    {
        private static readonly DFBlock house1 = (DFBlock)SaveLoadManager.Deserialize(typeof(DFBlock), GetFile("House1.json"));
        private static readonly DFBlock house2 = (DFBlock)SaveLoadManager.Deserialize(typeof(DFBlock), GetFile("House2.json"));
        private static readonly DFBlock house3 = (DFBlock)SaveLoadManager.Deserialize(typeof(DFBlock), GetFile("House3.json"));
        private static readonly DFBlock house4 = (DFBlock)SaveLoadManager.Deserialize(typeof(DFBlock), GetFile("House4.json"));
        private static readonly DFBlock house5 = (DFBlock)SaveLoadManager.Deserialize(typeof(DFBlock), GetFile("House5.json"));
        private static readonly DFBlock house6 = (DFBlock)SaveLoadManager.Deserialize(typeof(DFBlock), GetFile("House6.json"));
        private static readonly DFBlock houseForSale = (DFBlock)SaveLoadManager.Deserialize(typeof(DFBlock), GetFile("HouseForSale.json"));
        private static readonly DFBlock tavern = (DFBlock)SaveLoadManager.Deserialize(typeof(DFBlock), GetFile("Tavern.json"));
        private static readonly DFBlock guildHall = (DFBlock)SaveLoadManager.Deserialize(typeof(DFBlock), GetFile("GuildHall.json"));
        private static readonly DFBlock temple = (DFBlock)SaveLoadManager.Deserialize(typeof(DFBlock), GetFile("Temple.json"));
        private static readonly DFBlock furnitureStore = (DFBlock)SaveLoadManager.Deserialize(typeof(DFBlock), GetFile("FurnitureStore.json"));
        private static readonly DFBlock bank = (DFBlock)SaveLoadManager.Deserialize(typeof(DFBlock), GetFile("Bank.json"));
        private static readonly DFBlock generalStore = (DFBlock)SaveLoadManager.Deserialize(typeof(DFBlock), GetFile("GeneralStore.json"));
        private static readonly DFBlock pawnShop = (DFBlock)SaveLoadManager.Deserialize(typeof(DFBlock), GetFile("PawnShop.json"));
        private static readonly DFBlock armorer = (DFBlock)SaveLoadManager.Deserialize(typeof(DFBlock), GetFile("Armorer.json"));
        private static readonly DFBlock weaponSmith = (DFBlock)SaveLoadManager.Deserialize(typeof(DFBlock), GetFile("WeaponSmith.json"));
        private static readonly DFBlock clothingStore = (DFBlock)SaveLoadManager.Deserialize(typeof(DFBlock), GetFile("ClothingStore.json"));
        private static readonly DFBlock alchemist = (DFBlock)SaveLoadManager.Deserialize(typeof(DFBlock), GetFile("Alchemist.json"));
        private static readonly DFBlock gemStore = (DFBlock)SaveLoadManager.Deserialize(typeof(DFBlock), GetFile("GemStore.json"));
        private static readonly DFBlock bookseller = (DFBlock)SaveLoadManager.Deserialize(typeof(DFBlock), GetFile("Bookseller.json"));
        private static readonly DFBlock library = (DFBlock)SaveLoadManager.Deserialize(typeof(DFBlock), GetFile("Library.json"));
        private static readonly DFBlock palace = (DFBlock)SaveLoadManager.Deserialize(typeof(DFBlock), GetFile("Palace.json"));
        private static readonly DFBlock town23 = (DFBlock)SaveLoadManager.Deserialize(typeof(DFBlock), GetFile("Town23.json"));
        private static readonly DFBlock ship = (DFBlock)SaveLoadManager.Deserialize(typeof(DFBlock), GetFile("Ship.json"));

        public static BuildingReplacementData GetBuildingData(string buildingId)
        {
            var buildingGroupId = int.Parse(buildingId.Substring(0, 2));
            var buildingIndex = int.Parse(buildingId.Substring(2));
            DFBlock buildingGroup = GetBuildingGroup(buildingGroupId);
            DFLocation.BuildingData buildingData = buildingGroup.RmbBlock.FldHeader.BuildingDataList[buildingIndex - 1];
            DFBlock.RmbSubRecord subRecord =
                RmbBlockHelper.CloneRmbSubRecord(buildingGroup.RmbBlock.SubRecords[buildingIndex - 1]);

            return new BuildingReplacementData
            {
                FactionId = buildingData.FactionId,
                Quality = buildingData.Quality,
                BuildingType = (int)buildingData.BuildingType,
                NameSeed = buildingData.NameSeed,
                RmbSubRecord = subRecord
            };
        }

        public static GameObject AddBuildingObject(BuildingReplacementData building, Vector3 position, Vector3 rotation)
        {
            DFLocation.BuildingData buildingData = new DFLocation.BuildingData()
            {
                NameSeed = building.NameSeed, Quality = building.Quality,
                BuildingType = (DFLocation.BuildingTypes)building.BuildingType,
                FactionId = building.FactionId
            };
            DFBlock.RmbSubRecord subRecord = building.RmbSubRecord;

            subRecord.XPos = (int)position.x;
            subRecord.ZPos = (int)position.z;
            subRecord.YRotation = (short)rotation.y;
            for (var i = 0; i < subRecord.Exterior.Block3dObjectRecords.Length; i++)
            {
                var model = subRecord.Exterior.Block3dObjectRecords[i];
                subRecord.Exterior.Block3dObjectRecords[i].YPos = model.YPos + (int)position.y;
            }

            var go = new GameObject("Building From File");
            var buildingComponent = go.AddComponent<Building>();
            buildingComponent.CreateObject(buildingData, subRecord);

            return go;
        }

        public static GameObject AddBuildingObject(string buildingId, Vector3 position, Vector3 rotation)
        {
            var buildingGroupId = int.Parse(buildingId.Substring(0, 2));
            var buildingIndex = int.Parse(buildingId.Substring(2));
            DFBlock buildingGroup = GetBuildingGroup(buildingGroupId);

            DFLocation.BuildingData buildingData = buildingGroup.RmbBlock.FldHeader.BuildingDataList[buildingIndex - 1];
            DFBlock.RmbSubRecord subRecord =
                RmbBlockHelper.CloneRmbSubRecord(buildingGroup.RmbBlock.SubRecords[buildingIndex - 1]);

            subRecord.XPos = (int)position.x;
            subRecord.ZPos = (int)position.z;
            subRecord.YRotation = (short)rotation.y;
            for (var i = 0; i < subRecord.Exterior.Block3dObjectRecords.Length; i++)
            {
                var model = subRecord.Exterior.Block3dObjectRecords[i];
                subRecord.Exterior.Block3dObjectRecords[i].YPos = model.YPos + (int)position.y;
            }

            var go = new GameObject("Building " + buildingId);
            var buildingComponent = go.AddComponent<Building>();
            buildingComponent.CreateObject(buildingData, subRecord);

            return go;
        }

        public static GameObject ReplaceBuildingObject(BuildingReplacementData building, Building oldBuilding,
            Boolean useNewInterior, Boolean useNewExterior)
        {
            var oldSubRecord = oldBuilding.GetSubRecord();
            DFLocation.BuildingData buildingData = new DFLocation.BuildingData()
            {
                NameSeed = building.NameSeed,
                Quality = building.Quality,
                BuildingType = (DFLocation.BuildingTypes)building.BuildingType,
                FactionId = building.FactionId
            };

            var exterior = oldSubRecord.Exterior;

            if (useNewExterior)
            {
                exterior = building.RmbSubRecord.Exterior;
                for (var i = 0; i < exterior.Block3dObjectRecords.Length; i++)
                {
                    var model = exterior.Block3dObjectRecords[i];
                    exterior.Block3dObjectRecords[i].YPos = model.YPos + oldBuilding.ModelsYPos;
                }
            }

            DFBlock.RmbSubRecord subRecord = new DFBlock.RmbSubRecord()
            {
                XPos = oldBuilding.XPos,
                ZPos = oldBuilding.ZPos,
                YRotation = oldBuilding.YRotation,
                Interior = useNewInterior ? building.RmbSubRecord.Interior : oldSubRecord.Interior,
                Exterior = exterior
            };

            var go = new GameObject("Building From File");
            var buildingComponent = go.AddComponent<Building>();
            buildingComponent.CreateObject(buildingData, subRecord);

            return go;
        }

        public static GameObject ReplaceBuildingObject(string buildingId, Building oldBuilding, Boolean useNewInterior,
            Boolean useNewExterior)
        {
            var buildingGroupId = int.Parse(buildingId.Substring(0, 2));
            var buildingIndex = int.Parse(buildingId.Substring(2));
            DFBlock buildingGroup = GetBuildingGroup(buildingGroupId);

            DFLocation.BuildingData newBuilding = buildingGroup.RmbBlock.FldHeader.BuildingDataList[buildingIndex - 1];
            DFBlock.RmbSubRecord newSubRecord =
                RmbBlockHelper.CloneRmbSubRecord(buildingGroup.RmbBlock.SubRecords[buildingIndex - 1]);

            var oldSubRecord = oldBuilding.GetSubRecord();
            DFLocation.BuildingData buildingData = new DFLocation.BuildingData()
            {
                NameSeed = newBuilding.NameSeed,
                Quality = newBuilding.Quality,
                BuildingType = newBuilding.BuildingType,
                FactionId = newBuilding.FactionId
            };

            var exterior = oldSubRecord.Exterior;
            if (useNewExterior)
            {
                exterior = newSubRecord.Exterior;
                for (var i = 0; i < exterior.Block3dObjectRecords.Length; i++)
                {
                    var model = exterior.Block3dObjectRecords[i];
                    exterior.Block3dObjectRecords[i].YPos = model.YPos + oldBuilding.ModelsYPos;
                }
            }

            DFBlock.RmbSubRecord subRecord = new DFBlock.RmbSubRecord()
            {
                XPos = oldBuilding.XPos,
                ZPos = oldBuilding.ZPos,
                YRotation = oldBuilding.YRotation,
                Interior = useNewInterior ? newSubRecord.Interior : oldSubRecord.Interior,
                Exterior = exterior
            };

            var go = new GameObject("Building " + buildingId);
            var buildingComponent = go.AddComponent<Building>();
            buildingComponent.CreateObject(buildingData, subRecord);

            return go;
        }

        // Unlike in AddBuildingObject, we do not want to add the Building Component to this GameObject
        public static GameObject AddBuildingPlaceholder(string buildingId)
        {
            var buildingGroupId = int.Parse(buildingId.Substring(0, 2));
            var buildingIndex = int.Parse(buildingId.Substring(2));
            DFBlock buildingGroup = GetBuildingGroup(buildingGroupId);

            DFBlock.RmbSubRecord subRecord = buildingGroup.RmbBlock.SubRecords[buildingIndex - 1];

            return AddBuildingPlaceholder(subRecord);
        }

        public static GameObject AddBuildingPlaceholder(DFBlock.RmbSubRecord subRecord)
        {
            var placeholder = new GameObject();
            foreach (var blockRecord in subRecord.Exterior.Block3dObjectRecords)
            {
                var go = RmbBlockHelper.Add3dObject(blockRecord);
                go.transform.parent = placeholder.transform;
            }

            return placeholder;
        }

        public static GameObject GetExteriorPlaceholder(BuildingReplacementData buildingReplacementData)
        {
            var placeholder = new GameObject();
            if (buildingReplacementData.RmbSubRecord.Exterior.Block3dObjectRecords == null)
            {
                return placeholder;
            }
            foreach (var blockRecord in buildingReplacementData.RmbSubRecord.Exterior.Block3dObjectRecords)
            {
                var go = RmbBlockHelper.Add3dObject(blockRecord);
                go.transform.parent = placeholder.transform;
            }

            return placeholder;
        }

        public static GameObject GetInteriorPlaceholder(BuildingReplacementData buildingReplacementData)
        {
            var placeholder = new GameObject();
            if (buildingReplacementData.RmbSubRecord.Interior.Block3dObjectRecords == null)
            {
                return placeholder;
            }
            foreach (var blockRecord in buildingReplacementData.RmbSubRecord.Interior.Block3dObjectRecords)
            {
                var go = RmbBlockHelper.Add3dObject(blockRecord);
                go.transform.parent = placeholder.transform;
            }

            return placeholder;
        }

        public static VisualElement GetPreview(BuildingReplacementData buildingData)
        {
            var tabs = new Tabs();
            var tab1 = new Tab { value = 0, label = "Exterior" };
            var tab2 = new Tab { value = 1, label = "Interior" };

            var exteriorPreview = GetExteriorPreview(buildingData);
            var interiorPreview = GetInteriorPreview(buildingData);

            tab1.Add(exteriorPreview);
            tab2.Add(interiorPreview);
            tabs.Add(tab1);
            tabs.Add(tab2);

            return tabs;
        }

        public static VisualElement GetInteriorPreview(BuildingReplacementData buildingData)
        {
            try
            {
                var interior = GetInteriorPlaceholder(buildingData);
                return new GoPreview(interior);
            }
            catch (Exception e)
            {
                // The building might not have an Interior
                return new VisualElement();
            }
        }

        public static VisualElement GetExteriorPreview(BuildingReplacementData buildingData)
        {
            try
            {
                var exterior = GetExteriorPlaceholder(buildingData);
                return new GoPreview(exterior);
            }
            catch (Exception e)
            {
                // The building might not have an Exterior
                return new VisualElement();
            }
        }

        private static DFBlock GetBuildingGroup(int buildingGroupId)
        {
            DFBlock buildingGroup = new DFBlock();
            switch (buildingGroupId)
            {
                case 1:
                    buildingGroup = house1;
                    break;
                case 2:
                    buildingGroup = house2;
                    break;
                case 3:
                    buildingGroup = house3;
                    break;
                case 4:
                    buildingGroup = house4;
                    break;
                case 5:
                    buildingGroup = house5;
                    break;
                case 6:
                    buildingGroup = house6;
                    break;
                case 7:
                    buildingGroup = houseForSale;
                    break;
                case 8:
                    buildingGroup = tavern;
                    break;
                case 9:
                    buildingGroup = guildHall;
                    break;
                case 10:
                    buildingGroup = temple;
                    break;
                case 11:
                    buildingGroup = furnitureStore;
                    break;
                case 12:
                    buildingGroup = bank;
                    break;
                case 13:
                    buildingGroup = generalStore;
                    break;
                case 14:
                    buildingGroup = pawnShop;
                    break;
                case 15:
                    buildingGroup = armorer;
                    break;
                case 16:
                    buildingGroup = weaponSmith;
                    break;
                case 17:
                    buildingGroup = clothingStore;
                    break;
                case 18:
                    buildingGroup = alchemist;
                    break;
                case 19:
                    buildingGroup = gemStore;
                    break;
                case 20:
                    buildingGroup = bookseller;
                    break;
                case 21:
                    buildingGroup = library;
                    break;
                case 22:
                    buildingGroup = palace;
                    break;
                case 23:
                    buildingGroup = town23;
                    break;
                case 24:
                    buildingGroup = ship;
                    break;
            }

            return buildingGroup;
        }

        private static string GetFile(string fileName)
        {
            var path = Environment.CurrentDirectory + "/Assets/Game/Addons/RmbBlockEditor/Editor/BuildingPresets/" +
                       fileName;
            return File.ReadAllText(path);
        }
    }
}