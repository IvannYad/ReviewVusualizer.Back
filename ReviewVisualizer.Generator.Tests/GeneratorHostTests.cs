using Autofac;
using AutoFixture;
using ReviewVisualizer.Generator.Generator;
using ReviewVisualizer.Generator.Tests.Utils;

namespace ReviewVisualizer.Generator.Tests
{
    public class TestableGeneratorHost : GeneratorHost
    {
        public TestableGeneratorHost(ILifetimeScope container) : base(container)
        {
        }

        public int GetReviewersCount() => _reviewers.Count;
    }

    public class GeneratorHostTests : IClassFixture<GeneratorHostFixture>
    {
        private readonly GeneratorHostFixture _fixture;
        private readonly TestableGeneratorHost _generatorHost;
        
        public GeneratorHostTests(GeneratorHostFixture fixture)
        {
            _fixture = fixture;
            _fixture.FakeDbContext.Database.EnsureDeleted();
            _fixture.FakeDbContext.Database.EnsureCreated();

            // Add initial reviewers data to in-memory dbContext.
            _fixture.FakeDbContext.Reviewers.RemoveRange(_fixture.FakeDbContext.Reviewers);
            _fixture.FakeDbContext.Reviewers.AddRange(FakeDataGenerator.GetReviewersForGeneratorTests());
            _fixture.FakeDbContext.SaveChanges();

            _generatorHost = new TestableGeneratorHost(_fixture.Container);
        }

        [Fact]
        public void CreateReviewer_ValidReviewer_ReviewerIsAdded()
        {
            // Arrange.
            int initialReviewersCount = _generatorHost.GetReviewersCount();
            var newReviewer = FakeDataGenerator.GetReviewer(_fixture.FakeDbContext.Teachers.ToList());

            // Act.
            _generatorHost.CreateReviewer(newReviewer);
            int actualReviewersCount = _generatorHost.GetReviewersCount();

            // Assert.
            Assert.Equal(initialReviewersCount + 1, actualReviewersCount);
        }
    }
}
