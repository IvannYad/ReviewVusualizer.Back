using Autofac;
using Autofac.Core;
using Autofac.Core.Lifetime;

namespace ReviewVisualizer.Generator.Tests.Utils
{
    public class FakeLifetimeScope : LifetimeScope, ILifetimeScope
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private readonly ILifetimeScope? _innerScope;

        public FakeLifetimeScope(ILifetimeScope? innerScope)
            : base(default)
        {
            _innerScope = innerScope;
        }

        public void Register<TService>(TService service)
            where TService : notnull
        {
            _services[typeof(TService)] = service;
        }

        public TService Resolve<TService>() where TService : class
        {
            if (_services.TryGetValue(typeof(TService), out var serviceObj)
                && (serviceObj is TService service))
            {
                return service;
            }

            throw new InvalidOperationException();
        }

        public new ILifetimeScope BeginLifetimeScope()
        {
            if (_innerScope is not null)
                return _innerScope;

            throw new InvalidOperationException();
        }
    }
}