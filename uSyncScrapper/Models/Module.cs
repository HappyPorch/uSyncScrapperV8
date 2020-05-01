namespace uSyncScrapper.Models
{
    public class Module
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public string NcContentTypeAlias { get; set; }
        public DocumentType ContentType { get; set; }
        public string Link => $"module-{NcContentTypeAlias}";
    }
}