using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uSyncScrapper.Models
{
    public class DocumentType
    {
        public int Index { get; set; }
        public string Name { get; set; }
        public string Alias { get; set; }
        public string Description { get; set; }
        public IEnumerable<DocumentTypeProperty> Properties { get; set; }
        public IEnumerable<string> ParentDocTypes { get; set; }
        public IEnumerable<string> ChildDocTypes { get; set; }
    }
}
