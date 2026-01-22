use azure_identity::DeveloperToolsCredential;
use azure_security_keyvault_secrets::{ResourceExt, SecretClient};
use futures::TryStreamExt as _;
use std::env;

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let endpoint = env::args()
        .skip(1)
        .next()
        .or_else(|| env::var("AZURE_KEYVAULT_URL").ok())
        .ok_or_else(|| "Key Vault endpoint or $AZURE_KEYVAULT_URL required")?;

    // Create a new secret client
    let credential = DeveloperToolsCredential::new(None)?;
    let client = SecretClient::new(&endpoint, credential.clone(), None)?;

    let mut pager = client.list_secret_properties(None)?.into_stream();
    while let Some(secret) = pager.try_next().await? {
        // Get the secret name from the ID.
        let name = secret.resource_id()?.name;
        println!("{}", name);
    }

    Ok(())
}
