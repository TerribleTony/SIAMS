using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SIAMS.Controllers;
using SIAMS.Models;
using Xunit;

namespace SIAMS.Tests.Controllers
{
    public class HomeControllerTests
    {
        private readonly HomeController _controller;

        public HomeControllerTests()
        {
            var mockLogger = new Mock<ILogger<HomeController>>();
            _controller = new HomeController(mockLogger.Object);

            // Mock the HttpContext to avoid NullReferenceException
            var context = new DefaultHttpContext();
            context.TraceIdentifier = "TestRequestId";
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = context
            };
        }

        [Fact]
        public void Index_ShouldReturnView()
        {
            // Act
            var result = _controller.Index() as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result?.ViewName ?? "Index");
        }

        [Fact]
        public void Privacy_ShouldReturnView()
        {
            // Act
            var result = _controller.Privacy() as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Privacy", result?.ViewName ?? "Privacy");
        }

        [Fact]
        public void Error_ShouldReturnView_WithErrorViewModel()
        {
            // Act
            var result = _controller.Error() as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ViewResult>(result);

            var model = result?.Model as ErrorViewModel;
            Assert.NotNull(model);
            Assert.Equal("TestRequestId", model?.RequestId);
        }
    }
}
