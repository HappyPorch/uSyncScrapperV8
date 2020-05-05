using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;
using uSyncScrapper.Context;
using uSyncScrapper.Models;


namespace uSyncScrapper.Repositories
{
    public abstract class Repository
    {
        private readonly ILocalContext _context;

        protected Repository(ILocalContext context)
        {
            this._context = context;
        }

        public virtual IReadOnlyList<XDocument> GetAll()
        {
            var folder = Directory
                .GetDirectories(_context.BaseFolder, FolderForType, SearchOption.AllDirectories)
                .FirstOrDefault();

            if (folder == null) return Array.Empty<XDocument>();

            var files = Directory.GetFiles(folder, "*.config", SearchOption.AllDirectories)
                .Select(XDocument.Load)
                .Where(i => i.Root?.Name != "Empty" && !Constants.DocTypesToIgnore.Contains(i.Root?.Attribute("Alias")?.Value))
                .ToList();

            return files;
        }

        protected abstract string FolderForType { get; }
    }
}