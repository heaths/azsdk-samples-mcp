use std::env;

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    #[allow(unused_variables)]
    let endpoint = env::args()
        .nth(1)
        .or_else(|| env::var("AZURE_KEYVAULT_URL").ok())
        .ok_or("Key Vault endpoint or $AZURE_KEYVAULT_URL required")?;

    // Using configured MCP servers, list secrets in a Key Vault using developer credentials.
    // Secrets names should be sorted and printed on separate lines.
    // Make sure the sample builds.

    Ok(())
}
