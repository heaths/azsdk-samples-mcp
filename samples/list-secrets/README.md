# List Secrets Sample

This Rust sample demonstrates how to list secrets from an Azure Key Vault using developer credentials.

## Prerequisites

- [Rust](https://rustup.rs/)
- Azure Key Vault with secret access
- Authenticated with Azure (e.g., `az login`)

To provision Azure resources for this sample, see [infra/README.md](../../infra/README.md).

## Using with MCP Server

Enable the MCP server as configured in [.copilot/mcp-config.json](../../.copilot/mcp-config.json).
Start [Copilot CLI](https://github.com/features/copilot/cli/) or use Copilot in [VSCode](https://code.visualstudio.com/) and prompt it with the comment in [src/main.rs](src/main.rs).

## Building

Build the sample:

```bash
cargo build --manifest-path samples/list-secrets/Cargo.toml
```

## Running

Run the sample with a vault URL:

```bash
cargo run --manifest-path samples/list-secrets/Cargo.toml -- https://your-vault.vault.azure.net/
```

Or set the vault URL as an environment variable:

```bash
export AZURE_KEYVAULT_URL=https://your-vault.vault.azure.net/
cargo run --manifest-path samples/list-secrets/Cargo.toml
```

If you provisioned resources using `azd`, use the environment variable:

```bash
cargo run --manifest-path samples/list-secrets/Cargo.toml -- $(azd env get-value AZURE_KEYVAULT_ENDPOINT)
```

Note: `cargo run` automatically builds the project if needed.
