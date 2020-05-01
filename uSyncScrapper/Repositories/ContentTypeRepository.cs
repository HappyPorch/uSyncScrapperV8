using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uSyncScrapper.Context;

namespace uSyncScrapper.Repositories
{
    public class ContentTypeRepository : Repository, IContentTypeRepository
    {
        public ContentTypeRepository(ILocalContext context) : base(context)
        {
        }

        protected override string FolderForType => Constants.Entities.ContentTypes;
    }
}
