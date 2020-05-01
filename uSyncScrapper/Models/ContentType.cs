using System.Collections.Generic;
using System.Linq;

namespace uSyncScrapper.Models
{
    public class ContentType
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public string Alias { get; set; }
        public string Description { get; set; }
        public string Notes { get; set; }
        public IEnumerable<DocumentTypeProperty> PropertiesSelf { get; set; }
        public IEnumerable<DocumentTypeProperty> Properties { get; set; }
        public IEnumerable<string> ParentDocTypes { get; set; }
        public IEnumerable<string> ChildDocTypes { get; set; }
        public IEnumerable<ContentType> Compositions { get; set; }
        public IEnumerable<string> CompositionKeys { get; set; }
        public IEnumerable<Tab> TabsSelf { get; set; }
        public IEnumerable<Tab> Tabs => TabsSelf.Union(Compositions.SelectMany(c => c.TabsSelf)).OrderBy(i => i.SortOrder);
        public bool IsComposition { get; set; }
    }
}