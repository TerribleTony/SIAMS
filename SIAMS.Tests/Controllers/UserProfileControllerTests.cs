﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using SIAMS.Controllers;
using SIAMS.Data;
using SIAMS.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace SIAMS.Tests.Controllers
{
    public class UserProfileControllerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly UserProfileController _controller;

        public UserProfileControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("TestDatabase_UserProfile")
                .Options;

            _context = new ApplicationDbContext(options);
            _controller = new UserProfileController(_context);
        }

        private void InitializeHttpContext(string username)
        {
            var context = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, username)
                }, "mock"))
            };
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = context
            };
        }

        private void SeedTestData()
        {
            // Clear existing data
            _context.Assets.RemoveRange(_context.Assets);
            _context.Users.RemoveRange(_context.Users);
            _context.SaveChanges();

            _context.Users.AddRange(
                new User
                {
                    Username = "ExistingUser",
                    Email = "existinguser@example.com"
                }
            );

            _context.Logs.AddRange(
                new Log { PerformedBy = "ExistingUser", Action = "Login", Timestamp = DateTime.UtcNow },
                new Log { PerformedBy = "ExistingUser", Action = "Edit Profile", Timestamp = DateTime.UtcNow.AddMinutes(-5) }
            );

            _context.SaveChanges();
        }

        [Fact]
        public async Task Index_ShouldReturnViewWithUserProfile_WhenUserIsAuthenticated()
        {
            // Arrange
            SeedTestData();
            InitializeHttpContext("ExistingUser");

            // Act
            var result = await _controller.Index() as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.IsType<UserProfileViewModel>(result.Model);

            var model = result.Model as UserProfileViewModel;
            Assert.Equal("ExistingUser", model.User.Username);
            Assert.NotEmpty(model.RecentLogs);
        }

        [Fact]
        public async Task Index_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
        {
            // Act
            var result = await _controller.Index();

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Edit_ShouldReturnViewWithUserDetails_WhenUserExists()
        {
            // Arrange: Clear the database to avoid data leakage
            _context.Users.RemoveRange(_context.Users);
            await _context.SaveChangesAsync();

            // Seed fresh test data
            SeedTestData();
            InitializeHttpContext("ExistingUser");

            // Act
            var result = await _controller.Edit() as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.IsType<EditUserViewModel>(result.Model);

            var model = result.Model as EditUserViewModel;
            Assert.Equal("existinguser@example.com", model.Email);
        }


        [Fact]
        public async Task Edit_ShouldUpdateEmail_WhenModelIsValid()
        {
            // Arrange
            SeedTestData();
            InitializeHttpContext("ExistingUser");

            var updatedModel = new EditUserViewModel
            {
                Email = "updateduser@example.com"
            };

            // Act
            var result = await _controller.Edit(updatedModel) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);

            var updatedUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "ExistingUser");
            Assert.Equal("updateduser@example.com", updatedUser.Email);
        }

        [Fact]
        public async Task RequestAdmin_ShouldMarkRequest_WhenUserExists_AndNotAdmin()
        {
            // Arrange: Reset and seed data
            _context.Users.RemoveRange(_context.Users);
            _context.Logs.RemoveRange(_context.Logs);
            await _context.SaveChangesAsync();

            // Seed test data
            _context.Users.Add(new User
            {
                UserId = 1,
                Username = "ExistingUser",
                Role = "User",  // Not an admin initially
                IsAdminRequested = false
            });
            _context.Users.Add(new User
            {
                UserId = 2,
                Username = "AdminUser",
                Role = "Admin",  // Already an admin
                IsAdminRequested = false
            });
            await _context.SaveChangesAsync();

            // Mock HttpContext with an authenticated user who is **not an admin**
            var mockHttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.Name, "ExistingUser") }, "TestAuthType"))
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext
            };

            // Mock TempData
            var tempDataMock = new Mock<ITempDataDictionary>();
            _controller.TempData = tempDataMock.Object;

            // Act: Request admin access
            var result = await _controller.RequestAdmin() as RedirectToActionResult;

            // Assert: User's request should be marked and logged
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            Assert.Equal("UserProfile", result.ControllerName);

            var updatedUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "ExistingUser");
            Assert.NotNull(updatedUser);
            Assert.True(updatedUser.IsAdminRequested);

            var log = await _context.Logs.FirstOrDefaultAsync(l => l.Action.Contains("requested admin access"));
            Assert.NotNull(log);
            Assert.Equal("ExistingUser", log.PerformedBy);

            // Verify TempData for non-admin user
            tempDataMock.VerifySet(t => t["Message"] = "Admin request submitted successfully!", Times.Once);

            // Act: Simulate an admin trying to request admin access
            mockHttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Name, "AdminUser") }, "TestAuthType"));

            _controller.ControllerContext.HttpContext = mockHttpContext;

            result = await _controller.RequestAdmin() as RedirectToActionResult;

            // Assert: No changes should happen for an admin
            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "AdminUser");
            Assert.NotNull(adminUser);
            Assert.False(adminUser.IsAdminRequested);  // This should remain false

            log = await _context.Logs.FirstOrDefaultAsync(l => l.PerformedBy == "AdminUser");
            Assert.Null(log);  // No log should be created

            // Verify TempData for admin user
            tempDataMock.VerifySet(t => t["Message"] = "You are already an admin.", Times.Once);
        }    

    }
}
