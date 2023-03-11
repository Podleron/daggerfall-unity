using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using Newtonsoft.Json;
using UnityEngine;

namespace DaggerfallWorkshop.Game.Addons.RmbBlockEditor
{
    public class PersistedFlatsCatalog
    {
        private const string SettingsDirectory = "/Editor/Settings/RmbBlockEditor/";
        private const string SettingsFileName = "flats-catalog.json";
        private const string DefaultCatalogPath =
            "/Assets/Game/Addons/RmbBlockEditor/Editor/Catalogs/FlatsCatalogEditor/DefaultFlatsCatalog.json";

        private PersistedFlatsCatalog()
        {
        }

        private static readonly Catalog _catalog = new Catalog(SettingsDirectory, SettingsFileName,
            DefaultCatalogPath);

        public static void Load()
        {
            _catalog.Load();
        }

        public static void Save()
        {
            _catalog.Save();
        }

        public static void Set(List<CatalogItem> catalogList)
        {
            _catalog.Set(catalogList);
        }

        public static List<CatalogItem> List()
        {
            return _catalog.List();
        }

        public static Dictionary<string, CatalogItem> ItemsDictionary()
        {
            return _catalog.ItemsDictionary();
        }

        public static Dictionary<string, HashSet<string>> SubcatalogDictionary()
        {
            return _catalog.SubcatalogDictionary();
        }

        public static Dictionary<string, HashSet<string>> CatalogDictionary()
        {
            return _catalog.CatalogDictionary();
        }
    }
}