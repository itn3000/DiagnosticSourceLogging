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
using Octokit;
using System.IO;

[CheckBuildProjectConfigurations]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.Compile);

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
                    .SetNoBuild(true)
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
                    .SetNoBuild(true)
                );
        });
    Target Pack => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetPack(settings => settings.SetConfiguration(Configuration)
                .SetVersionSuffix(VersionSuffix)
                .SetOutputDirectory(RootDirectory / "dist" / Configuration)
                .SetNoBuild(true));
        });
    [Parameter("nuget api key")]
    readonly string ApiKey;
    [Parameter("nuget package source for push")]
    readonly string PackageSource;
    Target Push => _ => _
        .After(Pack)
        .Executes(() =>
        {
            var packagedir = RootDirectory / "dist" / Configuration;
            var nupkgs = !string.IsNullOrEmpty(VersionSuffix) ? GlobDirectories(packagedir, $"DiagnosticSourceLogging.*.{VersionSuffix}.*")
                : GlobFiles(packagedir, $"DiagnosticSourceLogging.*");
            foreach (var nupkgPath in nupkgs)
            {
                Logger.Info($"pushing {nupkgPath}");
                DotNetNuGetPush(cfg => cfg.SetApiKey(ApiKey)
                    .SetTargetPath(nupkgPath)
                    .SetSource(PackageSource));
            }
        });
    [Parameter]
    readonly string GithubToken;
    [Parameter]
    readonly string GithubOwner;
    [Parameter]
    readonly string ReleaseTag;
    Target CreateGitHubRelease => _ => _
        .After(Pack)
        .Requires(() => GithubToken)
        .Requires(() => GithubOwner)
        .Executes(async () =>
        {
            var packagedir = RootDirectory / "dist" / Configuration;
            var nupkgs = !string.IsNullOrEmpty(VersionSuffix) ? GlobDirectories(packagedir, $"DiagnosticSourceLogging.*.{VersionSuffix}.*")
                : GlobFiles(packagedir, $"DiagnosticSourceLogging.*");
            var client = new GitHubClient(new ProductHeaderValue("DiagnosticSourceLoggingClient"));
            var cred = new Credentials(GithubToken);
            client.Credentials = cred;
            var repo = await client.Repository.Get(GithubOwner, "DiagnosticSourceLogging");
            var release = await client.Repository.Release.Get(GithubOwner, "DiagnosticSourceLogging", ReleaseTag);
            if(release == null)
            {
                var newRelease = new NewRelease(ReleaseTag);
                newRelease.Draft = true;
                newRelease.Prerelease = false;
                release = await client.Repository.Release.Create(repo.Id, newRelease);
            }
            foreach(var nupkgPath in nupkgs)
            {
                using var f = System.IO.File.OpenRead(nupkgPath);
                var uploadAsset = new ReleaseAssetUpload(Path.GetFileName(nupkgPath), "application/zip", f, TimeSpan.FromMinutes(5));
                await client.Repository.Release.UploadAsset(release, uploadAsset);
            }
        });
}
