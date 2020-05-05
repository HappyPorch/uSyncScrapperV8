using System.Collections.Generic;
using System.Xml.Linq;

namespace uSyncScrapper.Repositories
{
    public interface IContentTypeRepository
    {
        IReadOnlyList<XDocument> GetAll();
    }
}