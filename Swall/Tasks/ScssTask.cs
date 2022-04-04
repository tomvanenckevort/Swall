using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LibSassHost;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Swall.Assets;
using Swall.IO;

namespace Swall.Tasks
{
    internal sealed class ScssTask : SwallTask
    {
        private readonly string src;
        private readonly string dest;
        private readonly bool minify;

        public override string Name => "scss";

        public ScssTask(IReadOnlyDictionary<string, object> config) : base(config)
        {
            src = Config["src"]?.ToString() ?? "\\";
            dest = Config["dest"]?.ToString() ?? "\\";
            minify = Config["minify"]?.ToString()?.ToLowerInvariant() == "true";
        }

        /// <summary>
        /// Compiles SCSS files to CSS, minifies the output, generates map files and includes hash in the file name.
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
            matcher.AddInclude("*.scss");

            var matchResult = matcher.Execute(new DirectoryInfoWrapper(srcDirectory));

            if (!matchResult.HasMatches)
            {
                WriteToConsole("No SCSS files found", ConsoleColor.Yellow);
                return;
            }

            foreach (var match in matchResult.Files)
            {
                WriteToConsole($"Compiling {match.Stem}");

                var file = new FileInfo(Path.GetFullPath(match.Path, srcDirectory.FullName));

                var inputFilePath = file.FullName;
                var outputFilePath = Path.GetFullPath(file.Name.Replace(file.Extension, $"-{HashGenerator.Placeholder}.css"), destDirectory.FullName);
                var sourceMapFilePath = Path.GetFullPath(file.Name.Replace(file.Extension, $"-{HashGenerator.Placeholder}.css.map"), destDirectory.FullName);

                var compileResult = SassCompiler.CompileFile(inputFilePath, outputFilePath, sourceMapFilePath, new CompilationOptions()
                {
                    SourceMap = true,
                    OutputStyle = minify ? OutputStyle.Compressed : OutputStyle.Nested,
                    LineFeedType = LineFeedType.CrLf
                });

                var css = compileResult.CompiledContent;

                if (minify)
                {
                    // replace any new lines
                    css = css.Replace(Environment.NewLine, null);
                }

                var hash = HashGenerator.GenerateRevisionHash(css);

                css = css.Replace(HashGenerator.Placeholder, hash);

                outputFilePath = outputFilePath.Replace(HashGenerator.Placeholder, hash);
                sourceMapFilePath = sourceMapFilePath.Replace(HashGenerator.Placeholder, hash);

                await FileAccessor.WriteAllText(outputFilePath, css);
                await FileAccessor.WriteAllText(sourceMapFilePath, compileResult.SourceMap);
            }
        }
    }
}
