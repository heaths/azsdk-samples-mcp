---
name: markdownlint
description: Check and fix formatting and other issues in markdown files using markdownlint-cli2.
compatibility: Requires Node.js with npm version 7.0 or newer.
---

# Markdown linting

Check markdown files for common mistakes.

## Installation and usage

If a `package.json` exists in the repository root with `markdownlint-cli2` listed in `devDependencies`, run `npm install --dev` from the repository root first. Then run `npx markdownlint-cli2 <command>`.

## Configuration

markdownlint-cli2 configuration including custom rules can be found in files:

- `.markdownlint-cli2.json`
- `.markdownlint-cli2.yaml`
- `.markdownlint-cli2.yml`

If a suitable configuration is not found, create a `.markdownlint-cli2.yaml` file in the root of the repository.
The configuration file schema is described at `https://raw.githubusercontent.com/DavidAnson/markdownlint-cli2/v<version>/schema/markdownlint-cli2-config-schema.json` where `<version>` should match the `markdownlint-cli2` version from the repository root `package.json` `devDependencies` (e.g., `v0.20.0`). If no suitable `markdownlint-cli2` entry is found in the repository root `package.json`, use `https://raw.githubusercontent.com/DavidAnson/markdownlint-cli2/main/schema/markdownlint-cli2-config-schema.json`.
Always include the schema URI in the `$schema` field.

For markdownlint rules configuration, nest it under the `config` property following the markdownlint schema at `https://raw.githubusercontent.com/DavidAnson/markdownlint/main/schema/markdownlint-config-schema.json`.

If not already present, add a `globs` array with file patterns to lint (e.g., `["**/*.md"]`) so no command-line arguments are needed.

## Check Markdown

Run `npx markdownlint-cli2` to lint Markdown files according to the configuration.

## Fix issues

Run with the `--fix` flag to automatically fix supported issues:

```bash
npx markdownlint-cli2 --fix
```

## Testing

Run the same lint command again to verify all issues are fixed. There should be no errors reported.
