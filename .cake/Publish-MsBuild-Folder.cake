#load "Configuration.cake"
#load "Configuration-MsBuild.cake"

Task("Publish:MsBuild")
    .IsDependentOn("Build")
    .IsDependeeOf("Publish")
    .Does<Configuration>(config => 
{
    var toolVersion = config.GetTaskParameter<MSBuildToolVersion>("MsBuild:Version", MSBuildToolVersion.Default);

    Information("MsBuild Tool Version: " + toolVersion.ToString());

    foreach(var webProject in config.Solution.WebProjects) {
        var assemblyName = config.Solution.GetProjectName(webProject);
        var projectArtifactDirectory = $"{config.Artifacts.GetRootFor(ArtifactTypeOption.Zip)}/{assemblyName}";
        var artifactZipName = $"{assemblyName}.zip";
        var artifactZipFullPath = $"{projectArtifactDirectory}/{artifactZipName}";

        MSBuild(webProject.ProjectFilePath, c => c
            .SetConfiguration(config.Solution.BuildConfiguration)
            .SetVerbosity(Verbosity.Quiet)
            .UseToolVersion(toolVersion)
            .WithWarningsAsError()
            .WithTarget("Package")
            .WithProperty("DeployTarget", "PipelinePreDeployCopyAllFilesToOneFolder")
            .WithProperty("SkipInvalidConfigurations", "false")
            .WithProperty("AutoParameterizationWebConfigConnectionStrings", "false")
            .WithProperty("PackageTempRootDir", projectArtifactDirectory)
        );

        Zip($"{projectArtifactDirectory}/PackageTmp", artifactZipFullPath);

        DeleteDirectory($"{projectArtifactDirectory}/PackageTmp", new DeleteDirectorySettings {
            Recursive = true,
            Force = true
        });

        config.Artifacts.Add(ArtifactTypeOption.Zip, assemblyName, $"{artifactZipFullPath}");
    }
});