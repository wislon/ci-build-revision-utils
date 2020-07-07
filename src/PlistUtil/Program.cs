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
        private const string ResetRevisionNumberToken = "-reset-revision-number";


        private static void Main(string[] args)
        {
            try
            {
                // Get the arguments
                var filenameArg = args.FirstOrDefault(a => a.StartsWith(FilenameToken, StringComparison.OrdinalIgnoreCase));
                var resetRevisionNumber = args.Any(a => a.ToLowerInvariant().Equals(ResetRevisionNumberToken));

                // If the argument doesn't exist or it doesn't have a value
                if (filenameArg == null || filenameArg.Length == FilenameToken.Length)
                {
                    // Show help
                    Console.WriteLine("Usage: PlistUtil.exe -filename=<path\\to\\Info.plist>");
                    Console.WriteLine("e.g. PlistUtil.exe -filename=Info.plist\tWill increment the CFBundleVersion, and CFBundleShortVersionString");
                    return;
                }

                // Get the file path
                var fileName = filenameArg.Substring(FilenameToken.Length);

                // Validate file name
                if (string.IsNullOrWhiteSpace(fileName))
                    throw new ArgumentException("Version file name must not be empty");

                // Check file exists
                if (!File.Exists(fileName))
                    throw new ArgumentException($"Couldn't locate file '{fileName}'");

                // Do the work
                LoadAndUpdate(fileName, resetRevisionNumber);
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
        /// <param name="resetRevisionNumber">true to update Reset the last item in the quartet to 0, e.g. '1.0.2.4' becomes '1.0.3.0'</param>
        private static void LoadAndUpdate(string fileName, bool resetRevisionNumber)
        {
            // Info
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Loading: {0}", fileName);

            // Load the XML plist
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(fileName);

            // Get the nodes to be updated
            var versionNode = xmlDoc.SelectSingleNode("/plist/dict/key[contains(., 'CFBundleVersion')]/following-sibling::string[1]");
            var shortVersionNode = xmlDoc.SelectSingleNode("/plist/dict/key[contains(., 'CFBundleShortVersionString')]/following-sibling::string[1]");

            // Validate CFBundleVersion node
            if (versionNode == null)
                throw new NullReferenceException("Cannot find CFBundleVersion in plist");

            // Validate CFBundleShortVersionString node
            if (shortVersionNode == null)
                throw new NullReferenceException("Cannot find CFBundleShortVersionString in plist");

            // Get the next build number
            var nextBuildNumber = int.Parse(versionNode.InnerText) + 1;

            // Show status
            Console.WriteLine("New build number: {0}", nextBuildNumber);

            // Set the version number
            versionNode.InnerText = nextBuildNumber.ToString();

            // update this always
            IncrementShortVersion(shortVersionNode, nextBuildNumber, resetRevisionNumber);

            // Save
            Console.WriteLine("Saving: {0}", fileName);
            xmlDoc.Save(fileName);
            Console.ResetColor();
            Console.WriteLine("Done");
        }

        /// <summary>
        /// Increment the short version using the build number provided
        /// </summary>
        /// <param name="node">xml node to update</param>
        /// <param name="buildNum">new build number</param>
        /// <param name="resetRevisionNumber"></param>
        private static void IncrementShortVersion(XmlNode node, int buildNum = 0, bool resetRevisionNumber=false)
        {
            // Get the current version 
            var currentValue = node.InnerText;

            // Apply a default if empty
            if (string.IsNullOrWhiteSpace(currentValue))
                currentValue = "1.0.0";

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

            revision = resetRevisionNumber ? 0 : revision + 1;

            // Get the new version
            var newRevision = $"{major}.{minor}.{buildNum}.{revision}";

            // Status
            Console.WriteLine("Current CFBundleShortVersionString: '{0}.{1}.{2}'", major, minor, build);
            Console.WriteLine("New     CFBundleShortVersionString: '{0}'", newRevision);

            // Update the xml
            node.InnerText = newRevision;
        }
    }
}
