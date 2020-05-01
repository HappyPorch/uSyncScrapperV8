using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uSyncScrapper.Context;

namespace uSyncScrapper.Repositories
{
    public class BlueprintRepository : Repository, IBlueprintRepository
    {
        public BlueprintRepository(ILocalContext context) : base(context)
        {
        }

        protected override string FolderForType => Constants.Entities.Blueprints;
    }
}
