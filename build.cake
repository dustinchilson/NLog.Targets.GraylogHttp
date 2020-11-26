// ARGUMENTS
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var skipTests = Argument("SkipTests", false);

var fullSemVer = Argument("fullSemVer", "0.0.1");
var assemblySemVer = Argument("assemblySemVer", "0.0.1");
var informationalVersion = Argument("informationalVersion", "0.0.1");

// Variables
var artifactsDirectory = Directory("./artifacts");
var solutionFile = "./NLog.Targets.GraylogHttp.sln";

var msBuildSettings = new DotNetCoreMSBuildSettings
{
    MaxCpuCount = 1
};

msBuildSettings.Properties.Add("PackageVersion", new List<string> { fullSemVer });
msBuildSettings.Properties.Add("Version", new List<string> { assemblySemVer });
msBuildSettings.Properties.Add("FileVersion", new List<string> { assemblySemVer });
msBuildSettings.Properties.Add("AssemblyVersion", new List<string> { assemblySemVer });
msBuildSettings.Properties.Add("AssemblyInformationalVersion", new List<string> { informationalVersion });

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
		var path = MakeAbsolute(new DirectoryPath(solutionFile));
		DotNetCoreTest(path.FullPath, new DotNetCoreTestSettings
		{
			Configuration = configuration,
			NoBuild = true,
            ResultsDirectory = artifactsDirectory,
            Logger = "trx;LogFileName=TestResults.xml"
		});
    });

Task("Pack")
    .IsDependentOn("Build")
    .Does(() =>
    {
		var path = MakeAbsolute(new DirectoryPath(solutionFile));
		DotNetCorePack(path.FullPath, new DotNetCorePackSettings
        {
            Configuration = configuration,
			NoRestore = true,
			NoBuild = true,
            OutputDirectory = artifactsDirectory,
            MSBuildSettings = msBuildSettings
        });
    });

Task("Default")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Pack");

RunTarget(target);
