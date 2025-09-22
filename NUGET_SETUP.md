# NuGet.org Publishing Setup Guide

This guide will help you set up automated NuGet.org publishing for the DStream .NET SDK packages.

## 🔑 Prerequisites

### 1. Create NuGet.org Account
1. Go to [nuget.org](https://nuget.org)
2. Sign up for a new account or sign in with your existing Microsoft account
3. Verify your email address

### 2. Generate NuGet API Key
1. Sign in to [nuget.org](https://nuget.org)
2. Click on your username in the top-right corner
3. Go to **API Keys** from the dropdown menu
4. Click **Create** to create a new API key
5. Configure the API key:
   - **Key Name**: `DStream SDK Publishing` (or similar descriptive name)
   - **Package Owner**: Select your account
   - **Scopes**: Select "Push new packages and package versions"
   - **Packages**: Select "All packages" or create patterns like `Katasec.DStream.*`
   - **Glob Pattern**: `Katasec.DStream.*` (to limit to DStream packages only)
6. Click **Create**
7. **Important**: Copy the generated API key immediately - you won't be able to see it again!

### 3. Add API Key to GitHub Secrets
1. Go to your GitHub repository: `https://github.com/katasec/dstream-dotnet-sdk`
2. Click on **Settings** tab
3. In the left sidebar, click **Secrets and variables** → **Actions**
4. Click **New repository secret**
5. Add the secret:
   - **Name**: `NUGET_API_KEY`
   - **Secret**: Paste the API key you copied from NuGet.org
6. Click **Add secret**

## 🚀 Usage

### Test the Workflow
1. **Build and test locally first**:
   ```bash
   cd ~/progs/dstream/dstream-dotnet-sdk
   dotnet restore dstream-dotnet-sdk.sln
   dotnet build dstream-dotnet-sdk.sln --configuration Release
   dotnet test dstream-dotnet-sdk.sln --configuration Release
   ```

2. **Create a test release**:
   ```bash
   # Use the version bump script
   ./scripts/version-bump.ps1 -Type patch -DryRun
   # Review what it would do, then run without -DryRun
   ./scripts/version-bump.ps1 -Type patch
   ```

3. **Push to trigger publishing**:
   ```bash
   git push origin main
   git push origin v0.1.1  # Replace with your actual version
   ```

4. **Monitor the workflow**:
   - Go to **Actions** tab in your GitHub repository
   - Watch the "Publish to NuGet.org" workflow
   - Check for any errors in the build/publish process

### Verify Publication
1. Wait 5-10 minutes after the workflow completes
2. Search for your packages on [nuget.org](https://nuget.org):
   - Search for "Katasec.DStream.Abstractions"
   - Search for "Katasec.DStream.SDK.Core"
3. Test installation in a new project:
   ```bash
   mkdir test-install
   cd test-install
   dotnet new console
   dotnet add package Katasec.DStream.SDK.Core --version 0.1.1
   ```

## 📦 Package Information

The workflow will publish these packages to NuGet.org:

### Katasec.DStream.Abstractions
- **Description**: Core interfaces and abstractions for the DStream data streaming platform
- **Dependencies**: None
- **Target Framework**: .NET 9.0

### Katasec.DStream.SDK.Core  
- **Description**: Core SDK for building DStream data streaming providers
- **Dependencies**: Katasec.DStream.Abstractions
- **Target Framework**: .NET 9.0

## 🔧 Troubleshooting

### Common Issues

**"The package version already exists"**
- This happens if you try to publish the same version twice
- The workflow includes `--skip-duplicate` to handle this gracefully
- Bump the version number before publishing again

**"Invalid API key"**
- Check that the `NUGET_API_KEY` GitHub secret is set correctly
- Verify the API key hasn't expired on NuGet.org
- Make sure the API key has the correct permissions

**"Build failed"**
- Ensure all tests pass locally first
- Check the GitHub Actions logs for specific error messages
- Common issues: missing dependencies, test failures, invalid project references

**"Package validation failed"**
- NuGet.org validates packages before accepting them
- Check for missing metadata, invalid license expressions, etc.
- Review the package metadata in the `.csproj` files

### Manual Publishing (Backup Method)

If GitHub Actions fails, you can publish manually:

```bash
# Build and pack locally
dotnet pack sdk/Katasec.DStream.Abstractions/Katasec.DStream.Abstractions.csproj -c Release -o ./packages
dotnet pack sdk/Katasec.DStream.SDK.Core/Katasec.DStream.SDK.Core.csproj -c Release -o ./packages

# Publish to NuGet.org (requires API key)
dotnet nuget push ./packages/*.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
```

## 🔄 Version Management

Use the provided PowerShell script for consistent versioning:

```bash
# Patch version bump (0.1.0 → 0.1.1)
./scripts/version-bump.ps1 -Type patch

# Minor version bump (0.1.0 → 0.2.0)  
./scripts/version-bump.ps1 -Type minor

# Major version bump (0.1.0 → 1.0.0)
./scripts/version-bump.ps1 -Type major

# Pre-release version (0.1.0 → 0.2.0-beta.1)
./scripts/version-bump.ps1 -Type minor -PreRelease beta

# Custom version
./scripts/version-bump.ps1 -Type custom -CustomVersion "1.0.0"
```

The script will:
1. Update `VERSION.txt`
2. Commit the version change
3. Create a git tag (e.g., `v0.1.1`)
4. Provide instructions for pushing to trigger the release

## ✅ Success Criteria

When everything is working correctly, you should be able to:

1. **Publish packages**: `git push origin vX.Y.Z` triggers automatic NuGet publishing
2. **Find packages**: Search and find your packages on nuget.org
3. **Install packages**: `dotnet add package Katasec.DStream.SDK.Core` works in any .NET project
4. **Use packages**: External projects can reference and use the SDK

This completes the foundation for external provider development! 🚀