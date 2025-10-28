# NuGet Package Setup - Quick Start Guide

This guide walks you through publishing your first NuGet package for BlazorFastMarquee.

## ✅ What's Been Configured

Your project is now fully configured for NuGet packaging with:

### 1. **Enhanced .csproj Configuration**
- ✅ Complete package metadata (title, description, tags, license)
- ✅ MinVer for automatic semantic versioning from Git tags
- ✅ Source Link for debugging support
- ✅ Symbol packages (.snupkg) for debugging
- ✅ Package validation to prevent breaking changes
- ✅ Reproducible builds for security
- ✅ AOT and trimming optimization
- ✅ Full .NET 9.0 analyzer support

### 2. **GitHub Actions CI/CD Workflow** (`.github/workflows/nuget.yml`)
- ✅ Automated build on push/PR
- ✅ Unit test execution with results reporting
- ✅ NuGet package creation with validation
- ✅ Automatic publishing to NuGet.org on Git tags
- ✅ Preview packages to GitHub Packages on main/develop
- ✅ GitHub Release creation with artifacts

### 3. **Supporting Files**
- ✅ `NuGet.config` - Package source configuration
- ✅ `.editorconfig` - Code style enforcement
- ✅ `.gitattributes` - Consistent line endings
- ✅ `build.sh` - Local build script
- ✅ `PACKAGING.md` - Comprehensive packaging documentation
- ✅ `.github/workflows/README.md` - Workflow documentation

## 🚀 Publishing Your First Package

### Step 1: Create NuGet Account & API Key

1. **Create account:** https://www.nuget.org/users/account/LogOn
2. **Create API key:** https://www.nuget.org/account/apikeys
   - Click "Create"
   - Key Name: `BlazorFastMarquee GitHub Actions`
   - Glob Pattern: `BlazorFastMarquee`
   - Select Scopes: ✅ Push new packages and package versions
   - Expiration: Choose your preference (365 days recommended)
   - Click "Create"
   - **Copy the API key** (shown only once!)

### Step 2: Add API Key to GitHub Secrets

1. Go to your repository on GitHub
2. Navigate to **Settings** → **Secrets and variables** → **Actions**
3. Click **"New repository secret"**
4. Name: `NUGET_API_KEY`
5. Secret: Paste your NuGet API key
6. Click **"Add secret"**

### Step 3: Create Your First Release

```bash
# Ensure you're on the main branch and up to date
git checkout main
git pull origin main

# Create and push a version tag (e.g., v1.0.0)
git tag v1.0.0 -m "Initial release v1.0.0"
git push origin v1.0.0
```

### Step 4: Monitor the Release

1. Go to **Actions** tab in your GitHub repository
2. Watch the "Build, Test & Publish NuGet Package" workflow
3. After success:
   - Check [NuGet.org](https://www.nuget.org/packages/BlazorFastMarquee) (may take a few minutes to appear)
   - Check **Releases** tab for the GitHub Release with artifacts

### Step 5: Enable Package Validation (After First Release)

After v1.0.0 is successfully published:

1. Edit `BlazorFastMarquee.csproj`
2. Uncomment these lines:
   ```xml
   <EnablePackageValidation>true</EnablePackageValidation>
   <PackageValidationBaselineVersion>1.0.0</PackageValidationBaselineVersion>
   ```
3. Commit and push:
   ```bash
   git add BlazorFastMarquee.csproj
   git commit -m "Enable package validation against v1.0.0 baseline"
   git push origin main
   ```

This enables validation to prevent accidental breaking changes in future releases. See `PACKAGE_VALIDATION.md` for details.

## 📦 How Versioning Works

BlazorFastMarquee uses **MinVer** for automatic versioning:

| Git State | Resulting Version |
|-----------|-------------------|
| Tag `v1.0.0` | `1.0.0` |
| 5 commits after `v1.0.0` | `1.0.0-preview.5+shaXXX` |
| No tags | `1.0.0-preview.0.X+shaXXX` |
| Tag `v2.1.0-beta.1` | `2.1.0-beta.1` |

**Version Format:** `MAJOR.MINOR.PATCH[-PRERELEASE][+BUILDMETADATA]`

## 🏷️ Version Tag Examples

```bash
# Stable releases
git tag v1.0.0 -m "Release v1.0.0"
git tag v1.1.0 -m "Release v1.1.0"
git tag v2.0.0 -m "Release v2.0.0"

# Pre-releases
git tag v1.0.0-alpha.1 -m "Alpha 1"
git tag v1.0.0-beta.1 -m "Beta 1"
git tag v1.0.0-rc.1 -m "Release Candidate 1"

# Push tags
git push origin v1.0.0
```

## 🔄 Development Workflow

### For Feature Development (No Publishing)
```bash
# Work on feature branches
git checkout -b feature/my-new-feature
# ... make changes ...
git commit -am "Add new feature"
git push origin feature/my-new-feature
# Create PR to main
```

**Result:** Workflow runs build + tests, but doesn't publish to NuGet.org

### For Releases (Publishing to NuGet.org)
```bash
# Merge feature to main
git checkout main
git merge feature/my-new-feature
git push origin main

# Create release tag
git tag v1.1.0 -m "Release v1.1.0"
git push origin v1.1.0
```

**Result:** Workflow builds, tests, validates, and publishes to NuGet.org + creates GitHub Release

### For Preview Builds (GitHub Packages)
```bash
# Push to main/master/develop without tags
git checkout develop
git commit -am "Preview changes"
git push origin develop
```

**Result:** Preview package published to GitHub Packages (not NuGet.org)

## 🛠️ Local Development

### Build Locally
```bash
# Make build script executable (first time only)
chmod +x build.sh

# Full build pipeline (clean, restore, build, test, pack)
./build.sh all

# Or individual steps
./build.sh clean
./build.sh build
./build.sh test
./build.sh pack
```

### Test Package Locally
```bash
# Create package
./build.sh pack

# Install in test project
cd samples/BlazorFastMarquee.Demo
dotnet add package BlazorFastMarquee --source ../../artifacts --prerelease

# Uninstall after testing
dotnet remove package BlazorFastMarquee
```

## 📋 Pre-Release Checklist

Before creating a release tag:

- [ ] All tests pass locally: `./build.sh test`
- [ ] No build warnings: `./build.sh build`
- [ ] README.md is up to date
- [ ] CHANGELOG.md is updated (if you have one)
- [ ] Demo app works correctly
- [ ] Version number follows SemVer
- [ ] Breaking changes documented (if any)

## 🔍 Troubleshooting

### "Package already exists" Error
NuGet.org doesn't allow overwriting versions. Delete and recreate tag:
```bash
git tag -d v1.0.0
git push origin :refs/tags/v1.0.0
git tag v1.0.1
git push origin v1.0.1
```

### Workflow Fails on "Publish to NuGet.org"
- Check `NUGET_API_KEY` secret is set correctly
- Verify API key hasn't expired on NuGet.org
- Ensure API key has "Push" permission

### Tests Fail in CI but Pass Locally
- Check .NET version matches (9.0)
- Verify all dependencies are restored
- Check GitHub Actions logs for specific error

### Version Not Incrementing
- Ensure you created and pushed a Git tag
- Check tag format: must be `v*.*.*` (e.g., `v1.0.0`)
- Verify fetch-depth: 0 in workflow (already configured)

## 📚 Additional Documentation

- **Detailed packaging guide:** See `PACKAGING.md`
- **Workflow documentation:** See `.github/workflows/README.md`
- **MinVer documentation:** https://github.com/adamralph/minver

## 🎯 Next Steps

1. ✅ Add NuGet API key to GitHub secrets (see Step 2 above)
2. ✅ Create your first release tag (see Step 3 above)
3. ✅ Watch the workflow run and package publish
4. ✅ Verify package appears on NuGet.org
5. ✅ Test installation: `dotnet add package BlazorFastMarquee`

## 💡 Tips

- **Always test locally first:** Run `./build.sh all` before tagging
- **Use semantic versioning:** MAJOR.MINOR.PATCH
- **Create GitHub Releases:** Auto-generated from tags, but can be edited
- **Monitor package health:** Check NuGet.org for download stats and issues
- **Update regularly:** Keep dependencies up to date

## 🎉 Success!

Once your package is published:
- View on NuGet.org: https://www.nuget.org/packages/BlazorFastMarquee
- Install with: `dotnet add package BlazorFastMarquee`
- Share on social media!
- Monitor GitHub Discussions for community feedback

---

**Questions or Issues?**
- Open an issue: https://github.com/Zettersten/BlazorFastMarquee/issues
- Check workflow logs in GitHub Actions
- Review PACKAGING.md for detailed information
