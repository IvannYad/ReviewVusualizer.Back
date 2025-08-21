using Microsoft.EntityFrameworkCore;
using ReviewVisualizer.AuthLibrary;
using ReviewVisualizer.AuthLibrary.Enums;
using ReviewVisualizer.AuthLibrary.Extensions;
using ReviewVisualizer.Data;
using ReviewVisualizer.Data.Enums;
using ReviewVisualizer.Data.Models;
using ReviewVisualizer.TestUtils;

namespace ReviewVisualizer.Generator.IntegrationTests.Utils
{
    internal static class TestDataSeeding
    {
        private const int InitialDepartmentsCount = 3;
        private const int InitialTeachersCount = 7;

        public const string TestUserPassword = "test1243";

        public static void SeedData(ApplicationDbContext context, PasswordService passwordService, Dictionary<TestUser, User>? userTypesIds = null)
        {
            try
            {
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();

                var userTypesAssociations = GetUsersTypesAssociations(passwordService);

                context.Users.AddRange(userTypesAssociations.Values);
                context.SaveChanges();

                if (userTypesIds is not null)
                {
                    foreach (var utAssoc in userTypesAssociations)
                    {
                        userTypesIds[utAssoc.Key] = utAssoc.Value;
                    }
                }

                context.Reviewers.AddRange(GetReviewers());
                context.SaveChanges();
            }
            catch (Exception)
            {
                Console.WriteLine("Error while seeding test data to the database");
                userTypesIds?.Clear();
                throw;
            }
        }

        private static Dictionary<TestUser, User> GetUsersTypesAssociations(PasswordService passwordService)
        {
            var userFaker = ModelFakers.UserFaker;
            var users = new Dictionary<TestUser, User>();

            foreach (TestUser testUser in Enum.GetValues(typeof(TestUser)))
            {
                var user = userFaker.Generate();
                user.PasswordHash = passwordService.HashPassword(TestUserPassword);
                user.Claims = GetUserClaims(testUser, user);
                users.Add(testUser, user);
            }

            return users;
        }

        private static List<Reviewer> GetReviewers()
        {
            var departments = Enumerable.Range(1, InitialDepartmentsCount)
                .Select(x => GetDepartment())
                .ToList();

            var teachers = Enumerable.Range(1, InitialTeachersCount)
                .Select(x => GetTeacher(departments))
                .ToList();

            List<Reviewer> reviewers = [
                GetReviewer(teachers, GeneratorType.FIRE_AND_FORGET),
                GetReviewer(teachers, GeneratorType.DELAYED),
                GetReviewer(teachers, GeneratorType.RECURRING),
            ];

            return reviewers;
        }

        public static Department GetDepartment()
        {
            var departmentFaker = ModelFakers.DepartmentFaker;
            return departmentFaker.Generate();
        }

        public static Teacher GetTeacher(List<Department> departments)
        {
            var teacherFaker = ModelFakers.TeacherFaker;
            var teacher = teacherFaker.Generate();

            teacher.Department = departments.GetRandomElement();

            return teacher;
        }

        public static Reviewer GetReviewer(List<Teacher> teachers, GeneratorType? type = null)
        {
            var reviewerFaker = ModelFakers.ReviewerFaker;
            var reviewer = reviewerFaker.Generate();

            if (type.HasValue)
                reviewer.Type = type.Value;

            reviewer.Teachers = teachers.GetRandomSubset();

            return reviewer;
        }

        private static List<UserClaim> GetUserClaims(TestUser userType, User user)
        {
            return userType switch
            {
                TestUser.Visitor => [new UserClaim()
                {
                    User = user,
                    ClaimType = ClaimTypes.SystemRole.GetClaimType(),
                    ClaimValue = SystemRoles.Visitor.ToString(),
                }],

                TestUser.Analyst => [new UserClaim()
                {
                    User = user,
                    ClaimType = ClaimTypes.SystemRole.GetClaimType(),
                    ClaimValue = SystemRoles.Analyst.ToString(),
                }],

                TestUser.GeneratorAdmin_FireAndForget => [
                    new UserClaim()
                    {
                        User = user,
                        ClaimType = ClaimTypes.SystemRole.GetClaimType(),
                        ClaimValue = SystemRoles.GeneratorAdmin.ToString(),
                    },
                    new UserClaim()
                    {
                        User = user,
                        ClaimType = ClaimTypes.GeneratorModifications.GetClaimType(),
                        ClaimValue = GeneratorModifications.ModifyFireAndForget.ToString(),
                    },
                ],

                TestUser.GeneratorAdmin_Delayed => [
                    new UserClaim()
                    {
                        User = user,
                        ClaimType = ClaimTypes.SystemRole.GetClaimType(),
                        ClaimValue = SystemRoles.GeneratorAdmin.ToString(),
                    },
                    new UserClaim()
                    {
                        User = user,
                        ClaimType = ClaimTypes.GeneratorModifications.GetClaimType(),
                        ClaimValue = GeneratorModifications.ModifyDelayed.ToString(),
                    },
                ],

                TestUser.GeneratorAdmin_Recurring => [
                    new UserClaim()
                    {
                        User = user,
                        ClaimType = ClaimTypes.SystemRole.GetClaimType(),
                        ClaimValue = SystemRoles.GeneratorAdmin.ToString(),
                    },
                    new UserClaim()
                    {
                        User = user,
                        ClaimType = ClaimTypes.GeneratorModifications.GetClaimType(),
                        ClaimValue = GeneratorModifications.ModifyRecurring.ToString(),
                    },
                ],

                TestUser.Owner => [new UserClaim()
                {
                    User = user,
                    ClaimType = ClaimTypes.SystemRole.GetClaimType(),
                    ClaimValue = SystemRoles.Owner.ToString(),
                }],

                _ => [new UserClaim()
                {
                    User = user,
                    ClaimType = ClaimTypes.SystemRole.GetClaimType(),
                    ClaimValue = SystemRoles.None.ToString(),
                }],
            };
        }
    }
}
