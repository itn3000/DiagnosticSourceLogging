using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using Nuke.Common.Tools.DotNet;

[CheckBuildProjectConfigurations]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;
    [Parameter("TargetPlatform")]
    readonly string TargetPlatform;
    [Parameter("Version suffix")]
    readonly string VersionSuffix = "";

    [Solution] readonly Solution Solution;

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            DotNetClean((settings) => settings.SetConfiguration(Configuration));
            EnsureCleanDirectory(RootDirectory / "dist" / Configuration);
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore();
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(settings => settings.SetConfiguration(Configuration));
        });
    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(settings => 
                settings.SetConfiguration(Configuration)
                    .SetLogger("trx")
                );
        });
    Target Publish => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetPublish(settings =>
                settings.SetProject(RootDirectory / "DiagnosticSourceLogging" / "DiagnosticSourceLogging.csproj")
                    .SetConfiguration(Configuration)
                    .SetVersionSuffix(VersionSuffix)
                );
        });
    Target Pack => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetPack(settings => settings.SetConfiguration(Configuration)
                .SetVersionSuffix(VersionSuffix)
                .SetOutputDirectory(RootDirectory / "dist" / Configuration));
        });

}
