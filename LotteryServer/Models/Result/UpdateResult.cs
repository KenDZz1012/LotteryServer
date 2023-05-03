using System;

namespace LotteryServer.Models.Result
{
    public class UpdateResult
    {
        public int resultId { get; set; }

        public DateTime dateIn { get; set; }

        public string specialPrize { get; set; }

        public string firstPrize { get; set; }

        public int categoryId { get; set; }
    }
}
