using System.Threading;
//////////////////////////////////////////////////////////////////////
// ADDINS
//////////////////////////////////////////////////////////////////////

#addin "Cake.FileHelpers"
#addin "Cake.AppleSimulator.SushiHangover"

//////////////////////////////////////////////////////////////////////
// TOOLS
//////////////////////////////////////////////////////////////////////

#tool "nuget:?package=Cake.AppleSimulator.SushiHangover"
#tool "nuget:?package=xunit.runner.console"
#tool "nuget:?package=GitVersion.CommandLine"
#tool "GitReleaseManager"
#tool "GitLink"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
if (string.IsNullOrWhiteSpace(target))
{
    target = "Default";
}

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// should MSBuild & GitLink treat any errors as warnings.
var treatWarningsAsErrors = false;

// Build configuration
var local = BuildSystem.IsLocalBuild;
var isRunningOnMacOS = IsRunningOnUnix(); //(Context.Environment.Platform.Family == PlatformFamily.OSX); // Still broken
var isRunningOnUnix = IsRunningOnUnix();
var isRunningOnWindows = IsRunningOnWindows();

var githubOwner = "sushihangover";
var githubRepository = "Cake.AppleSimulator";
var githubUrl = string.Format("https://github.com/{0}/{1}", githubOwner, githubRepository);

var isRunningOnAppVeyor = AppVeyor.IsRunningOnAppVeyor;
var isPullRequest = AppVeyor.Environment.PullRequest.IsPullRequest;
var isRepository = StringComparer.OrdinalIgnoreCase.Equals("sushihangover/Cake.AppleSimulator", AppVeyor.Environment.Repository.Name);

var isReleaseBranch = StringComparer.OrdinalIgnoreCase.Equals("sushi", AppVeyor.Environment.Repository.Branch);
var isTagged = AppVeyor.Environment.Repository.Tag.IsTag;

// Parse release notes.
var releaseNotes = ParseReleaseNotes("RELEASENOTES.md");

// Version
var gitVersion = GitVersion();
var majorMinorPatch = gitVersion.MajorMinorPatch;
var semVersion = gitVersion.SemVer;
var informationalVersion = gitVersion.InformationalVersion;
var nugetVersion = gitVersion.NuGetVersion;
var buildVersion = gitVersion.FullBuildMetaData;

// Artifacts
var artifactDirectory = "./artifacts/";
var packageWhitelist = new[] { "Cake.AppleSimulator.SushiHangover" };

// Macros
Action Abort = () => { throw new Exception("A non-recoverable fatal error occurred."); };
Action TestFailuresAbort = () => { throw new Exception("Testing revealed failed unit tests"); };
Action NonMacOSAbort = () => { throw new Exception("Running on platforms other macOS is not supported."); };
Action<string, string, string> buildThisApp = (p,c,t) =>
{
    Information("{0}:{1}:{2}", t,c,p);
    if (isRunningOnMacOS)
    {
        var settings = new XBuildSettings()
            .WithProperty("SolutionDir", new string[] { @"./" })
            .WithProperty("OutputPath", new string[] { @"../../artifacts/" })
            .SetConfiguration(c)
            .SetVerbosity(Verbosity.Quiet)
            .WithTarget(t);
        XBuild(p, settings);
    };
};

Action<string> RestorePackages = (solution) =>
{
    NuGetRestore(solution);
};

Action<string, string> Package = (nuspec, basePath) =>
{
    CreateDirectory(artifactDirectory);

    Information("Packaging {0} using {1} as the BasePath.", nuspec, basePath);

    NuGetPack(nuspec, new NuGetPackSettings {
        Authors                  = new [] { "SushiHangover/RobertN" },
        Owners                   = new [] { "sushihangover" },

        ProjectUrl               = new Uri(githubUrl),
        IconUrl                  = new Uri("https://raw.githubusercontent.com/cake-build/graphics/master/png/cake-medium.png"),
        LicenseUrl               = new Uri("https://opensource.org/licenses/MIT"),
        Copyright                = "Copyright (c) SushiHangover/RobertN",
        RequireLicenseAcceptance = false,

        Version                  = nugetVersion,
        Tags                     = new [] {  "Cake", "Script", "Build", "Xamarin", "iOS", "watchOS", "tvOS", "Simulator", "simctl", "xcrun" },
        ReleaseNotes             = new [] { string.Format("{0}/releases", githubUrl) },

        Symbols                  = true,
        Verbosity                = NuGetVerbosity.Detailed,
        OutputDirectory          = artifactDirectory,
        BasePath                 = basePath,
    });
};

Action<string> SourceLink = (solutionFileName) =>
{
    GitLink("./", new GitLinkSettings() {
        RepositoryUrl = githubUrl,
        SolutionFileName = solutionFileName,
        ErrorsAsWarnings = treatWarningsAsErrors,
    });
};

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////
Setup(() =>
{
    Information("Building version {0} of Cake.AppleSimulator.SushiHangover.", semVersion);
});

Teardown(() =>
{
    // Executed AFTER the last task.
});

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Build")
    .IsDependentOn("RestorePackages")
    .IsDependentOn("UpdateAssemblyInfo")
    .Does (() =>
{
    Action<string> build = (filename) =>
    {
        var solution = System.IO.Path.Combine("./src/", filename);
        
        Information("Building {0}", solution);

        if (isRunningOnMacOS)
        {
            var settings = new XBuildSettings()
                .SetConfiguration("Release")
                .SetVerbosity(Verbosity.Quiet)
                .WithTarget("Clean");
            XBuild(solution, settings);
            settings = new XBuildSettings()
                .SetConfiguration("Release")
                .SetVerbosity(Verbosity.Quiet)
                .WithTarget("Build");
            XBuild(solution, settings);
        }
        if (isRunningOnWindows)
        {
            MSBuild(solution, new MSBuildSettings()
                .SetConfiguration("Release")
                .SetVerbosity(Verbosity.Minimal)
                .SetNodeReuse(false));
            // SourceLink(solution);
        }
    };

    build("Cake.AppleSimulator.sln");
});

Task("UpdateAppVeyorBuildNumber")
    .WithCriteria(() => isRunningOnAppVeyor)
    .WithCriteria(() => isReleaseBranch)
    .WithCriteria(() => isTagged)
    .Does(() =>
{
    Information("{0}", semVersion);
    AppVeyor.UpdateBuildVersion(semVersion);
});

Task("UpdateAssemblyInfo")
    .IsDependentOn("UpdateAppVeyorBuildNumber")
    .Does (() =>
{
    var file = "./src/CommonAssemblyInfo.cs";

    CreateAssemblyInfo(file, new AssemblyInfoSettings {
        Product = "Cake.AppleSimulator.SushiHangover",
        Version = majorMinorPatch,
        FileVersion = majorMinorPatch,
        InformationalVersion = informationalVersion,
        Copyright = "Copyright (c) SushiHangover/RobertN"
    });
});

Task("RestorePackages").Does (() =>
{
    RestorePackages("./src/Cake.AppleSimulator.sln");
});

Task("CreateRelease")
    .IsDependentOn("RunUnitTests")
    .IsDependentOn("Package")
    .WithCriteria(() => !local)
    .WithCriteria(() => !isPullRequest)
    .WithCriteria(() => isRepository)
    .WithCriteria(() => isReleaseBranch)
    .WithCriteria(() => isTagged)
    .Does (() =>
{
    var username = EnvironmentVariable("GITHUB_USERNAME");
    if (string.IsNullOrEmpty(username))
    {
        throw new Exception("The GITHUB_USERNAME environment variable is not defined.");
    }

    var token = EnvironmentVariable("GITHUB_TOKEN");
    if (string.IsNullOrEmpty(token))
    {
        throw new Exception("The GITHUB_TOKEN environment variable is not defined.");
    }

    Information("username : {0}", username);
    Information("githubOwner : {0}", githubOwner);
    Information("githubRepository : {0}", githubRepository);
    Information("majorMinorPatch : {0}", majorMinorPatch);
    GitReleaseManagerCreate(username, token, githubOwner, githubRepository, new GitReleaseManagerCreateSettings {
        Milestone         = majorMinorPatch,
        Name              = majorMinorPatch,
        Prerelease        = true,
        TargetCommitish   = "sushi"
    });
});

Task("RunUnitTests")
   .IsDependentOn("Build")
    .Does(() =>
{
    XUnit2("./src/Cake.AppleSimulator.Tests/bin/Release/Cake.AppleSimulator.Tests.dll", new XUnit2Settings {
        OutputDirectory = artifactDirectory,
        XmlReportV1 = false,
        NoAppDomain = true
    });
});

Task("Package")
    .IsDependentOn("RunUnitTests")
    .Does (() =>
{
    Package("./src/Cake.AppleSimulator.nuspec", "./src/Cake.AppleSimulator");
});

Task("PublishPackages")
    .IsDependentOn("Package")
    .WithCriteria(() => !local)
    .WithCriteria(() => !isPullRequest)
    .WithCriteria(() => isReleaseBranch)
    .WithCriteria(() => isRepository)
    .WithCriteria(() => isTagged)
    .Does (() =>
{
    // Resolve the API key.
    var apiKey = EnvironmentVariable("NUGET_APIKEY");
    if (string.IsNullOrEmpty(apiKey))
    {
        throw new Exception("The NUGET_APIKEY environment variable is not defined.");
    }

    var source = EnvironmentVariable("NUGET_SOURCE");
    if (string.IsNullOrEmpty(source))
    {
        throw new Exception("The NUGET_SOURCE environment variable is not defined.");
    }

    // only push whitelisted packages.
    foreach(var package in packageWhitelist)
    {
        // only push the package which was created during this build run.
        var packagePath = artifactDirectory + File(string.Concat(package, ".", nugetVersion, ".nupkg"));

        // Push the package.
        NuGetPush(packagePath, new NuGetPushSettings {
            Source = source,
            ApiKey = apiKey
        });
    }
});

Task("PublishRelease")
    .IsDependentOn("Package")
    .WithCriteria(() => !local)
    .WithCriteria(() => !isPullRequest)
    .WithCriteria(() => isRepository)
    .WithCriteria(() => isReleaseBranch)
    .WithCriteria(() => isTagged)
    .Does (() =>
{
    var username = EnvironmentVariable("GITHUB_USERNAME");
    if (string.IsNullOrEmpty(username))
    {
        throw new Exception("The GITHUB_USERNAME environment variable is not defined.");
    }

    var token = EnvironmentVariable("GITHUB_TOKEN");
    if (string.IsNullOrEmpty(token))
    {
        throw new Exception("The GITHUB_TOKEN environment variable is not defined.");
    }

    // only push whitelisted packages.
    foreach(var package in packageWhitelist)
    {
        // only push the package which was created during this build run.
        var packagePath = artifactDirectory + File(string.Concat(package, ".", nugetVersion, ".nupkg"));

        GitReleaseManagerAddAssets(username, token, githubOwner, githubRepository, majorMinorPatch, packagePath);
    }

    GitReleaseManagerClose(username, token, githubOwner, githubRepository, majorMinorPatch);
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("CreateRelease")
    .IsDependentOn("PublishPackages")
    .IsDependentOn("PublishRelease")
    .Does (() =>
{
});

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
