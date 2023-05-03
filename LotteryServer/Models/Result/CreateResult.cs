using System;

namespace LotteryServer.Models.Result
{
    public class CreateResult
    {
        public DateTime dateIn { get; set; }

        public string specialPrize { get; set; }

        public string firstPrize { get; set; }

        public int categoryId { get; set; }
    }
}
