using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace Swall.Server
{
    /// <summary>
    /// Minimal web host environment class used by Kestrel middleware.
    /// </summary>
    internal class WebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; }
        public IFileProvider WebRootFileProvider { get; set; }
        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; }
        public string ContentRootPath { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; }
    }
}
