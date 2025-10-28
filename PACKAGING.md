# NuGet Packaging Guide

This document describes the NuGet packaging configuration and release process for BlazorFastMarquee.

## Package Configuration

The package is configured with production-ready settings in `BlazorFastMarquee.csproj`:

### Core Metadata
- **Package ID:** `BlazorFastMarquee`
- **License:** MIT
- **Target Framework:** .NET 9.0
- **Authors:** Zettersten
- **Repository:** https://github.com/Zettersten/BlazorFastMarquee

### Optimization Features

#### Trimming & AOT
```xml
<EnableTrimAnalyzer>true</EnableTrimAnalyzer>
<IsTrimmable>true</IsTrimmable>
<TrimMode>link</TrimMode>
<RunAOTCompilation>true</RunAOTCompilation>
```
The component is fully trimming-safe and AOT-compatible for minimal bundle sizes.

#### Reproducible Builds
```xml
<Deterministic>true</Deterministic>
<ContinuousIntegrationBuild Condition="'$(CI)' == 'true'">true</ContinuousIntegrationBuild>
```
Ensures byte-for-byte identical builds from the same source code.

#### Source Link
```xml
<PublishRepositoryUrl>true</PublishRepositoryUrl>
<EmbedUntrackedSources>true</EmbedUntrackedSources>
<IncludeSymbols>true</IncludeSymbols>
<SymbolPackageFormat>snupkg</SymbolPackageFormat>
```
Enables debugging into the library source code directly from NuGet packages.

#### Package Validation
```xml
<!-- Disabled until first version is published -->
<!-- <EnablePackageValidation>true</EnablePackageValidation> -->
<!-- <PackageValidationBaselineVersion>1.0.0</PackageValidationBaselineVersion> -->
```
Validates API compatibility across versions to prevent breaking changes. 

**Note:** This is currently disabled and should be enabled after the first successful publish to NuGet.org. See `PACKAGE_VALIDATION.md` for details.

## Versioning with MinVer

This project uses [MinVer](https://github.com/adamralph/minver) for automatic semantic versioning.

### How It Works

MinVer calculates the version from Git tags and commit history:

1. **On a tagged commit:** Version = tag (e.g., `v1.2.3` → `1.2.3`)
2. **After a tag:** Adds preview identifier with commit height
3. **No tags:** Uses minimum version + preview

### Configuration
```xml
<MinVerTagPrefix>v</MinVerTagPrefix>
<MinVerMinimumMajorMinor>1.0</MinVerMinimumMajorMinor>
<MinVerDefaultPreReleaseIdentifiers>preview.0</MinVerDefaultPreReleaseIdentifiers>
```

### Version Examples

```bash
# First commit, no tags
1.0.0-preview.0.1+abc1234

# Tag v1.0.0
1.0.0

# 3 commits after v1.0.0
1.0.0-preview.3+def5678

# Tag v1.1.0-beta.1
1.1.0-beta.1

# 2 commits after beta tag
1.1.0-beta.1.2+ghi9012
```

## Release Process

### 1. Prepare Release

```bash
# Ensure main branch is up to date
git checkout main
git pull origin main

# Run tests locally (optional but recommended)
dotnet test --configuration Release

# Verify current version
dotnet tool install --global minver-cli
minver -t v -m 1.0 -d preview.0 -v e
```

### 2. Create Release Tag

```bash
# For a production release (e.g., 1.2.3)
git tag v1.2.3 -m "Release v1.2.3"
git push origin v1.2.3

# For a pre-release (e.g., 1.2.3-beta.1)
git tag v1.2.3-beta.1 -m "Release v1.2.3-beta.1"
git push origin v1.2.3-beta.1
```

### 3. Automated CI/CD

GitHub Actions automatically:
1. ✅ Builds the project in Release mode
2. ✅ Runs all unit tests
3. ✅ Calculates version from Git tag using MinVer
4. ✅ Packs NuGet package (.nupkg) and symbol package (.snupkg)
5. ✅ Validates package structure and content
6. ✅ Publishes to NuGet.org
7. ✅ Creates GitHub Release with artifacts

### 4. Verify Publication

1. Check [NuGet.org](https://www.nuget.org/packages/BlazorFastMarquee)
2. Verify package appears in search results (may take a few minutes)
3. Check GitHub Releases page for artifacts

## Local Development

### Build Locally

```bash
# Restore dependencies
dotnet restore BlazorFastMarquee.csproj

# Build Debug
dotnet build BlazorFastMarquee.csproj --configuration Debug

# Build Release
dotnet build BlazorFastMarquee.csproj --configuration Release
```

### Create Package Locally

```bash
# Create package
dotnet pack BlazorFastMarquee.csproj \
  --configuration Release \
  --output ./artifacts

# Inspect package contents
unzip -l ./artifacts/BlazorFastMarquee.*.nupkg
```

### Test Package Locally

```bash
# Add local package source
dotnet nuget add source ./artifacts --name local

# Install in another project
dotnet add package BlazorFastMarquee --prerelease --source local

# Remove local source (cleanup)
dotnet nuget remove source local
```

### Validate Package Quality

```bash
# Install validation tool
dotnet tool install --global dotnet-validate --version 0.0.1-preview.304

# Validate package
dotnet-validate package local ./artifacts/BlazorFastMarquee.*.nupkg
```

## Package Contents

The NuGet package includes:

- ✅ **Razor Components** (`Components/*.razor`, `Components/*.razor.cs`)
- ✅ **Razor Class Library CSS** (`Components/*.razor.css`)
- ✅ **JavaScript Interop** (`wwwroot/js/marquee.js`)
- ✅ **Type Definitions** (`*.cs` enums and helpers)
- ✅ **XML Documentation** (for IntelliSense)
- ✅ **Symbol Package** (.snupkg for debugging)
- ✅ **README.md** (displayed on NuGet.org)
- ✅ **Icon** (`icon.png`, if present)

## Troubleshooting

### Version Not Incrementing
Ensure you've created and pushed a Git tag:
```bash
git tag v1.0.1
git push origin v1.0.1
```

### Package Already Exists Error
NuGet.org doesn't allow overwriting versions. Delete the tag, increment, and retry:
```bash
git tag -d v1.0.1
git push origin :refs/tags/v1.0.1
git tag v1.0.2
git push origin v1.0.2
```

### Build Fails in CI
Check the GitHub Actions logs for specific errors. Common issues:
- Missing dependencies
- Test failures
- Version conflicts

### Symbol Package Not Publishing
Ensure symbol publishing is enabled on NuGet.org:
1. Go to your NuGet.org account settings
2. Navigate to API Keys
3. Verify "Push Symbols" permission is enabled

## Best Practices

### Semantic Versioning

Follow [SemVer 2.0.0](https://semver.org/):

- **MAJOR** version: Breaking changes
- **MINOR** version: New features (backward compatible)
- **PATCH** version: Bug fixes (backward compatible)

Examples:
- `v1.0.0` → `v2.0.0`: Breaking API changes
- `v1.0.0` → `v1.1.0`: New optional parameters
- `v1.0.0` → `v1.0.1`: Bug fix

### Pre-release Versions

Use pre-release identifiers for testing:
- `v1.0.0-alpha.1`: Early testing
- `v1.0.0-beta.1`: Feature complete, testing
- `v1.0.0-rc.1`: Release candidate

### Changelog

Maintain a CHANGELOG.md following [Keep a Changelog](https://keepachangelog.com/):
```markdown
## [1.0.1] - 2025-10-28
### Fixed
- Fixed animation pause on hover

## [1.0.0] - 2025-10-28
### Added
- Initial release
```

### Package Quality

Before releasing:
1. ✅ All tests pass
2. ✅ No compiler warnings
3. ✅ Documentation is up to date
4. ✅ CHANGELOG is updated
5. ✅ README examples work
6. ✅ Demo app builds and runs

## Required Secrets

### NuGet API Key

Create at: https://www.nuget.org/account/apikeys

**Permissions:**
- Push new packages and package versions
- Push symbols (optional but recommended)

**Scope:**
- Globe pattern: `BlazorFastMarquee`
- Expiration: Set based on security requirements

**Add to GitHub:**
1. Repository → Settings → Secrets and variables → Actions
2. New repository secret
3. Name: `NUGET_API_KEY`
4. Value: Your API key

## Monitoring

### Package Health

Check NuGet.org package health:
- Download statistics
- Dependent packages
- Package validation results
- Security vulnerabilities (Dependabot)

### CI/CD Status

Monitor GitHub Actions:
- Build success rate
- Test pass rate
- Deployment frequency
- Failed pipeline alerts

## Support

For issues or questions:
- GitHub Issues: https://github.com/Zettersten/BlazorFastMarquee/issues
- GitHub Discussions: https://github.com/Zettersten/BlazorFastMarquee/discussions
