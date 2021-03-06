// ---------------- Constants ----------------

const string buildTarget = "build";
const string releaseTarget = "build_release";
const string testTarget = "run_test";
const string zipTarget = "create_zip";
const string defaultTarget = buildTarget;

string target = Argument( "target", defaultTarget );

FilePath sln = File( "src/VoiceMacroCodingPlugin.sln" );

const string distroPathName = "DistPackages";
DirectoryPath distroPath = Directory( distroPathName );

const string packageName = "VoiceMacro.Plugins.Coding";
const string version = "1.0.0";

MSBuildSettings msBuildSettings = new MSBuildSettings
{
    Restore = true,
    ToolVersion = MSBuildToolVersion.VS2019,
    MaxCpuCount = 0
};
msBuildSettings = msBuildSettings.WithProperty( "Version", version )
    .WithProperty( "AssemblyVersion", version )
    .WithProperty( "FileVersion", version );

// ---------------- Tasks ----------------

Task( buildTarget )
.Does(
    () =>
    {
        MSBuild( sln, msBuildSettings );
    }
).Description( "Builds the debug build." );

Task( releaseTarget )
.Does(
    () =>
    {
        EnsureDirectoryExists( distroPath );
        CleanDirectory( distroPath );

        msBuildSettings.Configuration = "Release";

        MSBuild( sln, msBuildSettings );
    }
).Description( "Builds the release build." );

Task( testTarget )
.Does(
    () =>
    {
        // TODO: Maybe this should go into Cake.Frosting and use the test helpers
        // in Seth.CakeLib?
        FilePath testProject = "src/VoiceMacro.Plugins.Coding.Tests/VoiceMacro.Plugins.Coding.Tests.csproj";
        DirectoryPath resultsDir = Directory( "TestResults" );
        FilePath resultFile = resultsDir.CombineWithFilePath( "test_results.xml" );

        EnsureDirectoryExists( resultsDir );
        CleanDirectory( resultsDir );

        DotNetCoreTestSettings settings = new DotNetCoreTestSettings
        {
            NoBuild = true,
            NoRestore = true,
            Configuration = "Debug",
            ResultsDirectory = resultsDir,
            VSTestReportPath = resultFile,
            Verbosity = DotNetCoreVerbosity.Normal
        };

        // Need to restore to download the TestHost, which is a NuGet package.
        Information( "Restoring..." );
        DotNetCoreRestore( testProject.ToString() );
        Information( "Restoring... Done!" );

        Information( "Running Tests..." );
        DotNetCoreTest( testProject.ToString(), settings );
        Information( "Running Tests... Done!" );
    }
).Description( "Runs all tests" );

Task( zipTarget )
.Does(
    () =>
    {
        CopyFile(
            File( "LICENSE_1_0.txt" ),
            distroPath.CombineWithFilePath( File( $"{packageName}.LICENSE_1_0.txt" ) )
        );

        CopyFile(
            File( "Readme.md" ),
            distroPath.CombineWithFilePath( File( $"{packageName}.Readme.md" ) )
        );

        FilePath glob = distroPath.CombineWithFilePath( File( "(*.dll|*.pdb|*.txt|*md)" ) );
        var files = GetFiles( glob.ToString() );

        FilePath outputFile = distroPath.CombineWithFilePath( File( $"{packageName}_{version}.zip" ) );

        Zip( distroPathName, outputFile.ToString(), files );
    }
).Description( "Creates the .zip that contains the plugin." )
.IsDependentOn( releaseTarget );

RunTarget( target );
