using HtmlAgilityPack;
using LotteryServer.Interfaces;
using LotteryServer.Models.Result;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace LotteryServer.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class ResultController : ControllerBase
    {
        private readonly IResultRepository _repository;
        private readonly IConfiguration _configuration;

        public ResultController(IResultRepository repository, IConfiguration configuration)
        {
            _repository = repository;
            _configuration = configuration;
        }
        [HttpGet]
        [Produces("application/json")]
        public async Task<ActionResult<ResultResponse>> GetResultList([FromQuery] FilterResult result, int page, CancellationToken cancellationToken)
        {
            var allGames = await _repository.GetResultList(result, page, cancellationToken);
            return Ok(allGames);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ResultVM>> GetResultById(int id)
        {
            var Result = await _repository.GetResultById(id);
            if (Result != null)
                return Ok(Result);
            else
                return NotFound();
        }

        [HttpPost]
        public async Task<ActionResult> Post([FromBody] CreateResult game)
        {
            try
            {
                await _repository.AddResult(game);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        public async Task<ActionResult> Put([FromBody] UpdateResult game)
        {
            try
            {
                await _repository.UpdateResult(game);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            await _repository.DeleteResult(id);
            return Ok();
        }

        [HttpGet("CalResultHead")]
        public async Task<ActionResult> GetCalHead([FromQuery] FilterCalculateResult filter)
        {
            var result = await _repository.CalResultHead(filter);
            return Ok(result);
        }

        [HttpGet("CalResultTail")]
        public async Task<ActionResult> GetCalTail([FromQuery] FilterCalculateResult filter)
        {
            var result = await _repository.CalResultTail(filter);
            return Ok(result);
        }

        [HttpPost("/AutoAddResultNorth")]
        public async Task<ActionResult> AutoAddResultNorth()
        {
            await _repository.AutoAddResultNorth();
            return Ok();
        }

        [HttpPost("/AutoAddResultSouth")]
        public async Task<ActionResult> AutoAddResultSouth()
        {
            await _repository.AutoAddResultSouth();
            return Ok();
        }

        [HttpPost("/AutoAddResultTrung")]
        public async Task<ActionResult> AutoAddResultTrung()
        {
            await _repository.AutoAddResultTrung();
            return Ok();
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

    }
}
