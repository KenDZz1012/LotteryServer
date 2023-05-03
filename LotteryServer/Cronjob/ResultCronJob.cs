using HtmlAgilityPack;
using LotteryServer.Interfaces;
using LotteryServer.Models.Result;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LotteryServer.Cronjob
{
    public class ResultCronJob : IJob
    {
        private readonly IResultRepository _repository;

        public ResultCronJob(IResultRepository repository)
        {
            _repository = repository;
        }
    
        public Task Execute(IJobExecutionContext context)
        {
            string dateNow = DateTime.Now.ToString("dd-MM-yyyy");
            string url = @$"https://www.minhngoc.com.vn/ket-qua-xo-so/{dateNow}.html";
            var web = new HtmlWeb();
            var doc = web.Load(url);
            string specialPrize = "";
            string firstPrize = "";
            List<string> listData = new List<string>();

            var data = doc.DocumentNode.SelectNodes("//table[contains(@class,'bkqmienbac')]");

            foreach (var table_mienbac in data)
            {
                doc.LoadHtml(table_mienbac.InnerHtml);
                var data2 = doc.DocumentNode.SelectNodes("//table//tbody//tr").ToArray();


                specialPrize = data2.Select(tr => tr
                    .SelectNodes("//td[contains(@class,'giaidb')]")).ToList()[1][1].InnerText;
                firstPrize = data2.Select(tr => tr
                    .SelectNodes("//td[contains(@class,'giai1')]")).ToList()[1][1].InnerText;
                break;

            }
            CreateResult newResult = new CreateResult()
            {
                firstPrize = firstPrize,
                specialPrize = specialPrize,
                dateIn = DateTime.Now,
                categoryId = 26
            };
            _repository.AddResult(newResult).Wait();

            return Task.CompletedTask;
        }
    }
}
