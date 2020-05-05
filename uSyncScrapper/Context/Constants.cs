namespace uSyncScrapper.Context
{
    public static class Constants
    {
        public const string NestedContentTypeName = "Umbraco.NestedContent";
        public const string NestedContentElementsTypeName = "Umbraco.NestedContentElements";

        public static string[] CompositionAliasToIgnore =
        {
            "sEOComposition", "visibilityComposition",
            "redirectComposition", "markupComposition", "allowDeleteComposition", "auxiliaryFoldersComposition",
            "bodyClassComposition"
        };

        public static string[] DocTypesToIgnore = { "errorPage" };

        public struct Entities
        {
            public const string ContentTypes = "ContentTypes";
            public const string DataTypes = "DataTypes";
            public const string Blueprints = "Blueprints";
        }
    }
}
