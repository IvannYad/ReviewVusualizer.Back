using FluentAssertions;
using ReviewVisualizer.Data.Dto;
using ReviewVisualizer.Data.Enums;
using ReviewVisualizer.Data.Models;
using ReviewVisualizer.Generator.IntegrationTests.Utils;
using ReviewVisualizer.TestUtils;
using System.Net;
using System.Net.Http.Json;
using Xunit.Abstractions;

namespace ReviewVisualizer.Generator.IntegrationTests
{
    public class ReviewerControllerTests : IClassFixture<VisualizerProjectFixture>
    {
        private const string ReviewerControllerBaseAddress = "/reviewers";
        private readonly VisualizerProjectFixture _visualizerFixture;
        private readonly ITestOutputHelper _output;

        public ReviewerControllerTests(VisualizerProjectFixture visualizerFixture
            , ITestOutputHelper output)
        {
            _visualizerFixture = visualizerFixture;
            _output = output;
        }

        [Fact]
        public async Task TryAccessGetReviewers_UnauthorizedUser_Returns401()
        {
            // Arrange.
            var visitorHandler = _visualizerFixture.HandlersFactory
                .GetHandler(TestUser.NotAuthorized);
            var generatorClient = _visualizerFixture.GeneratorFactory
                .CreateDefaultClient(visitorHandler);

            // Act.
            var response = await generatorClient.GetAsync(ReviewerControllerBaseAddress);

            // Assert.
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            //response.StatusCode.Should().Be(HttpStatusCode.OK);

        }

        [Theory]
        [InlineData(TestUser.Visitor)]
        [InlineData(TestUser.Analyst)]
        public async Task TryAccessGetReviewers_ForbiddenUser_Returns403(TestUser user)
        {
            // Arrange.
            var visitorHandler = _visualizerFixture.HandlersFactory
                .GetHandler(user);
            var generatorClient = _visualizerFixture.GeneratorFactory
                .CreateDefaultClient(visitorHandler);

            // Act.
            var response = await generatorClient.GetAsync(ReviewerControllerBaseAddress);

            // Assert.
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Theory]
        [InlineData(TestUser.GeneratorAdmin_FireAndForget)]
        [InlineData(TestUser.GeneratorAdmin_Delayed)]
        [InlineData(TestUser.GeneratorAdmin_Recurring)]
        [InlineData(TestUser.Owner)]
        public async Task TryAccessGetReviewers_UserWithAccess_UserAllowedToAccess(TestUser user)
        {
            // Arrange.
            var visitorHandler = _visualizerFixture.HandlersFactory
                .GetHandler(user);
            var generatorClient = _visualizerFixture.GeneratorFactory
                .CreateDefaultClient(visitorHandler);

            // Act.
            var response = await generatorClient.GetAsync(ReviewerControllerBaseAddress);

            // Assert.
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #region CreateAsync tests

        [Fact]
        public async Task CreateAsync_UnauthorizedUser_Returns401()
        {
            // Arrange.
            var visitorHandler = _visualizerFixture.HandlersFactory
                .GetHandler(TestUser.NotAuthorized);
            var generatorClient = _visualizerFixture.GeneratorFactory
                .CreateDefaultClient(visitorHandler);

            // Act.
            var response = await generatorClient.PostAsJsonAsync(ReviewerControllerBaseAddress, new { });

            // Assert.
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Theory]
        [InlineData(TestUser.Visitor)]
        [InlineData(TestUser.Analyst)]
        [InlineData(TestUser.GeneratorAdmin_FireAndForget, GeneratorType.DELAYED)]
        [InlineData(TestUser.GeneratorAdmin_Recurring, GeneratorType.FIRE_AND_FORGET)]
        [InlineData(TestUser.GeneratorAdmin_Delayed, GeneratorType.RECURRING)]
        public async Task CreateAsync_ForbiddenUsers_Returns403(TestUser user, GeneratorType generatorType = GeneratorType.FIRE_AND_FORGET)
        {
            // Arrange.
            var visitorHandler = _visualizerFixture.HandlersFactory
                .GetHandler(user);
            var generatorClient = _visualizerFixture.GeneratorFactory
                .CreateDefaultClient(visitorHandler);
            var reviewerCreateDto = DefaultReviewerCreateDto;
            reviewerCreateDto.Type = generatorType;

            // Act.
            var response = await generatorClient
                .PostAsJsonAsync(ReviewerControllerBaseAddress, reviewerCreateDto);

            // Assert.
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Theory]
        [InlineData(TestUser.GeneratorAdmin_FireAndForget, GeneratorType.FIRE_AND_FORGET)]
        [InlineData(TestUser.GeneratorAdmin_Recurring, GeneratorType.RECURRING)]
        [InlineData(TestUser.GeneratorAdmin_Delayed, GeneratorType.DELAYED)]
        [InlineData(TestUser.Owner, GeneratorType.FIRE_AND_FORGET)]
        [InlineData(TestUser.Owner, GeneratorType.RECURRING)]
        [InlineData(TestUser.Owner, GeneratorType.DELAYED)]
        public async Task CreateAsync_ValidCreateCombinations_ReviewerIsCreated(TestUser user, GeneratorType generatorType = GeneratorType.FIRE_AND_FORGET)
        {
            // Arrange.
            var visitorHandler = _visualizerFixture.HandlersFactory
                .GetHandler(user);
            var generatorClient = _visualizerFixture.GeneratorFactory
                .CreateDefaultClient(visitorHandler);
            var reviewerCreateDto = DefaultReviewerCreateDto;
            reviewerCreateDto.Type = generatorType;

            // Act.
            var response = await generatorClient
                .PostAsJsonAsync(ReviewerControllerBaseAddress, reviewerCreateDto);

            var reviewerId = (await response.Content.GetEntityAsync<Reviewer>())?.Id;
            var getReviewerResponse = await generatorClient
                .GetAsync($"{ReviewerControllerBaseAddress}/{reviewerId}");
            var getReviewer = await getReviewerResponse.Content.GetEntityAsync<Reviewer>();

            // Assert.
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            getReviewerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            getReviewer.Should().NotBeNull();
        }

        #endregion

        #region DeleteAsync tests

        [Fact]
        public async Task DeleteAsync_UnauthorizedUser_Returns401()
        {
            // Arrange.
            int defaultReviewerId = 1;
            var visitorHandler = _visualizerFixture.HandlersFactory
                .GetHandler(TestUser.NotAuthorized);
            var generatorClient = _visualizerFixture.GeneratorFactory
                .CreateDefaultClient(visitorHandler);

            // Act.
            var response = await generatorClient
                .DeleteAsync($"{ReviewerControllerBaseAddress}?reviewerId={defaultReviewerId}");

            // Assert.
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Theory]
        [InlineData(TestUser.GeneratorAdmin_FireAndForget)]
        [InlineData(TestUser.GeneratorAdmin_Recurring)]
        [InlineData(TestUser.GeneratorAdmin_Delayed)]
        [InlineData(TestUser.Owner)]
        public async Task DeleteAsync_NonExistingReviewer_ReturnsOk(TestUser userType)
        {
            // Arrange.
            int nonExistingReviewerId = 100;
            var visitorHandler = _visualizerFixture.HandlersFactory
                .GetHandler(userType);
            var generatorClient = _visualizerFixture.GeneratorFactory
                .CreateDefaultClient(visitorHandler);

            // Act.
            var response = await generatorClient
                .DeleteAsync($"{ReviewerControllerBaseAddress}?reviewerId={nonExistingReviewerId}");

            // Assert.
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }


        [Theory]
        [InlineData(TestUser.GeneratorAdmin_FireAndForget, GeneratorType.DELAYED)]
        [InlineData(TestUser.GeneratorAdmin_Recurring, GeneratorType.FIRE_AND_FORGET)]
        [InlineData(TestUser.GeneratorAdmin_Delayed, GeneratorType.RECURRING)]
        public async Task DeleteAsync_ForbiddenUsers_Returns403(TestUser user, GeneratorType generatorType = GeneratorType.FIRE_AND_FORGET)
        {
            // Arrange.
            var visitorHandler = _visualizerFixture.HandlersFactory
                .GetHandler(user);
            var generatorClient = _visualizerFixture.GeneratorFactory
                .CreateDefaultClient(visitorHandler);

            // Get reviewer with specified generatorType.
            // No need to create reviewer ahead, since we will be unsuccessfull to delete it.
            var getReviewerResponse = await generatorClient
                .GetAsync($"{ReviewerControllerBaseAddress}/type/{(int)generatorType}");
            var getReviewerId = (await getReviewerResponse.Content.GetEntityAsync<List<Reviewer>>())
                ?.FirstOrDefault()?.Id;

            // Act.
            var response = await generatorClient
                .DeleteAsync($"{ReviewerControllerBaseAddress}?reviewerId={getReviewerId}");

            // Assert.
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Theory]
        [InlineData(TestUser.GeneratorAdmin_FireAndForget, GeneratorType.FIRE_AND_FORGET)]
        [InlineData(TestUser.Owner, GeneratorType.FIRE_AND_FORGET)]
        [InlineData(TestUser.Owner, GeneratorType.DELAYED)]
        public async Task DeleteAsync_ValidDeleteCombinations_ReviewerIsDeleted(TestUser user, GeneratorType generatorType = GeneratorType.FIRE_AND_FORGET)
        {
            // Arrange.
            var visitorHandler = _visualizerFixture.HandlersFactory
                .GetHandler(user);
            var generatorClient = _visualizerFixture.GeneratorFactory
                .CreateDefaultClient(visitorHandler);

            // To make test more isolated, create test reviewer and delete it. This will not affect
            // main data, so other tests will not be dependent on when delete tests are run.
            var reviewerCreateDto = DefaultReviewerCreateDto;
            reviewerCreateDto.Type = generatorType;
            var response = await generatorClient
                .PostAsJsonAsync(ReviewerControllerBaseAddress, reviewerCreateDto);
            var reviewerId = (await response.Content.GetEntityAsync<Reviewer>())?.Id;

            // Act.
            var deleteResponse = await generatorClient
                .DeleteAsync($"{ReviewerControllerBaseAddress}?reviewerId={reviewerId}");
            _output.WriteLine($"deleteResponse: {await deleteResponse.Content.ReadAsStringAsync()}");
            _output.WriteLine($"deleteResponse StatusCode: {deleteResponse.StatusCode}");
            var getReviewerResponse = await generatorClient
                .GetAsync($"{ReviewerControllerBaseAddress}/{reviewerId}");

            // Assert.
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            getReviewerResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        //private static Reviewer GetReviewer(GeneratorType generatorType)
        //{
        //    var reviewer = ModelFakers.ReviewerFaker.Generate();
        //    reviewer.Type = generatorType;
        //    return reviewer;
        //}

        private static ReviewerCreateDTO DefaultReviewerCreateDto =>
            new ReviewerCreateDTO()
            {
                Name = "reviewer",
                Type = GeneratorType.FIRE_AND_FORGET,
                CommunicationMinGrage = 50,
                CommunicationMaxGrage = 100,
                StudentsSupportMinGrage = 50,
                StudentsSupportMaxGrage = 100,
                TeachingQualityMinGrage = 50,
                TeachingQualityMaxGrage = 100
            };
    }
}