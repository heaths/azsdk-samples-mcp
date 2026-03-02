package main

import (
	"fmt"
	"os"
)

func main() {
	endpoint := ""
	if len(os.Args) > 1 {
		endpoint = os.Args[1]
	} else {
		endpoint = os.Getenv("AZURE_APPCONFIG_ENDPOINT")
	}
	if endpoint == "" {
		fmt.Fprintln(os.Stderr, "App Configuration endpoint or $AZURE_APPCONFIG_ENDPOINT required")
		os.Exit(1)
	}

	// Using configured MCP servers, list values from Azure App Configuration given an endpoint using developer credentials.
	// The values should be printed alphabetically in the format "{key}: {content_type} = {value}" on each line.
	// Make sure the sample builds.

	_ = endpoint
}
