# Package Validation Guide

## Initial Setup Note

**IMPORTANT:** Package validation is currently **disabled** until the first version is published to NuGet.org.

The following properties are commented out in `BlazorFastMarquee.csproj`:

```xml
<!-- Enable after first version is published to NuGet.org -->
<!-- <EnablePackageValidation>true</EnablePackageValidation> -->
<!-- <PackageValidationBaselineVersion>1.0.0</PackageValidationBaselineVersion> -->
```

## Why Is It Disabled?

Package validation compares the current build against a baseline version from NuGet.org to detect:
- Breaking API changes
- Missing types or members
- Signature changes
- Assembly compatibility issues

Since the package hasn't been published yet, there's no baseline to compare against, which causes the build to fail with:

```
error NU1101: Unable to find package BlazorFastMarquee. No packages exist with this id in source(s): nuget.org
```

## Enabling Package Validation

After your first successful publish to NuGet.org (e.g., version `1.0.0`):

### 1. Update BlazorFastMarquee.csproj

Uncomment the package validation settings:

```xml
<!-- NuGet Package Validation -->
<EnablePackageValidation>true</EnablePackageValidation>
<PackageValidationBaselineVersion>1.0.0</PackageValidationBaselineVersion>
```

### 2. Update Baseline Version as You Release

When you publish breaking changes (e.g., v2.0.0), update the baseline:

```xml
<PackageValidationBaselineVersion>2.0.0</PackageValidationBaselineVersion>
```

### 3. Commit and Push

```bash
git add BlazorFastMarquee.csproj
git commit -m "Enable package validation against baseline v1.0.0"
git push origin main
```

## What Package Validation Does

Once enabled, package validation will:

✅ **Detect Breaking Changes**
- Removed public APIs
- Changed method signatures
- Modified return types
- Incompatible interface changes

✅ **Ensure Compatibility**
- Assembly version compatibility
- Target framework alignment
- Dependency consistency

✅ **Prevent Accidental Breaking Changes**
- Alerts you before publishing breaking changes
- Helps maintain semantic versioning discipline

## Example Validation Errors

### Breaking Change Detected
```
error CP0002: Member 'BlazorFastMarquee.Marquee.OldProperty' exists on the baseline assembly but not on the current assembly
```

**Solution:** Either restore the member or increment major version (e.g., 1.0.0 → 2.0.0)

### Type Removed
```
error CP0001: Type 'BlazorFastMarquee.OldType' exists on the baseline assembly but not on the current assembly
```

**Solution:** This is a breaking change. Increment major version.

## Validation Strategy

### For Major Versions (Breaking Changes Allowed)
```xml
<!-- Update baseline to current major version -->
<PackageValidationBaselineVersion>2.0.0</PackageValidationBaselineVersion>
```

### For Minor/Patch Versions (No Breaking Changes)
```xml
<!-- Keep baseline at major version start -->
<PackageValidationBaselineVersion>2.0.0</PackageValidationBaselineVersion>
```

This ensures all v2.x.x releases are compatible with v2.0.0.

## Bypassing Validation

If you need to temporarily disable validation:

```bash
# Build without validation
dotnet build -p:EnablePackageValidation=false

# Pack without validation
dotnet pack -p:EnablePackageValidation=false
```

## Best Practices

1. **Enable After First Release**
   - Publish v1.0.0 first
   - Then enable validation
   - Set baseline to 1.0.0

2. **Update Baseline on Major Versions**
   - Major version = breaking changes allowed
   - Update baseline to new major version
   - Example: v2.0.0 releases → set baseline to 2.0.0

3. **Keep Baseline on Minor/Patch Versions**
   - Minor/patch = no breaking changes
   - Keep baseline at major version start
   - Example: v2.1.0, v2.1.1, v2.2.0 → baseline stays 2.0.0

4. **Test Before Publishing**
   - Run `dotnet pack` locally
   - Validation runs during pack
   - Fix any breaking changes before tagging

## Timeline

### Current State (Pre-1.0.0)
- ❌ Package validation: **DISABLED**
- ✅ Build: Works
- ✅ Tests: Run
- ✅ Pack: Creates package
- ⏳ Ready to publish v1.0.0

### After First Publish (v1.0.0)
- ✅ Package published to NuGet.org
- 🔧 Enable package validation
- 🔧 Set baseline to 1.0.0
- ✅ Future builds validated against v1.0.0

### Ongoing (v1.1.0, v1.2.0, etc.)
- ✅ Validation active
- ✅ Baseline remains 1.0.0
- ✅ No breaking changes allowed
- ✅ Build fails if API breaks

### Next Major Version (v2.0.0)
- ✅ Breaking changes allowed
- 🔧 Update baseline to 2.0.0
- ✅ Future v2.x.x validated against v2.0.0

## Documentation Links

- [NuGet Package Validation](https://learn.microsoft.com/en-us/nuget/reference/package-validation/overview)
- [Detecting Breaking Changes](https://learn.microsoft.com/en-us/nuget/reference/package-validation/breaking-changes)
- [Baseline Validation](https://learn.microsoft.com/en-us/nuget/reference/package-validation/baseline-validation)

## Quick Reference

| Version | Validation | Baseline | Breaking Changes |
|---------|-----------|----------|------------------|
| < 1.0.0 | Disabled | None | N/A |
| 1.0.0 | Enable | 1.0.0 | Allowed (first) |
| 1.x.x | Enabled | 1.0.0 | Not allowed |
| 2.0.0 | Enabled | 2.0.0 | Allowed (major) |
| 2.x.x | Enabled | 2.0.0 | Not allowed |

---

**Next Steps:**
1. ✅ Publish v1.0.0 (validation disabled)
2. ⏳ Enable validation in BlazorFastMarquee.csproj
3. ⏳ Set baseline to 1.0.0
4. ✅ All future releases validated!
