using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace AndroidManifestUtil
{
    class Program
    {

        private const string filenameToken = "-filename=";
        private const string resetRevisionNumberToken = "-reset-revision-number";

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine($"Usage: AndroidManifestUtil.exe -filename=<path\\to\\AndroidManifest.xml> [{resetRevisionNumberToken}]");
                Console.WriteLine(
                  "e.g. AndroidManifestUtil.exe ..\\src\\MyProject\\Properties\\AndroidManifest.xml\tWill increment the version code in manifest/android:versionCode, and build number in manifest/android:versionName");
            }
            else
            {
                // remember in debug mode, this file passed on the debug command line will be in the bin\Debug\ folder :)
                string fileName = args.FirstOrDefault(a => a.StartsWith(filenameToken, StringComparison.OrdinalIgnoreCase)).Substring(filenameToken.Length);
                if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("Version file name must not be empty");
                if (!File.Exists(fileName)) throw new ArgumentException($"Couldn't locate file '{fileName}'");

                var resetRevisionNumber = args.Any(arg => arg.ToLowerInvariant().Equals(resetRevisionNumberToken));

                // this is just the steadily increasing build number, e.g. '10' or '11'
                LoadAndUpdateVersionCodeFor(fileName);
                LoadAndUpdateBuildRevisionFor(fileName, resetRevisionNumber);
            }

            // Console.ReadLine();
        }

        /// <summary>
        /// Tries to locate and increment the 'android:versionCode' attribute of the 'manifest' tag
        /// </summary>
        /// <param name="fileName"></param>
        private static void LoadAndUpdateVersionCodeFor(string fileName)
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
            var versionCode = attributes.GetNamedItem("android:versionCode");
            if (versionCode != null)
            {
                Console.WriteLine("Found android:versionCode attribute: {0}", versionCode.Value);
                int newValue = int.Parse(versionCode.Value);
                versionCode.Value = $"{++newValue}";
                Console.WriteLine("Updating android:versionName attribute to {0}", versionCode.Value);
                manifestNode.Attributes.SetNamedItem(versionCode);
                Console.WriteLine("Writing out updated manifest file: {0}", fileName);
                xmldoc.Save(fileName);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Did not find android:versionCode attribute, aborting!");
                Console.ResetColor();
            }
            Console.ResetColor();
            Console.WriteLine("Done");
        }

        /// <summary>
        /// Tries to locate and update the 'android:versionName' attribute of the 'manifest' tag
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="resetRevisionNumber"></param>
        private static void LoadAndUpdateBuildRevisionFor(string fileName, bool resetRevisionNumber)
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
                var newVersionName = MatchRevisionAndIncrement(versionName, resetRevisionNumber);
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

        private static XmlNode MatchRevisionAndIncrement(XmlNode versionNameAttribute, bool resetRevisionNumber)
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

                revision = resetRevisionNumber ? 0 : revision + 1;

                Console.WriteLine("Current build revision: '{0}.{1}.{2}.{3}'", major, minor, build, revision);
                var newRevision = $"{major}.{minor}.{++build}.{revision}";
                Console.WriteLine("New build revision: {0}", newRevision);
                versionNameAttribute.Value = newRevision;
            }
            return versionNameAttribute;
        }
    }
}
