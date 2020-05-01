using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using Newtonsoft.Json;
using RazorEngine.Templating;
using uSyncScrapper.Context;
using uSyncScrapper.Extensions;
using uSyncScrapper.Models;
using uSyncScrapper.Repositories;
using uSyncScrapper.Mappers;

namespace uSyncScrapper
{
    public partial class Form1 : Form
    {
        private ILocalContext _context;
        private readonly IContentTypeRepository _contentTypeRepository;
        private readonly IDataTypeRepository _dataTypeRepository;
        private readonly IBlueprintRepository _blueprintRepository;

        public Form1(ILocalContext context, IContentTypeRepository contentTypeRepository,
            IDataTypeRepository dataTypeRepository, IBlueprintRepository blueprintRepository)
        {
            _context = context;
            _contentTypeRepository = contentTypeRepository;
            _dataTypeRepository = dataTypeRepository;
            _blueprintRepository = blueprintRepository;
            InitializeComponent();
        }

        private void buttonBrowseFolder_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK) textBox1.Text = folderBrowserDialog1.SelectedPath;
        }

        private void buttonScrap_Click(object sender, EventArgs e)
        {
            ParseUSyncfilesToHtml(textBox1.Text);
        }

        private void ParseUSyncfilesToHtml(string folder)
        {
            var result = ParseUSyncFiles(folder);
            if (result.Item1 == null || result.Item2 == null) return;
            var html = GenerateHtml(result.Item1, result.Item2);
            var dlg = new SaveFileDialog
            {
                Filter = "Html Files (*.html)|*.html",
                DefaultExt = "html",
                AddExtension = true
            };
            if (dlg.ShowDialog() == DialogResult.OK) File.WriteAllText(dlg.FileName, html);
            textBoxResults.AppendText(Environment.NewLine + "Done");
        }

        private Tuple<IEnumerable<ContentType>, IEnumerable<Module>> ParseUSyncFiles(string folder)
        {
            //pages
            var uSyncFolder = Directory
                .GetDirectories(folder, "uSync", SearchOption.AllDirectories)
                .First(i => Directory.GetDirectories(i, "v8", SearchOption.AllDirectories).Any());
            _context.BaseFolder = uSyncFolder;

            var contentTypeFiles = _contentTypeRepository.GetAll();
            var dataTypeFiles = _dataTypeRepository.GetAll();
            var blueprintFiles = _blueprintRepository.GetAll();

            if (!blueprintFiles.Any())
            {
                textBoxResults.AppendText(Environment.NewLine + "Blueprints folder not found or empty!");
                return new Tuple<IEnumerable<ContentType>, IEnumerable<Module>>(null, null);
            }

            var contentTypes = contentTypeFiles.Map(dataTypeFiles, blueprintFiles);

            foreach (var contentType in contentTypes)
            {
                ComputeNestedContentProperties(contentType, dataTypeFiles);
                ComputeNestedContentElementsProperties(contentType, dataTypeFiles, blueprintFiles);
                //ComputeTreePickerMaxItems(dataTypeDocuments, allProperties);
                ComputeNotes(contentType, dataTypeFiles);

                var parentDocTypes = contentTypes.Where(i => i.ChildDocTypes.Contains(contentType.Alias));
                contentType.ParentDocTypes = parentDocTypes.Select(i => i.Name).ToList();

                var childDocTypesNames = new List<string>();
                foreach (var childAlias in contentType.ChildDocTypes)
                {
                    var name = contentTypes.FirstOrDefault(i => i.Alias == childAlias)?.Name;
                    if (!string.IsNullOrEmpty(name)) childDocTypesNames.Add(name);
                }
                contentType.ChildDocTypes = childDocTypesNames;
            }

            // fill nested content properties
            foreach (var docType in contentTypes)
                foreach (var prop in docType.PropertiesSelf)
                    if (prop.NestedContentDocTypes != null && prop.NestedContentDocTypes.Any())
                    {
                        var nestedContentList = new List<NestedContentDocType>();
                        foreach (var nestedContentDocType in prop.NestedContentDocTypes)
                        {
                            nestedContentDocType.Properties = contentTypes
                                .FirstOrDefault(i => i.Alias == nestedContentDocType.Alias)?.PropertiesSelf.ToList();
                            nestedContentList.Add(nestedContentDocType);
                        }

                        prop.NestedContentDocTypes = nestedContentList;
                    }
                    else if (prop.NestedContentElementsDocTypes != null && prop.NestedContentElementsDocTypes.Any())
                    {
                        foreach (var item in prop.NestedContentElementsDocTypes)
                            item.ContentType = contentTypes.First(i => i.Alias == item.NcContentTypeAlias);
                    }

            var allModules = contentTypes
                .SelectMany(i => i.PropertiesSelf)
                .Where(i => i.NestedContentElementsDocTypes != null && i.NestedContentElementsDocTypes.Any())
                .SelectMany(i => i.NestedContentElementsDocTypes)
                .GroupBy(i => i.NcContentTypeAlias)
                .Select(g => g.First())
                .OrderBy(i => i.Name)
                .ToList();

            var pages = contentTypes.Where(i => i.Alias.EndsWith("Page") || i.Alias.EndsWith("websiteSettings"));

            return new Tuple<IEnumerable<ContentType>, IEnumerable<Module>>(pages, allModules);
        }

        //private IEnumerable<Tab> GetCompositionsTabs(IEnumerable<XDocument> compositions)
        //{
        //    var tabs = new List<Tab>();
        //    foreach (var comp in compositions) tabs.AddRange(GetTabs(comp));
        //    return tabs;
        //}

        //private IEnumerable<XDocument> GetCompositions(IEnumerable<XDocument> compositionsDocuments, ContentType doc)
        //{
        //    var compositions = compositionsDocuments
        //        .Where(i => doc.CompositionKeys.Contains(i
        //            .Root
        //            .Attribute("Key")
        //            .Value));

        //    return compositions;
        //}

        private IEnumerable<DocumentTypeProperty> GetCompositionsProperties(
            IEnumerable<XDocument> compositionsDocuments, XDocument doc)
        {
            var allProperties = new List<DocumentTypeProperty>();
            foreach (var comp in compositionsDocuments)
                if (comp != null)
                    allProperties.AddRange(GetDocumentProperties(comp));
            return allProperties;
        }

        private IEnumerable<DocumentTypeProperty> GetDocumentProperties(XDocument doc)
        {
            var properties = doc
                .Root
                .Element("GenericProperties")
                .Elements("GenericProperty")
                .Where(i => checkBoxIncludePropertiesWithoutDescription.Checked
                    ? true
                    : !string.IsNullOrEmpty(i.Element("Description").Value))
                .Select(i => new DocumentTypeProperty
                {
                    Name = i.Element("Name").Value,
                    Alias = i.Element("Alias").Value,
                    Text = i.Element("Description").Value,
                    Tab = i.Element("Tab").Value,
                    SortOrder = int.Parse(i.Element("SortOrder").Value),
                    Type = i.Element("Type").Value,
                    Definition = i.Element("Definition").Value
                });
            return properties;
        }

        /// <summary>
        ///     Fills in nested Content properties of a doc type.
        /// </summary>
        /// <param name="docType"></param>
        /// <param name="dataTypeDocuments"></param>
        private void ComputeNestedContentProperties(ContentType docType, IEnumerable<XDocument> dataTypeDocuments)
        {
            var nestedContentProperties = docType.PropertiesSelf
                .Where(i => i.Type == Constants.NestedContentTypeName);

            foreach (var prop in nestedContentProperties)
            {
                var datatype = dataTypeDocuments.Where(i => i
                    .Root
                    .Attribute("Key")
                    .Value == prop.Definition).FirstOrDefault();
                if (datatype != null)
                {
                    //find max items
                    var config = JsonConvert.DeserializeObject<Config>(datatype
                        .Root
                        .Element("Config")
                        .Value);

                    prop.MaxItems = config.MaxItems;

                    //find available contentTypes for this nested content property
                    prop.NestedContentDocTypes = config.ContentTypes.Select(i => new NestedContentDocType
                    { Alias = i.ncAlias, Name = i.nameTemplate });
                }
            }
        }

        private void ComputeNestedContentElementsProperties(ContentType docType,
            IEnumerable<XDocument> dataTypeDocuments, IEnumerable<XDocument> blueprintDocuments)
        {
            var properties = docType.PropertiesSelf
                .Where(i => i.Type == Constants.NestedContentElementsTypeName);

            if (!properties.Any()) return;

            //find blueprint for this contenttype
            var blueprint = blueprintDocuments
                .Where(i => i
                    .Root
                    .Element("Info")?
                    .Element("ContentType")?
                    .Value == docType.Alias)
                .SingleOrDefault();

            if (blueprint == null) return;

            //find modules set on blueprint for this contenttype and this property
            foreach (var property in properties)
            {
                var modules = JsonConvert.DeserializeObject<IEnumerable<Module>>(blueprint
                    .Root
                    .Element("Properties")
                    .Element(property.Alias)
                    .Elements("Value")
                    .FirstOrDefault()?
                    .Value);

                property.NestedContentElementsDocTypes = modules;
            }
        }

        private void ComputeNotes(ContentType docType, IEnumerable<XDocument> dataTypeDocuments)
        {
            var notesProperties = docType.PropertiesSelf
                .Where(i => i.Alias.ToLower().Contains("notes"));

            foreach (var prop in notesProperties)
            {
                var datatype = dataTypeDocuments.Where(i => i
                    .Root
                    .Attribute("Key")
                    .Value == prop.Definition).FirstOrDefault();
                if (datatype == null || datatype.Root.Name == "Empty") continue;
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

        private string GenerateHtml(IEnumerable<ContentType> contentTypes, IEnumerable<Module> modules)
        {
            var contentTypeFilePath =
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Views", "ContentType.cshtml");
            var moduleFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Views", "Module.cshtml");
            var finalDocumentFilePath =
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Views", "FinalDocument.cshtml");

            var templateService = new TemplateService();
            templateService.AddNamespace("uSyncScrapper.Models");
            var contentTypeBody = new StringBuilder();
            var modulesBody = new StringBuilder();

            foreach (var contentType in contentTypes)
                contentTypeBody.Append(templateService.Parse(File.ReadAllText(contentTypeFilePath), contentType, null,
                    "ContentType"));

            foreach (var module in modules)
                modulesBody.Append(templateService.Parse(File.ReadAllText(moduleFilePath), module, null, "Module"));

            var finalDocType = new FinalDocument
            {
                DocTypesBody = contentTypeBody.ToString(),
                DocTypes = contentTypes,
                ModulesBody = modulesBody.ToString(),
                Modules = modules
            };
            var finalDocument = templateService.Parse(File.ReadAllText(finalDocumentFilePath), finalDocType, null,
                "FinalDocument");

            return WebUtility.HtmlDecode(finalDocument);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            textBoxResults.AppendText("Assumes all pages have a *page suffix.");
            textBoxResults.AppendText(Environment.NewLine +
                                      "Assumes all pages have a content template (blueprint). This is how we figure what modules are set.");
            textBoxResults.AppendText(Environment.NewLine +
                                      "Module's descriptions are fetched both from the element's description as well as Editor Notes properties.");
            textBoxResults.AppendText(Environment.NewLine +
                                      "Only renders correctly if the modules property is set as Umbraco.NestedContentElements type on the uSync contentType files.");
        }
    }
}