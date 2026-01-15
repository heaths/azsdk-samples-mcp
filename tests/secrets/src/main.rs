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
    let credential = DeveloperToolsCredential::new(None)?;
    let client = SecretClient::new(&endpoint, credential, None)?;

    let mut secrets = client.list_secret_properties(None)?;
    while let Some(secret) = secrets.try_next().await? {
        let name = secret.resource_id()?.name;
        println!("{name}");
    }

    Ok(())
}
