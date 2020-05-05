using System;
using System.Collections.Generic;

namespace uSyncScrapper.Models
{
    public class DocumentTypeProperty : ICloneable
    {
        public string Name { get; set; }
        public string Alias { get; set; }
        public string Text { get; set; }
        public string Tab { get; set; }
        public int SortOrder { get; set; }
        public string Type { get; set; }
        public string Definition { get; set; }
        public int? MaxItems { get; set; }

        /// <summary>
        ///     Allowed documents types on this nested content property.
        /// </summary>
        public IEnumerable<NestedContentDocType> NestedContentDocTypes { get; set; }

        /// <summary>
        ///     Body modules set on this particular page.
        ///     These are computed from the blueprint (content template).
        /// </summary>
        public IEnumerable<Module> NestedContentElementsDocTypes { get; set; }


        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}