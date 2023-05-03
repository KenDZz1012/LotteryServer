namespace LotteryServer.Models.Category
{
    public class CreateCategory
    {
        public string categoryName { get; set; }
        public int? parentid { get; set; }
        public bool isheader { get; set; }
    }
}
 