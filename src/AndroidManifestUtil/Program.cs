using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace AndroidManifestUtil
{
    internal class Program
    {
        private const string FilenameToken = "-filename=";
        private const string IncrementBuildNumberToken = "-increment-build-number";

        private static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine($"Usage: AndroidManifestUtil.exe -filename=<path\\to\\AndroidManifest.xml> [{IncrementBuildNumberToken}]");
                Console.WriteLine("e.g. AndroidManifestUtil.exe ..\\src\\MyProject\\Properties\\AndroidManifest.xml\tWill increment the version code in manifest/android:versionCode,");
                Console.WriteLine("and revision number (4th in the quartet) in manifest/android:versionName if used by itself.");
                Console.WriteLine("Otherwise it'll bump the BUILD number and RESET the revision number to '0'");
            }
            else
            {
                // remember in debug mode, this file passed on the debug command line will be in the bin\Debug\ folder :)
                var fileName = args.FirstOrDefault(a => a.StartsWith(FilenameToken, StringComparison.OrdinalIgnoreCase))?.Substring(FilenameToken.Length);
                if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("Version file name must not be empty");
                if (!File.Exists(fileName)) throw new ArgumentException($"Couldn't locate file '{fileName}'");

                var incrementBuildNumber = args.Any(arg => arg.ToLowerInvariant().Equals(IncrementBuildNumberToken));

                // this is just the steadily increasing build number, e.g. '10' or '11'
                LoadAndUpdateVersionCodeFor(fileName);
                LoadAndUpdateBuildRevisionFor(fileName, incrementBuildNumber);
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
            var attributes = manifestNode?.Attributes;
            var versionCode = attributes?.GetNamedItem("android:versionCode");
            if (versionCode != null)
            {
                Console.WriteLine("Found android:versionCode attribute: {0}", versionCode.Value);
                var newValue = int.Parse(versionCode.Value);
                versionCode.Value = $"{++newValue}";
                Console.WriteLine("Updating android:versionName attribute to {0}", versionCode.Value);
                manifestNode.Attributes?.SetNamedItem(versionCode);
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
        /// <param name="incrementBuildNumber"></param>
        private static void LoadAndUpdateBuildRevisionFor(string fileName, bool incrementBuildNumber)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Loading {0}", fileName);
            var xmldoc = new XmlDocument();
            xmldoc.Load(fileName);
            Console.WriteLine("Loaded {0}", fileName);

            Console.WriteLine("Loading manifest node...");
            var manifestNode = xmldoc.SelectSingleNode("/manifest");
            Console.WriteLine("Getting manifest attributes...");
            var attributes = manifestNode?.Attributes;
            var versionName = attributes?.GetNamedItem("android:versionName");
            if (versionName != null)
            {
                Console.WriteLine("Found android:versionName attribute: {0}", versionName.Value);
                var newVersionName = MatchRevisionAndIncrement(versionName, incrementBuildNumber);
                Console.WriteLine("Updating android:versionName attribute to {0}", newVersionName.Value);
                manifestNode.Attributes?.SetNamedItem(newVersionName);
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

        private static XmlNode MatchRevisionAndIncrement(XmlNode versionNameAttribute, bool incrementBuildNumber)
        {
            var versionValue = versionNameAttribute.Value;
            if (string.IsNullOrWhiteSpace(versionValue))
            {
                versionValue = "1.0.0.0";
            }

            const string versionMatcher = @"^(\d{1}).(\d{1}).(\d{1,}).(\d{1,})$";
            var rx = new Regex(versionMatcher);
            if (rx.IsMatch(versionValue))
            {
                var match = rx.Match(versionValue);
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
                versionNameAttribute.Value = newRevision;
            }
            return versionNameAttribute;
        }
    }
}
