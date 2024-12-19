using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using SIAMS.Controllers;
using SIAMS.Data;
using SIAMS.Models;
using Xunit;

namespace SIAMS.Tests.Controllers
{
    public class UserManagementControllerTests
    {
        /// <summary>
        /// create a unique in memory database for each subsequent test on the page. 
        /// </summary>
        /// <returns></returns>
        private ApplicationDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())  // Unique DB per test
                .Options;

            return new ApplicationDbContext(options);
        }

        /// <summary>
        /// Create the UserManagementController and mock TempData
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private UserManagementController GetController(ApplicationDbContext context)
        {
            var controller = new UserManagementController(context);

            // Mock TempData
            var tempDataMock = new Mock<ITempDataDictionary>();
            controller.TempData = tempDataMock.Object;

            return controller;
        }

        /// <summary>
        /// seed the test data into the database
        /// </summary>
        /// <param name="context"></param>
        private void SeedTestData(ApplicationDbContext context)
        {
            context.Users.AddRange(
                new User { UserId = 1, Username = "AdminUser", Email = "admin@example.com", Role = "Admin" },
                new User { UserId = 2, Username = "RegularUser", Email = "user@example.com", Role = "User" }
            );
            context.SaveChanges();
        }

        // Test: Index action should return a view with the list of users
        [Fact]
        public async Task Index_ShouldReturnViewWithUsers()
        {
            var context = GetDbContext(); 
            SeedTestData(context);

            var controller = GetController(context);

            // Act: Call the Index action as if the page is opening
            var result = await controller.Index();

            // Assert: Check if the result is a ViewResult and contains the expected model
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<User>>(viewResult.Model);
            Assert.Equal(2, model.Count);
        }

        [Fact]
        public async Task Edit_Post_ShouldUpdateUser_WhenModelIsValid()
        {
            var context = GetDbContext();
            SeedTestData(context);
            var controller = GetController(context);

            var updatedUser = new User
            {
                UserId = 1,
                Username = "UpdatedAdmin",
                Email = "updatedadmin@example.com",
                Role = "Admin"
            };

            var result = await controller.Edit(updatedUser);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            var userInDb = await context.Users.FindAsync(1);
            Assert.Equal("UpdatedAdmin", userInDb.Username);
            Assert.Equal("updatedadmin@example.com", userInDb.Email);
        }

        [Fact]
        public async Task Delete_ShouldRemoveUser_WhenUserExists()
        {
            var context = GetDbContext();
            SeedTestData(context);
            var controller = GetController(context);

            var result = await controller.Delete(2);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            var user = await context.Users.FindAsync(2);
            Assert.NotNull(user);
            Assert.True(user.IsDeleted);
        }
     
    }
}
