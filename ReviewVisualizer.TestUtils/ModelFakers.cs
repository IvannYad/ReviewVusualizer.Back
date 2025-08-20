using Bogus;
using ReviewVisualizer.AuthLibrary;
using ReviewVisualizer.Data.Enums;
using ReviewVisualizer.Data.Models;

namespace ReviewVisualizer.TestUtils
{
    public static class ModelFakers
    {
        private static PasswordService _passwordService = new PasswordService("");

        private static Faker<Department>? _departmentFaker;
        private static Faker<Teacher>? _teacherFaker;
        private static Faker<Reviewer>? _reviewerFaker;
        private static Faker<User>? _userFaker;

        /// <summary>
        /// Faker to generate <see cref="Department"/> objects.
        /// </summary>
        /// <remarks>
        /// Faker pre-populates the following properties:
        /// <list type="bullet">
        ///   <item>
        ///     <description><see cref="Department.Id"/> — auto-assigned Id, DO NOT override this property with your own value.</description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="Department.Name"/> — random uppercase string of length 1 to 10 characters.</description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="Department.LogoUrl"/> — random avatar image URL.</description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="Department.Rating"/> — random double between 1 and 100.</description>
        ///   </item>
        /// </list>
        /// </remarks>
        public static Faker<Department> DepartmentFaker
        {
            get
            {
                if (_departmentFaker is null)
                {
                    _departmentFaker = new Faker<Department>()
                        //.RuleFor(d => d.Id, f => f.IndexFaker + 1)
                        .RuleFor(d => d.Name, f => f.Random.String2(f.Random.Int(1, 10), "ABCDEFGHIJKLMNOPQRSTUVWXYZ"))
                        .RuleFor(d => d.LogoUrl, f => f.Internet.Avatar())
                        .RuleFor(d => d.Rating, f => f.Random.Double(1, 100));
                }

                return _departmentFaker;
            }
        }

        /// <summary>
        /// Faker to generate <see cref="Teacher"/> objects.
        /// </summary>
        /// <remarks>
        /// Faker pre-populates the following properties:
        /// <list type="bullet">
        ///   <item>
        ///     <description><see cref="Teacher.Id"/> — auto-assigned Id, DO NOT override this property with your own value.</description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="Teacher.FirstName"/> — human-like first name.</description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="Teacher.LastName"/> — human-like last name.</description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="Teacher.AcademicDegree"/> — random value from the <see cref="AcademicDegree"/> enum.</description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="Teacher.AcademicRank"/> — random value from the <see cref="AcademicRank"/> enum.</description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="Teacher.PhotoUrl"/> — random avatar image URL.</description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="Teacher.Rating"/> — random double between 1 and 100.</description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="Teacher.Reviewers"/> — initialized as an empty list.</description>
        ///   </item>
        /// </list>
        /// <para>
        /// For correctness, <see cref="Teacher.Reviewers"/>, <see cref="Teacher.DepartmentId"/>
        /// and <see cref="Teacher.Department"/> should
        /// be populated manually after generating the <see cref="Teacher"/> instance.
        /// </para>
        /// </remarks>
        public static Faker<Teacher> TeacherFaker
        {
            get
            {
                if (_teacherFaker is null)
                {
                    _teacherFaker = new Faker<Teacher>()
                        //.RuleFor(t => t.Id, f => f.IndexFaker + 1)
                        .RuleFor(t => t.FirstName, f => f.Name.FirstName())
                        .RuleFor(t => t.LastName, f => f.Name.LastName())
                        .RuleFor(t => t.AcademicDegree, f => f.PickRandom<AcademicDegree>())
                        .RuleFor(t => t.AcademicRank, f => f.PickRandom<AcademicRank>())
                        .RuleFor(t => t.PhotoUrl, f => f.Internet.Avatar())
                        .RuleFor(t => t.Rating, f => f.Random.Double(1, 100))
                        .RuleFor(t => t.Reviewers, _ => new List<Reviewer>());
                }

                return _teacherFaker;
            }
        }

        /// <summary>
        /// Faker to generate <see cref="Reviewer"/> objects.
        /// </summary>
        /// <remarks>
        /// Faker pre-populates the following properties:
        /// <list type="bullet">
        ///   <item>
        ///     <description><see cref="Reviewer.Id"/> — auto-assigned Id, DO NOT override this property with your own value.</description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="Reviewer.Name"/> — human-like name.</description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="Reviewer.TeachingQualityMinGrage"/> — random integer between 1 and 90.</description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="Reviewer.TeachingQualityMaxGrage"/> — random integer between <see cref="Reviewer.TeachingQualityMinGrage"/> and 100.</description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="Reviewer.StudentsSupportMinGrage"/> — random integer between 1 and 90.</description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="Reviewer.StudentsSupportMaxGrage"/> — random integer between <see cref="Reviewer.StudentsSupportMinGrage"/> and 100.</description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="Reviewer.CommunicationMinGrage"/> — random integer between 1 and 90.</description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="Reviewer.CommunicationMaxGrage"/> — random integer between <see cref="Reviewer.CommunicationMinGrage"/> and 100.</description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="Reviewer.Type"/> — random value from the <see cref="GeneratorType"/> enum.</description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="Reviewer.Teachers"/> — initialized as an empty list.</description>
        ///   </item>
        /// </list>
        /// <para>
        /// For correctness, the <see cref="Reviewer.Teachers"/> collection should be populated manually after
        /// generating the <see cref="Reviewer"/> instance.
        /// </para>
        /// </remarks>
        public static Faker<Reviewer> ReviewerFaker
        {
            get
            {
                if (_reviewerFaker is null)
                {
                    _reviewerFaker = new Faker<Reviewer>()
                        //.RuleFor(r => r.Id, f => f.IndexFaker + 1)
                        .RuleFor(r => r.Name, f => f.Name.FullName())
                        .RuleFor(r => r.TeachingQualityMinGrage, f => f.Random.Int(1, 90))
                        .RuleFor(r => r.TeachingQualityMaxGrage, (f, r) => f.Random.Int(r.TeachingQualityMinGrage, 100))
                        .RuleFor(r => r.StudentsSupportMinGrage, f => f.Random.Int(1, 90))
                        .RuleFor(r => r.StudentsSupportMaxGrage, (f, r) => f.Random.Int(r.StudentsSupportMinGrage, 100))
                        .RuleFor(r => r.CommunicationMinGrage, f => f.Random.Int(1, 90))
                        .RuleFor(r => r.CommunicationMaxGrage, (f, r) => f.Random.Int(r.CommunicationMinGrage, 100))
                        .RuleFor(r => r.Type, f => f.PickRandom<GeneratorType>())
                        .RuleFor(r => r.Teachers, f => []);
                }

                return _reviewerFaker;
            }
        }

        /// <summary>
        /// Faker to generate <see cref="User"/> objects.
        /// </summary>
        /// <remarks>
        /// Faker pre-populates the following properties:
        /// <list type="bullet">
        ///   <item>
        ///     <description><see cref="User.Id"/> — auto-assigned Id, DO NOT override this property with your own value.</description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="User.UserName"/> — internet-like userName.</description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="User.PasswordHash"/> — set to a hash of an empty string by default.</description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="User.Claims"/> — initialized as an empty list.</description>
        ///   </item>
        /// </list>
        /// <para>
        /// For security and correctness, both <see cref="User.PasswordHash"/> and <see cref="User.Claims"/> should
        /// be manually set after generating the user instance to provide meaningful data.
        /// </para>
        /// </remarks>
        public static Faker<User> UserFaker
        {
            get
            {
                if (_userFaker is null)
                {
                    _userFaker = new Faker<User>()
                        //.RuleFor(r => r.Id, f => f.IndexFaker + 1)
                        .RuleFor(r => r.UserName, f => f.Internet.UserName())
                        .RuleFor(r => r.PasswordHash, f => _passwordService.HashPassword(string.Empty))
                        .RuleFor(r => r.Claims, f => []);
                }

                return _userFaker;
            }
        }
    }
}
