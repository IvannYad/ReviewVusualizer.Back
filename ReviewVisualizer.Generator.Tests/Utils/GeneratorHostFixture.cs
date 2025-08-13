using Autofac;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ReviewVisualizer.Data.Models;
using ReviewVisualizer.Data;
using ReviewVisualizer.Generator.Generator;
using Moq;

namespace ReviewVisualizer.Generator.Tests.Utils
{
    public class GeneratorHostFixture
    {
        public IContainer Container { get; init; }
        public ApplicationDbContext FakeDbContext { get; init; }
        public Mock<ILogger<GeneratorHost>> MockLogger { get; init; }
        public Mock<IMapper> MockMapper { get; init; }
        
        public GeneratorHostFixture()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                  .UseInMemoryDatabase("TestDb")
                  .Options;

            FakeDbContext = new ApplicationDbContext(options);
            MockLogger = new Mock<ILogger<GeneratorHost>>();
            MockMapper = new Mock<IMapper>();

            var builder = new ContainerBuilder();

            // Register ApplicationDbContext as a scoped service
            builder.Register(x => FakeDbContext)
                   .As<ApplicationDbContext>()
                   .InstancePerLifetimeScope();

            // Register ILogger<T>
            builder.Register(x => MockLogger.Object)
                   .As<ILogger<GeneratorHost>>()
                   .SingleInstance();

            // Register AutoMapper IMapper
            builder.Register(ctx => MockMapper.Object).As<IMapper>().SingleInstance();

            Container = builder.Build();
        }
    }
}
