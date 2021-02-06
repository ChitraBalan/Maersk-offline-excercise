using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maersk.Sorting.Api
{
    public interface ISortJobProcessor
    {
        Task<SortJob> Process(SortJob job);
        SortJob PushQueue(SortJob sortJob);
        List<SortJob> GetSortJobs();
        SortJob GetSortJobById(Guid id);
    }
}
