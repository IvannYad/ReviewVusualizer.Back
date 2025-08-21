using Microsoft.AspNetCore.Mvc.Testing.Handlers;
using ReviewVisualizer.Generator.IntegrationTests.Utils;
using System.Net;

namespace ReviewVisualizer.TestUtils
{
    public class HttpHandlersFactory
    {
        private readonly Dictionary<TestUser, CookieContainer> _usersCookies;

        public HttpHandlersFactory(Dictionary<TestUser, CookieContainer> usersCookies)
        {
            _usersCookies = usersCookies;
        }

        public CookieContainerHandler GetHandler(TestUser userType = TestUser.NotAuthorized)
        {
            if (_usersCookies.TryGetValue(userType, out var cookies))
            {
                return new CookieContainerHandler(cookies);
            }

            return new CookieContainerHandler();
        }
    }
}