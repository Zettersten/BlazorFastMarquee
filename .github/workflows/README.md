# GitHub Actions Workflows

## Deploy Demo to GitHub Pages

### Overview
This workflow automatically builds and deploys the BlazorFastMarquee demo application to GitHub Pages whenever changes are pushed to the main branch.

### Workflow: `deploy-demo.yml`

#### Triggers
- **Push to main/master**: Automatically deploys when changes are pushed to the main or master branch
- **Path filters**: Only triggers when relevant files change:
  - Demo project files (`samples/BlazorFastMarquee.Demo/**`)
  - Component files (`Components/**`)
  - Static assets (`wwwroot/**`)
  - C# and Razor files
  - Project files
  - The workflow file itself
- **Manual trigger**: Can be manually triggered from the Actions tab (`workflow_dispatch`)

#### What It Does

1. **Checkout**: Clones the repository
2. **Setup .NET**: Installs .NET 9.0 SDK
3. **Restore**: Restores NuGet packages
4. **Build Library**: Builds the BlazorFastMarquee component library
5. **Publish WASM**: Publishes the demo as a WebAssembly application
6. **Update Base Path**: Automatically adjusts the base href for GitHub Pages subdirectory hosting
7. **Add .nojekyll**: Ensures GitHub Pages doesn't process files with Jekyll
8. **Upload Artifact**: Prepares the build output for deployment
9. **Deploy**: Deploys to GitHub Pages

#### Requirements

To use this workflow, you need to:

1. **Enable GitHub Pages** in your repository settings:
   - Go to Settings → Pages
   - Source: Select "GitHub Actions"

2. **Permissions**: The workflow needs these permissions (already configured in the workflow):
   - `contents: read` - To checkout the code
   - `pages: write` - To deploy to Pages
   - `id-token: write` - For secure deployment

#### Viewing Your Demo

Once deployed, your demo will be available at:
```
https://[your-username].github.io/[repository-name]/
```

For example:
```
https://johndoe.github.io/BlazorFastMarquee/
```

#### Manual Deployment

You can manually trigger a deployment:

1. Go to the **Actions** tab in your repository
2. Select **Deploy Demo to GitHub Pages**
3. Click **Run workflow**
4. Select the branch (usually main/master)
5. Click **Run workflow**

#### Monitoring Deployments

- View deployment status in the **Actions** tab
- Each deployment shows:
  - Build logs
  - Publish output
  - Deployment URL
  - Success/failure status

#### Troubleshooting

**Build Failures:**
- Check the Actions tab for detailed error logs
- Ensure .NET 9.0 SDK is being used
- Verify all project references are correct

**404 Errors After Deployment:**
- Verify GitHub Pages is enabled in Settings
- Check that the base href is correctly updated
- Ensure `.nojekyll` file exists in the output

**Base Path Issues:**
- The workflow automatically updates the base path
- Manual adjustments can be made in the workflow file if needed
- The base path should match your repository name

#### Customization

**Change Deployment Branch:**
```yaml
on:
  push:
    branches: [ "your-branch-name" ]
```

**Adjust Path Filters:**
Add or remove paths in the `paths:` section to control when the workflow runs.

**Enable AOT Compilation:**
In `BlazorFastMarquee.Demo.csproj`, change:
```xml
<RunAOTCompilation>true</RunAOTCompilation>
```
Note: AOT compilation increases build time but improves runtime performance.

#### Performance Optimization

The published WASM app includes:
- ✅ Trimmed assemblies (smaller download size)
- ✅ Compressed with Brotli and GZip
- ✅ Static asset optimization
- ✅ Framework-dependent deployment

For production, consider:
- Enabling AOT compilation for better performance
- Using a CDN for the _framework files
- Implementing service worker for offline support

---

**Status**: ✅ Ready to use  
**Last Updated**: 2025-10-28
