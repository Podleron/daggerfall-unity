using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace DaggerfallWorkshop.Game.Addons.RmbBlockEditor
{
    public class Catalog
    {
        private string SettingsDirectory;
        private string SettingsFileName;
        private string DefaultCatalogPath;
        private string directoryPath;
        private string filePath;

        [JsonProperty] private List<CatalogItem> _list;
        private Dictionary<string, CatalogItem> _items;
        private Dictionary<string, HashSet<string>> _subcategories;
        private Dictionary<string, HashSet<string>> _categories;

        public Catalog(string settingsDirectory, string settingsFileName, string defaultCatalogPath)
        {
            SettingsDirectory = settingsDirectory;
            SettingsFileName = settingsFileName;
            DefaultCatalogPath = defaultCatalogPath;
            directoryPath = Application.dataPath + SettingsDirectory;
            filePath = directoryPath + SettingsFileName;
        }

        public void Load()
        {
            try
            {
                StreamReader reader = new StreamReader(this.filePath);
                var data = reader.ReadToEnd();
                try
                {
                    var deserialized = JsonConvert.DeserializeObject<Catalog>(data);
                    this._list = deserialized._list;
                    GenerateCatalogDictionaries(this._list, ref this._items,
                        ref this._subcategories,
                        ref this._categories);
                }
                catch (Exception error)
                {
                    // The file is corrupt, so save a new one
                    Save();
                }
                finally
                {
                    reader.Close();
                }
            }
            catch (Exception error)
            {
                // The file does not exist, so save the default catalog
                var path = Environment.CurrentDirectory + this.DefaultCatalogPath;
                try
                {
                    var catalogJson = File.ReadAllText(path);
                    this._list = JsonConvert.DeserializeObject<List<CatalogItem>>(catalogJson);
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                }

                Save();
            }
        }

        public void Save()
        {
            Directory.CreateDirectory(this.directoryPath);
            var writer = File.CreateText(this.filePath);
            var fileContent = JsonConvert.SerializeObject(this);

            writer.Write(fileContent);
            writer.Close();
        }

        // setters
        public void Set(List<CatalogItem> list)
        {
            this._list = list;
            Save();
        }

        // getters
        public List<CatalogItem> List()
        {
            return this._list;
        }

        public Dictionary<string, CatalogItem> ItemsDictionary()
        {
            return this._items;
        }

        public Dictionary<string, HashSet<string>> SubcatalogDictionary()
        {
            return this._subcategories;
        }

        public Dictionary<string, HashSet<string>> CatalogDictionary()
        {
            return this._categories;
        }

        // helper
        public static void GenerateCatalogDictionaries(List<CatalogItem> catalog,
            ref Dictionary<string, CatalogItem> catalogItems,
            ref Dictionary<string, HashSet<string>> subcategories, ref Dictionary<string, HashSet<string>> categories)
        {
            catalogItems = new Dictionary<string, CatalogItem>();
            subcategories = new Dictionary<string, HashSet<string>>();
            categories = new Dictionary<string, HashSet<string>>();

            foreach (var t in catalog)
            {
                var catalogItem = t;

                // If the item has no category or no subcategory, fill these fields
                if (catalogItem.Category == "")
                {
                    catalogItem.Category = "Other";
                }

                if (catalogItem.Subcategory == "")
                {
                    catalogItem.Subcategory = $"{catalogItem.Category}_root";
                }

                // Save the item in a Dictionary for fast referencing
                catalogItems[catalogItem.ID] = catalogItem;

                // If the subcategory list of objects does not exist, create it...
                if (!subcategories.ContainsKey(catalogItem.Subcategory))
                {
                    subcategories.Add(catalogItem.Subcategory, new HashSet<string>());
                }

                // ...and add this item's id to it
                subcategories[catalogItem.Subcategory].Add(catalogItem.ID);

                // If the category's list of subcategories does not exist, create it...
                if (!categories.ContainsKey(catalogItem.Category))
                {
                    categories.Add(catalogItem.Category, new HashSet<string>());
                }

                // ...and add this subcategory to it
                categories[catalogItem.Category].Add(catalogItem.Subcategory);
            }
        }
    }
}