using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using NUglify;
using NUglify.JavaScript;
using Swall.Assets;
using Swall.IO;

namespace Swall.Tasks
{
    internal class JsTask : SwallTask
    {
        private readonly string src;
        private readonly string dest;
        private readonly bool minify;

        public override string Name => "js";

        public JsTask(IReadOnlyDictionary<string, object> config) : base(config)
        {
            src = Config["src"]?.ToString() ?? "\\";
            dest = Config["dest"]?.ToString() ?? "\\";
            minify = Config["minify"]?.ToString()?.ToLowerInvariant() == "true";
        }

        /// <summary>
        /// Minifies JS files in specified directory, generates map files and includes hash in the file name.
        /// </summary>
        /// <param name="subTask"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        protected override async Task Execute(string subTask = null, string parameters = null)
        {
            var srcDirectory = new DirectoryInfo(src);

            if (!srcDirectory.Exists)
            {
                WriteToConsole("Source directory missing", ConsoleColor.Red);
                return;
            }

            var destDirectory = new DirectoryInfo(dest);

            if (!destDirectory.Exists)
            {
                Directory.CreateDirectory(destDirectory.FullName);
            }

            var matcher = new Matcher();
            matcher.AddInclude("*.js");

            var matchResult = matcher.Execute(new DirectoryInfoWrapper(srcDirectory));

            if (!matchResult.HasMatches)
            {
                WriteToConsole("No JS files found", ConsoleColor.Yellow);
                return;
            }

            foreach (var match in matchResult.Files)
            {
                WriteToConsole($"Compiling {match.Stem}");

                var file = new FileInfo(Path.GetFullPath(match.Path, srcDirectory.FullName));

                var inputFilePath = file.FullName;
                var outputFilePath = Path.GetFullPath(file.Name.Replace(file.Extension, $"-{HashGenerator.Placeholder}.js"), destDirectory.FullName);
                var sourceMapFilePath = Path.GetFullPath(file.Name.Replace(file.Extension, $"-{HashGenerator.Placeholder}.js.map"), destDirectory.FullName);

                var js = await FileAccessor.ReadAllText(inputFilePath);
                var jsSourceMap = string.Empty;

                if (minify)
                {
                    using var sourceMapWriter = new StringWriter();
                    using var sourceMap = new V3SourceMap(sourceMapWriter);

                    sourceMap.StartPackage(outputFilePath, sourceMapFilePath);

                    js = Uglify.Js(js, new CodeSettings()
                    {
                        SymbolsMap = sourceMap
                    }).Code;

                    sourceMap.EndPackage();
                    sourceMap.Dispose();

                    jsSourceMap = sourceMapWriter.ToString();
                }

                var hash = HashGenerator.GenerateRevisionHash(js);

                var encodedRevisionPlaceholder = Uri.EscapeDataString(HashGenerator.Placeholder);

                js = js.Replace(encodedRevisionPlaceholder, hash);
                jsSourceMap = jsSourceMap.Replace(encodedRevisionPlaceholder, hash);

                outputFilePath = outputFilePath.Replace(HashGenerator.Placeholder, hash);
                sourceMapFilePath = sourceMapFilePath.Replace(HashGenerator.Placeholder, hash);

                await FileAccessor.WriteAllText(outputFilePath, js);
                await FileAccessor.WriteAllText(sourceMapFilePath, jsSourceMap);
            }
        }
    }
}
