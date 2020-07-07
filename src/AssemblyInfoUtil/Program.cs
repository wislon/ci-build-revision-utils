using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace AssemblyInfoUtil
{
  class Program
  {
    private static bool _versionUpdated;
    private const string filenameToken = "-filename=";

    private const string incrementBuildNumberToken = "-increment-build-number";
    private const string resetRevisionNumberToken = "-reset-revision-number";

    static void Main(string[] args)
    {
      if (args.Length < 1)
      {
        Console.WriteLine($"Usage: AssemblyInfoUtil.exe -filename=<file containing assembly version> [{incrementBuildNumberToken}] [{resetRevisionNumberToken}]");
        Console.WriteLine(
          "e.g. AssemblyInfoUtil.exe ..\\src\\MyProject\\SharedAssemblyVersion.cs\tWill increment the build revision in SharedAssemblyVersion.cs\r\nboth AssemblyVersion and AssemblyFileVersion will be updated");
      }
      else
      {
        // remember in debug mode, this file passed on the debug command line will be in the bin\Debug\ folder :)
        string fileName = args.FirstOrDefault(a => a.StartsWith(filenameToken, StringComparison.OrdinalIgnoreCase)).Substring(filenameToken.Length);
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("Version file name must not be empty");
        if (!File.Exists(fileName)) throw new ArgumentException($"Couldn't locate file '{fileName}'");

        var incrementBuildNumber = args.Any(arg => arg.ToLowerInvariant().Equals(incrementBuildNumberToken));
        var resetRevisionNumber = args.Any(arg => arg.ToLowerInvariant().Equals(resetRevisionNumberToken));

        LoadAndUpdateBuildRevisionFor(fileName, incrementBuildNumber, resetRevisionNumber);
      }
      // Console.ReadLine();
    }

    private static void LoadAndUpdateBuildRevisionFor(string fileName, bool incrementBuildNumber, bool resetRevisionNumber) {
      Console.ForegroundColor = ConsoleColor.Green;
      Console.WriteLine("Loading {0}", fileName);
      var assemblyFile = File.ReadAllLines(fileName).ToList();
      Console.WriteLine("Loaded {0}", fileName);


      Console.WriteLine("Looking for AssemblyVersion line...");
      LocateAndUpdateVersionLine("AssemblyVersion", assemblyFile, incrementBuildNumber, resetRevisionNumber);

      Console.WriteLine("Looking for AssemblyFileVersion line...");
      LocateAndUpdateVersionLine("AssemblyFileVersion", assemblyFile, incrementBuildNumber, resetRevisionNumber);

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

    private static void LocateAndUpdateVersionLine(string assemblyVersionKey, List<string> assemblyFile, bool incrementBuildNumber, bool resetRevisionNumber)
    {
      // [assembly: AssemblyVersion("1.0.2.0")]
      // [assembly: AssemblyFileVersion("1.0.2.0")]
      string matchingLine = assemblyFile.FirstOrDefault(l => l.Contains(assemblyVersionKey) && !l.Contains("//"));
      if (!string.IsNullOrWhiteSpace(matchingLine))
      {
        int index = assemblyFile.ToList().IndexOf(matchingLine);
        string newRevisionLine = MatchRevisionAndIncrement(matchingLine, incrementBuildNumber, resetRevisionNumber);
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

    private static string MatchRevisionAndIncrement(string versionLine, bool incrementBuildNumber, bool resetRevisionNumber)
    {
      const string versionMatcher = @"(\d{1}).(\d{1}).(\d{1,}).(\d{1,})"; // no ^$ start/stop delimiters here
      var rx = new Regex(versionMatcher);

      if (rx.IsMatch(versionLine))
      {
        Match match = rx.Match(versionLine);
        int major = int.Parse(match.Groups[1].Value);
        int minor = int.Parse(match.Groups[2].Value);
        int build = int.Parse(match.Groups[3].Value);
        int revision = int.Parse(match.Groups[4].Value);

        Console.WriteLine("Current build revision: '{0}.{1}.{2}.{3}'", major, minor, build, revision);

        build = incrementBuildNumber ? build + 1 : build;
        revision = resetRevisionNumber ? 0 : revision + 1;

        var newRevision = $"{major}.{minor}.{build}.{revision}";
        Console.WriteLine("New build revision: {0}", newRevision);
        versionLine = rx.Replace(versionLine, newRevision);
      }
      return versionLine;
    }
  }
}
