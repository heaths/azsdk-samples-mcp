---
name: cspell
description: Check and fix spelling in project source files using cSpell.
compatibility: Requires Node.js with npm version 7.0 or newer.
allowed-tools: bash(npx:*)
---

# Spell checking

## Installation and usage

Run `npx -y cspell@<version> <command>` from the repository root.

If a `package.json` exists in the repository root with `cspell` listed in `devDependencies`, use that version. Otherwise, use the latest version (omit `@<version>`).

## Configuration

cSpell configuration including custom dictionaries and word lists can be found in files:

- `cspell.json`
- `cspell.config.json`
- `cspell.yaml` or `cspell.yml`
- `cspell.config.yaml` or `cspell.config.yml`

They may be prefaced with an optional `.` or in a `.config/` directory e.g.:

- `.cspell.json`
- `.config/cspell.yaml`
- `.config/.cspell.yml`

They may also be located in a `.vscode/` directory e.g.:

- `.vscode/cspell.json`

If a suitable configuration is not found, create a `.cspell.json` file in the root of the repository.
The configuration file schema is described at `https://raw.githubusercontent.com/streetsidesoftware/cspell/v<version>/cspell.schema.json` where `<version>` should match the version from `package.json` devDependencies (e.g., `v9.6.2`). If no `package.json` exists, use `https://raw.githubusercontent.com/streetsidesoftware/cspell/main/cspell.schema.json`.
Always include the schema URI in the `$schema` field and set the current project language e.g., "en".
Inside a git repository you should also set `useGitignore` to `true` in the configuration file.

When running any cspell command, pass `--config <path>` with the path to the configuration file.

## Check spelling

Run `npx -y cspell@<version> lint [options] [globs...]` to check a list of file globs or `.` for the directory tree.

## Fix spelling

Show a summary of the misspelling to the user. Prompt the user for which words should be replaced with another word. All other words should be added to the `words` array of the configuration file according to the schema.

If any misspellings are only in files that match any `filename` globs in an optional `overrides` section, add those words to the `words` array  in that override according to the schema.

Seldom used words can be ignored within the file they are used by adding a appropriate comment e.g.:

```js
// cspell:ignore <word>
```

## Testing

Run the same command again used to check spelling. All misspellings should be fixed.
