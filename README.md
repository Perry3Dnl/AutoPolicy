# AutoPolicy

Automatic, default-deny route permissions for ASP.NET Core Razor Pages.

> **Development status:** AutoPolicy is under active development toward `0.1.0`. No `0.1.0` release has been published yet.

AutoPolicy maps Razor Pages to canonical permission identities and evaluates access through the standard ASP.NET Core authorization pipeline. The current implementation supports permission groups, roles, direct grants and denies, aliases, anonymous route patterns, and deny-wins evaluation.

## Repository layout

```text
src/AutoPolicy/                    Package source
tests/AutoPolicy.Tests/            Automated tests
smoke/AutoPolicy.ConsumerSmoke/    Packaged-consumer smoke test
scripts/                           Package validation scripts
AutoPolicy.slnx                    Main development solution
```

## Development

Open `AutoPolicy.slnx` in Visual Studio or another .NET-compatible IDE.

Run the test suite:

```bash
dotnet test AutoPolicy.slnx --configuration Release
```

Build a NuGet package locally:

```bash
dotnet pack src/AutoPolicy/AutoPolicy.csproj --configuration Release --output artifacts
```

Validate the generated package:

```powershell
./scripts/Validate-Packages.ps1 -ArtifactsPath ./artifacts
```

The version under development is defined centrally in `Directory.Build.props`.

## Releases

Publishing is intentionally tag-gated. Normal pushes and pull requests build, test, pack, and validate the project but do not publish it. A NuGet release can only run from a matching `v*` tag through the protected GitHub `release` environment.

## License

AutoPolicy is licensed under the Mozilla Public License 2.0 (`MPL-2.0`).
