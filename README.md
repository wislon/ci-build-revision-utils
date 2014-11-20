ci-build-revision-utils
=======================

### Continuous Integration :: Simple Build-Revision CLI utilities
The stupidest thing that actually works. 

This is a couple of .NET exes I built over a couple of hours to automate the incrementing of build revision numbers in 

* Xamarin/Android `AndroidManifest.xml` files
* .NET `AssemblyInfo` related files

All they do is load and parse a given file name looking for specific string/regex matches. If they are able to locate the requested bits of information, they'll auto-increment the __build revision #__, and write the file back out again.

That is all they do. Their primary function is simply to operate as part of a custom build/build-tool chain, so it's up to the developer to integrate them with their CI process (examples below).

_Yes, I know, TeamCity and some other CI toolkits can do stuff like this too. I can't use TeamCity in a couple of current projects, which is why I slapped these little programs together._

Application versions are displayed as a set of numbers indicating major version, minor version, build # and build revision. This is usually displayed in the format: `1.0.1.2` or `1.0.2.0-alpha` or something similar. Normally what happens when you (re)build an app, these would be incremented automatically for you. Some "increments" aren't actually increments. They could be git commit hashes, timestamps, you name it.

In the case of these utilities, all they do is try and locate something that looks like a set of numbers like this, and if they find it, they'll increment the build revision # (the last number in the quartet).

For example, if the existing version # is 1.2.3.4, then running these utilities will simply update that last digit to 1.2.3.5. 

That is all they do. 

_Yes, they could do so much more, and I'm sure you've got a million ideas about how yours would be 10x more awesome. But this solved an immediate problem I had._

### AndroidManifestUtil (for AndroidManifest.xml files)
This updates the `AndroidManifest.xml` file commonly found in Android and Xamarin.Android projects.

Normally when you build your Android app, you have to provide two version values, a version __code__ (`versionCode` in the manifest file) and a version __name__ (`versionName` in the manifest file).

The `versionCode` value is a single integer used by the Google Play store to determine whether the app is a newer version than one it already has. 

The `versionName` is a set of numbers (and perhaps other identifiers, as described above, which you can use for "proper" version tracking in your app (maybe for analytics or crash reporting or something else). 

`AndroidManifestUtil` extracts the `versionName` attribute from the `manifest` xml node, tries to parse it to extract the 4 numbers, increments the last one (the build revision), and writes it back to the file. 

If you've got some funky version numbering you do (e.g. '1.0.rc-02.02Jan2014'), then you're on your own.

`AndroidManifestUtil` doesn't change the `versionCode` at all (that's up to you to do when you're ready to publish, since not every build that you do will be worth pushing up to Google Play). 

#### Usage
Theres's a test `AndroidManifest.xml` file which looks something like:

```xml
<?xml version="1.0" encoding="utf-8"?>
<!-- dummy xml file for /manifest/android:versionName attribute revision # increment testing -->
<manifest xmlns:android="http://schemas.android.com/apk/res/android" android:installLocation="auto" package="io.wislon.testApp" android:versionCode="1" android:versionName="1.0.2.0">
  <uses-sdk android:minSdkVersion="15" android:targetSdkVersion="19" />
  <application android:icon="@drawable/Icon" android:label="testApp">
  </application>
  <uses-permission android:name="android.permission.ACCESS_WIFI_STATE" />
  <uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" />
  <uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
  <uses-permission android:name="android.permission.INTERNET" />
</manifest>
```

As you can see, the `versionName` is set to 1.0.2.0.

Run the utility from the command line (or script):

```text
AndroidManifestUtil.exe -filename=.\AndroidManifest.xml
```

It'll give you some basic info about what it's doing, or if it's got a problem:

```text
D:\test\AndroidManifestUtil.exe -filename=.\AndroidManifest.xml
Loading .\AndroidManifest.xml
Loaded .\AndroidManifest.xml
Loading manifest node...
Getting manifest attributes...
Found android:versionName attribute: 1.0.2.0
Current build revision: '1.0.2.0'
New build revision: 1.0.2.1
Updating android:versionName attribute to 1.0.2.1
Writing out updated manifest file: .\AndroidManifest.xml
Done
```

...and now it looks like:

```xml
<?xml version="1.0" encoding="utf-8"?>
<!-- dummy xml file for /manifest/android:versionName attribute revision # increment testing -->
<manifest xmlns:android="http://schemas.android.com/apk/res/android" android:installLocation="auto" package="io.wislon.testApp" android:versionCode="1" android:versionName="1.0.2.1">
<snip />
</manifest>
```

### AssemblyInfoUtil (for updating .NET AssemblyInfo-related files)
This updates the `AssemblyVersion` and `AssemblyFileVersion` values commonly found in `AssemblyInfo.cs` (or global/shared versions of them) in .NET projects. 

It simply opens the file you specify (as text), and searches for for the quartet of numbers. If it finds one (or both), it will parse out the build revision #, increment it, and then write it back to the file.

#### Usage
Similarly to the AndroidManifestUtil, simply point the `-filename` parameter at the file containing the version strings you're interested in updating:

Run the utility from the command line (or script):
```text
AssemblyInfoUtil.exe -filename=.\GlobalVersionInfo.cs.txt
```

_(it has a '.txt' extension because Visual Studio kept trying to compile it into the application, and then couldn't write to it when running the debugger :)). This won't be a problem when you point it at a 'real' file from a command line._

This file will look something like:

```csharp
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// sample text file for revision # increment update testing. 
// Application just reads and writes text, it doesn't 
// care about the file extension

// You can specify all the values or you can default the Build and Revision 
// Numbers by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.2.0")]
[assembly: AssemblyFileVersion("1.0.2.0")]
```


```text
D:\test\AssemblyInfoUtil.exe -filename=.\GlobalVersionInfo.cs.txt
Loading .\GlobalVersionInfo.cs.txt
Loaded .\GlobalVersionInfo.cs.txt
Looking for AssemblyVersion line...
Current build revision: '1.0.2.0'
New build revision: 1.0.2.1
Looking for AssemblyFileVersion line...
Current build revision: '1.0.2.0'
New build revision: 1.0.2.1
Revision updated. Writing out new .\GlobalVersionInfo.cs.txt
Done
```

With the result:
```csharp
[assembly: AssemblyVersion("1.0.2.1")]
[assembly: AssemblyFileVersion("1.0.2.1")]
```

And that's pretty much it.

Sure, they can be tweaked to add flags for turn this on, but don't do this, or ignore this thing and do that instead. But this was the simplest thing that actually worked and did exactly what I need it to do.

### Build Loops
Be careful when you integrate this methodology with CI servers that push and pull code changes and rebuild on new changes that appear in your source control system. If your build script increments the number (as it's supposed to), and then pushes the update back to source control, and your source control triggers a build because of the new code change, you'll end up in a build/commit/push/trigger/pull/build loop. 

What? 

Andrew Harcourt ([@uglybugger](https://twitter.com/uglybugger)) has more on this: https://twitter.com/uglybugger/status/531056237681442816  (and provides a solution over at [https://teamcity-github-filter.azurewebsites.net/](https://teamcity-github-filter.azurewebsites.net/)).

_Released under the free-for-all MIT License, so if you want to copy it and do better stuff with it, go right ahead! :)_