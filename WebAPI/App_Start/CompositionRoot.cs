using System;
using System.Globalization;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;
using WebAPI.Auth;
using WebAPI.Controllers;
using WebAPI.Data;

namespace WebAPI
{
    /// <summary>
    /// Creates API controllers with their dependencies; registered as Web API's controller activator.
    /// </summary>
    public class CompositionRoot : IHttpControllerActivator
    {
        private readonly Func<ICustomerRepository> repositoryFactory;
        private readonly ITokenService tokenService;
        private readonly IPasswordHasher passwordHasher;

        /// <param name="repositoryFactory">Creates a repository for each controller instance.</param>
        /// <param name="tokenService">Issues and validates bearer tokens.</param>
        /// <param name="passwordHasher">Hashes and checks passwords.</param>
        /// <exception cref="ArgumentNullException">When any argument is null.</exception>
        public CompositionRoot(Func<ICustomerRepository> repositoryFactory, ITokenService tokenService, IPasswordHasher passwordHasher)
        {
            if (repositoryFactory == null)
            {
                throw new ArgumentNullException("repositoryFactory");
            }
            if (tokenService == null)
            {
                throw new ArgumentNullException("tokenService");
            }
            if (passwordHasher == null)
            {
                throw new ArgumentNullException("passwordHasher");
            }
            this.repositoryFactory = repositoryFactory;
            this.tokenService = tokenService;
            this.passwordHasher = passwordHasher;
        }

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">When no composition is registered for <paramref name="controllerType"/>.</exception>
        public IHttpController Create(HttpRequestMessage request, HttpControllerDescriptor controllerDescriptor, Type controllerType)
        {
            if (controllerType == typeof(FuelDetailController))
            {
                return new FuelDetailController(repositoryFactory());
            }
            if (controllerType == typeof(UserController))
            {
                return new UserController(repositoryFactory(), tokenService, passwordHasher);
            }

            throw new InvalidOperationException(string.Format(
                CultureInfo.InvariantCulture, "No composition is registered for controller '{0}'.", controllerType.FullName));
        }
    }
}
