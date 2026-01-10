using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace AzureSdk.SamplesMcp;

[McpServerResourceType]
public static class Resources
{
    static readonly TextResourceContents[] files =
    [
        new()
        {
            Uri = "samples://foo/README.md",
            Text = """
            # Foo

            ## Examples

            ```rust
            let x = foo();
            ```
            """,
            MimeType = "text/plain",
        },
        new()
        {
            Uri = "samples://foo/examples/example.rs",
            Text = """
            use foo;

            fn main() {
                println("{}", foo());
            }
            """,
            MimeType = "text/plain",
        },
        new()
        {
            Uri = "samples://bar/README.md",
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

    [McpServerResource(UriTemplate = "samples://{dependency}/{+path}", Name = "samples")]
    [Description("Gets sample content from a project dependency demonstrating how to use that dependency")]
    public static async Task<IEnumerable<ResourceContents>> GetSampleContent(
        RequestContext<ReadResourceRequestParams> context,
        [Description("A specific dependency from which samples are retrieved")] string dependency,
        [Description("The path of a sample file relative to the root directory of the dependency")] string path,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(dependency))
        {
            throw new ArgumentException("Missing required dependency parameter");
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Missing required path parameter");
        }

        return files.Where(file => string.Equals(file.Uri, context?.Params?.Uri, StringComparison.InvariantCultureIgnoreCase)) ??
            throw new NotSupportedException($"Unknown resource: {context.Params?.Uri}");
    }
}
