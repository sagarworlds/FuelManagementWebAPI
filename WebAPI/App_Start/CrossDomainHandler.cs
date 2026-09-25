using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WebAPI
{
    /// <summary>
    /// Handles CORS for browser clients served from other origins, such as the Angular app during development.
    /// </summary>
    /// <remarks>
    /// Only origins on the allowlist get CORS headers. Requests from other origins still reach the
    /// API (CORS is enforced by the browser), but the browser won't let the calling page read the
    /// response, and their preflights are refused. Same-origin requests need no CORS headers at all.
    /// </remarks>
    public class CrossDomainHandler : DelegatingHandler
    {
        const string Origin = "Origin";
        const string AccessControlRequestMethod = "Access-Control-Request-Method";
        const string AccessControlRequestHeaders = "Access-Control-Request-Headers";
        const string AccessControlAllowOrigin = "Access-Control-Allow-Origin";
        const string AccessControlAllowMethods = "Access-Control-Allow-Methods";
        const string AccessControlAllowHeaders = "Access-Control-Allow-Headers";
        const string AnyOrigin = "*";

        private readonly HashSet<string> allowedOrigins;
        private readonly bool allowAnyOrigin;

        /// <param name="allowedOrigins">
        /// Origins allowed to call the API, as scheme, host and optional port (e.g. "https://fuel.example.com");
        /// "*" allows every origin.
        /// </param>
        /// <exception cref="ArgumentNullException">When <paramref name="allowedOrigins"/> is null.</exception>
        public CrossDomainHandler(IEnumerable<string> allowedOrigins)
        {
            if (allowedOrigins == null)
            {
                throw new ArgumentNullException("allowedOrigins");
            }
            // Browsers send the origin without a trailing slash; a pasted URL often has one.
            var origins = allowedOrigins.Select(o => o.Trim().TrimEnd('/')).Where(o => o.Length > 0).ToList();
            allowAnyOrigin = origins.Contains(AnyOrigin);
            this.allowedOrigins = new HashSet<string>(origins, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Splits the comma-separated AllowedOrigins setting.
        /// </summary>
        /// <param name="setting">E.g. "http://localhost:4200, https://fuel.example.com".</param>
        /// <returns>The individual origins; empty when the setting is null or blank.</returns>
        public static IEnumerable<string> ParseOrigins(string setting)
        {
            return (setting ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(o => o.Trim())
                .Where(o => o.Length > 0)
                .ToArray();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var origin = HeaderValue(request, Origin);
            if (origin == null)
            {
                return await base.SendAsync(request, cancellationToken);
            }

            var isAllowed = allowAnyOrigin || allowedOrigins.Contains(origin);
            if (request.Method == HttpMethod.Options)
            {
                return Preflight(request, origin, isAllowed);
            }

            var response = await base.SendAsync(request, cancellationToken);
            if (isAllowed)
            {
                response.Headers.Add(AccessControlAllowOrigin, origin);
            }
            // The Allow-Origin header depends on the request's Origin, so caches must not share responses across origins.
            response.Headers.Vary.Add(Origin);
            return response;
        }

        /// <summary>
        /// Answers a preflight: the requested method and headers are allowed for allowed origins,
        /// and the preflight is refused (403, no CORS headers) for any other origin.
        /// </summary>
        private static HttpResponseMessage Preflight(HttpRequestMessage request, string origin, bool isAllowed)
        {
            var response = new HttpResponseMessage(isAllowed ? HttpStatusCode.OK : HttpStatusCode.Forbidden);
            response.Headers.Vary.Add(Origin);
            if (!isAllowed)
            {
                return response;
            }

            response.Headers.Add(AccessControlAllowOrigin, origin);
            var requestedMethod = HeaderValue(request, AccessControlRequestMethod);
            if (requestedMethod != null)
            {
                response.Headers.Add(AccessControlAllowMethods, requestedMethod);
            }
            var requestedHeaders = HeaderValue(request, AccessControlRequestHeaders);
            if (requestedHeaders != null)
            {
                response.Headers.Add(AccessControlAllowHeaders, requestedHeaders);
            }
            return response;
        }

        /// <summary>The header's values joined with ", ", or null when the request doesn't have it.</summary>
        private static string HeaderValue(HttpRequestMessage request, string name)
        {
            IEnumerable<string> values;
            return request.Headers.TryGetValues(name, out values) ? string.Join(", ", values) : null;
        }
    }
}
