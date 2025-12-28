// Labs.Tests/DishesControllerTests.cs
using Labs.API.Controllers;
using Labs.API.Data;
using Labs.Domain.Entities;
using Labs.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Labs.Tests
{
    public class DishesControllerTests : IDisposable
    {
        private readonly DbConnection _connection;
        private readonly DbContextOptions<AppDbContext> _contextOptions;

        public DishesControllerTests()
        {
            // Create and open a connection. This creates the SQLite in-memory database
            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();

            // These options will be used by the context instances
            _contextOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            // Create the schema and seed some data
            using var context = new AppDbContext(_contextOptions);
            context.Database.EnsureCreated();

            var categories = new Category[]
            {
                new Category {Id=1, Name="Супы", NormalizedName="soups"},
                new Category {Id=2, Name="Основные блюда", NormalizedName="main"}
            };
            context.Categories.AddRange(categories);
            context.SaveChanges();

            var dishes = new List<Dish>
            {
                new Dish {Id=1, Name="Суп-харчо", Description="Очень острый", Calories=200, CategoryId=1},
                new Dish {Id=2, Name="Борщ", Description="Много сала", Calories=330, CategoryId=1},
                new Dish {Id=3, Name="Цезарь", Description="С курицей", Calories=450, CategoryId=2},
                new Dish {Id=4, Name="Стейк", Description="Мраморная говядина", Calories=800, CategoryId=2},
                new Dish {Id=5, Name="Греческий салат", Description="С фетой", Calories=350, CategoryId=2}
            };
            context.Dishes.AddRange(dishes);
            context.SaveChanges();
        }

        public void Dispose() => _connection?.Dispose();

        AppDbContext CreateContext() => new AppDbContext(_contextOptions);

        // Проверка фильтра по категории
        
        [Fact]
        public async Task ControllerFiltersCategory()
        {
            // arrange
            using var context = CreateContext();
            var category = context.Categories.First();
            var controller = new DishesController(context);

            // act
            var response = await controller.GetDishes(category.NormalizedName);
            Assert.NotNull(response);
            Assert.NotNull(response.Value);
            ResponseData<ListModel<Dish>> responseData = response.Value;

            Assert.NotNull(responseData);
            Assert.NotNull(responseData.Data);
            Assert.NotNull(responseData.Data.Items);

            var dishesList = responseData.Data.Items;

            //assert
            Assert.True(dishesList.All(d => d.CategoryId == category.Id));
        }

        // Проверка подсчета количества страниц
        [Theory]
        [InlineData(2, 3)]
        [InlineData(3, 2)]
        public async Task ControllerReturnsCorrectPagesCount(int size, int qty)
        {
            using var context = CreateContext();
            var controller = new DishesController(context);

            // act
            var response = await controller.GetDishes(null, 1, size);
            Assert.NotNull(response);
            Assert.NotNull(response.Value);
            ResponseData<ListModel<Dish>> responseData = response.Value;
            Assert.NotNull(responseData);
            Assert.NotNull(responseData.Data);
            Assert.NotNull(responseData.Data.Items);

            var totalPages = responseData.Data.TotalPages;

            //assert
            Assert.Equal(qty, totalPages);
        }

        [Fact]
        public async Task ControllerReturnsCorrectPage()
        {
            using var context = CreateContext();
            var controller = new DishesController(context);

            // При размере страницы 3 и общем количестве объектов 5
            // на 2-й странице должно быть 2 объекта
            // Первый объект на второй странице
            var allDishes = context.Dishes.OrderBy(d => d.Id).ToArray();
            Dish firstItem = allDishes[3]; // Четвертый элемент (0-based index)
            // act
            var response = await controller.GetDishes(null, 2, 3);
            Assert.NotNull(response);
            Assert.NotNull(response.Value);
            ResponseData<ListModel<Dish>> responseData = response.Value;
            Assert.NotNull(responseData);
            Assert.NotNull(responseData.Data);
            Assert.NotNull(responseData.Data.Items);
            var dishesList = responseData.Data.Items;
            var currentPage = responseData.Data.CurrentPage;

            //assert
            Assert.Equal(2, currentPage);
            Assert.Equal(2, dishesList.Count);
            Assert.Equal(firstItem.Id, dishesList[0].Id);
        }
    }
}
