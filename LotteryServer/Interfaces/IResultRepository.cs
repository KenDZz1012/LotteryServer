using LotteryServer.Models.Result;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace LotteryServer.Interfaces
{
    public interface IResultRepository
    {
        Task<ResultResponse> GetResultList(FilterResult result,int page, CancellationToken cancellationToken);

        Task<ResultVM> GetResultById(int id);

        Task AddResult(CreateResult game);

        Task UpdateResult(UpdateResult game);

        Task DeleteResult(int id);

        Task<IEnumerable<ResultHead>> CalResultHead(FilterCalculateResult filter);

        Task<IEnumerable<ResultTail>> CalResultTail(FilterCalculateResult filter);

        Task AutoAddResultNorth();

        Task AutoAddResultSouth();

        Task AutoAddResultTrung();

    }
}
