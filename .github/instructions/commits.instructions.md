# Commit Message Guidelines

Write commit messages following the standard git commit convention with a short title and descriptive body.

## Format

```text
<short title describing the change>

<more detailed description explaining why the change was made,
what problem it solves, and any relevant context>
```

## Title Guidelines

- Use imperative mood (e.g., "Add", "Update", "Fix", "Implement")
- Keep to 50 characters or less
- Do not end with a period
- Examples:
  - "Add XML doc comments to Providers"
  - "Update prompt names to use underscores"
  - "Implement Azure SDK sample discovery"
  - "Rename linting approach to use skills"

## Description Guidelines

- Explain **why** the change was made, not just what changed
- Describe the problem being solved
- Include relevant context or motivation
- Separate the title from body with a blank line
- Can reference issues if applicable (e.g., "Fixes #123")

## Pull Requests

- PR title should follow the same short imperative format
- PR description should provide an overview of changes
- PR description should not include plan details or tasks
- Focus on the big picture—don't describe every file changed
- Link to any related issues
