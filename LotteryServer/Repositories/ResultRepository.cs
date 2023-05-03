using HtmlAgilityPack;
using LotteryServer.Interfaces;
using LotteryServer.Models.Result;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace LotteryServer.Repositories
{
    public class ResultRepository : IResultRepository
    {
        private readonly IConfiguration _configuration;
        private readonly ICategoryRepository _categoryRepository;

        public ResultRepository(IConfiguration configuration, ICategoryRepository categoryRepository)
        {
            _configuration = configuration;
            _categoryRepository = categoryRepository;
        }

        public async Task<ResultResponse> GetResultList(FilterResult filter, int page, CancellationToken cancellationToken)
        {
            ResultResponse response = new ResultResponse();
            NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetConnectionString("LotteryDBConnection"));
            conn.Open();
            List<ResultVM> results = new List<ResultVM>();
            string query = "Select * from Result";
            if (filter.categoryId > 0)
            {
                query += $@" where categoryId = {filter.categoryId}";
            }
            query += @$" order by dateIn desc limit 10 offset {(page - 1) * 10}";
            NpgsqlCommand nc = new NpgsqlCommand(query, conn);
            await using (NpgsqlDataReader reader = await nc.ExecuteReaderAsync())
                while (await reader.ReadAsync())
                {
                    ResultVM result = ReadResult(reader);
                    results.Add(result);
                }
            string commandText = $"Select * from ( SELECT Count(*) as total FROM Result";
            if (filter.categoryId > 0)
            {
                commandText += $@" where categoryId = {filter.categoryId}) b";
            }
            NpgsqlCommand nc1 = new NpgsqlCommand(commandText, conn);
            await using (NpgsqlDataReader reader1 = await nc1.ExecuteReaderAsync())
                while (await reader1.ReadAsync())
                {
                    response.totalSize = (reader1["total"]).ToString();
                }
            response.ResultVMs = results;
            return response;
        }

        public async Task<ResultVM> GetResultById(int id)
        {
            NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetConnectionString("LotteryDBConnection"));
            conn.Open();
            string commandText = $"SELECT * FROM Result WHERE resultId = @id";
            await using (NpgsqlCommand cmd = new NpgsqlCommand(commandText, conn))
            {
                cmd.Parameters.AddWithValue("id", id);
                await using (NpgsqlDataReader reader = await cmd.ExecuteReaderAsync())
                    while (await reader.ReadAsync())
                    {
                        ResultVM game = ReadResult(reader);
                        return game;
                    }
            }
            return null;
        }

        public async Task AddResult(CreateResult result)
        {
            TimeSpan ts = new TimeSpan(19, 00, 00);
            result.dateIn = result.dateIn.Date + ts;
            if (await CheckExist(result.categoryId, result.dateIn) == false)
            {
                NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetConnectionString("LotteryDBConnection"));
                conn.Open();
                string commandText = "INSERT INTO Result (dateIn,firstPrize,specialPrize,categoryId)  VALUES (@dateIn,@firstPrize,@specialPrize,@categoryId)";
                await using (var cmd = new NpgsqlCommand(commandText, conn))
                {
                    cmd.Parameters.AddWithValue("dateIn", result.dateIn);
                    cmd.Parameters.AddWithValue("firstPrize", result.firstPrize);
                    cmd.Parameters.AddWithValue("specialPrize", result.specialPrize);
                    cmd.Parameters.AddWithValue("categoryId", result.categoryId);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
            else
            {
                throw new Exception("Thời gian này đã có dữ liệu");
            }

        }

        public async Task UpdateResult(UpdateResult result)
        {
            NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetConnectionString("LotteryDBConnection"));
            conn.Open();
            var commandText = $@"UPDATE Result
                        SET firstPrize = @firstPrize,specialPrize = @specialPrize,categoryId = @categoryId
                        WHERE resultId = @id";

            await using (var cmd = new NpgsqlCommand(commandText, conn))
            {
                cmd.Parameters.AddWithValue("id", result.resultId);
                cmd.Parameters.AddWithValue("firstPrize", result.firstPrize);
                cmd.Parameters.AddWithValue("specialPrize", result.specialPrize);
                cmd.Parameters.AddWithValue("categoryId", result.categoryId);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task DeleteResult(int id)
        {
            NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetConnectionString("LotteryDBConnection"));
            conn.Open();
            string commandText = $"DELETE FROM Result WHERE resultId=(@p)";
            await using (var cmd = new NpgsqlCommand(commandText, conn))
            {
                cmd.Parameters.AddWithValue("p", id);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task<bool> CheckExist(int categoryId, DateTime dateIn)
        {
            NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetConnectionString("LotteryDBConnection"));
            conn.Open();
            string query = @$"Select * from Result where categoryId = {categoryId} and dateIn = '{dateIn.ToString("yyyy-MM-dd HH:mm:ss")}'";
            await using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
            {
                await using (NpgsqlDataReader reader = await cmd.ExecuteReaderAsync())
                    while (await reader.ReadAsync())
                    {
                        return true;
                    }
            }
            return false;
        }

        public async Task<IEnumerable<ResultHead>> CalResultHead(FilterCalculateResult filter)
        {
            NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetConnectionString("LotteryDBConnection"));
            conn.Open();

            List<ResultHead> resultHead = new List<ResultHead>();
            for (int i = 0; i < 10; i++)
            {
                ResultHead result = new ResultHead();
                string query = $@"select * from (select substring(r.{filter.type},1,1) as b, ROW_NUMBER() OVER(ORDER BY r.datein desc) - 1 AS Row
                                from result r 
                                where r.categoryid = {filter.categoryId} order by r.datein desc) a where b = '{i}' limit 1"; 
                NpgsqlCommand nc = new NpgsqlCommand(query, conn);
                await using (NpgsqlDataReader reader = await nc.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            string dateCal = reader["Row"].ToString();
                            result.FirstHead = dateCal;
                        }
                    }
                    else
                    {
                        result.FirstHead = "";
                    }
                }

                string query1 = $@"select * from (select substring(r.{filter.type},2,1) as b, ROW_NUMBER() OVER(ORDER BY r.datein desc) - 1 AS Row
                                from result r 
                                where r.categoryid = {filter.categoryId} order by r.datein desc) a where b = '{i}' limit 1";
                NpgsqlCommand nc1 = new NpgsqlCommand(query1, conn);
                await using (NpgsqlDataReader reader1 = await nc1.ExecuteReaderAsync())
                {
                    if (reader1.HasRows)
                    {
                        while (await reader1.ReadAsync())
                        {
                            string dateCal = reader1["Row"].ToString();
                            result.SecondHead = dateCal;
                        }
                    }
                    else
                    {
                        result.SecondHead = "";
                    }
                }
                resultHead.Add(result);

            }
            return resultHead;
        }

        public async Task<IEnumerable<ResultTail>> CalResultTail(FilterCalculateResult filter)
        {
            NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetConnectionString("LotteryDBConnection"));
            conn.Open();

            List<ResultTail> resultHead = new List<ResultTail>();
            for (int i = 0; i < 10; i++)
            {
                ResultTail result = new ResultTail();
                string query = $@"select * from (select substring(r.{filter.type},LENGTH(r.{filter.type})-1,1) as b, ROW_NUMBER() OVER(ORDER BY r.datein desc) - 1 AS Row
                                from result r 
                                where r.categoryid = {filter.categoryId} order by r.datein desc) a where b = '{i}' limit 1";
                NpgsqlCommand nc = new NpgsqlCommand(query, conn);
                await using (NpgsqlDataReader reader = await nc.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            string dateCal = reader["Row"].ToString();
                            result.FirstTail = dateCal;
                        }
                    }
                    else
                    {
                        result.FirstTail = "";
                    }
                }

                string query1 = $@"select * from (select substring(r.{filter.type},LENGTH(r.{filter.type}),1) as b, ROW_NUMBER() OVER(ORDER BY r.datein desc) - 1 AS Row
                                from result r 
                                where r.categoryid = {filter.categoryId} order by r.datein desc) a where b = '{i}' limit 1";
                NpgsqlCommand nc1 = new NpgsqlCommand(query1, conn);
                await using (NpgsqlDataReader reader1 = await nc1.ExecuteReaderAsync())
                {
                    if (reader1.HasRows)
                    {
                        while (await reader1.ReadAsync())
                        {
                            string dateCal = reader1["Row"].ToString();
                            result.SecondTail = dateCal;
                        }
                    }
                    else
                    {
                        result.SecondTail = "";
                    }
                }
                resultHead.Add(result);

            }
            return resultHead;
        }

        public async Task AutoAddResultNorth()
        {
            var category = _categoryRepository.GetCategoryByName("sổ xố miền Bắc");
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
                categoryId = category.Result.categoryId
            };
            await AddResult(newResult);
        }

        public async Task AutoAddResultSouth()
        {
            string dateNow = DateTime.Now.ToString("dd-MM-yyyy");
            string firstKey = "";
            string url = @$"https://www.minhngoc.com.vn/ket-qua-xo-so/{dateNow}.html";

            var web = new HtmlWeb();
            var doc = web.Load(url);

            List<string> listData = new List<string>();

            var data = doc.DocumentNode.SelectNodes("//table[contains(@class,'bkqmiennam')]");

            foreach (var table_miennam in data)
            {
                doc.LoadHtml(table_miennam.InnerHtml);
                var data2 = doc.DocumentNode.SelectNodes("//table//tbody//tr");

                var rows = data2.Select(tr => tr
                    .Elements("td")
                    .Select(td => System.Net.WebUtility.HtmlDecode(td.InnerHtml).Trim())
                    .ToArray());
                foreach (var row in rows)
                {
                    var temp = HtmlToPlainText(row[0].Replace("<div>", "").Replace("</div>", "-").Trim());
                    var text = string.Join(" - ", temp.Split('-').Where(x => !string.IsNullOrEmpty(x)).ToArray());
                    listData.Add(text);
                }
                break;
            }

            var listOfLists = new List<IEnumerable<string>>();
            for (int i = 0; i < listData.Count(); i += 11)
            {
                listOfLists.Add(listData.Skip(i).Take(11));
            }
            var table = new DataTable();
            foreach (var item in listOfLists)
            {
                table.Columns.Add(item.ToList().FirstOrDefault());
            }
            firstKey = listOfLists[0].First();
            for (int i = 1; i < listOfLists.Count; i++)
            {
                var category = _categoryRepository.GetCategoryByName(listOfLists[i].First());
                CreateResult newResult = new CreateResult()
                {
                    firstPrize = listOfLists[i].ToList()[listOfLists[i].ToList().Count() - 2],
                    specialPrize = listOfLists[i].ToList()[listOfLists[i].ToList().Count() - 1],
                    dateIn = DateTime.Now,
                    categoryId = category.Result.categoryId
                };
                await AddResult(newResult);
            }
        }

        private static string HtmlToPlainText(string html)
        {
            const string tagWhiteSpace = @"(>|$)(\W|\n|\r)+<";
            const string stripFormatting = @"<[^>]*(>|$)";
            const string lineBreak = @"<(br|BR)\s{0,1}\/{0,1}>";
            var lineBreakRegex = new Regex(lineBreak, RegexOptions.Multiline);
            var stripFormattingRegex = new Regex(stripFormatting, RegexOptions.Multiline);
            var tagWhiteSpaceRegex = new Regex(tagWhiteSpace, RegexOptions.Multiline);

            var text = html;
            text = System.Net.WebUtility.HtmlDecode(text);
            text = tagWhiteSpaceRegex.Replace(text, "><");
            text = lineBreakRegex.Replace(text, Environment.NewLine);
            text = stripFormattingRegex.Replace(text, string.Empty);

            return text;
        }

        private static ResultVM ReadResult(NpgsqlDataReader reader)
        {
            int? resultId = reader["resultId"] as int?;
            DateTime? dateIn = reader["dateIn"] as DateTime?;
            string firstPrize = reader["firstPrize"] as string;
            string specialPrize = reader["specialPrize"] as string;
            int? categoryId = reader["categoryId"] as int?;

            ResultVM game = new ResultVM
            {
                resultId = resultId.Value,
                dateIn = dateIn,
                firstPrize = firstPrize,
                specialPrize = specialPrize,
                categoryId = categoryId.Value
            };
            return game;
        }

        public async Task AutoAddResultTrung()
        {
            string dateNow = DateTime.Now.ToString("dd-MM-yyyy");
            string firstKey = "";
            string url = @$"https://www.minhngoc.com.vn/ket-qua-xo-so/{dateNow}.html";

            var web = new HtmlWeb();
            var doc = web.Load(url);

            List<string> listData = new List<string>();

            var data = doc.DocumentNode.SelectNodes("//table[contains(@class,'bkqmiennam')]");
            doc.LoadHtml(data[2].InnerHtml);
            var data2 = doc.DocumentNode.SelectNodes("//table//tbody//tr");

            var rows = data2.Select(tr => tr
                .Elements("td")
                .Select(td => System.Net.WebUtility.HtmlDecode(td.InnerHtml).Trim())
                .ToArray());
            foreach (var row in rows)
            {
                var temp = HtmlToPlainText(row[0].Replace("<div>", "").Replace("</div>", "-").Trim());
                var text = string.Join(" - ", temp.Split('-').Where(x => !string.IsNullOrEmpty(x)).ToArray());
                listData.Add(text);
            }

            var listOfLists = new List<IEnumerable<string>>();
            for (int i = 0; i < listData.Count(); i += 11)
            {
                listOfLists.Add(listData.Skip(i).Take(11));
            }
            var table = new DataTable();
            foreach (var item in listOfLists)
            {
                table.Columns.Add(item.ToList().FirstOrDefault());
            }
            firstKey = listOfLists[0].First();
            for (int i = 1; i < listOfLists.Count; i++)
            {
                var category = _categoryRepository.GetCategoryByName(listOfLists[i].First());
                CreateResult newResult = new CreateResult()
                {
                    firstPrize = listOfLists[i].ToList()[listOfLists[i].ToList().Count() - 2],
                    specialPrize = listOfLists[i].ToList()[listOfLists[i].ToList().Count() - 1],
                    dateIn = DateTime.Now,
                    categoryId = category.Result.categoryId
                };
                await AddResult(newResult);
            }
        }
    }
}
