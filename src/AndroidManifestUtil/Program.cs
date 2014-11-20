using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace AndroidManifestUtil
{
  class Program
  {

    private const string filenameToken = "-filename=";

    static void Main(string[] args)
    {
      if (args.Length < 1)
      {
        Console.WriteLine("Usage: AndroidManifestUtil.exe -filename=<file containing assembly version>");
        Console.WriteLine(
          "e.g. AndroidManifestUtil.exe ..\\src\\MyProject\\Properties\\AndroidManifest.xml\tWill increment the build revision in manifest/android:versionName");
      }
      else
      {
        // remember in debug mode, this file passed on the debug command line will be in the bin\Debug\ folder :)
        string fileName = args.FirstOrDefault(a => a.StartsWith(filenameToken, StringComparison.OrdinalIgnoreCase)).Substring(filenameToken.Length);
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("Version file name must not be empty");
        if (!File.Exists(fileName)) throw new ArgumentException(string.Format("Couldn't locate file '{0}'", fileName));

        LoadAndUpdateBuildRevisionFor(fileName);
      }

      // Console.ReadLine();
    }

    private static void LoadAndUpdateBuildRevisionFor(string fileName)
    {
      Console.ForegroundColor = ConsoleColor.Green;
      Console.WriteLine("Loading {0}", fileName);
      var xmldoc = new XmlDocument();
      xmldoc.Load(fileName);
      Console.WriteLine("Loaded {0}", fileName);

      Console.WriteLine("Loading manifest node...");
      var manifestNode = xmldoc.SelectSingleNode("/manifest");
      Console.WriteLine("Getting manifest attributes...");
      var attributes = manifestNode.Attributes;
      var versionName = attributes.GetNamedItem("android:versionName");
      if (versionName != null)
      {
        Console.WriteLine("Found android:versionName attribute: {0}", versionName.Value);
        XmlNode newVersionName = MatchRevisionAndIncrement(versionName);
        Console.WriteLine("Updating android:versionName attribute to {0}", newVersionName.Value);
        manifestNode.Attributes.SetNamedItem(newVersionName);
        Console.WriteLine("Writing out updated manifest file: {0}", fileName);
        xmldoc.Save(fileName);
      }
      else
      {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Did not find android:versionName attribute, aborting!");
        Console.ResetColor();
      }
      Console.ResetColor();
      Console.WriteLine("Done");
    }

    private static XmlNode MatchRevisionAndIncrement(XmlNode versionNameAttribute)
    {
      string versionValue = versionNameAttribute.Value;
      if (string.IsNullOrWhiteSpace(versionValue))
      {
        versionValue = "1.0.0.0";
      }

      const string versionMatcher = @"^(\d{1}).(\d{1}).(\d{1,}).(\d{1,})$";
      var rx = new Regex(versionMatcher);
      if (rx.IsMatch(versionValue))
      {
        Match match = rx.Match(versionValue);
        int major = int.Parse(match.Groups[1].Value);
        int minor = int.Parse(match.Groups[2].Value);
        int build = int.Parse(match.Groups[3].Value);
        int revision = int.Parse(match.Groups[4].Value);
        Console.WriteLine("Current build revision: '{0}.{1}.{2}.{3}'", major, minor, build, revision);
        string newRevision = string.Format("{0}.{1}.{2}.{3}", major, minor, build, ++revision);
        Console.WriteLine("New build revision: {0}", newRevision);
        versionNameAttribute.Value = newRevision;
      }
      return versionNameAttribute;
    }
  }
}
