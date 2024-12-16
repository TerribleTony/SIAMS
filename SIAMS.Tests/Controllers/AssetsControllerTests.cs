using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAMS.Controllers;
using SIAMS.Data;
using SIAMS.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SIAMS.Tests.Controllers
{
    public class AssetsControllerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly AssetsController _controller;

        public AssetsControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())  // Unique DB for each test
                .Options;


            _context = new ApplicationDbContext(options);
            _controller = new AssetsController(_context);

            SeedTestData();
        }

        private void SeedTestData()
        {
            // Clear existing data
            _context.Assets.RemoveRange(_context.Assets);
            _context.Users.RemoveRange(_context.Users);
            _context.SaveChanges();

            _context.Users.Add(new User { UserId = 1, Username = "TestUser" });
            _context.Assets.AddRange(
                new Asset { AssetId = 1, AssetName = "Asset1", Category = "Category1", AssignedUserId = 1 },
                new Asset { AssetId = 2, AssetName = "Asset2", Category = "Category2", AssignedUserId = 1 }
            );
            _context.SaveChanges();
        }

        // Test 1: Index Action
        [Fact]
        public async Task Index_ShouldReturnViewWithAssets()
        {
            // Act
            var result = await _controller.Index() as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsAssignableFrom<List<Asset>>(result.Model);
            Assert.Equal(2, model.Count);
        }

        // Test 2: Details Action - Valid Asset
        [Fact]
        public async Task Details_ShouldReturnView_WhenAssetExists()
        {
            // Act
            var result = await _controller.Details(1) as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsType<Asset>(result.Model);
            Assert.Equal("Asset1", model.AssetName);
        }

        // Test 3: Details Action - Invalid Asset
        [Fact]
        public async Task Details_ShouldReturnNotFound_WhenAssetDoesNotExist()
        {
            // Act
            var result = await _controller.Details(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        // Test 4: Create Action (GET)
        [Fact]
        public void Create_ShouldReturnView()
        {
            // Act
            var result = _controller.Create();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        // Test 5: Create Action (POST) - Valid
        [Fact]
        public async Task Create_ShouldRedirectToIndex_WhenModelIsValid()
        {
            // Arrange
            var asset = new Asset
            {
                AssetId = 3,
                AssetName = "NewAsset",
                Category = "NewCategory",
                AssignedUserId = 1
            };

            // Act
            var result = await _controller.Create(asset) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            var filteredAssets = await _context.Assets.Where(a => a.AssetName == "NewAsset").ToListAsync();
            Assert.Single(filteredAssets);

        }

        // Test 6: Delete Action - Valid
        [Fact]
        public async Task DeleteConfirmed_ShouldRedirectToIndex_WhenAssetExists()
        {
            // Act
            var result = await _controller.DeleteConfirmed(1) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            Assert.DoesNotContain(await _context.Assets.ToListAsync(), a => a.AssetId == 1);
        }

        // Test 7: Delete Action - Invalid
        [Fact]
        public async Task DeleteConfirmed_ShouldReturnRedirect_WhenAssetDoesNotExist()
        {
            // Act
            var result = await _controller.DeleteConfirmed(999) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
        }
    }
}
