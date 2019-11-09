#tool "nuget:?package=GitVersion.CommandLine&version=5.1.2"

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
        GitVersion(new GitVersionSettings{
            UpdateAssemblyInfo = false,
            OutputType = GitVersionOutput.BuildServer
        });
        versionInfo = GitVersion(new GitVersionSettings{ OutputType = GitVersionOutput.Json });

        Information($"::set-env name=GIT_VERSION::{versionInfo.FullSemVer}");

        msBuildSettings.Properties.Add("PackageVersion", new List<string> { versionInfo.NuGetVersionV2 });
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

        // if (AppVeyor.IsRunningOnAppVeyor)
        // {
        //     var testResultsFile = MakeAbsolute(new FilePath($"{MakeAbsolute(artifactsDirectory)}/TestResults.xml"));
        //     BuildSystem.AppVeyor.UploadTestResults(testResultsFile, AppVeyorTestResultsType.MSTest);
        // }
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
            MSBuildSettings = msBuildSettings,
			//IncludeSymbols = true
        });
    });

Task("Default")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Pack");

RunTarget(target);
