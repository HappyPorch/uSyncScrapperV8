using System.Collections.Generic;

namespace uSyncScrapper.Models
{
    public class TabGroupedProperties
    {
        public string Tab { get; set; }
        public IEnumerable<DocumentTypeProperty> Properties { get; set; }
    }
}