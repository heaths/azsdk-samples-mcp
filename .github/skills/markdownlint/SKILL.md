---
name: markdownlint
description: Lint and fix Markdown files using markdownlint-cli2.
compatibility: Requires Node.js with npm version 7.0 or newer.
allowed-tools: bash(npx:*)
---

# Markdown linting

Check markdown files for common mistakes.

## Installation and usage

Run `npx -y markdownlint-cli2@<version> <command>` from the repository root.

If a `package.json` exists in the repository root with `markdownlint-cli2` listed in `devDependencies`, use that version. Otherwise, use the latest version (omit `@<version>`).

## Configuration

markdownlint-cli2 configuration including custom rules can be found in files:

- `.markdownlint-cli2.json`
- `.markdownlint-cli2.yaml`
- `.markdownlint-cli2.yml`

If a suitable configuration is not found, create a `.markdownlint-cli2.yaml` file in the root of the repository.
The configuration file schema is described at `https://raw.githubusercontent.com/DavidAnson/markdownlint-cli2/v<version>/schema/markdownlint-cli2-config-schema.json` where `<version>` should match the version from `package.json` devDependencies (e.g., `v0.20.0`). If no `package.json` exists, use `https://raw.githubusercontent.com/DavidAnson/markdownlint-cli2/main/schema/markdownlint-cli2-config-schema.json`.
Always include the schema URI in the `$schema` field.

For markdownlint rules configuration, nest it under the `config` property following the markdownlint schema at `https://raw.githubusercontent.com/DavidAnson/markdownlint/main/schema/markdownlint-config-schema.json`.

If not already present, add a `globs` array with file patterns to lint (e.g., `["**/*.md"]`) so no command-line arguments are needed.

## Check Markdown

Run `npx -y markdownlint-cli2@<version>` to lint Markdown files according to the configuration.

## Fix issues

Run with the `--fix` flag to automatically fix supported issues:

```bash
npx -y markdownlint-cli2@<version> --fix
```

## Testing

Run the same lint command again to verify all issues are fixed. There should be no errors reported.
