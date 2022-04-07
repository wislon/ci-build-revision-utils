using System;
using System.IO;
using System.Linq;
using System.Text;
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
                    Console.WriteLine("e.g. PlistUtil.exe -filename=Info.plist\tWill increment the CFBundleShortVersionString (i.e. the REVISION/PATCH) if used by itself, otherwise it will increment the BUILD number and reset the revision to '0'");
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

            // for the difference between CFBundleVersion and CFBundleShortVersion, see https://hboon.com/difference-between-cfbundleversion-and-cfbundleshortversionstring/
            // it's counter-intuitive: the ShortVersion is the version they see in the app-store (i.e. 1.2.3) and the Version is the build-revision number (would normally
            // show up as the 4th segment in a 4-segment version string).
            
            // this is the actual 3-segment version we need to work with.
            var appVersionNode = xmlDoc.SelectSingleNode("/plist/dict/key[contains(., 'CFBundleShortVersionString')]/following-sibling::string[1]");
            if (appVersionNode == null)
                throw new NullReferenceException("Cannot find CFBundleShortVersionString in plist");
            
            // this is the build revision (would be the last segment in a 4-segment version string)
            var revisionNode = xmlDoc.SelectSingleNode("/plist/dict/key[contains(., 'CFBundleVersion')]/following-sibling::string[1]");
            if (revisionNode == null)
                throw new NullReferenceException("Cannot find CFBundleVersion in plist");
            
            IncrementVersion(appVersionNode, revisionNode , incrementBuildNumber);

            Console.WriteLine("Saving: {0}", fileName);
            
            // Convert the XmlDocument to a string and replace the square brackets
            var content = ConvertToString(xmlDoc).Replace("[]>", ">");
            File.WriteAllText(fileName, content);
            Console.ResetColor();
            Console.WriteLine("Done");
        }

        /// <summary>
        /// Increment the short version using the build number provided
        /// </summary>
        /// <param name="revisionNode">xml revisionNode to update</param>
        /// <param name="appVersionNode"></param>
        /// <param name="incrementBuildNumber">If false, will just bump the revision, otherwise the entire build number (will reset revision to '0')</param>
        private static void IncrementVersion(XmlNode appVersionNode, XmlNode revisionNode, bool incrementBuildNumber)
        {
            const string revisionNumberZero = "0";
            if (incrementBuildNumber)
            {
                Console.WriteLine("'IncrementBuildNumber' is set, so the revision/patch number ('CFBundleVersion' will be reset to '0')");
            }
            // Get the current version 
            var currentAppVersionString = appVersionNode.InnerText;

            // Apply a default if empty
            if (string.IsNullOrWhiteSpace(currentAppVersionString))
                currentAppVersionString = "1.0.0";

            // Regex for matching the version number. Note that it's only 3 parts. The App Store
            // will NOT accept a 4-part version number, and the app will be rejected immediately.
            const string versionMatcher = @"^(\d{1,}).(\d{1,}).(\d{1,})$";
            var rx = new Regex(versionMatcher);

            // If it's not a '1.2.3' match, but is a single number, we'll bump it here and bail out.
            if (!rx.IsMatch(currentAppVersionString) && Regex.IsMatch(currentAppVersionString, @"^\d+$"))
            {
                var num = int.Parse(currentAppVersionString) + 1;
            
                // Get the new version
            
                Console.WriteLine("New CFBundleShortVersionString: '{0}'", num);
            
                // Update the xml
                appVersionNode.InnerText = $"{num}";
                // and force the revision back to 0
                revisionNode.InnerText = "0";
                return;
            }
            
            if (!rx.IsMatch(currentAppVersionString))
            {
                Console.WriteLine($"'CFBundleShortVersionString' section located, but no regex match for '{versionMatcher}' value found");
                return;
            }

            // Get the matches
            var match = rx.Match(currentAppVersionString);
            var major = int.Parse(match.Groups[1].Value);
            var minor = int.Parse(match.Groups[2].Value);
            var build = int.Parse(match.Groups[3].Value);

            Console.WriteLine("Current CFBundleShortVersionString: '{0}.{1}.{2}'", major, minor, build);

            if (incrementBuildNumber)
            {
                build++;
            }

            var newBuildVersion = $"{major}.{minor}.{build}";
            Console.WriteLine("New     CFBundleShortVersionString: '{0}' (Remember: Apple App Store only accepts 3-part versions!)", newBuildVersion);
            // Update the xml
            appVersionNode.InnerText = newBuildVersion;
            
            // now handle the revision number
            var currentRevisionString = revisionNode.InnerText;

            // Apply a default if empty
            if (string.IsNullOrWhiteSpace(currentRevisionString))
            {
                currentRevisionString = revisionNumberZero;
            }
                
            // If it's not a number on its own, e.g. '1' or '123' match, we won't touch it
            if (int.TryParse(currentRevisionString, out var currentRevision))
            {
                switch (incrementBuildNumber)
                {
                    case false:
                        currentRevision++;
                        break;
                    default:
                        currentRevision = 0;
                        break;
                }
                Console.WriteLine("New CFBundleShortVersionString: '{0}'", currentRevision);
                revisionNode.InnerText = $"{currentRevision}";
                return;
            }
            
            Console.WriteLine($"'CFBundleShortVersionString' section located, but is not numeric. Found: '{currentRevisionString}'");
        }

        /// <summary>
        /// Convert the XmlDocument to a string preserving formatting (indent, newlines).
        /// From: https://stackoverflow.com/questions/203528/what-is-the-simplest-way-to-get-indented-xml-with-line-breaks-from-xmldocument
        /// </summary>
        private static string ConvertToString(XmlDocument doc)
        {
            var sb = new StringWriterWithEncoding();
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace,
                Encoding = Encoding.UTF8
            };
            using (var writer = XmlWriter.Create(sb, settings))
            {
                doc.Save(writer);
            }
            return sb.ToString();
        }
    }
}
