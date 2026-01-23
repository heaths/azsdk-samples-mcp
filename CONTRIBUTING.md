# Contributing

> Note: This repository is a prototype and is not officially supported by Microsoft.

## Prerequisites

- .NET 10 SDK: <https://dotnet.microsoft.com/download>
- Node.js (LTS): <https://nodejs.org>
- Visual Studio Code (optional): <https://code.visualstudio.com>
- Rust (optional, for user testing the Rust sample): <https://www.rust-lang.org/tools/install>

## Getting Started

- Clone the repository and work from the repo root.
- Use your preferred editor (VS Code recommended) and ensure the prerequisites above are installed.

## Tests and Checks

- Pull requests run build/test and analyze jobs (formatting, spelling, markdown lint) in CI.
- You can run them locally if desired:
  - dotnet build
  - dotnet test
  - npx cspell lint .
  - npx markdownlint-cli2

## Contributing Workflow

- For substantive code changes, please open an issue first to discuss the approach.
- Small changes (e.g., typo fixes) can go straight to a PR without opening an issue.
- Keep PRs focused and reasonably sized for easier review.
