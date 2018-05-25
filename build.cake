
// ARGUMENTS
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var platform = Argument("platform", "Any CPU");
var skipTests = Argument("SkipTests", false);

// Variables
var artifactsDirectory = Directory("../artifacts");
var solutionFile = "./NLog.Targets.GraylogHttp.sln";
var isRunningOnWindows = IsRunningOnWindows();
var IsOnAppVeyorAndNotPR = AppVeyor.IsRunningOnAppVeyor && !AppVeyor.Environment.PullRequest.IsPullRequest;

var msBuildSettings = new DotNetCoreMSBuildSettings
{
    MaxCpuCount = 1
};

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define directories.
var buildDir = Directory("./build/bin") + Directory(configuration);

Task("Clean")
    .Does(() =>
    {
        CleanDirectory(artifactsDirectory);
    });

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
    {
        DotNetCoreRestore(solutionFile);
    });

Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
    {
        var path = MakeAbsolute(new DirectoryPath(solutionFile));
        DotNetCoreBuild(path.FullPath, new DotNetCoreBuildSettings
        {
            Configuration = configuration,
            NoRestore = true,
            DiagnosticOutput = true,
            MSBuildSettings = msBuildSettings,
            Verbosity = DotNetCoreVerbosity.Minimal
        });
    });

Task("Test")
    .IsDependentOn("Build")
    .WithCriteria(!skipTests)
    .Does(() =>
    {
		var settings = new DotNetCoreTestSettings
		{
			Configuration = configuration
		};
		DotNetCoreTest("./src/NLog.Targets.GraylogHttp.Tests/NLog.Targets.GraylogHttp.Tests.csproj", settings);
    });

Task("Pack")
    .IsDependentOn("Build")
    .WithCriteria((IsOnAppVeyorAndNotPR || string.Equals(target, "pack", StringComparison.OrdinalIgnoreCase)) && isRunningOnWindows)
    .Does(() =>
    {
        var settings = new DotNetCorePackSettings
        {
            Configuration = configuration,
			NoBuild = true,
            OutputDirectory = artifactsDirectory,
			//IncludeSymbols = true
        };

        var projects = GetFiles("./src/**/NLog.Targets.GraylogHttp.csproj");
        foreach(var project in projects)
        {
            DotNetCorePack(project.FullPath, settings);
        }
    });

Task("Default")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Pack");

RunTarget(target);
