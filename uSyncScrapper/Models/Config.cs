using System.Collections.Generic;

namespace uSyncScrapper.Models
{
    public class Config
    {
        public IEnumerable<ContentType2> ContentTypes { get; set; }
        public int? MinItems { get; set; }
        public int? MaxItems { get; set; }
    }
}