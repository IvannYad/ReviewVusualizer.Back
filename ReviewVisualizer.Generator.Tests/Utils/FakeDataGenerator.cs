using Bogus;
using ReviewVisualizer.Data.Enums;
using ReviewVisualizer.Data.Models;

namespace ReviewVisualizer.Generator.Tests.Utils
{
    public static class FakeDataGenerator
    {
        private const int InitialDepartmentsCount = 3;
        private const int InitialTeachersCount = 7;

        private static int _departmentCurrentId = 1;
        private static int _teacherCurrentId = 1;
        private static int _reviewerCurrentId = 1;

        private static Random _random = new Random();

        public static Department GetDepartment()
        {
            var departmentFaker = GetDepartmentFaker();
            return departmentFaker.Generate();
        }

        public static Teacher GetTeacher(List<Department> departments)
        {
            var teacherFaker = GetTeacherFaker(departments);
            return teacherFaker.Generate();
        }

        public static Reviewer GetReviewer(List<Teacher> teachers, GeneratorType? type = null)
        {
            var reviewerFaker = GetReviewerFaker(teachers, type);
            return reviewerFaker.Generate();
        }

        private static List<T> GetRandomSubset<T>(this List<T> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            int count = _random.Next(0, source.Count + 1);

            return source.OrderBy(x => _random.Next()).Take(count).ToList();
        }

        public static List<Reviewer> GetReviewersForGeneratorTests()
        {
            var departments = Enumerable.Range(1, InitialDepartmentsCount)
                .Select(x => GetDepartment())
                .ToList();

            var teachers = Enumerable.Range(1, InitialTeachersCount)
                .Select(x => GetTeacher(departments))
                .ToList();

            List<Reviewer> reviewers = [
                GetReviewer(GetRandomSubset(teachers), GeneratorType.FIRE_AND_FORGET),
                GetReviewer(GetRandomSubset(teachers), GeneratorType.DELAYED),
                GetReviewer(GetRandomSubset(teachers), GeneratorType.RECURRING),
            ];

            return reviewers;
        }

        private static Faker<Department> GetDepartmentFaker() =>
            new Faker<Department>()
                .RuleFor(d => d.Id, f => Interlocked.Increment(ref _departmentCurrentId))
                .RuleFor(d => d.Name, f => f.Random.String2(f.Random.Int(1, 10), "ABCDEFGHIJKLMNOPQRSTUVWXYZ"))
                .RuleFor(d => d.LogoUrl, f => f.Internet.Avatar())
                .RuleFor(d => d.Rating, f => f.Random.Double(1, 100));

        private static Faker<Teacher> GetTeacherFaker(List<Department> departments) =>
            new Faker<Teacher>()
                .RuleFor(t => t.Id, f => Interlocked.Increment(ref _teacherCurrentId))
                .RuleFor(t => t.FirstName, f => f.Name.FirstName())
                .RuleFor(t => t.LastName, f => f.Name.LastName())
                .RuleFor(t => t.AcademicDegree, f => f.PickRandom<AcademicDegree>())
                .RuleFor(t => t.AcademicRank, f => f.PickRandom<AcademicRank>())
                .RuleFor(t => t.PhotoUrl, f => f.Internet.Avatar())
                .RuleFor(t => t.Rating, f => f.Random.Double(1, 100))
                .RuleFor(t => t.DepartmentId, f => f.PickRandom(departments).Id)
                .RuleFor(t => t.Department, (f, t) => departments.Find(d => d.Id == t.DepartmentId))
                .RuleFor(t => t.Reviewers, _ => new List<Reviewer>());
        private static Faker<Reviewer> GetReviewerFaker(List<Teacher> teachers, GeneratorType? type) =>
            new Faker<Reviewer>()
                .RuleFor(r => r.Id, f => Interlocked.Increment(ref _reviewerCurrentId))
                .RuleFor(r => r.Name, f => f.Name.FullName())
                .RuleFor(r => r.TeachingQualityMinGrage, f => f.Random.Int(1, 90))
                .RuleFor(r => r.TeachingQualityMaxGrage, (f, r) => f.Random.Int(r.TeachingQualityMinGrage, 100))
                .RuleFor(r => r.StudentsSupportMinGrage, f => f.Random.Int(1, 90))
                .RuleFor(r => r.StudentsSupportMaxGrage, (f, r) => f.Random.Int(r.StudentsSupportMinGrage, 100))
                .RuleFor(r => r.CommunicationMinGrage, f => f.Random.Int(1, 90))
                .RuleFor(r => r.CommunicationMaxGrage, (f, r) => f.Random.Int(r.CommunicationMinGrage, 100))
                .RuleFor(r => r.Type, f => type ?? f.PickRandom<GeneratorType>())
                .RuleFor(r => r.Teachers, f => teachers);
    }
}
