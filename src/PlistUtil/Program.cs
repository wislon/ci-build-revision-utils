using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace PlistUtil
{
    internal class Program
    {
        // Argument tokens
        private const string FilenameToken = "-filename=";
        private const string IncrementBuildNumberToken = "-increment-build-number";

        private static void Main(string[] args)
        {
            try
            {
                // Get the arguments
                var filenameArg = args.FirstOrDefault(a => a.StartsWith(FilenameToken, StringComparison.OrdinalIgnoreCase));
                var incrementBuildNumber = args.Any(a => a.ToLowerInvariant().Equals(IncrementBuildNumberToken));

                // If the argument doesn't exist or it doesn't have a value
                if (filenameArg == null || filenameArg.Length == FilenameToken.Length)
                {
                    // Show help
                    Console.WriteLine($"Usage: PlistUtil.exe -filename=<path\\to\\Info.plist> [{IncrementBuildNumberToken}]");
                    Console.WriteLine("e.g. PlistUtil.exe -filename=Info.plist\tWill increment the CFBundleVersion REVISION if used by itself, otherwise it will increment the BUILD number and reset the revision to '0'");
                    return;
                }

                var fileName = filenameArg.Substring(FilenameToken.Length);

                if (string.IsNullOrWhiteSpace(fileName))
                    throw new ArgumentException("Version file name must not be empty");

                if (!File.Exists(fileName))
                    throw new ArgumentException($"Couldn't locate file '{fileName}'");

                LoadAndUpdate(fileName, incrementBuildNumber);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            
        }

        /// <summary>
        /// Load the XML doc and update the build number (note that this is 'Apple flavour' xml,
        /// and doesn't follow normal xml rules; it's like they heard about xml somewhere and decided
        /// to use it, without actually reading about how to use it properly).
        /// </summary>
        /// <param name="fileName">Path to the info.plist</param>
        /// <param name="incrementBuildNumber">true to update Reset the last item in the quartet to 0, e.g. '1.0.2.4' becomes '1.0.3.0'</param>
        private static void LoadAndUpdate(string fileName, bool incrementBuildNumber)
        {
            // Info
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Loading: {0}", fileName);

            // Load the XML plist
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(fileName);

            // Get the node to be updated
            var versionNode = xmlDoc.SelectSingleNode("/plist/dict/key[contains(., 'CFBundleVersion')]/following-sibling::string[1]");

            // Validate CFBundleVersion node
            if (versionNode == null)
                throw new NullReferenceException("Cannot find CFBundleVersion in plist");

            IncrementCFBundleVersion(versionNode, incrementBuildNumber);

            Console.WriteLine("Saving: {0}", fileName);
            xmlDoc.Save(fileName);
            Console.ResetColor();
            Console.WriteLine("Done");
        }

        /// <summary>
        /// Increment the short version using the build number provided
        /// </summary>
        /// <param name="node">xml node to update</param>
        /// <param name="incrementBuildNumber"></param>
        private static void IncrementCFBundleVersion(XmlNode node, bool incrementBuildNumber)
        {
            // Get the current version 
            var currentValue = node.InnerText;

            // Apply a default if empty
            if (string.IsNullOrWhiteSpace(currentValue))
                currentValue = "1.0.0.0";

            // Regex for matching the version number
            const string versionMatcher = @"^(\d{1,}).(\d{1,}).(\d{1,}).(\d{1,})$";
            var rx = new Regex(versionMatcher);

            // If it's not a match, we won't touch it
            if (!rx.IsMatch(currentValue))
                return;

            // Get the matches
            var match = rx.Match(currentValue);
            var major = int.Parse(match.Groups[1].Value);
            var minor = int.Parse(match.Groups[2].Value);
            var build = int.Parse(match.Groups[3].Value);
            var revision = int.Parse(match.Groups[4].Value);

            Console.WriteLine("Current CFBundleVersion: '{0}.{1}.{2}.{3}'", major, minor, build, revision);

            if (incrementBuildNumber)
            {
                build++;
                revision = 0;
            }
            else
            {
                revision++;
            }

            // Get the new version
            var newRevision = $"{major}.{minor}.{build}.{revision}";

            Console.WriteLine("New     CFBundleVersion: '{0}'", newRevision);

            // Update the xml
            node.InnerText = newRevision;
        }
    }
}
