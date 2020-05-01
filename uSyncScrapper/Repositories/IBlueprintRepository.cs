using System.Collections.Generic;
using System.Xml.Linq;

namespace uSyncScrapper.Repositories
{
    public interface IBlueprintRepository
    {
        IReadOnlyList<XDocument> GetAll();
    }
}