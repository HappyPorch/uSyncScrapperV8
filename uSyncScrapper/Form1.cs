using Newtonsoft.Json;
using RazorEngine.Templating;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using uSyncScrapper.Extensions;
using uSyncScrapper.Models;

namespace uSyncScrapper
{
    public partial class Form1 : Form
    {
        private string[] compositionAliasToIgnore = new string[] { "sEOComposition", "visibilityComposition",
            "redirectComposition", "markupComposition", "allowDeleteComposition", "auxiliaryFoldersComposition", "bodyClassComposition" };
        private string[] docTypesToIgnore = new string[] { "errorPage" };

        private const string nestedContentTypeName = "Umbraco.NestedContent";
        private const string nestedContentElementsTypeName = "Umbraco.NestedContentElements";
        public Form1()
        {
            InitializeComponent();
        }

        private void buttonBrowseFolder_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void buttonScrap_Click(object sender, EventArgs e)
        {
            ParseUSyncfilesToHtml(textBox1.Text);
        }

        private void ParseUSyncfilesToHtml(string folder)
        {
            var result = ParseUSyncFiles(folder);
            var html = GenerateHtml(result.Item1, result.Item2);
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Html Files (*.html)|*.html";
            dlg.DefaultExt = "html";
            dlg.AddExtension = true;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(dlg.FileName, html);
            }
            textBoxResults.AppendText(Environment.NewLine + "Done");
        }

        private Tuple<IEnumerable<DocumentType>, IEnumerable<Module>> ParseUSyncFiles(string folder)
        {
            //pages
            string uSyncFolder = Directory
                .GetDirectories(folder, "uSync", SearchOption.AllDirectories)
                .First(i => Directory.GetDirectories(i, "v8", SearchOption.AllDirectories).Count() > 0);

            string documentTypeFolder = Directory
                        .GetDirectories(uSyncFolder, "ContentTypes", SearchOption.AllDirectories)
                        .First();
            var allContentTypesFiles = Directory.GetFiles(documentTypeFolder, "*.config", SearchOption.AllDirectories);

            //var allPages = allContentTypesFiles.Where(i => i.EndsWith("page.config"));
            ////var nestedContent = Directory.GetFiles(documentTypeFolder, "*.config", SearchOption.AllDirectories);

            ////show home pages first
            //allPages = allPages.Select(p => new { page = p, sort = p.Contains("home", StringComparison.OrdinalIgnoreCase) ? 1 : 0 })
            //    .OrderByDescending(p => p.sort)
            //    .Select(p => p.page)
            //    .ToArray();

            //datatypes
            string datatypeFolder = Directory
                        .GetDirectories(uSyncFolder, "DataTypes", SearchOption.AllDirectories)
                        .First();
            var allDataTypes = Directory.GetFiles(datatypeFolder, "*.config", SearchOption.AllDirectories);

            var dataTypeDocuments = new List<XDocument>();
            foreach (var datatypeFile in allDataTypes)
            {
                dataTypeDocuments.Add(XDocument.Load(datatypeFile));
            }

            //compositions
            var compositionsFiles = allContentTypesFiles.Where(i => i.EndsWith("composition.config"));
            var allCompositionsDocuments = new List<XDocument>();
            foreach (var compositionsFile in compositionsFiles)
            {
                allCompositionsDocuments.Add(XDocument.Load(compositionsFile));
            }

            allCompositionsDocuments = allCompositionsDocuments
                .Where(c => c.Root.Name != "Empty")
                .Where(c => !compositionAliasToIgnore.Contains(c
                    .Root
                    .Attribute("Alias")
                    .Value)).ToList();

            //blueprints
            string blueprintsFolder = Directory
                        .GetDirectories(uSyncFolder, "Blueprints", SearchOption.AllDirectories)
                        .First();
            var blueprintsFiles = Directory.GetFiles(blueprintsFolder, "*.config", SearchOption.AllDirectories);
            var blueprintsDocuments = blueprintsFiles
                        .Select(i => XDocument.Load(i))
                        .ToList();

            int index = 1;
            var allContentTypes = new List<DocumentType>();

            foreach (var file in allContentTypesFiles)
            {
                var docType = new DocumentType();
                XDocument doc = XDocument.Load(file);

                if (doc.Root.Name == "Empty") { continue; }

                var name = doc
                    .Root
                    .Element("Info")
                    .Element("Name")
                    .Value;
                docType.Name = name;

                var alias = doc
                   .Root
                   .Attribute("Alias")
                   .Value;
                docType.Alias = alias;
                if (docTypesToIgnore.Any(i => i == alias)) { continue; }

                var childDocTypes = doc
                    .Root
                    .Element("Structure")
                    .Elements("ContentType")
                    .Select(i => i.Value);
                docType.ChildDocTypes = childDocTypes;

                var description = doc
                    .Root
                    .Element("Info")
                    .Element("Description")
                    .Value;
                docType.Description = description;

                var compositionsDocuments = GetCompositions(allCompositionsDocuments, doc);

                var allTabs = new List<Tab>();
                allTabs.AddRange(GetTabs(doc));
                allTabs.AddRange(GetCompositionsTabs(compositionsDocuments));
                allTabs = allTabs.OrderBy(i => i.Order).ToList();

                var allProperties = new List<DocumentTypeProperty>();
                allProperties.AddRange(GetCompositionsProperties(compositionsDocuments, doc));
                allProperties.AddRange(GetDocumentProperties(doc));

                allProperties = allProperties
                    .OrderBy(p => allTabs.IndexOf(allTabs.First(t => t.Caption == p.Tab)))
                    .ThenBy(i => i.Order)
                    .ToList();
                docType.Properties = allProperties;

                ComputeNestedContentProperties(docType, dataTypeDocuments);
                ComputeNestedContentElementsProperties(docType, dataTypeDocuments, blueprintsDocuments);
                //ComputeTreePickerMaxItems(dataTypeDocuments, allProperties);
                ComputeNotes(docType, dataTypeDocuments);


                if (!docType.Properties.Any()) { continue; }
                allContentTypes.Add(docType);
                docType.Index = index;
                index++;
            }

            // figure out parent doc types
            foreach (var docType in allContentTypes)
            {
                var parentDocTypes = allContentTypes.Where(i => i.ChildDocTypes.Contains(docType.Alias));
                docType.ParentDocTypes = parentDocTypes.Select(i => i.Name).ToList();
            }

            // move child doc types alias to names
            foreach (var docType in allContentTypes)
            {
                var childDocTypesNames = new List<string>();
                foreach (var childAlias in docType.ChildDocTypes)
                {
                    var name = allContentTypes.FirstOrDefault(i => i.Alias == childAlias)?.Name;
                    if (!string.IsNullOrEmpty(name))
                    {
                        childDocTypesNames.Add(name);
                    }
                }
                docType.ChildDocTypes = childDocTypesNames;
            }

            // fill nested content properties
            foreach (var docType in allContentTypes)
            {
                foreach (var prop in docType.Properties)
                {
                    if (prop.NestedContentDocTypes != null && prop.NestedContentDocTypes.Any())
                    {
                        var nestedContentList = new List<NestedContentDocType>();
                        foreach (var nestedContentDocType in prop.NestedContentDocTypes)
                        {
                            nestedContentDocType.Properties = allContentTypes.FirstOrDefault(i => i.Alias == nestedContentDocType.Alias)?.Properties.ToList();
                            nestedContentList.Add(nestedContentDocType);
                        }
                        prop.NestedContentDocTypes = nestedContentList;
                    }
                    else if (prop.NestedContentElementsDocTypes != null && prop.NestedContentElementsDocTypes.Any())
                    {
                        foreach (var item in prop.NestedContentElementsDocTypes)
                        {
                            item.ContentType = allContentTypes.First(i => i.Alias == item.NcContentTypeAlias);
                        }
                    }
                }
            }

            var allModules = allContentTypes
                    .SelectMany(i => i.Properties)
                    .Where(i => i.NestedContentElementsDocTypes != null && i.NestedContentElementsDocTypes.Any())
                    .SelectMany(i => i.NestedContentElementsDocTypes)
                    .GroupBy(i => i.NcContentTypeAlias)
                    .Select(g => g.First())
                    .OrderBy(i => i.Name)
                    .ToList();

            var pages = allContentTypes.Where(i => i.Alias.EndsWith("Page") || i.Alias.EndsWith("websiteSettings"));

            return new Tuple<IEnumerable<DocumentType>, IEnumerable<Module>>(pages, allModules);
        }

        private IEnumerable<Tab> GetCompositionsTabs(IEnumerable<XDocument> compositions)
        {
            var tabs = new List<Tab>();
            foreach (var comp in compositions)
            {
                tabs.AddRange(GetTabs(comp));
            }
            return tabs;
        }

        private IEnumerable<Tab> GetTabs(XDocument doc)
        {
            return doc
                .Root
                .Element("Tabs")
                .Elements("Tab")
                .Select(i => new Tab { Caption = i.Element("Caption").Value, Order = int.Parse(i.Element("SortOrder").Value) });
        }

        private IEnumerable<XDocument> GetCompositions(List<XDocument> compositionsDocuments, XDocument doc)
        {
            var compositionKeys = doc
                    .Root
                    .Element("Info")
                    .Element("Compositions")
                    .Elements("Composition")
                    .Select(i => (string)i.Attribute("Key"));

            var compositions = compositionsDocuments
                    .Where(i => compositionKeys.Contains(i
                        .Root
                        .Attribute("Key")
                        .Value));

            return compositions;
        }

        private IEnumerable<DocumentTypeProperty> GetCompositionsProperties(IEnumerable<XDocument> compositionsDocuments, XDocument doc)
        {
            var allProperties = new List<DocumentTypeProperty>();
            foreach (var comp in compositionsDocuments)
            {
                if (comp != null)
                {
                    allProperties.AddRange(GetDocumentProperties(comp));
                }
            }
            return allProperties;
        }

        private IEnumerable<DocumentTypeProperty> GetDocumentProperties(XDocument doc)
        {
            var properties = doc
                    .Root
                    .Element("GenericProperties")
                    .Elements("GenericProperty")
                    .Where(i => checkBoxIncludePropertiesWithoutDescription.Checked ? true : !string.IsNullOrEmpty(i.Element("Description").Value))
                    .Select(i => new DocumentTypeProperty { Name = i.Element("Name").Value, Alias = i.Element("Alias").Value, Text = i.Element("Description").Value, Tab = i.Element("Tab").Value, Order = int.Parse(i.Element("SortOrder").Value), Type = i.Element("Type").Value, Definition = i.Element("Definition").Value });
            return properties;
        }

        /// <summary>
        /// Fills in nested Content properties of a doc type.
        /// </summary>
        /// <param name="docType"></param>
        /// <param name="dataTypeDocuments"></param>
        private void ComputeNestedContentProperties(DocumentType docType, IEnumerable<XDocument> dataTypeDocuments)
        {
            var nestedContentProperties = docType.Properties
                                    .Where(i => i.Type == nestedContentTypeName);

            foreach (var prop in nestedContentProperties)
            {
                var datatype = dataTypeDocuments.Where(i => i
                    .Root
                    .Attribute("Key")
                    .Value == prop.Definition).FirstOrDefault();
                if (datatype != null && datatype.Root.Name != "Empty")
                {
                    //find max items
                    var config = JsonConvert.DeserializeObject<Config>(datatype
                        .Root
                        .Element("Config")
                        .Value);

                    prop.MaxItems = config.MaxItems;

                    //find available contentTypes for this nested content property
                    prop.NestedContentDocTypes = config.ContentTypes.Select(i => new NestedContentDocType { Alias = i.ncAlias, Name = i.nameTemplate });
                }
            }
        }

        private void ComputeNestedContentElementsProperties(DocumentType docType, IEnumerable<XDocument> dataTypeDocuments, IEnumerable<XDocument> blueprintDocuments)
        {
            var properties = docType.Properties
                                    .Where(i => i.Type == nestedContentElementsTypeName);

            if (!properties.Any()) { return; }

            //find blueprint for this contenttype
            var blueprint = blueprintDocuments
                .Where(i => i
                    .Root
                    .Element("Info")
                    .Element("ContentType")
                    .Value == docType.Alias)
                .SingleOrDefault();

            if (blueprint == null) { return; }

            //find modules set on blueprint for this contenttype and this property
            foreach (var property in properties)
            {
                var modules = JsonConvert.DeserializeObject<IEnumerable<Module>>(blueprint
                    .Root
                    .Element("Properties")
                    .Element(property.Alias)
                    .Elements("Value")
                    .FirstOrDefault()?
                    .Value?
                    .ToString());

                property.NestedContentElementsDocTypes = modules;
            }

        }

        private void ComputeNotes(DocumentType docType, List<XDocument> dataTypeDocuments)
        {
            var notesProperties = docType.Properties
                                    .Where(i => i.Alias.ToLower().Contains("notes"));

            foreach (var prop in notesProperties)
            {
                var datatype = dataTypeDocuments.Where(i => i
                    .Root
                    .Attribute("Key")
                    .Value == prop.Definition).FirstOrDefault();
                var config = JsonConvert.DeserializeObject<ConfigNotes>(datatype
                        .Root
                        .Element("Config")
                        .Value);
                docType.Notes = config.EditorNotes?.StripHTML() ?? string.Empty;
            }
        }

        private void ComputeTreePickerMaxItems(List<XDocument> dataTypeDocuments, List<DocumentTypeProperty> properties)
        {
            var treePickerProperties = properties
                                    .Where(i => i.Type.StartsWith("Umbraco.MultiNodeTreePicker"));

            foreach (var prop in treePickerProperties)
            {
                var datatype = dataTypeDocuments.Where(i => i
                    .Root
                    .Attribute("Key")
                    .Value == prop.Definition).FirstOrDefault();
                if (datatype != null)
                {
                    var maxItems = JsonConvert.DeserializeObject<DataTypeConfig>(datatype
                        .Root
                        .Element("Config")
                        .Value).MaxNumber;

                    prop.MaxItems = maxItems;
                }
            }
        }

        private string GenerateHtml(IEnumerable<DocumentType> docTypes, IEnumerable<Module> modules)
        {
            string documentTypeFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Views", "DocumentType.cshtml");
            string moduleFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Views", "Module.cshtml");
            string finalDocumentFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Views", "FinalDocument.cshtml");

            var templateService = new TemplateService();
            templateService.AddNamespace("uSyncScrapper.Models");
            var docTypeBody = new StringBuilder();
            var modulesBody = new StringBuilder();

            foreach (var docType in docTypes)
            {
                docTypeBody.Append(templateService.Parse(File.ReadAllText(documentTypeFilePath), docType, null, "DocumentType"));
            }

            foreach (var module in modules)
            {
                modulesBody.Append(templateService.Parse(File.ReadAllText(moduleFilePath), module, null, "Module"));
            }

            var finalDocType = new FinalDocument
            {
                DocTypesBody = docTypeBody.ToString(),
                DocTypes = docTypes,
                ModulesBody = modulesBody.ToString(),
                Modules = modules
            };
            var finalDocument = templateService.Parse(File.ReadAllText(finalDocumentFilePath), finalDocType, null, "FinalDocument");

            return WebUtility.HtmlDecode(finalDocument);
        }

        private bool IsPage(string alias) => alias.EndsWith("page.config");

    }
}
