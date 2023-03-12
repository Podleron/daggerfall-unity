using System.Collections.Generic;

namespace DaggerfallWorkshop.Game.Addons.RmbBlockEditor
{
    public class PersistedModelsCatalog
    {
        private const string SettingsDirectory = "/Editor/Settings/RmbBlockEditor/";
        private const string SettingsFileName = "models-catalog.json";
        private const string DefaultCatalogPath =
            "/Assets/Game/Addons/RmbBlockEditor/Persisted/DefaultModelsCatalog.json";

        private PersistedModelsCatalog()
        {
        }

        private static readonly Catalog _catalog = new Catalog(SettingsDirectory, SettingsFileName,
            DefaultCatalogPath);


        public static Catalog Get()
        {
            return _catalog;
        }
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