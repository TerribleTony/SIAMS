using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using SIAMS.Controllers;
using SIAMS.Data;
using SIAMS.Models;
using SIAMS.Services;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;


namespace SIAMS.Tests.Controllers
{
    public class AccountControllerTests
    {
        private readonly AccountController _controller;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly ApplicationDbContext _context;
        private readonly Mock<IConfiguration> _mockConfiguration;


        public AccountControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("TestDatabase")
                .Options;

            _context = new ApplicationDbContext(options);
            _mockEmailService = new Mock<IEmailService>();

            // Mock IConfiguration
            _mockConfiguration = new Mock<IConfiguration>();
            _mockConfiguration.SetupGet(x => x["Security:Pepper"]).Returns("TestPepper");

            _controller = new AccountController(_context, _mockEmailService.Object, _mockConfiguration.Object);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            SeedTestData();
        }

        private void SeedTestData()
        {
            // Clear existing data
            _context.Users.RemoveRange(_context.Users);
            _context.SaveChanges();

            // Add initial test data
            _context.Users.AddRange(
                new User
                {
                    
                    Username = "ExistingUser",
                    Email = "existinguser@example.com",
                    IsEmailConfirmed = false,
                    EmailConfirmationToken = "valid-token"
                }
            );
            _context.SaveChanges();
        }

        private void SeedTestData2()
        {
            _context.Users.RemoveRange(_context.Users);
            _context.SaveChanges();

            string pepper = "TestPepper";  // Ensure it matches the mocked value
            var (hashedPassword, salt) = AccountController.HashPasswordArgon2("CorrectPassword", pepper);

            _context.Users.Add(new User
            {
                Username = "ExistingUser",
                PasswordHash = hashedPassword,
                Salt = salt,
                Email = "existinguser@example.com",
                IsEmailConfirmed = true
            });

            _context.SaveChanges();
        }




        private void InitializeControllerWithHttpContextAndTempData()
        {
            // Create a default HttpContext
            var context = new DefaultHttpContext();

            // Mock authentication service
            var mockAuthService = new Mock<IAuthenticationService>();
            mockAuthService.Setup(x => x.SignInAsync(
                It.IsAny<HttpContext>(),
                CookieAuthenticationDefaults.AuthenticationScheme,
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()
            )).Returns(Task.CompletedTask);

            // Mock URL Helper Service
            var mockUrlHelper = new Mock<IUrlHelper>();
            mockUrlHelper.Setup(x => x.Action(It.IsAny<UrlActionContext>()))
                .Returns("http://mock-redirect-url");

            var mockUrlHelperFactory = new Mock<IUrlHelperFactory>();
            mockUrlHelperFactory.Setup(x => x.GetUrlHelper(It.IsAny<ActionContext>()))
                .Returns(mockUrlHelper.Object);

            // Create service provider mock
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(x => x.GetService(typeof(IAuthenticationService)))
                .Returns(mockAuthService.Object);
            serviceProviderMock.Setup(x => x.GetService(typeof(IUrlHelperFactory)))
                .Returns(mockUrlHelperFactory.Object);

            // Mock TempData
            var tempDataMock = new Mock<ITempDataDictionary>();

            // Assign to controller
            context.RequestServices = serviceProviderMock.Object;
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = context
            };
            _controller.TempData = tempDataMock.Object;
        }


        [Fact]
        public void HashPassword_ShouldReturnCorrectHash()
        {
            // Arrange
            string password = "Test@1234";
            string pepper = _mockConfiguration.Object["Security:Pepper"];

            // Act
            var (hash, salt) = AccountController.HashPasswordArgon2(password, pepper);

            // Assert
            Assert.NotNull(hash);
            Assert.NotEmpty(hash);
            Assert.NotEqual(password, hash);  // Ensure the hash differs from the raw password
            Assert.NotNull(salt); // Ensure a salt was generated
            Assert.NotEmpty(salt);
        }


        [Fact]
        public async Task Register_ShouldRedirectToLogin_WhenRegistrationSuccessful()
        {
            // Arrange: Initialize Controller Context
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            // Mock TempData
            var tempDataMock = new Mock<ITempDataDictionary>();
            _controller.TempData = tempDataMock.Object;

            var mockUrlHelper = new Mock<IUrlHelper>();
            mockUrlHelper
                .Setup(x => x.Action(It.IsAny<UrlActionContext>()))
                .Returns("http://mock-confirmation-link");

            // Assign mock to controller
            _controller.Url = mockUrlHelper.Object;

            // Create Registration Model
            var model = new RegisterViewModel
            {
                Username = "newuser",
                Password = "StrongP@ssw0rd!",
                Email = "newuser@example.com"
            };

            // Mock Email Service Expectation
            _mockEmailService
                .Setup(x => x.SendEmailAsync(
                    model.Email,
                    "Confirm Your Email",
                    It.IsAny<string>()
                ))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act: Call Register Method
            var result = await _controller.Register(model) as RedirectToActionResult;

            // Assert: Check Redirection & Email
            Assert.NotNull(result);
            Assert.Equal("Login", result.ActionName);
            Assert.Equal("Account", result.ControllerName);

            // Verify Email Sent
            _mockEmailService.Verify(
                x => x.SendEmailAsync(
                    model.Email,
                    "Confirm Your Email",
                    It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task Login_ShouldRedirectToHome_WhenCredentialsAreValid()
        {
            InitializeControllerWithHttpContextAndTempData();
            SeedTestData2();

            var model = new LoginViewModel
            {
                Username = "ExistingUser",
                Password = "CorrectPassword"
            };

            // Simulate environment variable setup for pepper
            Environment.SetEnvironmentVariable("PEPPER", "TestPepper");

            // Act
            var result = await _controller.Login(model) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            Assert.Equal("Home", result.ControllerName);
        }



        [Fact]
        public async Task Register_ShouldReturnViewWithErrors_WhenUsernameAlreadyExists()
        {
            // Arrange
            SeedTestData2(); // Ensure existing user in the DB
            var model = new RegisterViewModel
            {
                Username = "ExistingUser",
                Password = "StrongP@ssw0rd!",
                Email = "newemail@example.com"
            };

            // Mock UrlHelper
            var mockUrlHelper = new Mock<IUrlHelper>();
            mockUrlHelper
                .Setup(x => x.Action(It.IsAny<UrlActionContext>()))
                .Returns("http://mock-confirmation-link");

            _controller.Url = mockUrlHelper.Object;
           
            // Act
            var result = await _controller.Register(model) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.False(_controller.ModelState.IsValid, "ModelState should be invalid.");
            Assert.Contains("The username is already taken.", _controller.ModelState["Username"].Errors[0].ErrorMessage);
        }


        [Fact]
        public async Task Login_ShouldReturnViewWithErrors_WhenCredentialsAreInvalid()
        {
            InitializeControllerWithHttpContextAndTempData();

            // Arrange
            var model = new LoginViewModel
            {
                Username = "NonExistentUser",
                Password = "WrongPassword"
            };

            // Act
            var result = await _controller.Login(model) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.False(_controller.ModelState.IsValid);
            Assert.Contains("Invalid username or password.", _controller.ModelState[""].Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task ConfirmEmail_ShouldConfirmUser_WhenTokenIsValid()
        {
            SeedTestData();
            var tempDataMock = new Mock<ITempDataDictionary>();
            _controller.TempData = tempDataMock.Object;
            // Act
            var result = await _controller.ConfirmEmail("valid-token", "existinguser@example.com") as RedirectToActionResult;

            // Assert: Redirection Check
            Assert.NotNull(result);
            Assert.Equal("Login", result.ActionName);

            // Assert: Email Confirmation Check
            var updatedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "existinguser@example.com");
            Assert.True(updatedUser.IsEmailConfirmed);
            Assert.Null(updatedUser.EmailConfirmationToken);
        }


        [Fact]
        public async Task ConfirmEmail_ShouldReturnBadRequest_WhenTokenIsInvalid()
        {

            SeedTestData();
            // Arrange: Set up TempData to avoid null reference issues
            var tempDataMock = new Mock<ITempDataDictionary>();
            _controller.TempData = tempDataMock.Object;

            // Act: Call ConfirmEmail with an invalid token
            var result = await _controller.ConfirmEmail("invalid-token", "nonexistentuser@example.com");

            // Assert: Check that a BadRequestResult is returned
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("User not found.", notFoundResult.Value);
        }

        [Fact]
        public async Task Logout_ShouldSignOutUser_AndRedirectToLogin()
        {
            // Arrange: Mock services
            var mockAuthService = new Mock<IAuthenticationService>();
            mockAuthService.Setup(x => x.SignOutAsync(
                It.IsAny<HttpContext>(),
                CookieAuthenticationDefaults.AuthenticationScheme,
                null))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var mockUrlHelper = new Mock<IUrlHelper>();
            mockUrlHelper.Setup(x => x.Action(It.IsAny<UrlActionContext>()))
                .Returns("http://mock-redirect-url");

            var mockUrlHelperFactory = new Mock<IUrlHelperFactory>();
            mockUrlHelperFactory.Setup(x => x.GetUrlHelper(It.IsAny<ActionContext>()))
                .Returns(mockUrlHelper.Object);

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(x => x.GetService(typeof(IAuthenticationService)))
                .Returns(mockAuthService.Object);
            serviceProviderMock.Setup(x => x.GetService(typeof(IUrlHelperFactory)))
                .Returns(mockUrlHelperFactory.Object);

            var httpContext = new DefaultHttpContext
            {
                RequestServices = serviceProviderMock.Object
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act: Call the Logout action
            var result = await _controller.Logout();

            // Assert: Verify SignOutAsync was called
            mockAuthService.Verify(
                x => x.SignOutAsync(
                    It.IsAny<HttpContext>(),
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    null),
                Times.Once);

            // Assert: Check redirection result
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
            Assert.Equal("Account", redirectResult.ControllerName);
        }





    }
}
