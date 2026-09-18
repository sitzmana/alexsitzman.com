using YamlDotNet.RepresentationModel;

namespace Portfolio.Tests;

public sealed class DeploymentWorkflowTests
{
    private const string DeploymentWorkflow = "azure-static-web-apps-jolly-water-0e62d601e.yml";

    private static readonly string WorkflowDirectory = Path.Combine(RepositoryPaths.Root, ".github", "workflows");

    [Fact]
    public void Only_one_workflow_can_deploy_to_static_web_apps()
    {
        var publishers = Directory.EnumerateFiles(WorkflowDirectory)
            .Where(path => Path.GetExtension(path) is ".yml" or ".yaml")
            .Where(path => Map(Load(path), "jobs").Children.Values
                .Select(job => Assert.IsType<YamlMappingNode>(job))
                .SelectMany(Steps)
                .Any(step => Text(step, "uses")?.StartsWith("Azure/static-web-apps-deploy@", StringComparison.Ordinal) == true))
            .Select(Path.GetFileName);

        Assert.Equal(DeploymentWorkflow, Assert.Single(publishers));
    }

    [Fact]
    public void Deployment_consumes_the_validated_artifact_without_another_build()
    {
        var jobs = Map(Load(Path.Combine(WorkflowDirectory, DeploymentWorkflow)), "jobs");
        Assert.Equal("./.github/workflows/ci.yml", Text(Map(jobs, "build"), "uses"));
        var deploy = Map(jobs, "deploy");
        Assert.Equal("build", Text(deploy, "needs"));

        var steps = Steps(deploy).ToArray();
        var download = Action(steps, "actions/download-artifact@");
        var upload = Action(steps, "Azure/static-web-apps-deploy@");
        Assert.True(Array.IndexOf(steps, download) < Array.IndexOf(steps, upload));
        var downloadOptions = Map(download, "with");
        var uploadOptions = Map(upload, "with");
        Assert.Equal("site", Text(downloadOptions, "name"));
        Assert.Equal("dist", Text(downloadOptions, "path"));
        Assert.Equal(Text(downloadOptions, "path"), Text(uploadOptions, "app_location"));
        Assert.Equal("", Text(uploadOptions, "output_location"));
        Assert.Equal("", Text(uploadOptions, "api_location"));
        Assert.Equal("true", Text(uploadOptions, "skip_app_build"));

        var ciJobs = Map(Load(Path.Combine(WorkflowDirectory, "ci.yml")), "jobs");
        var artifact = Assert.Single(Steps(Map(ciJobs, "build")), step =>
            Text(step, "uses")?.StartsWith("actions/upload-artifact@", StringComparison.Ordinal) == true
            && Text(Map(step, "with"), "name") == "site");
        Assert.Equal("dist", Text(Map(artifact, "with"), "path"));
    }

    [Fact]
    public void Upload_and_preview_cleanup_reference_the_same_deployment_secret()
    {
        var jobs = Map(Load(Path.Combine(WorkflowDirectory, DeploymentWorkflow)), "jobs");
        var upload = Map(Action(Steps(Map(jobs, "deploy")), "Azure/static-web-apps-deploy@"), "with");
        var close = Map(Action(Steps(Map(jobs, "close-preview")), "Azure/static-web-apps-deploy@"), "with");
        var token = Assert.IsType<string>(Text(upload, "azure_static_web_apps_api_token"));

        Assert.Matches(@"^\$\{\{\s*secrets\.[A-Z0-9_]+\s*\}\}$", token);
        Assert.Equal(token, Text(close, "azure_static_web_apps_api_token"));
        Assert.Null(Text(upload, "github_id_token"));
        Assert.Null(Text(close, "github_id_token"));
        Assert.Equal("upload", Text(upload, "action"));
        Assert.Equal("close", Text(close, "action"));
    }

    private static YamlMappingNode Load(string path)
    {
        using var reader = File.OpenText(path);
        var yaml = new YamlStream();
        yaml.Load(reader);
        return Assert.IsType<YamlMappingNode>(Assert.Single(yaml.Documents).RootNode);
    }

    private static YamlMappingNode Map(YamlMappingNode parent, string key) =>
        Assert.IsType<YamlMappingNode>(parent.Children[new YamlScalarNode(key)]);

    private static string? Text(YamlMappingNode parent, string key) =>
        parent.Children.TryGetValue(new YamlScalarNode(key), out var node)
            ? Assert.IsType<YamlScalarNode>(node).Value
            : null;

    private static IEnumerable<YamlMappingNode> Steps(YamlMappingNode job) =>
        job.Children.TryGetValue(new YamlScalarNode("steps"), out var node)
            ? Assert.IsType<YamlSequenceNode>(node).Children.Select(step => Assert.IsType<YamlMappingNode>(step))
            : [];

    private static YamlMappingNode Action(IEnumerable<YamlMappingNode> steps, string prefix) =>
        Assert.Single(steps, step => Text(step, "uses")?.StartsWith(prefix, StringComparison.Ordinal) == true);
}
