using System.Collections.Generic;

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