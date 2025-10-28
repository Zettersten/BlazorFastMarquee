# GitHub Pages Setup Instructions

## ⚠️ Required Configuration

The GitHub Actions workflow will **fail with a 404 error** until GitHub Pages is properly configured.

## 🔧 Setup Steps (Required - One Time Only)

Follow these steps to enable GitHub Pages deployment:

### Step 1: Navigate to Repository Settings
1. Go to your repository on GitHub
2. Click on **Settings** (top navigation bar)
3. Scroll down and click on **Pages** (left sidebar)

Or directly visit: `https://github.com/YOUR_USERNAME/BlazorFastMarquee/settings/pages`

### Step 2: Configure GitHub Pages Source
1. Under **"Build and deployment"** section
2. Find the **"Source"** dropdown
3. Select **"GitHub Actions"** (NOT "Deploy from a branch")
4. Click **Save** (if prompted)

### Step 3: Verify Configuration
You should see a message like:
> "Your site is ready to be published at `https://YOUR_USERNAME.github.io/BlazorFastMarquee/`"

## 🚀 Trigger Deployment

After configuration, trigger a deployment by either:

### Option A: Push to Main
```bash
git push origin main
```

### Option B: Manual Trigger
1. Go to **Actions** tab
2. Select **"Deploy Demo to GitHub Pages"** workflow
3. Click **"Run workflow"** button
4. Select branch (usually `main`)
5. Click **"Run workflow"**

## ✅ Expected Workflow Behavior

Once GitHub Pages is configured:

1. **Build Job**: Compiles and publishes the Blazor WASM app
2. **Deploy Job**: Uploads and deploys to GitHub Pages
3. **Result**: Demo available at `https://YOUR_USERNAME.github.io/BlazorFastMarquee/`

Typical deployment time: **2-5 minutes**

## ❌ Common Errors

### Error: "Failed to create deployment (status: 404)"

**Cause**: GitHub Pages is not enabled or not set to use "GitHub Actions"

**Solution**: Follow the setup steps above to configure GitHub Pages

### Error: "Resource not accessible by integration"

**Cause**: Workflow lacks required permissions

**Solution**: Verify the workflow has these permissions:
```yaml
permissions:
  contents: read
  pages: write
  id-token: write
```

### Error: "Workflow does not have 'pages: write' permission"

**Cause**: Repository settings restrict workflow permissions

**Solution**: 
1. Go to Repository Settings → Actions → General
2. Scroll to "Workflow permissions"
3. Select "Read and write permissions"
4. Save changes

## 🔍 Troubleshooting

### Check Workflow Run
1. Go to **Actions** tab
2. Click on the failed workflow run
3. Expand the "Deploy to GitHub Pages" step
4. Look for specific error messages

### Verify Deployment
After successful deployment:
```bash
# Check if site is accessible
curl -I https://YOUR_USERNAME.github.io/BlazorFastMarquee/

# Should return: HTTP/2 200
```

### Inspect Build Artifacts
The workflow creates detailed logs showing:
- Published file structure
- Base path updates
- File verification
- Deployment status

## 📚 Additional Resources

- [GitHub Pages Documentation](https://docs.github.com/en/pages)
- [GitHub Actions for Pages](https://github.com/actions/deploy-pages)
- [Blazor WebAssembly Hosting](https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/webassembly)

## 💡 Tips

- **First deployment** may take slightly longer (5-10 minutes)
- **Subsequent deployments** are faster (2-3 minutes)
- **Failed deployments** don't affect your existing live site
- **Branch protection** rules apply to the workflow

## 🎯 Quick Checklist

- [ ] GitHub Pages enabled
- [ ] Source set to "GitHub Actions"
- [ ] Workflow permissions configured
- [ ] At least one successful workflow run
- [ ] Site accessible at the GitHub Pages URL

---

**Last Updated**: 2025-10-28  
**Workflow Version**: v1.1  
**Status**: Production Ready
