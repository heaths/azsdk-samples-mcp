---
name: check-spelling
description: Check and fix spelling in project source files using cSpell
compatibility: Requires Node.js with npm version 7.0 or newer.
---

# Spell checking

## Installation and usage

Run `npm install` from the repository root to install `cspell` from `devDependencies` in the root `package.json`. Then run `npx cspell <command>` to use the locally installed version.

## Configuration

The cSpell configuration is in `.cspell.json` at the repository root. When running any cspell command, pass `--config .cspell.json`.

The configuration has the following structure:

- `words`: array of accepted words applied repo-wide.
- `overrides`: array of per-glob overrides, each with a `filename` glob and its own `words` array. For example, `**/tsconfig.json` has its own word list.
- `ignorePaths`: array of paths excluded from spell checking.

## Check spelling

Run `npx cspell lint --config .cspell.json .` to check spelling across the repository.

## Fix spelling

Show a summary of the misspelling to the user. Prompt the user for which words should be replaced with another word.

Add remaining words to `.cspell.json`:

- If a word appears only in files matching an existing override `filename` glob, add it to that override's `words` array.
- Otherwise, add it to the top-level `words` array.

Seldom used words can be ignored within the file they are used by adding an appropriate comment e.g.:

```js
// cspell:ignore <word>
```

## Testing

Run the same command again used to check spelling. All misspellings should be fixed.
