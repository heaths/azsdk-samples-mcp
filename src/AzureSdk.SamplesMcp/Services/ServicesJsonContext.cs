// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace AzureSdk.SamplesMcp.Services;

[JsonSerializable(typeof(ExternalProcessService.ParseError))]
[JsonSerializable(typeof(ExternalProcessService.ParseOutput))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class ServicesJsonContext : JsonSerializerContext
{

}
