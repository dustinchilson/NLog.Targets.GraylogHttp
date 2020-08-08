#addin "nuget:?package=Cake.Json&version=4.0.0"
#addin "nuget:?package=Newtonsoft.Json&version=11.0.2"

// ARGUMENTS
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var platform = Argument("platform", "Any CPU");
var skipTests = Argument("SkipTests", false);

// Variables
var artifactsDirectory = Directory("./artifacts");
var solutionFile = "./NLog.Targets.GraylogHttp.sln";

var msBuildSettings = new DotNetCoreMSBuildSettings
{
    MaxCpuCount = 1
};

GitVersion versionInfo = null;

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

Task("Version")
    .Does(() => {
        // dotnet tool install --global GitVersion.Tool --version 5.1.2
        DotNetCoreTool("tool",
            new DotNetCoreToolSettings {
                ArgumentCustomization = args => args.Append("restore")
            });

        // dotnet gitversion /output json
        IEnumerable<string> redirectedStandardOutput;
        StartProcess(
            "dotnet",
            new ProcessSettings {
                Arguments = "dotnet-gitversion /output json",
                RedirectStandardOutput = true
            },
            out redirectedStandardOutput
        );

        versionInfo = DeserializeJson<GitVersion>(string.Join(Environment.NewLine, redirectedStandardOutput));

        Information($"::set-env name=GIT_VERSION::{versionInfo.FullSemVer}");

        msBuildSettings.Properties.Add("PackageVersion", new List<string> { versionInfo.FullSemVer });
        msBuildSettings.Properties.Add("Version", new List<string> { versionInfo.AssemblySemVer });
        msBuildSettings.Properties.Add("FileVersion", new List<string> { versionInfo.AssemblySemVer });
        msBuildSettings.Properties.Add("AssemblyVersion", new List<string> { versionInfo.AssemblySemVer });
        msBuildSettings.Properties.Add("AssemblyInformationalVersion", new List<string> { versionInfo.InformationalVersion });
    });

Task("Build")
    .IsDependentOn("Restore")
    .IsDependentOn("Version")
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
