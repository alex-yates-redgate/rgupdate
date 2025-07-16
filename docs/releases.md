# Release Process and Download URLs

## Predictable Download URLs

The GitHub Actions workflow automatically creates releases with predictable URLs:

### Direct Download URLs (Always Latest)
- **Windows**: `https://github.com/alex-yates-redgate/rgupdate/releases/latest/download/rgupdate-windows.exe`
- **Linux**: `https://github.com/alex-yates-redgate/rgupdate/releases/latest/download/rgupdate-linux`

### Download Scripts
- **Windows PowerShell**: `scripts/download-latest.ps1`
- **Linux/macOS Bash**: `scripts/download-latest.sh`

## How It Works

1. **Automatic Release Creation**: When code is pushed to the `main` branch:
   - Windows tests run (`test-windows` job)
   - Cross-platform builds are created (`build` job)
   - A GitHub release is automatically created (`create-release` job)
   - The release is marked as "latest" 

2. **Artifact Generation**: Each build produces:
   - `rgupdate-windows.exe` (self-contained Windows executable)
   - `rgupdate-linux` (self-contained Linux executable)
   - `build-info.md` (build metadata)

3. **Release Tagging**: Releases use timestamp-based tags:
   - Format: `build-YYYYMMDD-HHMMSS-[run_number]`
   - Example: `build-20250716-143025-42`

4. **URL Consistency**: The `/releases/latest/download/` URLs always point to the most recent successful build from the main branch.

## Release Workflow Details

### Triggers
- Push to `main` branch
- Only creates releases for the main branch (not feature branches)

### Jobs Sequence
1. `test-windows` - Run Windows-specific tests
2. `build` - Create cross-platform executables
3. `create-release-info` - Generate build metadata
4. `create-release` - Package and publish GitHub release

### Dependencies
- Only runs if Windows tests pass
- Linux tests are currently commented out but can be re-enabled

## Manual Release Process

If you need to create a manual release:

1. **Create a tag**:
   ```bash
   git tag -a v1.0.0 -m "Release version 1.0.0"
   git push origin v1.0.0
   ```

2. **Build locally**:
   ```bash
   dotnet publish src/rgupdate/rgupdate.csproj --configuration Release --runtime win-x64 --self-contained true --output ./publish/win-x64
   dotnet publish src/rgupdate/rgupdate.csproj --configuration Release --runtime linux-x64 --self-contained true --output ./publish/linux-x64
   ```

3. **Create GitHub release manually** via the GitHub UI or CLI

## Integration Examples

### CI/CD Pipelines
```yaml
# Example: Download latest rgupdate in another workflow
- name: Download rgupdate
  run: |
    curl -L -o rgupdate "https://github.com/alex-yates-redgate/rgupdate/releases/latest/download/rgupdate-linux"
    chmod +x rgupdate
    ./rgupdate --version
```

### Docker Images
```dockerfile
# Example: Include rgupdate in a Docker image
FROM mcr.microsoft.com/dotnet/runtime:8.0
RUN curl -L -o /usr/local/bin/rgupdate "https://github.com/alex-yates-redgate/rgupdate/releases/latest/download/rgupdate-linux" \
    && chmod +x /usr/local/bin/rgupdate
```

### PowerShell Modules
```powershell
# Example: Download in a PowerShell module
function Install-RgUpdate {
    $url = "https://github.com/alex-yates-redgate/rgupdate/releases/latest/download/rgupdate-windows.exe"
    $output = "$env:TEMP\rgupdate.exe"
    Invoke-WebRequest -Uri $url -OutFile $output
    return $output
}
```

## Troubleshooting

### Common Issues
1. **404 Not Found**: Check if the latest release exists and contains the expected files
2. **Permission Denied**: Ensure the downloaded file has execute permissions (Linux/macOS)
3. **Antivirus Blocks**: Some antivirus software may block downloaded executables

### Verification
```bash
# Verify download integrity
curl -L -I "https://github.com/alex-yates-redgate/rgupdate/releases/latest/download/rgupdate-linux"

# Check file size and permissions
ls -la rgupdate
```

## Security Considerations

1. **HTTPS Only**: All download URLs use HTTPS
2. **GitHub Security**: Releases are hosted on GitHub's secure infrastructure
3. **Checksum Verification**: Future enhancement could add SHA256 checksums
4. **Code Signing**: Future enhancement for Windows executables

## Future Enhancements

- [ ] Add SHA256 checksums for verification
- [ ] Code signing for Windows executables
- [ ] Multi-architecture support (ARM64)
- [ ] Package manager integration (Chocolatey, Homebrew, apt)
- [ ] Docker Hub automated builds
