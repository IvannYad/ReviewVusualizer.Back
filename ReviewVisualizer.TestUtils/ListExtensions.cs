using System;

namespace ReviewVisualizer.TestUtils
{
    public static class ListExtensions
    {
        private static Random _random = new Random();

        public static List<T> GetRandomSubset<T>(this IList<T> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            int count = _random.Next(0, source.Count + 1);

            return source.OrderBy(x => _random.Next()).Take(count).ToList();
        }

        public static T GetRandomElement<T>(this IList<T> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source[_random.Next(source.Count())];
        }
    }
}