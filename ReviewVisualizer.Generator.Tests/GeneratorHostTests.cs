using Autofac;
using AutoFixture;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using ReviewVisualizer.Data;
using ReviewVisualizer.Data.Dto;
using ReviewVisualizer.Data.Enums;
using ReviewVisualizer.Generator.Generator;
using ReviewVisualizer.Generator.Tests.Utils;
using System.Linq.Expressions;

namespace ReviewVisualizer.Generator.Tests
{
    public class TestableGeneratorHost : GeneratorHost
    {
        public TestableGeneratorHost(ILifetimeScope container) : base(container)
        {
        }

        public int GetReviewersCount() => _reviewers.Count;

        public new void GenerateFireAndForget(int reviewerId)
        {
            base.GenerateFireAndForget(reviewerId);
        }

        public new void GenerateDelayed(int reviewerId, TimeSpan delay)
        {
            base.GenerateDelayed(reviewerId, delay);
        }

        public new void GenerateRecurring(int reviewerId, string cron)
        {
            base.GenerateRecurring(reviewerId, cron);
        }
    }

    public class GeneratorHostTests : IClassFixture<GeneratorHostFixture>
    {
        private readonly GeneratorHostFixture _fixture;
        private readonly TestableGeneratorHost _generatorHost;
        private readonly Mock<IHangfireProxy> _mockHangfireProxy;

        public GeneratorHostTests(GeneratorHostFixture fixture)
        {
            _fixture = fixture;
            _fixture.FakeDbContext.Database.EnsureDeleted();
            _fixture.FakeDbContext.Database.EnsureCreated();

            // Add initial reviewers data to in-memory dbContext.
            _fixture.FakeDbContext.Reviewers.RemoveRange(_fixture.FakeDbContext.Reviewers);
            _fixture.FakeDbContext.Reviewers.AddRange(FakeDataGenerator.GetReviewersForGeneratorTests());
            _fixture.FakeDbContext.SaveChanges();

            var builder = new ContainerBuilder();

            // Register ApplicationDbContext as a scoped service.
            builder.Register(x => _fixture.FakeDbContext)
                   .As<ApplicationDbContext>()
                   .InstancePerLifetimeScope();

            // Register ILogger<T>.
            builder.Register(x => _fixture.MockLogger.Object)
                   .As<ILogger<GeneratorHost>>()
                   .SingleInstance();

            // Register AutoMapper IMapper.
            builder.Register(ctx => _fixture.MockMapper.Object).As<IMapper>().SingleInstance();

            // Register HangfireProxy.
            _mockHangfireProxy = new Mock<IHangfireProxy>();
            builder.Register(ctx => _mockHangfireProxy.Object).As<IHangfireProxy>();


            _generatorHost = new TestableGeneratorHost(builder.Build());
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

        [Fact]
        public void GenerateFireAndForget_ValidReviewer_ReviewIsGenerated()
        {
            // Arrange.
            int reviewerId = 1;

            // Act.
            _generatorHost.GenerateFireAndForget(reviewerId);

            // Assert.
            _mockHangfireProxy.Verify(pr => pr.SheduleFireAndForget(It.IsAny<Expression<Action>>()), Times.Once);
        }

        [Fact]
        public void GenerateDelayed_ValidReviewer_ReviewIsGenerated()
        {
            // Arrange.
            int reviewerId = 1;
            TimeSpan delay = TimeSpan.FromSeconds(1);
            string queueName = "delayed_queue";

            // Act.
            _generatorHost.GenerateDelayed(reviewerId, delay);

            // Assert.
            _mockHangfireProxy.Verify(pr =>
                pr.SheduleDelayed(queueName, It.IsAny<Expression<Action>>(), delay),
                Times.Once);
        }

        [Fact]
        public void GenerateRecurring_ValidReviewer_ReviewIsGenerated()
        {
            // Arrange.
            int reviewerId = 1;
            string cronExpression = "*/15 * * * *";

            // Act.
            _generatorHost.GenerateRecurring(reviewerId, cronExpression);

            // Assert.
            _mockHangfireProxy.Verify(pr =>
                pr.SheduleRecurring(reviewerId.ToString(), It.IsAny<Expression<Action>>(), cronExpression),
                Times.Once);
        }

        [Theory]
        [InlineData(GeneratorType.FIRE_AND_FORGET)]
        [InlineData(GeneratorType.DELAYED)]
        public void DeleteReviewer_NotRecurringReviewerPresentInGenerator_ReviewerIsRemoved(GeneratorType type)
        {
            // Arrange.
            int initialReviewersCount = _generatorHost.GetReviewersCount();
            var reviewer = _fixture.FakeDbContext.Reviewers.First(r => r.Type == type);

            // Act.
            _generatorHost.DeleteReviewer(reviewer.Id);
            int actualReviewersCount = _generatorHost.GetReviewersCount();

            // Assert.
            Assert.Equal(initialReviewersCount - 1, actualReviewersCount);
            _mockHangfireProxy.Verify(
                pr => pr.RemoveIfExists(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void DeleteReviewer_RecurringReviewer_ReviewerIsRemovedAndRecurringJobIsUnregistered()
        {
            // Arrange.
            int initialReviewersCount = _generatorHost.GetReviewersCount();
            var reviewer = _fixture.FakeDbContext.Reviewers.First(r => r.Type == GeneratorType.RECURRING);

            // Act.
            _generatorHost.DeleteReviewer(reviewer.Id);
            int actualReviewersCount = _generatorHost.GetReviewersCount();

            // Assert.
            Assert.Equal(initialReviewersCount - 1, actualReviewersCount);
            _mockHangfireProxy.Verify(
                pr => pr.RemoveIfExists(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void DeleteReviewer_ReviewerNotInGenerator_ReviewersCollectionsIsNotCHanged()
        {
            // Arrange.
            int nonExistingReviewerId = 100;
            int initialReviewersCount = _generatorHost.GetReviewersCount();

            // Act.
            _generatorHost.DeleteReviewer(nonExistingReviewerId);
            int actualReviewersCount = _generatorHost.GetReviewersCount();

            // Assert.
            Assert.Equal(initialReviewersCount, actualReviewersCount);
            _mockHangfireProxy.Verify(
                pr => pr.RemoveIfExists(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void GenerateReviewWithRequest_FireAndForgetReviewer_CorrespondingReviewGenerated()
        {
            // Arrange.
            var reviewer = _fixture.FakeDbContext.Reviewers
                .FirstOrDefault(r => r.Type == GeneratorType.FIRE_AND_FORGET);
            var generateReviewRequest = _fixture.AutoFixture.Create<GenerateReviewRequest>();
            generateReviewRequest.ReviewerId = reviewer?.Id ?? 0;
            generateReviewRequest.Type = reviewer?.Type ?? GeneratorType.FIRE_AND_FORGET;

            // Act.
            _generatorHost.GenerateReview(generateReviewRequest);

            // Assert.
            _mockHangfireProxy.Verify(pr => pr.SheduleFireAndForget(It.IsAny<Expression<Action>>()), Times.Once);
        }

        [Fact]
        public void GenerateReviewWithRequest_DelayedReviewer_CorrespondingReviewGenerated()
        {
            // Arrange.
            var reviewer = _fixture.FakeDbContext.Reviewers
                .FirstOrDefault(r => r.Type == GeneratorType.DELAYED);
            var generateReviewRequest = _fixture.AutoFixture.Create<GenerateReviewRequest>();
            generateReviewRequest.ReviewerId = reviewer?.Id ?? 0;
            generateReviewRequest.Type = reviewer?.Type ?? GeneratorType.DELAYED;

            // Act.
            _generatorHost.GenerateReview(generateReviewRequest);

            // Assert.
            _mockHangfireProxy.Verify(pr =>
                pr.SheduleDelayed(It.IsAny<string>(), It.IsAny<Expression<Action>>(), generateReviewRequest.Delay!.Value),
                Times.Once);
        }

        [Fact]
        public void GenerateReviewWithRequest_RecurringReviewer_CorrespondingReviewGenerated()
        {
            // Arrange.
            var reviewer = _fixture.FakeDbContext.Reviewers
                .FirstOrDefault(r => r.Type == GeneratorType.RECURRING);
            var generateReviewRequest = _fixture.AutoFixture.Create<GenerateReviewRequest>();
            generateReviewRequest.ReviewerId = reviewer?.Id ?? 0;
            generateReviewRequest.Type = reviewer?.Type ?? GeneratorType.RECURRING;

            // Act.
            _generatorHost.GenerateReview(generateReviewRequest);

            // Assert.
            _mockHangfireProxy.Verify(pr =>
                pr.SheduleRecurring(generateReviewRequest.ReviewerId.ToString(), It.IsAny<Expression<Action>>(), generateReviewRequest.Cron!),
                Times.Once);
        }

        [Fact]
        public void GenerateReviewWithRequest_NonExistingReviewer_ExceptionIsThrown()
        {
            // Arrange.
            var generateReviewRequest = _fixture.AutoFixture.Create<GenerateReviewRequest>();
            generateReviewRequest.ReviewerId = 0;

            // Act.
            var exceptionAction = () => _generatorHost.GenerateReview(generateReviewRequest);

            // Assert.
            Assert.Throws<ArgumentException>(exceptionAction);
        }
    }
}