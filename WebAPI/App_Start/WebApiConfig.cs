using System;
using System.Collections.Generic;
using System.Web.Http;
using Newtonsoft.Json;
using System.Web.Http.Dispatcher;
using WebAPI.Auth;
using WebAPI.Data;
using WebAPI.Model;


namespace WebAPI
{
    public static class WebApiConfig
    {
        /// <summary>
        /// Configures Web API for the hosted application, with dependencies built from Web.config settings.
        /// </summary>
        /// <exception cref="Exception">When a required appSetting, such as JwtSecret, is missing.</exception>
        /// <exception cref="ArgumentException">When the JwtSecret setting is shorter than 32 bytes.</exception>
        public static void Register(HttpConfiguration config)
        {
            // Built at startup so a missing or weak secret stops the app instead of failing the first login.
            var tokenService = new JwtTokenService(AppSettings.JwtSecret, TimeSpan.FromMinutes(AppSettings.JwtLifetimeMinutes));
            Configure(config, () => new SqLiteCustomerRepository(), tokenService, new BCryptPasswordHasher(),
                CrossDomainHandler.ParseOrigins(AppSettings.AllowedOrigins));
        }

        /// <summary>
        /// Configures routes, CORS, authentication and controller creation.
        /// Separate from <see cref="Register"/> so tests can supply their own dependencies.
        /// </summary>
        /// <param name="config">The configuration to populate.</param>
        /// <param name="repositoryFactory">Creates a repository for each controller instance.</param>
        /// <param name="tokenService">Issues and validates bearer tokens.</param>
        /// <param name="passwordHasher">Hashes and checks passwords.</param>
        /// <param name="allowedOrigins">Browser origins allowed to call the API across origins.</param>
        public static void Configure(HttpConfiguration config, Func<ICustomerRepository> repositoryFactory, ITokenService tokenService,
            IPasswordHasher passwordHasher, IEnumerable<string> allowedOrigins)
        {
            // Web API configuration and services            
            //var cors = new EnableCorsAttribute("*", "*", "*");
            //config.EnableCors(cors);

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{action}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );


            config.MessageHandlers.Add(new CrossDomainHandler(allowedOrigins));

            // Dates are UTC end to end: incoming values with an offset are converted to UTC, values
            // without one are taken as UTC, and responses always carry a "Z" so clients parse them
            // as instants rather than as their own local time.
            config.Formatters.JsonFormatter.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;

            // Every action requires a valid bearer token unless it is marked [AllowAnonymous].
            config.Filters.Add(new JwtAuthenticationFilter(tokenService));
            config.Filters.Add(new AuthorizeAttribute());

            config.Services.Replace(typeof(IHttpControllerActivator), new CompositionRoot(repositoryFactory, tokenService, passwordHasher));
        }
    }
}
