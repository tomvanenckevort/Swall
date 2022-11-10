using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Swall.Server
{
    internal sealed class HttpApplication : IHttpApplication<HttpContext>
    {
        private readonly DefaultFilesMiddleware defaultFilesMiddleware;

        private readonly StaticFileMiddleware staticFileMiddleware;

        /// <summary>
        /// Creates instance of HttpApplication with the required middleware for hosting a static file site.
        /// </summary>
        /// <param name="serverPath"></param>
        /// <param name="loggerFactory"></param>
        public HttpApplication(string serverPath, ILoggerFactory loggerFactory)
        {
            var environment = new WebHostEnvironment();

            var fileProvider = new PhysicalFileProvider(serverPath);

            static Task next(HttpContext c) => Task.CompletedTask;

            defaultFilesMiddleware = new DefaultFilesMiddleware(
                next,
                environment,
                Options.Create(new DefaultFilesOptions()
                {
                    FileProvider = fileProvider,
                    DefaultFileNames = new string[] { "index.html" }
                }));

            staticFileMiddleware = new StaticFileMiddleware(
                next,
                environment,
                Options.Create(new StaticFileOptions()
                {
                    FileProvider = fileProvider,
                    DefaultContentType = "application/octet-stream"
                }),
                loggerFactory);
        }

        /// <summary>
        /// Creates a new default HttpContext.
        /// </summary>
        /// <param name="contextFeatures"></param>
        /// <returns></returns>
        public HttpContext CreateContext(IFeatureCollection contextFeatures)
        {
            return new DefaultHttpContext(contextFeatures);
        }

        public void DisposeContext(HttpContext context, Exception exception)
        {
            ArgumentNullException.ThrowIfNull(context);
        }

        /// <summary>
        /// Process incoming request using the registered middleware.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task ProcessRequestAsync(HttpContext context)
        {
            await defaultFilesMiddleware.Invoke(context);

            await staticFileMiddleware.Invoke(context);
        }
    }
}
