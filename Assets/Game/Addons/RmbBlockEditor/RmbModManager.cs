using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Utility.AssetInjection;
using FullSerializer;
using UnityEditor;
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
        static Dictionary<string, HashSet<string>> PackagedModelIds;
        static Dictionary<string, HashSet<string>> PackagedTextureIds;

        static Dictionary<string, Component[]> PackagedModels;
        static Dictionary<string, Texture2D> PackagedTextures;
        static Dictionary<string, TextAsset> PackagedXmls;

        private static Dictionary<string, string> DevModModelPaths;
        private static Dictionary<string, string> DevModBillboardImagePaths;
        private static Dictionary<string, string> DevModBillboardXmlPaths;

        public static void LoadDevModInfos()
        {
            InstantiateDevModDictionaries();

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
                    var isCustomModel = subFile.EndsWith(".obj") || subFile.EndsWith(".fbx") ||
                                        subFile.EndsWith(".prefab");
                    var isCustomBillboard = IsCustomBillboardImage(subFile);
                    var isCustomXml = IsCustomBillboardXML(subFile);

                    var fileName = Path.GetFileNameWithoutExtension(subFile);
                    if (isCustomModel)
                    {
                        modelIds.Add(fileName);
                        if (!DevModModelPaths.ContainsKey(fileName))
                        {
                            DevModModelPaths.Add(fileName, subFile);
                        }
                    }
                    else if (isCustomBillboard)
                    {
                        var id = FileToBillboardId(fileName);
                        textureIds.Add(id);
                        if (!DevModBillboardImagePaths.ContainsKey(fileName))
                        {
                            DevModBillboardImagePaths.Add(fileName, subFile);
                        }
                    }
                    else if (isCustomXml)
                    {
                        if (!DevModBillboardXmlPaths.ContainsKey(fileName))
                        {
                            DevModBillboardXmlPaths.Add(fileName, subFile);
                        }
                    }
                }

                DevModInfo.Add(modInfo.ModTitle, modInfo);
                DevModModels.Add(modInfo.ModTitle, modelIds);
                DevModTextures.Add(modInfo.ModTitle, textureIds);
            }
        }

        public static void LoadPackagedMods()
        {
            InstantiatePackagedModsDictionaries();

            foreach (string file in Directory.EnumerateFiles(
                         Path.Combine(Application.dataPath, "StreamingAssets", "Mods"), "*.dfmod"))
            {
                AssetBundle bundle = null;
                try
                {
                    bundle = AssetBundle.LoadFromFile(file);
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
                        var fileName = Path.GetFileNameWithoutExtension(subFile);
                        if (IsCustomModel(subFile))
                        {
                            modelIds.Add(fileName);
                            var go = bundle.LoadAsset<GameObject>(subFile);
                            var meshFilter = go.GetComponent<MeshFilter>();
                            var meshRenderer = go.GetComponent<MeshRenderer>();
                            PackagedModels.Add(fileName, new Component[] { meshFilter, meshRenderer });
                        }
                        else if (IsCustomBillboardImage(subFile))
                        {
                            var id = FileToBillboardId(fileName);
                            flatIds.Add(id);
                            PackagedTextures.Add(fileName, bundle.LoadAsset<Texture2D>(subFile));
                        }
                        else if (IsCustomBillboardXML(subFile))
                        {
                            var xml = bundle.LoadAsset<TextAsset>(subFile);
                            PackagedXmls.Add(fileName, xml);
                        }
                    }

                    PackagedModInfo.Add(modInfo.ModTitle, modInfo);
                    PackagedModelIds.Add(modInfo.ModTitle, modelIds);
                    PackagedTextureIds.Add(modInfo.ModTitle, flatIds);
                }
                catch (Exception err)
                {
                    Debug.Log(err);
                }
                finally
                {
                    bundle?.Unload(false);
                }
            }
        }

        public static List<CatalogItem> GetCustomCatalogModels()
        {
            LoadPackagedMods();
            LoadDevModInfos();
            var devMods = DevModInfo.Keys.ToList();
            var packagedMods = PackagedModInfo.Keys.ToList();
            var allIds = new List<CatalogItem>();

            foreach (var mod in packagedMods)
            {
                var models = PackagedModelIds[mod];
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
            LoadPackagedMods();
            LoadDevModInfos();
            var devMods = DevModModels.Keys.ToList();
            var packagedMods = PackagedModInfo.Keys.ToList();
            var allIds = new List<CatalogItem>();

            foreach (var mod in packagedMods)
            {
                var flats = PackagedTextureIds[mod];
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

        public static GameObject GetCustomBillboard(string id)
        {
            var go = GetDevModBillboard(id);
            return go == null ? GetPackagedModBillboard(id) : go;
        }

        public static GameObject GetCustomModel(string id)
        {
            var go = GetDevModModel(id);
            return go == null ? GetPackagedModModel(id) : go;
        }

        private static GameObject GetDevModModel(string id)
        {
            if (!DevModModelPaths.ContainsKey(id)) return null;
            var subFile = DevModModelPaths[id];
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(subFile);
            var go = Object.Instantiate(asset);
            var goTransform = go.GetComponent<Transform>();
            goTransform.position = Vector3.zero;
            goTransform.localPosition = Vector3.zero;
            go.name = $"Custom Daggerfall Mesh [ID={id}]";
            var runtimeMaterial = go.GetComponent<RuntimeMaterials>();
            var renderer = go.GetComponent<Renderer>();
            if (runtimeMaterial == null) return go;

            // RuntimeMaterials do not show up in the editor so we need to apply some normal materials to the renderer
            try
            {
                // Use reflection to read the private variable 'Materials' from the RuntimeMaterials Component
                var materials =
                    typeof(RuntimeMaterials).GetField("Materials", BindingFlags.NonPublic | BindingFlags.Instance)
                        .GetValue(runtimeMaterial) as RuntimeMaterial[];

                // Iterate through the runtime materials and create normal materials from them
                var materialsToApply = new Material [materials.Length];
                for (var index = 0; index < materials.Length; index++)
                {
                    var material = materials[index];
                    var resolvedMaterial = GetMaterialFromRuntimeMaterial(material);
                    materialsToApply[index] = resolvedMaterial;
                }

                // apply the materials to the renderer
                renderer.materials = materialsToApply;
            }
            catch (Exception err)
            {
                Debug.Log(err);
            }

            return go;
        }

        private static GameObject GetPackagedModModel(string id)
        {
            var components = PackagedModels.ContainsKey(id) ? PackagedModels[id] : null;
            if (components == null) return null;

            try
            {
                var go = new GameObject($"Custom DaggerfallMesh [ID={id}]");
                var savedMeshFilter = components[0] as MeshFilter;
                var savedMeshRenderer = components[1] as MeshRenderer;

                var meshFilter = go.AddComponent<MeshFilter>();
                var meshRenderer = go.AddComponent<MeshRenderer>();

                meshFilter.sharedMesh = savedMeshFilter.sharedMesh;
                meshRenderer.sharedMaterial = savedMeshRenderer.sharedMaterial;

                return go;
            }
            catch (Exception err)
            {
                Debug.Log(err);
            }

            return null;
        }

        public static GameObject GetPackagedModBillboard(string id)
        {
            var parts = id.Split('.');
            var fileName = TextureReplacement.GetName(int.Parse(parts[0]), int.Parse(parts[1]));
            Texture2D texture = null;
            if (PackagedTextures.ContainsKey(fileName))
            {
                texture = PackagedTextures[fileName];
            }

            var scale = Vector2.one;
            if (PackagedXmls.ContainsKey(fileName))
            {
                scale = GetScale(PackagedXmls[fileName]);
            }

            if (texture == null)
            {
                return null;
            }

            var go = new GameObject($"Custom Billboard {id}");
            var x = texture.width * scale.x * MeshReader.GlobalScale;
            var y = texture.height * scale.y * MeshReader.GlobalScale;

            var newScale = new Vector2(x, y);
            var billboard = go.AddComponent<DaggerfallBillboard>();
            billboard.SetMaterial(texture, newScale);

            return go;
        }

        public static GameObject GetDevModBillboard(string id)
        {
            // Get the file id
            var parts = id.Split('.');
            var fileId = TextureReplacement.GetName(int.Parse(parts[0]), int.Parse(parts[1]));

            // If the file id is not found, return null
            if (!DevModBillboardImagePaths.ContainsKey(fileId)) return null;

            // Load the texture from the AssetDatabase
            var imageSubFile = DevModBillboardImagePaths[fileId];
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(imageSubFile);

            // Check if there is an XML file for the same id
            var scale = Vector2.one;
            if (DevModBillboardXmlPaths.ContainsKey(fileId))
            {
                var xmlSubFile = DevModBillboardXmlPaths[fileId];
                var xml = AssetDatabase.LoadAssetAtPath<TextAsset>(xmlSubFile);
                scale = GetScale(xml);
            }

            // Create the game object
            var go = new GameObject($"Custom Billboard {id}");
            var x = texture.width * scale.x * MeshReader.GlobalScale;
            var y = texture.height * scale.y * MeshReader.GlobalScale;

            var newScale = new Vector2(x, y);
            var billboard = go.AddComponent<DaggerfallBillboard>();
            billboard.SetMaterial(texture, newScale);

            return go;
        }

        private static void InstantiatePackagedModsDictionaries()
        {
            PackagedModInfo = new Dictionary<string, ModInfo>(StringComparer.OrdinalIgnoreCase);
            PackagedModelIds = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            PackagedTextureIds = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            PackagedModels = new Dictionary<string, Component[]>(StringComparer.OrdinalIgnoreCase);
            PackagedTextures = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
            PackagedXmls = new Dictionary<string, TextAsset>(StringComparer.OrdinalIgnoreCase);
        }

        private static void InstantiateDevModDictionaries()
        {
            DevModInfo = new Dictionary<string, ModInfo>(StringComparer.OrdinalIgnoreCase);
            DevModModels = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            DevModTextures = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            DevModModelPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            DevModBillboardImagePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            DevModBillboardXmlPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        private static Material GetMaterialFromRuntimeMaterial(RuntimeMaterial runtimeMaterial)
        {
            int archive = runtimeMaterial.Archive;
            int record = runtimeMaterial.Record;

            var climate = PersistedSettings.ClimateBases();
            var season = PersistedSettings.ClimateSeason();


            if (runtimeMaterial.ApplyClimate)
                archive = ClimateSwaps.ApplyClimate(archive, record, climate, season);

            return DaggerfallUnity.Instance.MaterialReader.GetMaterial(archive, record);
        }

        private static Vector2 GetScale(TextAsset xml)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml.text);

            var infoElement = xmlDoc.SelectSingleNode("info");
            if (infoElement == null)
            {
                return Vector2.one;
            }

            // Get the "scaleX" element if it exists
            var scaleX = 1f;
            var scaleXElement = infoElement.SelectSingleNode("scaleX");
            if (scaleXElement != null)
            {
                float.TryParse(scaleXElement.InnerText, NumberStyles.Float, CultureInfo.InvariantCulture, out scaleX);
            }

            // Get the "scaleY" element if it exists
            var scaleY = 1f;
            var scaleYElement = infoElement.SelectSingleNode("scaleY");
            if (scaleYElement != null)
            {
                float.TryParse(scaleYElement.InnerText, NumberStyles.Float, CultureInfo.InvariantCulture, out scaleY);
            }

            return new Vector2(scaleX, scaleY);
        }

        private static bool IsCustomModel(string filePath)
        {
            return filePath.EndsWith(".prefab") && int.TryParse(Path.GetFileNameWithoutExtension(filePath), out int _);
        }

        private static bool IsCustomBillboardImage(string filePath)
        {
            Regex r = new Regex(@"/\d+_\d+-\d+\.(jpg|png)");
            return r.IsMatch(filePath);
        }

        private static bool IsCustomBillboardXML(string filePath)
        {
            Regex r = new Regex(@"/\d+_\d+-\d+\.(xml)");
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