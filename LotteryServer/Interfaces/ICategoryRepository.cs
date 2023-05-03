using LotteryServer.Models.Category;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LotteryServer.Interfaces
{
    public interface ICategoryRepository
    {
        Task<IEnumerable<CategoryVM>> GetCategoryList(FilterCategory filter);

        Task<CategoryVM> GetCategoryByName(string Name);

        Task<CategoryVM> GetCategoryById(int id);

        Task AddCategory(CreateCategory game);

        Task UpdateCategory(UpdateCategory game);

        Task DeleteCategory(int id);
    }
}
