using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uSyncScrapper.Models
{
    public class FinalDocument
    {
        public string DocTypesBody { get; set; }
        public IEnumerable<DocumentType> DocTypes { get; set; }
        public string ModulesBody { get; set; }
        public IEnumerable<Module> Modules { get; set; }
    }
}
