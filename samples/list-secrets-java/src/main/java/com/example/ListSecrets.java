package com.example;

// Using configured MCP servers, list secrets in a Key Vault using developer credentials.
// Secrets names should be sorted and printed on separate lines.
// Make sure the sample builds.
public class ListSecrets {
    public static void main(String[] args) {
        String endpoint = args.length > 0 ? args[0] : System.getenv("AZURE_KEYVAULT_URL");
        if (endpoint == null || endpoint.isEmpty()) {
            throw new IllegalArgumentException("Key Vault endpoint or $AZURE_KEYVAULT_URL required");
        }
    }
}
