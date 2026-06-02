# Contributing to Symphonia Legato

Thank you for your interest in contributing!

## How to Contribute

1. **Fork** the repository.
2. Create a **feature branch**: `git checkout -b feature/my-feature`
3. Make your changes, following the code style below.
4. **Write tests** for new functionality.
5. Ensure `dotnet test` passes.
6. Submit a **pull request** with a clear description.

## Code Style

- C# 13 with top-level statements and records where appropriate.
- Nullable reference types enabled — no `!` suppressions without comments.
- No comments that restate what the code does. Only comment the *why*.
- MVVM strictly: ViewModels must not reference Avalonia UI types.
- Use `CommunityToolkit.Mvvm` source generators; avoid manual `INotifyPropertyChanged`.

## Commit Messages

Follow Conventional Commits:

```
feat: add slur rendering
fix: correct ledger line count for bass clef
test: add StaffPositionCalculator edge cases
docs: update plugin SDK guide
```

## Issue Reporting

Open a GitHub Issue with:
- OS and .NET version
- Steps to reproduce
- Expected vs actual behaviour
- A sample `.enscore` file if relevant

## Code of Conduct

See [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md).
