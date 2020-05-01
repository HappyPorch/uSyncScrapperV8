using System.Collections.Generic;

namespace uSyncScrapper.Models
{
    public class NestedContentDocType
    {
        public string Name { get; set; }
        public string Text { get; set; }
        public string Alias { get; set; }
        public IEnumerable<DocumentTypeProperty> Properties { get; set; }
    }
}