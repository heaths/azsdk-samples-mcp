using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace AzureSdk.SamplesMcp;

[McpServerResourceType]
public static class Resources
{
    [McpServerResource(UriTemplate = "samples://{dependency}", Name = "samples")]
    [Description("Gets sample content from a project dependency demonstrating how to use that dependency")]
    public static async Task<ResourceContents> GetSampleContent(
        RequestContext<ReadResourceRequestParams> context,
        string dependency,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(dependency))
        {
            throw new ArgumentException("Missing required dependency parameter");
        }

        throw new NotImplementedException();
    }
}
