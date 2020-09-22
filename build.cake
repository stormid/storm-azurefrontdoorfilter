#load ".cake/Configuration.cake"

/**********************************************************/
Setup<Configuration>(Configuration.Create);
/**********************************************************/

#load ".cake/CI.cake"

// -- DotNetCore
#load ".cake/Restore-DotNetCore.cake"
#load ".cake/Build-DotNetCore.cake"
#load ".cake/Test-DotNetCore.cake"
#load ".cake/Publish-Pack-DotNetCore.cake"
// -------------

Task("Restore:DotNetCore:Tools")
    .IsDependeeOf("Restore")
    .Does<Configuration>(config => {

    var settings = new ProcessSettings() 
    { 
        RedirectStandardOutput = false
    };

    settings.Arguments = string.Format($"tool restore");
    using(var process = StartAndReturnProcess("dotnet", settings))
    {
        process.WaitForExit();
    }
});

RunTarget(Argument("target", Argument("Target", "Default")));