# Credits

## Inspiration

Symphonia Legato's UI approach is inspired by **Encore**, the classic music
notation application — no code, assets, or organizational affiliation, just a
shared design sensibility (see `docs/FAQ.md`).

## Open-source dependencies

Symphonia Legato is built on the following open-source projects. Full list
with versions and purpose: `docs/DEPENDENCIES.md`.

- **[Avalonia](https://avaloniaui.net/)** — the cross-platform .NET UI
  framework this entire app's interface is built on.
- **[DryWetMidi](https://github.com/melanchall/drywetmidi)** — MIDI file
  reading, writing, and playback.
- **[QuestPDF](https://www.questpdf.com/)** — PDF generation for score export.
- **[LibGit2Sharp](https://github.com/libgit2/libgit2sharp)** — per-score
  version history.
- **[CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)** —
  MVVM source generators (`[ObservableProperty]`, `[RelayCommand]`).
- **[xUnit](https://xunit.net/)**, **[FluentAssertions](https://fluentassertions.com/)**,
  **[Moq](https://github.com/devlooped/moq)** — the test stack.

## AI assistance

Optional AI features (harmonisation, score analysis, practice
recommendations) are powered by **Anthropic's Claude API**, used only when
the end user supplies their own API key — see `docs/AUTHENTICATION.md`.

See also `AUTHORS.md` for project authorship.
