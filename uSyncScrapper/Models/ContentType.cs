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
        public IReadOnlyList<DocumentTypeProperty> PropertiesSelf { get; set; }
        public IReadOnlyList<DocumentTypeProperty> Properties { get; set; }
        public IReadOnlyList<string> ParentDocTypes { get; set; }
        public IReadOnlyList<string> ChildDocTypes { get; set; }
        public IReadOnlyList<ContentType> Compositions { get; set; }
        public IReadOnlyList<string> CompositionKeys { get; set; }
        public IReadOnlyList<Tab> TabsSelf { get; set; }
        public IReadOnlyList<Tab> Tabs => TabsSelf.Union(Compositions.SelectMany(c => c.TabsSelf)).OrderBy(i => i.SortOrder).ToList();
        public bool IsComposition { get; set; }
    }
}