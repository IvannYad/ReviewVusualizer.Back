using ReviewVisualizer.Data.Enums;
using ReviewVisualizer.Data.Models;
using ReviewVisualizer.TestUtils;

namespace ReviewVisualizer.Generator.Tests.Utils
{
    public static class FakeDataGenerator
    {
        private const int InitialDepartmentsCount = 3;
        private const int InitialTeachersCount = 7;

        private static int _departmentId = 1;
        private static int _teacherId = 1;
        private static int _reviewerId = 1;

        public static Department GetDepartment()
        {
            var departmentFaker = ModelFakers.DepartmentFaker;
            var department = departmentFaker.Generate();
            department.Id = Interlocked.Increment(ref _departmentId);

            return department;
        }

        public static Teacher GetTeacher(List<Department> departments)
        {
            var teacherFaker = ModelFakers.TeacherFaker;
            var teacher = teacherFaker.Generate();

            teacher.Department = departments.GetRandomElement();
            teacher.DepartmentId = teacher.Department.Id;
            teacher.Id = Interlocked.Increment(ref _teacherId);

            return teacher;
        }

        public static Reviewer GetReviewer(List<Teacher> teachers, GeneratorType? type = null)
        {
            var reviewerFaker = ModelFakers.ReviewerFaker;
            var reviewer = reviewerFaker.Generate();

            if (type.HasValue)
                reviewer.Type = type.Value;

            reviewer.Teachers = teachers.GetRandomSubset();
            reviewer.Id = Interlocked.Increment(ref _reviewerId);

            return reviewer;
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
                GetReviewer(teachers, GeneratorType.FIRE_AND_FORGET),
                GetReviewer(teachers, GeneratorType.DELAYED),
                GetReviewer(teachers, GeneratorType.RECURRING),
            ];

            return reviewers;
        }
    }
}