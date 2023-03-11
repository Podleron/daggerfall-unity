namespace DaggerfallWorkshop.Game.Addons.RmbBlockEditor
{
    public struct CatalogItem
    {
        public string ID;
        public string Label;
        public string Category;
        public string Subcategory;
        public string Tags;

        public CatalogItem(string ID)
        {
            this.ID = ID;
            this.Label = ID;
            this.Category = "";
            this.Subcategory = "";
            this.Tags = "";
        }

        public CatalogItem(string ID, string label)
        {
            this.ID = ID;
            this.Label = label;
            this.Category = "";
            this.Subcategory = "";
            this.Tags = "";
        }

        public CatalogItem(string ID, string label, string Category)
        {
            this.ID = ID;
            this.Label = label;
            this.Category = Category;
            this.Subcategory = "";
            this.Tags = "";
        }

        public CatalogItem(string ID, string Label, string Category, string Subcategory)
        {
            this.ID = ID;
            this.Label = Label;
            this.Category = Category;
            this.Subcategory = Subcategory;
            this.Tags = "";
        }

        public CatalogItem(string ID, string Label, string Category, string Subcategory, string Tags)
        {
            this.ID = ID;
            this.Label = Label;
            this.Category = Category;
            this.Subcategory = Subcategory;
            this.Tags = Tags;
        }
    }
}