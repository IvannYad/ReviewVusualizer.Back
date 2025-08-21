using Autofac;
using AutoFixture;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ReviewVisualizer.Data;
using ReviewVisualizer.Generator.Generator;

namespace ReviewVisualizer.Generator.Tests.Utils
{
    public class GeneratorHostFixture
    {
        public ApplicationDbContext FakeDbContext { get; init; }
        public Mock<ILogger<GeneratorHost>> MockLogger { get; init; }
        public Mock<IMapper> MockMapper { get; init; }
        public Fixture AutoFixture { get; init; }

        public GeneratorHostFixture()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                  .UseInMemoryDatabase("TestDb")
                  .Options;

            FakeDbContext = new ApplicationDbContext(options);
            MockLogger = new Mock<ILogger<GeneratorHost>>();
            MockMapper = new Mock<IMapper>();
            AutoFixture = new Fixture();
        }
    }
}