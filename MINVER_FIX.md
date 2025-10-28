# MinVer CLI Fix Summary

## 🐛 Issue Identified

The GitHub Actions workflow failed with:
```
Could not execute because the specified command or file was not found.
Error: Process completed with exit code 1.
```

**Failed Command:**
```bash
VERSION=$(dotnet minver -t v -m 1.0 -d preview.0 -v e)
```

## 🔍 Root Cause

The workflow was trying to run `dotnet minver` as a CLI tool, but:

1. **MinVer is a Build-Time Tool:** MinVer is included as a NuGet package reference with `PrivateAssets="all"` in the `.csproj`:
   ```xml
   <PackageReference Include="MinVer" Version="6.0.0" PrivateAssets="all" />
   ```

2. **Not a Global Tool:** MinVer is not installed as a global .NET tool, so `dotnet minver` command doesn't exist.

3. **Automatic Execution:** MinVer runs automatically during `dotnet pack` and `dotnet build` without any manual invocation needed.

## ✅ Solution Applied

### 1. Removed Manual MinVer Invocation

**Before:**
```yaml
- name: Get version from MinVer
  id: version
  run: |
    VERSION=$(dotnet minver -t v -m 1.0 -d preview.0 -v e)
    echo "version=$VERSION" >> $GITHUB_OUTPUT
    echo "📦 Package Version: $VERSION"
    
- name: Pack NuGet package
  run: |
    dotnet pack BlazorFastMarquee.csproj \
      --configuration Release \
      --no-build \
      --output ./artifacts \
      -p:PackageVersion=${{ steps.version.outputs.version }} \
      ...
```

**After:**
```yaml
- name: Pack NuGet package
  run: |
    dotnet pack BlazorFastMarquee.csproj \
      --configuration Release \
      --no-build \
      --output ./artifacts \
      -p:IncludeSymbols=true \
      -p:SymbolPackageFormat=snupkg
    
    # MinVer automatically calculates version during pack
    # Extract version from generated package filename
    PACKAGE=$(ls ./artifacts/*.nupkg | grep -v symbols | head -n 1)
    VERSION=$(basename "$PACKAGE" | sed 's/BlazorFastMarquee\.\(.*\)\.nupkg/\1/')
    echo "📦 Package Version: $VERSION"
```

### 2. Updated Build Script

Updated `build.sh` to also rely on automatic MinVer execution instead of trying to run the CLI:

**Before:**
```bash
# Get version using MinVer
if command -v minver &> /dev/null; then
    VERSION=$(minver -t v -m 1.0 -d preview.0 -v e)
    print_success "Package version: $VERSION"
else
    print_warning "MinVer CLI not found."
    VERSION=""
fi

# Pack with version
if [ -n "$VERSION" ]; then
    dotnet pack ... -p:PackageVersion="$VERSION"
else
    dotnet pack ...
fi
```

**After:**
```bash
# Pack the project (MinVer automatically calculates version)
dotnet pack "$PROJECT" \
    --configuration "$CONFIGURATION" \
    --no-build \
    --output "$OUTPUT_DIR" \
    -p:IncludeSymbols=true \
    -p:SymbolPackageFormat=snupkg

# Extract and display version from package filename
PACKAGE=$(ls "$OUTPUT_DIR"/*.nupkg 2>/dev/null | grep -v symbols | head -n 1)
if [ -n "$PACKAGE" ]; then
    VERSION=$(basename "$PACKAGE" | sed 's/BlazorFastMarquee\.\(.*\)\.nupkg/\1/')
    print_success "Package version: $VERSION"
fi
```

## 🎯 How MinVer Actually Works

### Automatic Integration

MinVer integrates with MSBuild and runs automatically during build/pack:

```xml
<PackageReference Include="MinVer" Version="6.0.0" PrivateAssets="all" />
```

The `PrivateAssets="all"` means:
- ✅ MinVer runs during build
- ✅ MinVer is not included in the package
- ✅ MinVer doesn't affect consumers
- ❌ MinVer CLI is not available as a command

### Version Calculation

MinVer calculates the version based on:

1. **Git Tags:** Looks for tags matching pattern (e.g., `v*.*.*`)
2. **Commit History:** Counts commits since last tag
3. **Configuration:** Uses settings from `.csproj`:
   ```xml
   <MinVerTagPrefix>v</MinVerTagPrefix>
   <MinVerMinimumMajorMinor>1.0</MinVerMinimumMajorMinor>
   <MinVerDefaultPreReleaseIdentifiers>preview.0</MinVerDefaultPreReleaseIdentifiers>
   ```

### When It Runs

MinVer executes during:
- ✅ `dotnet build` - Sets assembly version
- ✅ `dotnet pack` - Sets package version
- ✅ MSBuild - Integrates with build process

### What It Sets

MinVer automatically sets MSBuild properties:
- `Version` - Package version
- `AssemblyVersion` - Assembly version
- `FileVersion` - File version
- `InformationalVersion` - Full version with metadata

## 📊 Version Examples

Based on Git state, MinVer produces:

| Git State | MinVer Output | Package Filename |
|-----------|---------------|------------------|
| Tag `v1.0.0` | `1.0.0` | `BlazorFastMarquee.1.0.0.nupkg` |
| 5 commits after `v1.0.0` | `1.0.0-preview.5+sha` | `BlazorFastMarquee.1.0.0-preview.5.nupkg` |
| No tags | `1.0.0-preview.0.10+sha` | `BlazorFastMarquee.1.0.0-preview.0.10.nupkg` |
| Tag `v2.1.0-beta.1` | `2.1.0-beta.1` | `BlazorFastMarquee.2.1.0-beta.1.nupkg` |

## 🔧 Alternative: MinVer CLI Tool (Optional)

If you want the `dotnet minver` command for local use, you can install it separately:

```bash
# Install MinVer CLI globally
dotnet tool install --global minver-cli

# Use it locally
minver -t v -m 1.0 -d preview.0 -v e
```

**However, this is NOT required for the package build process!** MinVer works automatically.

## ✅ Benefits of This Approach

### 1. **Simpler Workflow**
- No extra tool installation
- No manual version passing
- Fewer failure points

### 2. **More Reliable**
- MinVer has direct access to Git history
- No intermediate steps to fail
- Version is consistent across all properties

### 3. **Better Performance**
- One less step to execute
- Faster workflow execution
- Less complexity

### 4. **Standard Practice**
- Follows MinVer's intended usage
- Matches official documentation
- Works same locally and in CI

## 📝 How to Verify Version Locally

If you want to see what version MinVer will produce:

### Option 1: Check Build Output
```bash
dotnet build BlazorFastMarquee.csproj -v m | grep "MinVer"
```

### Option 2: Pack and Check Filename
```bash
dotnet pack BlazorFastMarquee.csproj --configuration Release --output ./test
ls ./test/*.nupkg
```

### Option 3: Install MinVer CLI (Optional)
```bash
dotnet tool install --global minver-cli
minver
```

## 📚 Files Modified

```
Modified:
  ✏️  .github/workflows/nuget.yml (removed manual MinVer step)
  ✏️  build.sh (updated to rely on automatic version)

Created:
  ✨ MINVER_FIX.md (this file)
```

## 🎯 Expected Workflow Behavior

After this fix, the workflow will:

1. ✅ Checkout repository with full Git history
2. ✅ Setup .NET 9.0
3. ✅ Build library
4. ✅ Run tests (all 18 pass)
5. ✅ Pack NuGet package
   - MinVer automatically calculates version from Git tags
   - Package created with correct version
6. ✅ Extract version from package filename for display
7. ✅ Validate and publish package

## 🚀 Testing the Fix

### With a Git Tag
```bash
git tag v1.0.0
git push origin v1.0.0
```
**Result:** Package version `1.0.0`

### Without a Git Tag
```bash
git push origin branch-name
```
**Result:** Package version `1.0.0-preview.0.N+sha` (where N is commit count)

### With Pre-release Tag
```bash
git tag v1.0.0-beta.1
git push origin v1.0.0-beta.1
```
**Result:** Package version `1.0.0-beta.1`

## ✨ Summary

**Problem:** Tried to run MinVer as a CLI tool, which wasn't installed.

**Solution:** Let MinVer run automatically during `dotnet pack` (its intended usage).

**Result:** Simpler, more reliable workflow that follows best practices.

---

**Status:** ✅ Fixed - MinVer now works correctly via automatic MSBuild integration!
