# CI/CD Quick Start Guide

## Overview

This project uses GitHub Actions for automated building, testing, and releasing.

## For Developers

### Every Push
```bash
git add .
git commit -m "feat: your changes"
git push
```

**What happens:**
- ? CI workflow runs
- ? Code builds
- ? Tests run
- ? Status visible in PR/commit

### Nightly Builds (master/main)

**Automatic:**
- Push to `master` or `main`
- Creates nightly build artifact
- Available in Actions tab for 90 days

**Manual:**
1. Go to Actions ? Build and Publish
2. Click "Run workflow"
3. Select branch
4. Click "Run workflow"

## For Maintainers

### Creating a Release

**Step 1: Prepare**
```bash
# Update changelog
nano GuiGenericBuilderDesktop/Changelog.md

# Commit changes
git add .
git commit -m "chore: prepare release v2.1.0"
git push
```

**Step 2: Tag and Push**
```bash
# Create annotated tag
git tag -a v2.1.0 -m "Release version 2.1.0"

# Push tag to trigger release
git push origin v2.1.0
```

**Step 3: Wait**
- GitHub Actions builds
- Tests run
- Release created
- Artifacts uploaded

**Step 4: Verify**
- Check [Releases](../../releases)
- Download and test artifacts
- Update release notes if needed

### Release Artifacts

Two variants created:

**Self-Contained (~150MB)**
- `GuiGenericBuilder-v2.1.0-win-x64.zip`
- No .NET runtime required
- Recommended for end users

**Framework-Dependent (~10MB)**
- `GuiGenericBuilder-v2.1.0-win-x64-framework-dependent.zip`
- Requires .NET 10 runtime
- For users with .NET installed

## Version Numbering

Use [Semantic Versioning](https://semver.org/):

```
vMAJOR.MINOR.PATCH[-PRERELEASE]
```

**Examples:**
- `v2.1.0` - Major/minor/patch release
- `v2.1.1` - Patch release
- `v2.2.0-alpha.1` - Alpha prerelease
- `v2.2.0-beta.1` - Beta prerelease
- `v2.2.0-rc.1` - Release candidate

**When to increment:**
- `MAJOR`: Breaking changes
- `MINOR`: New features (backward compatible)
- `PATCH`: Bug fixes

## Workflow Files

Three workflows in `.github/workflows/`:

1. **`ci.yml`** - Quick validation on every push
2. **`build-and-publish.yml`** - Full build with artifacts
3. **`release.yml`** - Release automation

## Common Tasks

### Check Build Status

Visit: [Actions Tab](../../actions)

### Download Nightly Build

1. Go to Actions
2. Select "Build and Publish" workflow
3. Find recent run on master/main
4. Download artifact from "Artifacts" section

### Fix Failed Build

```bash
# Check locally first
dotnet build --configuration Release
dotnet test --configuration Release

# Fix issues and push
git add .
git commit -m "fix: resolve build issues"
git push
```

### Delete Bad Release

```bash
# Delete release on GitHub (via web UI)

# Delete tag locally
git tag -d v2.1.0

# Delete tag remotely
git push origin :refs/tags/v2.1.0

# Create new tag
git tag -a v2.1.0 -m "Release 2.1.0"
git push origin v2.1.0
```

## Monitoring

### Build Status Badges

Add to README.md:
```markdown
[![CI](https://github.com/USERNAME/REPO/actions/workflows/ci.yml/badge.svg)](https://github.com/USERNAME/REPO/actions/workflows/ci.yml)
```

### Email Notifications

- Configure in GitHub Settings ? Notifications
- Get notified on workflow failures

## Troubleshooting

### "Workflow not running"

**Cause:** Workflow file syntax error

**Fix:**
```bash
# Validate YAML
yamllint .github/workflows/*.yml

# Check workflow logs in Actions tab
```

### "Tests failing in CI but pass locally"

**Cause:** Environment differences

**Fix:**
- Check .NET version matches
- Verify dependencies
- Check paths (use Path.Combine)

### "Release not created"

**Cause:** Tag format or permissions

**Fix:**
- Ensure tag matches `v*.*.*`
- Check repository permissions
- Verify GITHUB_TOKEN scope

## Best Practices

### ? Do

- Test locally before pushing
- Write meaningful commit messages
- Update changelog before release
- Use semantic versioning
- Wait for CI before merging PR

### ? Don't

- Skip tests
- Push directly to master (use PRs)
- Create releases without testing
- Use non-standard version formats
- Ignore CI failures

## Support

- **Documentation:** `.github/workflows/README.md`
- **Issues:** [GitHub Issues](../../issues)
- **Questions:** [Discussions](../../discussions)

## Quick Reference

```bash
# Build locally
dotnet build --configuration Release

# Test locally
dotnet test --configuration Release

# Create release
git tag -a v2.1.0 -m "Release 2.1.0"
git push origin v2.1.0

# Manual workflow trigger
# Go to Actions ? Select workflow ? Run workflow

# Check status
# Actions tab ? Select workflow run
```

---

**Ready to release?** Follow the [Creating a Release](#creating-a-release) guide above!
