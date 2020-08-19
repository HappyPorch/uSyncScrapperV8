using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Newtonsoft.Json;
using uSyncScrapper.Context;
using uSyncScrapper.Models;

namespace uSyncScrapper.Builders
{
    public static class ContentTypeBuilder
    {
        public static IReadOnlyList<ContentType> Build(this IReadOnlyList<XDocument> contentTypes, IReadOnlyList<XDocument> dataTypes,
            IReadOnlyList<XDocument> blueprints)
        {
            var result = contentTypes
                .Select(i => BuildFromXml(i, contentTypes, dataTypes, blueprints))
                .ToList();

            var compositions = result
                .Where(i => i.IsComposition)
                .ToList();

            result = result
                .Select(i => BuildCompositions(i, contentTypes, dataTypes, blueprints, compositions))
                .ToList();

            result = result
                .Select(i => BuildProperties(i, contentTypes, dataTypes, blueprints, compositions))
                .ToList();

            return result;
        }

        private static ContentType BuildFromXml(XDocument doc, IReadOnlyList<XDocument> contentTypes, IReadOnlyList<XDocument> dataTypes,
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
                    .Select(i => i.Value)
                    .ToList(),

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
                    .Select(i => (string)i.Attribute("Key"))
                    .ToList(),

                TabsSelf = doc
                    .Root?
                    .Element("Tabs")?
                    .Elements("Tab")
                    .Select(i => new Tab
                    {
                        Caption = i.Element("Caption")?.Value,
                        SortOrder = int.Parse(i.Element("SortOrder")?.Value ?? string.Empty)
                    })
                    .ToList(),

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
                    .ToList()
            };
        }

        private static ContentType BuildCompositions(ContentType contentType, IReadOnlyList<XDocument> contentTypes,
             IReadOnlyList<XDocument> dataTypes, IReadOnlyList<XDocument> blueprints, IReadOnlyList<ContentType> compositions)
        {
            contentType.Compositions = contentType.CompositionKeys
                .Select(i => compositions.Single(c => c.Key == i))
                .Where(i => !Constants.CompositionAliasToIgnore.Contains(i.Alias))
                .ToList();

            return contentType;
        }

        private static ContentType BuildProperties(ContentType contentType, IReadOnlyList<XDocument> contentTypes,
            IReadOnlyList<XDocument> dataTypes, IReadOnlyList<XDocument> blueprints, IReadOnlyList<ContentType> compositions)
        {
            contentType.Properties = contentType.PropertiesSelf
                .Union(contentType.Compositions.SelectMany(c => c.PropertiesSelf).Select(x => x.Clone() as DocumentTypeProperty))
                .Select(p => new { Property = p, Tab = contentType.Tabs.Single(t => t.Caption == p.Tab) })
                .OrderBy(i => i.Tab.SortOrder)
                .ThenBy(i => i.Property.SortOrder)
                .Select(i => i.Property)
                .ToList();

            var properties = contentType.Properties
                .Where(i => i.Type == Constants.NestedContentElementsTypeName)
                .ToList();

            if (!properties.Any()) return contentType;

            //find blueprint for this contentType
            var blueprint = blueprints
                .SingleOrDefault(i => i
                    .Root?
                    .Element("Info")?
                    .Element("ContentType")?
                    .Value == contentType.Alias);

            if (blueprint == null) return contentType;

            //find modules set on blueprint for this contentType and this property
            foreach (var property in properties)
            {
                var modules = JsonConvert.DeserializeObject<IEnumerable<Module>>(blueprint
                        .Root?
                        .Element("Properties")?
                        .Element(property.Alias)?
                        .Elements("Value")
                        .FirstOrDefault()?
                        .Value)
                    .Select(c =>
                    {
                        c.ContentType = contentType;
                        return c;
                    })
                    .ToList();

                property.NestedContentElementsDocTypes = modules;
            }
            return contentType;
        }
    }
}
