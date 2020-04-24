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
        private string[] compositionAliasToIgnore = new string[] { "seo", "visibility", "redirect" };

        private string[] docTypesToIgnore = new string[] { "errorPage" };

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
            var docTypes = ParseUSyncFiles(folder);
            var html = GenerateHtml(docTypes);
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Html Files (*.html)|*.html";
            dlg.DefaultExt = "html";
            dlg.AddExtension = true;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(dlg.FileName, html);
            }
        }

        private IEnumerable<DocumentType> ParseUSyncFiles(string folder)
        {
            //pages
            var contentTypes = new List<DocumentType>();

            string uSyncFolder = Directory
                .GetDirectories(folder, "uSync", SearchOption.AllDirectories)
                .First(i => Directory.GetDirectories(i, "v8", SearchOption.AllDirectories).Count() > 0);

            string documentTypeFolder = Directory
                        .GetDirectories(uSyncFolder, "ContentTypes", SearchOption.AllDirectories)
                        .First();
            var allContentTypes = Directory.GetFiles(documentTypeFolder, "*.config", SearchOption.AllDirectories);

            var allPages = allContentTypes.Where(i => i.EndsWith("page.config"));
            //var nestedContent = Directory.GetFiles(documentTypeFolder, "*.config", SearchOption.AllDirectories);

            //show home pages first
            allPages = allPages.Select(p => new { page = p, sort = p.Contains("home", StringComparison.OrdinalIgnoreCase) ? 1 : 0 })
                .OrderByDescending(p => p.sort)
                .Select(p => p.page)
                .ToArray();

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
            var compositionsFiles = allContentTypes.Where(i => i.EndsWith("composition.config"));
            var allCompositionsDocuments = new List<XDocument>();
            foreach (var compositionsFile in compositionsFiles)
            {
                allCompositionsDocuments.Add(XDocument.Load(compositionsFile));
            }

            int index = 1;
            foreach (var file in allPages)
            {
                try
                {
                    var docType = new DocumentType();
                    XDocument doc = XDocument.Load(file);

                    var name = doc
                        .Root
                        .Element("Info")
                        .Element("Name")
                        .Value;
                    docType.Name = name;

                    var alias = doc
                       .Root
                       .Element("Info")
                       .Element("Alias")
                       .Value;
                    docType.Alias = alias;
                    if (docTypesToIgnore.Any(i => i == alias)) { continue; }

                    var childDocTypes = doc
                        .Root
                        .Element("Structure")
                        .Elements("DocumentType")
                        .Select(i => i.Value);
                    docType.ChildDocTypes = childDocTypes;

                    var description = doc
                        .Root
                        .Element("Info")
                        .Element("Description")
                        .Value;
                    docType.Description = description;

                    docType.Folder = doc
                        .Root
                        .Element("Info")
                        .Element("Folder")?
                        .Value;

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

                    ComputeNestedContentProperties(dataTypeDocuments, allProperties);
                    ComputeTreePickerMaxItems(dataTypeDocuments, allProperties);

                    if (!docType.Properties.Any()) { continue; }
                    contentTypes.Add(docType);
                    docType.Index = index;
                    index++;
                }
                catch (Exception ex)
                {
                }
            }

            // figure out parent doc types
            foreach (var docType in contentTypes)
            {
                var parentDocTypes = contentTypes.Where(i => i.ChildDocTypes.Contains(docType.Alias));
                docType.ParentDocTypes = parentDocTypes.Select(i => i.Name).ToList();
            }

            // move child doc types alias to names
            foreach (var docType in contentTypes)
            {
                var childDocTypesNames = new List<string>();
                foreach (var childAlias in docType.ChildDocTypes)
                {
                    var name = contentTypes.FirstOrDefault(i => i.Alias == childAlias)?.Name;
                    if (!string.IsNullOrEmpty(name))
                    {
                        childDocTypesNames.Add(name);
                    }
                }
                docType.ChildDocTypes = childDocTypesNames;
            }

            // fill nested content properties
            foreach (var docType in contentTypes)
            {
                foreach (var prop in docType.Properties)
                {
                    if (prop.NestedContentDocTypes != null && prop.NestedContentDocTypes.Any())
                    {
                        var nestedContentList = new List<NestedContentDocType>();
                        foreach (var nestedContentDocType in prop.NestedContentDocTypes)
                        {
                            nestedContentDocType.Properties = contentTypes.FirstOrDefault(i => i.Alias == nestedContentDocType.Alias)?.Properties.ToList();
                            nestedContentList.Add(nestedContentDocType);
                        }
                        prop.NestedContentDocTypes = nestedContentList;
                    }
                }
            }

            return contentTypes.Where(i => !i.Folder.Contains("nested", StringComparison.OrdinalIgnoreCase)).ToList();
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
            var compositions = doc
                    .Root
                    .Element("Info")
                    .Element("Compositions")
                    .Elements("Composition")
                    .Select(i => (string)i.Attribute("Key"))
                    .Select(i => compositionsDocuments.Where(j => j
                        .Root
                        .Element("Info")
                        .Element("Key")
                        .Value == i).FirstOrDefault())
                    .Where(c => c != null)
                    .Where(c => !compositionAliasToIgnore.Contains(c
                        .Root
                        .Element("Info")
                        .Element("Alias")
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
                    .Select(i => new DocumentTypeProperty { Name = i.Element("Name").Value, Text = i.Element("Description").Value, Tab = i.Element("Tab").Value, Order = int.Parse(i.Element("SortOrder").Value), Type = i.Element("Type").Value, Definition = i.Element("Definition").Value });
            return properties;
        }

        private void ComputeNestedContentProperties(List<XDocument> dataTypeDocuments, List<DocumentTypeProperty> properties)
        {
            var nestedContentProperties = properties
                                    .Where(i => i.Type == "Umbraco.NestedContent");

            foreach (var prop in nestedContentProperties)
            {
                var datatype = dataTypeDocuments.Where(i => i
                    .Root
                    .Attribute("Key")
                    .Value == prop.Definition).FirstOrDefault();
                if (datatype != null)
                {
                    //find max items
                    var maxItems = datatype
                        .Root
                        .Element("PreValues")
                        .Elements("PreValue")
                        .FirstOrDefault(i => (string)i.Attribute("Alias") == "maxItems")
                        .Value;
                    var maxItemsDefault = 0;
                    int.TryParse(maxItems, out maxItemsDefault);
                    prop.MaxItems = maxItemsDefault;

                    //find available contentTypes for this nested content property
                    var contentTypesString = datatype
                        .Root
                        .Element("PreValues")
                        .Elements("PreValue")
                        .FirstOrDefault(i => (string)i.Attribute("Alias") == "contentTypes")
                        .Value;
                    var deserializedContentTypes = JsonConvert.DeserializeObject<IEnumerable<ContentType>>(contentTypesString);
                    prop.NestedContentDocTypes = deserializedContentTypes.Select(i => new NestedContentDocType { Alias = i.ncAlias, Name = i.nameTemplate });
                }
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
                    var maxItems = datatype
                        .Root
                        .Element("PreValues")
                        .Elements("PreValue")
                        .FirstOrDefault(i => (string)i.Attribute("Alias") == "maxNumber")
                        .Value;
                    var maxItemsDefault = 0;
                    int.TryParse(maxItems, out maxItemsDefault);
                    prop.MaxItems = maxItemsDefault;
                }
            }
        }

        private string GenerateHtml(IEnumerable<DocumentType> docTypes)
        {
            string documentTypeFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Views", "DocumentType.cshtml");
            string finalDocumentFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Views", "FinalDocument.cshtml");

            var templateService = new TemplateService();
            templateService.AddNamespace("uSyncScrapper.Models");
            var body = new StringBuilder();

            foreach (var docType in docTypes)
            {
                body.Append(templateService.Parse(File.ReadAllText(documentTypeFilePath), docType, null, "DocumentType"));
            }

            var finalDocType = new FinalDocument { Body = body.ToString(), DocTypes = docTypes };
            var finalDocument = templateService.Parse(File.ReadAllText(finalDocumentFilePath), finalDocType, null, "FinalDocument");

            return WebUtility.HtmlDecode(finalDocument);
        }

    }
}
