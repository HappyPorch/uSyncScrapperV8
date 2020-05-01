using System.Collections.Generic;
using System.Xml.Linq;

namespace uSyncScrapper.Repositories
{
    public interface IDataTypeRepository
    {
        IReadOnlyList<XDocument> GetAll();
    }
}