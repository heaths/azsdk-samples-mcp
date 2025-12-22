using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace AzureSdk.SamplesMcp;

[McpServerResourceType]
public static class Resources
{
    [McpServerResource(UriTemplate = "samples://{dependency}", Name = "samples")]
    [Description("Gets sample content from a project dependency demonstrating how to use that dependency")]
    public static async Task<IEnumerable<ResourceContents>> GetSampleContent(
        RequestContext<ReadResourceRequestParams> context,
        [Description("A specific dependency from which samples are retrieved")] string dependency,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(dependency))
        {
            throw new ArgumentException("Missing required dependency parameter");
        }

        if (string.Equals(dependency, "foo", StringComparison.InvariantCultureIgnoreCase))
        {
            return [
                new TextResourceContents
                {
                    Uri = "samples://foo",
                    Text = """
                    # Foo

                    ## Examples

                    ```rust
                    let x = foo();
                    ```
                    """,
                    MimeType = "text/plain",
                },
                new TextResourceContents
                {
                    Uri = "samples://foo",
                    Text = """
                    use foo;

                    fn main() {
                        println("{}", foo());
                    }
                    """,
                    MimeType = "text/plain",
                },
            ];
        }

        if (string.Equals(dependency, "bar", StringComparison.InvariantCultureIgnoreCase))
        {
            return [
                new TextResourceContents
                {
                    Uri = "samples://bar",
                    Text = """
                    # Bar

                    ## Examples

                    ```rust
                    let x = bar();
                    ```
                    """,
                    MimeType = "text/plain",
                },
            ];
        }

        throw new NotSupportedException($"Unknown resource: {context.Params?.Uri}");
    }
}
