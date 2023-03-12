using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Utility;
using FullSerializer;
using UnityEditor;
using UnityEditor.Localization.Plugins.XLIFF.V20;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DaggerfallWorkshop.Game.Addons.RmbBlockEditor
{
#if UNITY_EDITOR
    public static class RMBModManager
    {
        static Dictionary<string, ModInfo> DevModInfo;
        private static Dictionary<string, HashSet<string>> DevModModels;
        private static Dictionary<string, HashSet<string>> DevModTextures;

        static Dictionary<string, ModInfo> PackagedModInfo;
        static Dictionary<string, HashSet<string>> PackagedModModels;
        static Dictionary<string, HashSet<string>> PackagedModTextures;

        public static void LoadDevModInfos()
        {
            if (DevModInfo != null)
                return;

            DevModInfo = new Dictionary<string, ModInfo>(StringComparer.OrdinalIgnoreCase);
            DevModModels = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            DevModTextures = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            string modsfolder = Path.Combine(Application.dataPath, "Game", "Mods");
            foreach (var directory in Directory.EnumerateDirectories(modsfolder))
            {
                string foundFile = Directory.EnumerateFiles(directory)
                    .FirstOrDefault(file => file.EndsWith(".dfmod.json"));
                if (string.IsNullOrEmpty(foundFile))
                    continue;

                ModInfo modInfo = null;
                if (ModManager._serializer.TryDeserialize(fsJsonParser.Parse(File.ReadAllText(foundFile)), ref modInfo)
                    .Failed)
                    continue;

                var modelIds = new HashSet<string>();
                var textureIds = new HashSet<string>();

                foreach (var subFile in modInfo.Files)
                {
                    var isCustomModel = subFile.EndsWith(".obj") || subFile.EndsWith(".fbx");
                    var isCustomTexture = subFile.EndsWith(".png") || subFile.EndsWith(".jpg");
                    var id = Path.GetFileNameWithoutExtension(subFile);
                    if (isCustomModel)
                    {
                        modelIds.Add(id);
                    }
                    else if (isCustomTexture)
                    {
                        textureIds.Add(id);
                    }
                }

                DevModInfo.Add(modInfo.ModTitle, modInfo);
                DevModModels.Add(modInfo.ModTitle, modelIds);
                DevModTextures.Add(modInfo.ModTitle, textureIds);
            }
        }

        public static void LoadPackagedMods()
        {
            if (PackagedModInfo != null)
                return;

            PackagedModInfo = new Dictionary<string, ModInfo>(StringComparer.OrdinalIgnoreCase);
            PackagedModModels = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            PackagedModTextures = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (string file in Directory.EnumerateFiles(
                         Path.Combine(Application.dataPath, "StreamingAssets", "Mods"), "*.dfmod"))
            {
                AssetBundle bundle = AssetBundle.LoadFromFile(file);
                if (bundle == null)
                    continue;

                string dfmodAssetName = bundle.GetAllAssetNames()
                    .FirstOrDefault(assetName => assetName.EndsWith(".dfmod.json"));
                if (string.IsNullOrEmpty(dfmodAssetName))
                    continue;

                TextAsset dfmodAsset = bundle.LoadAsset<TextAsset>(dfmodAssetName);
                if (dfmodAsset == null)
                    continue;

                ModInfo modInfo = null;
                if (ModManager._serializer.TryDeserialize(fsJsonParser.Parse(dfmodAsset.text), ref modInfo).Failed)
                    continue;

                var modelIds = new HashSet<string>();
                var flatIds = new HashSet<string>();

                foreach (var subFile in modInfo.Files)
                {
                    var id = Path.GetFileNameWithoutExtension(subFile);
                    if (IsCustomModel(subFile))
                    {
                        modelIds.Add(id);
                    }
                    else if (IsCustomBillboard(subFile))
                    {
                        flatIds.Add(FileToBillboardId(id));
                    }
                }

                PackagedModInfo.Add(modInfo.ModTitle, modInfo);
                PackagedModModels.Add(modInfo.ModTitle, modelIds);
                PackagedModTextures.Add(modInfo.ModTitle, flatIds);

                bundle.Unload(false);
            }
        }

        public static List<CatalogItem> GetCustomCatalogModels()
        {
            LoadDevModInfos();
            LoadPackagedMods();
            var devMods = DevModInfo.Keys.ToList();
            var packagedMods = PackagedModInfo.Keys.ToList();
            var allIds = new List<CatalogItem>();

            foreach (var mod in packagedMods)
            {
                var models = PackagedModModels[mod];
                foreach (var id in models)
                {
                    allIds.Add(new CatalogItem(id, id, "Mods", mod));
                }
            }

            foreach (var mod in devMods)
            {
                var models = DevModModels[mod];
                foreach (var id in models)
                {
                    allIds.Add(new CatalogItem(id, id, "Mods", mod));
                }
            }

            return allIds;
        }

        public static List<CatalogItem> GetCustomCatalogFlats()
        {
            LoadDevModInfos();
            LoadPackagedMods();
            var devMods = DevModModels.Keys.ToList();
            var packagedMods = PackagedModInfo.Keys.ToList();
            var allIds = new List<CatalogItem>();

            foreach (var mod in packagedMods)
            {
                var flats = PackagedModTextures[mod];
                foreach (var id in flats)
                {
                    allIds.Add(new CatalogItem(id, id, "Mods", mod));
                }
            }

            foreach (var mod in devMods)
            {
                var flats = DevModTextures[mod];
                foreach (var id in flats)
                {
                    allIds.Add(new CatalogItem(id, id, "Mods", mod));
                }
            }

            return allIds;
        }

        private static bool IsCustomModel(string filePath)
        {
            return filePath.EndsWith(".prefab") && int.TryParse(Path.GetFileNameWithoutExtension(filePath), out int _);
        }

        private static bool IsCustomBillboard(string filePath)
        {
            Regex r = new Regex(@"/\d+_\d+-\d+\.(jpg|png)");
            return r.IsMatch(filePath);
        }

        private static string FileToBillboardId(string id)
        {
            var parts = id.Split('-');
            return parts[0].Replace('_', '.');
        }
    }
#endif
}