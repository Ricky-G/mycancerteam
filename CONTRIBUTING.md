# Contributing to MyCancerTeam

Thank you for your interest in contributing. This document covers the principles and practices that keep the codebase healthy and approachable.

## Guiding philosophy

**Keep It Simple, Stupid (KISS).** Every design choice, abstraction, and PR should justify its complexity. If a simpler approach works, use it.

## Quick checklist

Before opening a PR:

1. Code compiles: `dotnet build MyCancerTeam.slnx`
2. Tests pass: `dotnet test MyCancerTeam.slnx`
3. New behaviour has tests.
4. No secrets, credentials, or patient data in committed files.
5. Follows the coding standards in [`docs/coding-standards.md`](docs/coding-standards.md).

## Branching and commits

- Branch from `main`.
- Use descriptive branch names: `feature/`, `fix/`, `docs/`, `refactor/`.
- Write clear, concise commit messages in imperative mood ("Add X", not "Added X").
- Keep commits focused — one logical change per commit.

## Pull requests

- Describe *what* changed and *why*.
- Keep PRs small and reviewable. Break large changes into a series of smaller PRs.
- Link related issues if they exist.

## Architecture overview

The solution follows a **layered architecture** with strict dependency direction:

```
MyCancerTeam.App          → thin host / entry point
    ↓ references
MyCancerTeam.Infrastructure  → implementations, external services, I/O
    ↓ references
MyCancerTeam.Core            → domain models, abstractions, interfaces (no external dependencies)
```

- **Core** knows nothing about Infrastructure or App.
- **Infrastructure** implements Core interfaces but never references App.
- **App** wires everything together — dependency injection, configuration, and the interactive loop.

See [`docs/coding-standards.md`](docs/coding-standards.md) for the full set of coding and architecture rules.

## Safety and clinical content

This project supports informed patient conversations — it does **not** replace clinicians. All contributions must respect the safety boundaries documented in the README.

## Questions?

Open an issue or start a discussion. We're happy to help.
