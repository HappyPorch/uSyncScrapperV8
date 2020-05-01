using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using uSyncScrapper.Context;
using uSyncScrapper.Models;

namespace uSyncScrapper.Mappers
{
    public static class ContentTypeMapper
    {
        public static IReadOnlyList<ContentType> Map(this IReadOnlyList<XDocument> contentTypes, IReadOnlyList<XDocument> dataTypes,
            IReadOnlyList<XDocument> blueprints)
        {
            var result = contentTypes
                .Select(i => MapFirstPass(i, contentTypes, dataTypes, blueprints))
                .ToList();

            var compositions = result
                .Where(i => i.IsComposition)
                .ToList();

            result = result
                .Select(i => MapSecondPass(i, contentTypes, dataTypes, blueprints, compositions))
                .ToList();

            result = result
                .Select(i => MapThirdPass(i, contentTypes, dataTypes, blueprints, compositions))
                .ToList();

            return result;
        }

        private static ContentType MapFirstPass(XDocument doc, IReadOnlyList<XDocument> contentTypes, IReadOnlyList<XDocument> dataTypes,
            IReadOnlyList<XDocument> blueprints)
        {
            return new ContentType
            {
                Name = doc.Root?.Element("Info")?.Element("Name")?.Value,
                Alias = doc.Root?.Attribute("Alias")?.Value,
                Key = doc.Root?.Attribute("Key")?.Value,
                IsComposition = doc.Root?.Element("Info")?.Element("Folder")?.Value == "Compositions",

                ChildDocTypes = doc
                    .Root
                    ?.Element("Structure")
                    ?.Elements("ContentType")
                    .Select(i => i.Value),

                Description = doc
                    .Root
                    ?.Element("Info")
                    ?.Element("Description")
                    ?.Value,

                CompositionKeys = doc
                    .Root?
                    .Element("Info")?
                    .Element("Compositions")?
                    .Elements("Composition")
                    .Select(i => (string)i.Attribute("Key")),

                TabsSelf = doc
                    .Root?
                    .Element("Tabs")?
                    .Elements("Tab")
                    .Select(i => new Tab
                    {
                        Caption = i.Element("Caption")?.Value,
                        SortOrder = int.Parse(i.Element("SortOrder")?.Value ?? string.Empty)
                    }),

                PropertiesSelf = doc
                    .Root?
                    .Element("GenericProperties")?
                    .Elements("GenericProperty")
                    .Select(i => new DocumentTypeProperty
                    {
                        Name = i.Element("Name")?.Value,
                        Alias = i.Element("Alias")?.Value,
                        Text = i.Element("Description")?.Value,
                        Tab = i.Element("Tab")?.Value,
                        SortOrder = int.Parse(i.Element("SortOrder")?.Value ?? string.Empty),
                        Type = i.Element("Type")?.Value,
                        Definition = i.Element("Definition")?.Value
                    })
            };
        }

        private static ContentType MapSecondPass(ContentType contentType, IReadOnlyList<XDocument> contentTypes,
             IReadOnlyList<XDocument> dataTypes, IReadOnlyList<XDocument> blueprints, IReadOnlyList<ContentType> compositions)
        {
            contentType.Compositions = contentType.CompositionKeys
                .Select(i => compositions.Single(c => c.Key == i))
                .Where(i => !Constants.CompositionAliasToIgnore.Contains(i.Alias));
            return contentType;
        }

        private static ContentType MapThirdPass(ContentType contentType, IReadOnlyList<XDocument> contentTypes,
            IReadOnlyList<XDocument> dataTypes, IReadOnlyList<XDocument> blueprints, IReadOnlyList<ContentType> compositions)
        {
            contentType.Properties = contentType.PropertiesSelf
                .Union(contentType.Compositions.SelectMany(c => c.PropertiesSelf))
                .Select(p => new { Property = p, Tab = contentType.Tabs.Single(t => t.Caption == p.Tab) })
                .OrderBy(i => i.Tab.SortOrder)
                .ThenBy(i => i.Property.SortOrder)
                .Select(i => i.Property);
            return contentType;
        }
    }
}
