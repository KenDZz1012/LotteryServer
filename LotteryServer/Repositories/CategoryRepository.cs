using LotteryServer.Interfaces;
using LotteryServer.Models.Category;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LotteryServer.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {

        private readonly IConfiguration _configuration;
        public CategoryRepository(IConfiguration configuration)
        {
            _configuration = configuration;

        }

        public async Task<IEnumerable<CategoryVM>> GetCategoryList(FilterCategory filter)
        {
            NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetConnectionString("LotteryDBConnection"));
            conn.Open();
            List<CategoryVM> categories = new List<CategoryVM>();
            string query = "Select * from Category Where 1 = 1 ";
            if (filter.parentId > 0)
            {
                query += $@" And parentid = {filter.parentId} ";
            }
            if (filter.isHeader != null)
            {
                query += $@" And isheader = {filter.isHeader} ";
            }
            query += " order by categoryId ";
            NpgsqlCommand nc = new NpgsqlCommand(query, conn);
            await using (NpgsqlDataReader reader = await nc.ExecuteReaderAsync())
                while (await reader.ReadAsync())
                {
                    CategoryVM category = ReadCategory(reader);
                    categories.Add(category);
                }
            return categories;
        }

        public async Task<CategoryVM> GetCategoryById(int id)
        {
            NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetConnectionString("LotteryDBConnection"));
            conn.Open();
            string commandText = $"SELECT * FROM Category WHERE categoryId = @id";
            await using (NpgsqlCommand cmd = new NpgsqlCommand(commandText, conn))
            {
                cmd.Parameters.AddWithValue("id", id);
                await using (NpgsqlDataReader reader = await cmd.ExecuteReaderAsync())
                    while (await reader.ReadAsync())
                    {
                        CategoryVM category = ReadCategory(reader);
                        return category;
                    }
            }
            return null;
        }

        public async Task<CategoryVM> GetCategoryByName(string Name)
        {
            NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetConnectionString("LotteryDBConnection"));
            conn.Open();
            string commandText = $@"SELECT * FROM Category WHERE categoryName LIKE '%{Name}%'";
            await using (NpgsqlCommand cmd = new NpgsqlCommand(commandText, conn))
            {
                await using (NpgsqlDataReader reader = await cmd.ExecuteReaderAsync())
                    while (await reader.ReadAsync())
                    {
                        CategoryVM category = ReadCategory(reader);
                        return category;
                    }
            }
            return null;
        }

        public async Task AddCategory(CreateCategory category)
        {
            if (await CheckExist(category.categoryName) == false)
            {
                NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetConnectionString("LotteryDBConnection"));
                conn.Open();
                string commandText = "INSERT INTO Category (categoryName,isheader,parentid)  VALUES (@categoryName,@isheader,@parentid)";
                await using (NpgsqlCommand cmd = new NpgsqlCommand(commandText, conn))
                {
                    cmd.Parameters.AddWithValue("categoryName", category.categoryName);
                    cmd.Parameters.AddWithValue("isheader", category.isheader);
                    cmd.Parameters.AddWithValue("parentid", category.parentid.HasValue?category.parentid.Value : DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            else
            {
                throw new Exception("Sổ xố này đã tồn tại");
            }

        }

        public async Task UpdateCategory(UpdateCategory category)
        {
            NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetConnectionString("LotteryDBConnection"));
            conn.Open();
            var commandText = $@"UPDATE Category
                        SET categoryName = @name
                        WHERE categoryId = @id";

            await using (NpgsqlCommand cmd = new NpgsqlCommand(commandText, conn))
            {
                cmd.Parameters.AddWithValue("id", category.categoryId);
                cmd.Parameters.AddWithValue("name", category.categoryName);
                await cmd.ExecuteNonQueryAsync();
            }


        }

        public async Task DeleteCategory(int id)
        {
            NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetConnectionString("LotteryDBConnection"));
            conn.Open();
            string commandText = $"DELETE FROM Category WHERE categoryId=(@p)";
            await using (var cmd = new NpgsqlCommand(commandText, conn))
            {
                cmd.Parameters.AddWithValue("p", id);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task<bool> CheckExist(string categoryName)
        {
            NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetConnectionString("LotteryDBConnection"));
            conn.Open();
            string query = @$"SELECT * FROM Category WHERE categoryName = '{categoryName}'";
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


        private static CategoryVM ReadCategory(NpgsqlDataReader reader)
        {
            int? categoryId = reader["categoryId"] as int?;
            string categoryName = reader["categoryName"] as string;
            bool isheader = Boolean.Parse(reader["isheader"].ToString());
            int? parentid = reader["parentid"] as int?;

            CategoryVM game = new CategoryVM
            {
                categoryId = categoryId.Value,
                categoryName = categoryName,
                isheader = isheader,
                parentid = parentid != null ? parentid.Value : 0,
            };
            return game;
        }
    }
}
