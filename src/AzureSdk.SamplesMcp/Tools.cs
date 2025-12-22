using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace AzureSdk.SamplesMcp;

[McpServerToolType]
public static class Tools
{
    [McpServerTool(Name = "dependencies")]
    [Description("Lists dependencies of the current project")]
    public static async Task<IEnumerable<string>> GetDependencies()
    {
        return [
            "foo",
            "bar",
        ];
    }

    [McpServerTool(Name = "samples")]
    [Description("Lists samples of dependencies of the current project")]
    public static async Task<IEnumerable<ContentBlock>> GetSamples(
        [Description("A specific dependency from which samples are retrieved")] string? dependency = null
    )
    {
        List<ContentBlock> resources = [];
        if (string.IsNullOrWhiteSpace(dependency) || string.Equals(dependency, "foo", StringComparison.InvariantCultureIgnoreCase))
        {
            resources.Add(new ResourceLinkBlock
            {
                Name = "foo",
                Uri = "samples://foo",
            });
        }
        if (string.IsNullOrWhiteSpace(dependency) || string.Equals(dependency, "bar", StringComparison.InvariantCultureIgnoreCase))
        {
            resources.Add(new ResourceLinkBlock
            {
                Name = "bar",
                Uri = "samples://bar",
            });
        }

        return resources;
    }
}
