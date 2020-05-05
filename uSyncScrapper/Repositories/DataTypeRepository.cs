using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uSyncScrapper.Context;

namespace uSyncScrapper.Repositories
{
    public class DataTypeRepository : Repository, IDataTypeRepository
    {
        public DataTypeRepository(ILocalContext context) : base(context)
        {
        }

        protected override string FolderForType => Constants.Entities.DataTypes;
    }
}
