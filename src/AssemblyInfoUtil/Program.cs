using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AssemblyInfoUtil
{
    internal class Program
    {
        private static bool _versionUpdated;

        private const string FilenameToken = "-filename=";
        private const string IncrementBuildNumberToken = "-increment-build-number";

        private static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine($"Usage: AssemblyInfoUtil.exe -filename=<file containing assembly version> [{IncrementBuildNumberToken}]");
                Console.WriteLine(
                  "e.g. AssemblyInfoUtil.exe ..\\src\\MyProject\\SharedAssemblyVersion.cs\tWill increment the REVISION (i.e. last in the quartet) in SharedAssemblyVersion.cs\r\nboth AssemblyVersion and AssemblyFileVersion will be updated");
            }
            else
            {
                // remember in debug mode, this file passed on the debug command line will be in the bin\Debug\ folder :)
                var fileName = args.FirstOrDefault(a => a.StartsWith(FilenameToken, StringComparison.OrdinalIgnoreCase))?.Substring(FilenameToken.Length);
                if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("Version file name must not be empty");
                if (!File.Exists(fileName)) throw new ArgumentException($"Couldn't locate file '{fileName}'");

                var incrementBuildNumber = args.Any(arg => arg.ToLowerInvariant().Equals(IncrementBuildNumberToken));

                LoadAndUpdateBuildRevisionFor(fileName, incrementBuildNumber);
            }
            // Console.ReadLine();
        }

        private static void LoadAndUpdateBuildRevisionFor(string fileName, bool incrementBuildNumber)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Loading {0}", fileName);
            var assemblyFile = File.ReadAllLines(fileName).ToList();
            Console.WriteLine("Loaded {0}", fileName);


            Console.WriteLine("Looking for AssemblyVersion line...");
            LocateAndUpdateVersionLine("AssemblyVersion", assemblyFile, incrementBuildNumber);

            Console.WriteLine("Looking for AssemblyFileVersion line...");
            LocateAndUpdateVersionLine("AssemblyFileVersion", assemblyFile, incrementBuildNumber);

            Console.WriteLine("Looking for AssemblyInformationalVersion line...");
            LocateAndUpdateVersionLine("AssemblyInformationalVersion", assemblyFile, incrementBuildNumber);

            if (_versionUpdated)
            {
                Console.WriteLine("Revision updated. Writing out new {0}", fileName);
                File.WriteAllLines(fileName, assemblyFile);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Did not find the 'AssemblyVersion' code line, aborting!");
            }
            Console.ResetColor();
            Console.WriteLine("Done");
        }

        private static void LocateAndUpdateVersionLine(string assemblyVersionKey, List<string> assemblyFile, bool incrementBuildNumber)
        {
            // [assembly: AssemblyVersion("1.0.2.0")]
            // [assembly: AssemblyFileVersion("1.0.2.0")]
            var matchingLine = assemblyFile.FirstOrDefault(l => l.Contains(assemblyVersionKey) && !l.Contains("//"));
            if (!string.IsNullOrWhiteSpace(matchingLine))
            {
                var index = assemblyFile.ToList().IndexOf(matchingLine);
                var newRevisionLine = MatchRevisionAndIncrement(matchingLine, incrementBuildNumber);
                assemblyFile[index] = newRevisionLine; // overwrite the old one.
                _versionUpdated = true;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Did not find the '{0}' code line, skipping...", assemblyVersionKey);
                Console.ResetColor();
            }
        }

        private static string MatchRevisionAndIncrement(string versionLine, bool incrementBuildNumber)
        {
            const string versionMatcher = @"(\d{1}).(\d{1}).(\d{1,}).(\d{1,})"; // no ^$ start/stop delimiters here
            var rx = new Regex(versionMatcher);

            if (rx.IsMatch(versionLine))
            {
                var match = rx.Match(versionLine);
                var major = int.Parse(match.Groups[1].Value);
                var minor = int.Parse(match.Groups[2].Value);
                var build = int.Parse(match.Groups[3].Value);
                var revision = int.Parse(match.Groups[4].Value);

                Console.WriteLine("Current build revision: '{0}.{1}.{2}.{3}'", major, minor, build, revision);

                if (incrementBuildNumber)
                {
                    build++;
                    revision = 0;
                }
                else
                {
                    revision++;
                }

                var newRevision = $"{major}.{minor}.{build}.{revision}";
                Console.WriteLine("New build revision: {0}", newRevision);
                versionLine = rx.Replace(versionLine, newRevision);
            }
            return versionLine;
        }
    }
}
