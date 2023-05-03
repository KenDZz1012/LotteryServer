using System;
using System.Collections.Generic;

namespace LotteryServer.Models.Result
{
    public class ResultResponse
    {
        public List<ResultVM> ResultVMs { get; set; }   

        public string totalSize { get; set; }
    }


    public class ResultVM
    {
        public int resultId { get; set; }

        public DateTime? dateIn { get; set; }

        public string specialPrize { get; set; }

        public string firstPrize { get; set; }

        public int categoryId { get; set; }
    }
}
