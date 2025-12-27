# GitHub Actions CI/CD Setup

This repository uses GitHub Actions for continuous integration and deployment.

## Workflows

### 1. CI Workflow (`.github/workflows/ci.yml`)

**Triggers:**
- Every push to any branch
- Pull requests to `master`, `main`, or `develop` branches

**What it does:**
- Restores dependencies
- Builds the solution in Release configuration
- Runs all tests

**Purpose:** Quick validation that code compiles and tests pass.

---

### 2. Build and Publish Workflow (`.github/workflows/build-and-publish.yml`)

**Triggers:**
- Push to `master`, `main`, or `develop` branches
- Tags matching `v*.*.*` pattern
- Manual workflow dispatch

**What it does:**

**Build Job:**
- Builds the solution
- Runs tests
- Publishes the application
- Uploads artifacts (retained for 30 days)

**Package Job (on tags only):**
- Creates release archive
- Creates GitHub release with downloadable ZIP

**Publish Artifacts Job (on master/main only):**
- Creates nightly builds
- Archives with timestamp and commit SHA
- Retained for 90 days

---

### 3. Release Workflow (`.github/workflows/release.yml`)

**Triggers:**
- Tags matching `v*.*.*` (e.g., `v2.1.0`)

**What it does:**
- Builds with version from tag
- Runs tests
- Creates two publish variants:
  - **Self-Contained**: No .NET runtime required (~150MB)
  - **Framework-Dependent**: Requires .NET 10 runtime (~10MB)
- Copies documentation and builder.json
- Creates ZIP archives
- Creates GitHub release with:
  - Release notes
  - Both ZIP files
  - Installation instructions

---

## Creating a Release

### Step 1: Update Version

Update version in:
- `GuiGenericBuilderDesktop/Changelog.md` - Add release notes
- Project files (optional, will be set from tag)

### Step 2: Commit and Push

```bash
git add .
git commit -m "Release v2.1.0"
git push origin master
```

### Step 3: Create and Push Tag

```bash
git tag -a v2.1.0 -m "Release version 2.1.0"
git push origin v2.1.0
```

### Step 4: Wait for Workflow

- GitHub Actions will automatically:
  - Build the application
  - Run tests
  - Create release packages
  - Publish GitHub release

### Step 5: Verify Release

Check the [Releases page](../../releases) for:
- Release notes
- Download links for both variants
- Installation instructions

---

## Release Naming Convention

Use semantic versioning: `vMAJOR.MINOR.PATCH`

**Examples:**
- `v2.1.0` - Normal release
- `v2.1.1` - Patch release
- `v2.2.0-alpha.1` - Alpha prerelease
- `v2.2.0-beta.1` - Beta prerelease
- `v2.2.0-rc.1` - Release candidate

**Prerelease Detection:**
- Tags containing `alpha`, `beta`, or `rc` are marked as prereleases

---

## Build Artifacts

### Nightly Builds (master/main branch)

Available in Actions artifacts:
- Name: `nightly-build-{run_number}`
- Format: `GuiGenericBuilder-nightly-{date}-{sha}.zip`
- Retention: 90 days

### Release Builds (tags)

Available in GitHub Releases:
- Self-contained: `GuiGenericBuilder-v{version}-win-x64.zip`
- Framework-dependent: `GuiGenericBuilder-v{version}-win-x64-framework-dependent.zip`

---

## Manual Workflow Trigger

You can manually trigger the "Build and Publish" workflow:

1. Go to [Actions](../../actions)
2. Select "Build and Publish" workflow
3. Click "Run workflow"
4. Select branch
5. Click "Run workflow"

This is useful for testing the workflow or creating artifacts without a tag.

---

## Workflow Status Badges

Add these badges to your README.md:

```markdown
[![CI](https://github.com/YOUR_USERNAME/GuiGenericV2/actions/workflows/ci.yml/badge.svg)](https://github.com/YOUR_USERNAME/GuiGenericV2/actions/workflows/ci.yml)
[![Release](https://github.com/YOUR_USERNAME/GuiGenericV2/actions/workflows/release.yml/badge.svg)](https://github.com/YOUR_USERNAME/GuiGenericV2/actions/workflows/release.yml)
```

Replace `YOUR_USERNAME` with your GitHub username.

---

## Troubleshooting

### Build Fails

**Check:**
- .NET 10 SDK is specified correctly
- All project references are valid
- Solution file path is correct

**Fix:** Update `DOTNET_VERSION` or paths in workflow files

### Tests Fail

**Check:**
- All tests pass locally
- Test dependencies are available
- No environment-specific tests

**Fix:** Run tests locally first: `dotnet test --configuration Release`

### Release Creation Fails

**Check:**
- Tag format is correct (`v*.*.*`)
- GITHUB_TOKEN has required permissions
- No existing release with same tag

**Fix:** Delete tag and recreate:
```bash
git tag -d v2.1.0
git push origin :refs/tags/v2.1.0
git tag -a v2.1.0 -m "Release 2.1.0"
git push origin v2.1.0
```

### Artifacts Missing

**Check:**
- Build completed successfully
- Publish step succeeded
- Upload artifact step succeeded

**Fix:** Check workflow logs in Actions tab

---

## Local Testing

Test publish locally before release:

```bash
# Self-contained
dotnet publish GuiGenericBuilderDesktop/GuiGenericBuilderDesktop.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  --output ./publish/win-x64 `
  -p:PublishSingleFile=true

# Framework-dependent
dotnet publish GuiGenericBuilderDesktop/GuiGenericBuilderDesktop.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained false `
  --output ./publish/win-x64-fd
```

---

## Security

### Permissions

Workflows use:
- `GITHUB_TOKEN`: Automatically provided, limited scope
- `contents: write`: Required for creating releases

### Secrets

No custom secrets required. Uses built-in `GITHUB_TOKEN`.

---

## Customization

### Change .NET Version

Edit in all workflow files:
```yaml
env:
  DOTNET_VERSION: '10.0.x'  # Change this
```

### Change Retention

**Artifacts:**
```yaml
retention-days: 30  # Change this (max 90 for free accounts)
```

**Nightly Builds:**
```yaml
retention-days: 90  # Change this
```

### Add More Platforms

Add additional publish steps for other platforms:
```yaml
- name: Publish Linux x64
  run: |
    dotnet publish ${{ env.PROJECT_PATH }} `
      --configuration ${{ env.BUILD_CONFIGURATION }} `
      --runtime linux-x64 `
      --self-contained true `
      --output ./publish/linux-x64
```

---

## References

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [.NET publish command](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-publish)
- [Semantic Versioning](https://semver.org/)
