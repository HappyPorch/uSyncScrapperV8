using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uSyncScrapper.Models
{
    public class Config
    {
        public IEnumerable<ContentType> ContentTypes { get; set; }
        public int? MinItems { get; set; }
        public int? MaxItems { get; set; }
    }
}
