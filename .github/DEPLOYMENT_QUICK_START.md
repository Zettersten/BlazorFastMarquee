# 🚀 Quick Start: Deploy to GitHub Pages

## Step 1: Push to GitHub
```bash
git add .
git commit -m "Add GitHub Pages deployment"
git push origin main
```

## Step 2: Enable GitHub Pages
1. Go to **Settings** → **Pages**
2. Set **Source** to: **GitHub Actions**
3. Save

## Step 3: Watch Deployment
1. Go to **Actions** tab
2. Watch "Deploy Demo to GitHub Pages"
3. Wait ~2-5 minutes

## Step 4: Visit Your Demo
```
https://[your-username].github.io/[repository-name]/
```

---

## Manual Trigger
1. **Actions** tab
2. **Deploy Demo to GitHub Pages**
3. **Run workflow** → Select branch → **Run**

---

## Troubleshooting

**Build fails?**
- Check Actions tab for logs
- Verify .NET 9.0 is used

**404 errors?**
- Check base href in index.html
- Verify .nojekyll file exists

**Blank page?**
- Open browser console
- Check network tab
- Verify base path

---

## Full Documentation
- `/GITHUB_PAGES_DEPLOYMENT.md` - Complete guide
- `/GITHUB_PAGES_SETUP_COMPLETE.md` - Setup summary
- `/.github/workflows/README.md` - Workflow docs

---

**That's it! Your demo will be live in minutes.** 🎉
