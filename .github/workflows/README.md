# GitHub Actions Workflows

This repository uses GitHub Actions for continuous integration and deployment.

## Workflows

### 1. `nuget.yml` - Build, Test & Publish NuGet Package

**Triggers:**
- Push to `main`, `master`, or `develop` branches
- Pull requests to these branches
- Git tags matching `v*.*.*` pattern
- Manual workflow dispatch

**Jobs:**

#### Build & Test
- Builds the library in both Debug and Release configurations
- Runs all unit tests with BUnit
- Uploads test results as artifacts
- Fails fast on test failures

#### Pack NuGet Package
- Uses MinVer for automatic semantic versioning from Git tags
- Creates NuGet package (.nupkg) and symbol package (.snupkg)
- Validates package contents
- Uploads packages as artifacts

#### Publish to NuGet.org (Production)
- **Trigger:** Only on Git tags (e.g., `v1.2.3`)
- Requires `NUGET_API_KEY` secret
- Publishes to NuGet.org
- Creates GitHub Release with package files

#### Publish Preview to GitHub Packages
- **Trigger:** Push to main/master/develop branches
- Publishes preview versions to GitHub Packages
- Uses GITHUB_TOKEN for authentication

### 2. `deploy-demo.yml` - Deploy Demo to GitHub Pages

Deploys the Blazor WASM demo application to GitHub Pages.

## Versioning Strategy

This project uses **MinVer** for automatic semantic versioning based on Git tags.

### Version Calculation

- **Tagged commits:** Use the tag as version (e.g., `v1.2.3` → `1.2.3`)
- **Commits after tag:** Add height and commit SHA (e.g., `1.2.3-preview.5+abc1234`)
- **No tags:** Uses `MinVerMinimumMajorMinor` + preview identifier (e.g., `1.0.0-preview.0.5+abc1234`)

### Creating a Release

1. **Create and push a Git tag:**
   ```bash
   git tag v1.2.3
   git push origin v1.2.3
   ```

2. **GitHub Actions automatically:**
   - Builds and tests the package
   - Creates NuGet package with version `1.2.3`
   - Publishes to NuGet.org
   - Creates GitHub Release

### Version Examples

| Git State | Version Output |
|-----------|---------------|
| `v1.0.0` tag | `1.0.0` |
| 5 commits after `v1.0.0` | `1.0.0-preview.5+sha` |
| No tags, 10 commits | `1.0.0-preview.0.10+sha` |
| `v2.1.0-beta.1` tag | `2.1.0-beta.1` |

## Required Secrets

### `NUGET_API_KEY`
Create a NuGet API key at https://www.nuget.org/account/apikeys

**Setup:**
1. Go to repository Settings → Secrets and variables → Actions
2. Click "New repository secret"
3. Name: `NUGET_API_KEY`
4. Value: Your NuGet API key
5. Save

### `GITHUB_TOKEN`
Automatically provided by GitHub Actions. No setup required.

## Environment Protection (Optional)

For added safety, configure environment protection rules:

1. Go to Settings → Environments
2. Create environment: `nuget-production`
3. Add protection rules:
   - Required reviewers
   - Wait timer
   - Deployment branches: Only tags matching `v*.*.*`

## Local Development

### Build Package Locally

```bash
# Restore and build
dotnet restore
dotnet build --configuration Release

# Run tests
dotnet test --configuration Release

# Create package
dotnet pack --configuration Release --output ./artifacts
```

### Preview Version Locally

```bash
# Install MinVer CLI tool
dotnet tool install --global minver-cli

# Check current version
minver -t v -m 1.0 -d preview.0 -v e
```

### Test Package Locally

```bash
# Pack the library
dotnet pack --configuration Release --output ./artifacts

# Reference in test project
dotnet add samples/BlazorFastMarquee.Demo/BlazorFastMarquee.Demo.csproj \
  package BlazorFastMarquee \
  --source ./artifacts \
  --prerelease
```

## Package Validation

The workflow validates packages using:
- **EnablePackageValidation:** Ensures API compatibility with baseline version
- **dotnet-validate:** Validates package structure and content
- **Manual inspection:** Lists package contents in workflow logs

## Troubleshooting

### Build Fails on `MinVer`
Ensure you have at least one commit in your repository. MinVer requires Git history.

### Tests Fail
Run tests locally first:
```bash
dotnet test --configuration Release --verbosity detailed
```

### Package Already Exists
NuGet.org doesn't allow republishing the same version. Increment your version tag.

### GitHub Packages Authentication
For consuming packages from GitHub Packages, add authentication:
```bash
dotnet nuget add source \
  --username YOUR_GITHUB_USERNAME \
  --password YOUR_GITHUB_TOKEN \
  --store-password-in-clear-text \
  --name github \
  "https://nuget.pkg.github.com/OWNER/index.json"
```

## Best Practices

1. **Always create tags for releases:** Don't push directly to NuGet without a tag
2. **Test before tagging:** Ensure all tests pass locally
3. **Use semantic versioning:** Follow [SemVer](https://semver.org/) guidelines
4. **Write release notes:** GitHub Releases are auto-generated but can be edited
5. **Monitor package quality:** Check NuGet.org package health after publishing
