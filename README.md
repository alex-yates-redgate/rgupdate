# rgupdate

A cross-platform .NET Core console application that manages Red Gate CLI tool versions on Windows and Linux.

## Features

- **Cross-platform**: Runs on both Windows and Linux
- **Version Management**: Install, validate, and manage multiple versions of Red Gate CLI tools
- **Path Management**: Automatically manages PATH environment variables
- **Clean Architecture**: Built with System.CommandLine for modern CLI experience

## Supported Products

- `flyway` - Database migration tool
- `rgsubset` - Database subsetting tool  
- `rganonymize` - Database anonymization tool

More products (like `sqlcompare`) may be added in future versions.

## Installation

### Quick Download (Latest Version)

**Predictable URLs for automation:**
- **Windows**: https://github.com/alex-yates-redgate/rgupdate/releases/latest/download/rgupdate-windows.exe
- **Linux**: https://github.com/alex-yates-redgate/rgupdate/releases/latest/download/rgupdate-linux

**One-liner download commands:**

**Windows (PowerShell):**
```powershell
Invoke-WebRequest -Uri "https://github.com/alex-yates-redgate/rgupdate/releases/latest/download/rgupdate-windows.exe" -OutFile "rgupdate.exe"
```

**Linux/macOS:**
```bash
curl -L -o rgupdate "https://github.com/alex-yates-redgate/rgupdate/releases/latest/download/rgupdate-linux" && chmod +x rgupdate
```

### Automated Download Scripts

For convenience, use the provided download scripts:

**Windows (PowerShell):**
```powershell
# Download the script and run it
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/alex-yates-redgate/rgupdate/main/scripts/download-latest.ps1" -OutFile "download-latest.ps1"
./download-latest.ps1
```

**Linux/macOS:**
```bash
# Download the script and run it
curl -L -o download-latest.sh "https://raw.githubusercontent.com/alex-yates-redgate/rgupdate/main/scripts/download-latest.sh"
chmod +x download-latest.sh
./download-latest.sh
```

### Manual Download
Browse all releases at the [GitHub Releases page](https://github.com/alex-yates-redgate/rgupdate/releases).

**File Info:**
- **Windows**: `rgupdate-windows.exe` (≈16 MB)
- **Linux**: `rgupdate-linux` (≈18 MB)

These are completely self-contained executables with no dependencies - just download and run!

### Building from Source
```bash
git clone https://github.com/alex-yates-redgate/rgupdate.git
cd rgupdate
dotnet publish --configuration Release --runtime win-x64 --self-contained true
# Or for Linux: --runtime linux-x64
```

## Commands

### validate
Validates that the correct version is installed and runs properly.

```bash
rgupdate validate [product] [--version 1.2.3]
```

**Examples:**
```bash
rgupdate validate flyway
rgupdate validate flyway --version 8.1.23
rgupdate validate rgsubset --version latest
```

### get
Downloads and installs the specified product version. Checks if software is already installed and validates it. If not installed or validation fails, installs to `Program Files/Red Gate/[product]/CLI/[version]`.

```bash
rgupdate get [product] [--version 1.2.3]
```

**Examples:**
```bash
rgupdate get flyway
rgupdate get flyway --version 8.1.23
rgupdate get rgsubset --version 1
```

### use
Sets a specific version as active and ensures it's available in PATH. Copies the correct version to `Program Files/Red Gate/[product]/CLI/active` and manages PATH environment variables.

```bash
rgupdate use [product] [--version 1.2.3] [--local-copy] [--local-only]
```

**Examples:**
```bash
rgupdate use flyway --version 8.1.23
rgupdate use rgsubset --version latest
rgupdate use flyway --version 8.1.23 --local-copy
rgupdate use rgsubset --version latest --local-only
```

**Options:**
- `--local-copy`: In addition to setting the global active version, creates a local copy of the CLI executable in the current directory for immediate use
- `--local-only`: Only creates a local copy in the current directory without updating the global active version or PATH

**⚠️ PATH Update Warnings:**
When rgupdate modifies your PATH environment variable, you'll need to:
- **Open a new terminal/command prompt**, OR
- **Restart your current terminal session**

Local copies work immediately without requiring terminal restart.

### list
Lists available versions of a product, showing the most recent 10 versions by default, along with installation and active status.

```bash
rgupdate list [product] [--all] [--output format]
```

**Examples:**
```bash
rgupdate list rgsubset
rgupdate list rgsubset --all
rgupdate list rgsubset --output json
rgupdate list rganonymize --output yaml
```

**Output Formats:**
- **Default (table)**: Human-readable table format
- **JSON**: Machine-readable JSON format (`--output json`)
- **YAML**: Machine-readable YAML format (`--output yaml`)

**Sample Table Output:**
```
Version         | Release Date  | Size      | Status
----------------|---------------|-----------|--------
2.1.15.1477     | 2025-06-24    | 37.4 MB   | -
2.1.14.1471     | 2025-06-24    | 37.4 MB   | -
2.1.13.1440     | 2025-06-23    | 37.4 MB   | installed
2.1.12.1380     | 2025-06-17    | 38.4 MB   | -
2.1.10.8038     | 2025-05-21    | 37.4 MB   | -
2.1.9.7997      | 2025-05-19    | 37.4 MB   | installed
2.1.8.7948      | 2025-05-09    | 37.4 MB   | -
2.1.7.7933      | 2025-05-08    | 37.4 MB   | -
2.1.6.7744      | 2025-03-19    | 37.1 MB   | -
2.1.5.7733      | 2025-03-18    | 37.1 MB   | -
2.1.4.7676      | 2025-02-27    | 37.1 MB   | -
2.1.3.7501      | 2025-01-14    | 37.3 MB   | ACTIVE

42 older versions (To see all versions, run: rgupdate list rgsubset --all)
```

**Sample JSON Output:**
```json
{
  "product": "rgsubset",
  "activeVersion": "2.1.3.7501",
  "totalVersions": 54,
  "displayedVersions": 10,
  "showingAll": false,
  "versions": [
    {
      "version": "2.1.15.1477",
      "releaseDate": "2025-06-24",
      "sizeBytes": 39252723,
      "sizeFormatted": "37.4 MB",
      "status": "-",
      "isLocalOnly": false,
      "isInstalled": false,
      "isActive": false
    }
  ]
}
```

### remove
Removes a specific version or all versions of a product. Includes safety checks to prevent accidental removal of active versions.

```bash
rgupdate remove [product] [--version 1.2.3] [--force]
rgupdate remove [product] --all [--force]
```

**Examples:**
```bash
rgupdate remove flyway --version 8.1.23
rgupdate remove flyway --all --force
```

**Safety Features:**
- Prevents deletion of active version without `--force` flag
- Clear error messages when attempting unsafe operations

### purge
Removes old versions while keeping only the specified number of most recent versions.

```bash
rgupdate purge [product] --keep 3 [--force]
```

**Examples:**
```bash
rgupdate purge flyway --keep 3
rgupdate purge rgsubset --keep 5 --force
```

### info
Displays configuration and system information, including install paths and environment variables.

```bash
rgupdate info
```

**Sample Output:**
```
rgupdate Configuration Information
=================================
Install Location: C:\Program Files\Red Gate
Machine-level env var: (not set)
User-level env var: C:\Program Files\Red Gate
Supported Products:
  - flyway
    Version path: C:\Program Files\Red Gate\Flyway\CLI\<version>
    Active path:  C:\Program Files\Red Gate\Flyway\CLI\active
  - rgsubset
    Version path: C:\Program Files\Red Gate\Test Data Manager\rgsubset\<version>
    Active path:  C:\Program Files\Red Gate\Test Data Manager\rgsubset\active
```

### config
Configure rgupdate settings, including the installation location.

#### set-location
Changes the installation location where rgupdate stores downloaded tools.

```bash
rgupdate config set-location <path> [--force]
```

**Examples:**
```bash
# Change to a custom directory
rgupdate config set-location "D:\MyTools\RedGate"

# Use environment variables
rgupdate config set-location "%USERPROFILE%\RedGateTools"

# Force change despite existing installations
rgupdate config set-location "C:\RedGate" --force
```

**Features:**
- **Safety Warnings**: Warns if existing installations will be left behind
- **Environment Variable Expansion**: Supports variables like `%USERPROFILE%`, `%TEMP%`, etc.
- **Permission Handling**: Automatically falls back to user-level if admin rights unavailable  
- **Path Validation**: Creates directories and verifies write permissions
- **Force Override**: Use `--force` to bypass safety warnings

**Important Notes:**
- Existing installations are NOT moved automatically
- You may need to restart your terminal after changing the location
- Use `rgupdate info` to verify the change took effect

## Version Formats

The `--version` parameter accepts several formats:

- **Full version**: `1.2.3` - Exact version match
- **Shorthand**: `1` - Most recent version matching `1.*`
- **Latest**: `latest` - Most recent available version

## Environment Variable Management

`rgupdate` automatically manages the `RGUPDATE_INSTALL_LOCATION` environment variable:

1. **First Run**: On first execution, `rgupdate` checks if the environment variable exists
2. **Auto-Setup**: If not found, it automatically sets the variable to the default location:
   - **Windows**: `%Program Files%\Red Gate`
   - **Linux/macOS**: `/opt/Red Gate`
3. **Permission Handling**: 
   - Attempts to set machine-level variable first (requires admin privileges)
   - Falls back to user-level variable if admin privileges not available
4. **Runtime Refresh**: Automatically refreshes environment variables to ensure up-to-date values

You can check the current configuration using:
```bash
rgupdate info
```

You can change the installation location using:
```bash
rgupdate config set-location <new-path>
```

## Structured Output for Automation

The `list` command supports structured output formats for automation and scripting:

**JSON Output:**
```bash
rgupdate list rgsubset --output json | jq '.versions[0].version'
```

**YAML Output:**
```bash
rgupdate list rgsubset --output yaml
```

This enables integration with build systems, deployment scripts, and other automation tools that need to programmatically work with version information.

## Installation Paths

Products are installed to the following paths based on their product family:

**Flyway:**
```
%RGUPDATE_INSTALL_LOCATION%\Flyway\CLI\<version>\
%RGUPDATE_INSTALL_LOCATION%\Flyway\CLI\active\    (active version)
```

**Test Data Manager (rgsubset and rganonymize):**
```
%RGUPDATE_INSTALL_LOCATION%\Test Data Manager\rgsubset\<version>\
%RGUPDATE_INSTALL_LOCATION%\Test Data Manager\rgsubset\active\    (active version)

%RGUPDATE_INSTALL_LOCATION%\Test Data Manager\rganonymize\<version>\
%RGUPDATE_INSTALL_LOCATION%\Test Data Manager\rganonymize\active\    (active version)
```

The `RGUPDATE_INSTALL_LOCATION` environment variable is automatically set to:
- **Windows**: `%Program Files%\Red Gate`
- **Linux/macOS**: `/opt/Red Gate`

## Local Copy Options and PATH Management

### Local Copy Features

The `use` command supports two local copy options for greater flexibility:

**`--local-copy`** (Additive):
- Creates a local copy in the current directory
- **ALSO** updates the global active version and PATH
- Best for: Immediate access while maintaining system-wide changes

**`--local-only`** (Exclusive):  
- Creates a local copy in the current directory only
- **Does NOT** update the global active version or PATH
- Best for: Project-specific versions, CI/CD environments, testing

### Usage Examples

```bash
# Traditional approach - updates PATH only
rgupdate use flyway --version 8.1.23

# Additive approach - updates PATH AND creates local copy
rgupdate use flyway --version 8.1.23 --local-copy

# Local-only approach - creates local copy without touching PATH
rgupdate use flyway --version 8.1.23 --local-only
```

### PATH Update Warnings

Whenever rgupdate modifies your PATH environment variable, you'll see clear warnings:

```
📋 Next Steps:
⚠️  PATH has been updated. To use the new version:
   • Open a new terminal/command prompt, OR
   • Restart your current terminal session

💡 Local copy created. You can now use:
   ./flyway.exe [command] [options]

   This local copy works immediately without PATH changes.

🔧 Environment: Windows, cmd, Administrator
```

**Why PATH Updates Require Terminal Restart:**
- Environment variables are inherited when a terminal starts
- Existing terminals don't automatically pick up PATH changes
- Local copies bypass this limitation entirely

### Best Practices

**Use `--local-copy` when:**
- You want both immediate access and permanent system changes
- Working in development environments
- Need to ensure the version persists across projects

**Use `--local-only` when:**
- Working on specific projects with version requirements
- In CI/CD pipelines where you don't want to affect the system
- Testing different versions without permanent changes
- Working in environments where you can't modify PATH

## Building and Running

### Prerequisites
- .NET 8.0 SDK or later
- Windows or Linux operating system

### Build
```bash
dotnet build
```

### Run
```bash
dotnet run -- [command] [arguments]
```

Or use the compiled executable:
```bash
# Windows
.\bin\Debug\net8.0\rgupdate.exe [command] [arguments]

# Linux
./bin/Debug/net8.0/rgupdate [command] [arguments]
```

## Development

The application is built using:
- **.NET 9.0**: For cross-platform compatibility
- **System.CommandLine**: For modern CLI parsing and help generation
- **YamlDotNet**: For YAML serialization in structured output
- **Async/await**: For responsive command execution

### Project Structure
```
rgupdate/
├── Program.cs          # Main application and command definitions
├── CommandHandlers.cs  # Shared command option definitions
├── ConfigService.cs    # Configuration management (install location)
├── ListingService.cs   # Version listing with structured output support
├── EnvironmentManager.cs # Environment and installation management
├── ValidationService.cs # Product validation logic
├── InfoService.cs      # System information display
├── InstallationService.cs # Product download and installation
├── ActivationService.cs # Version activation and PATH management
├── RemovalService.cs   # Version removal and cleanup
├── PathManager.cs      # Installation path management
├── ProductInfo.cs      # Product configuration and metadata
├── Constants.cs        # Application constants
├── rgupdate.csproj     # Project configuration
├── rgupdate.sln        # Solution file
└── README.md           # This file
```

## Limitations

- **Licensing**: rgupdate explicitly does NOT handle licensing in any way
- **Product Support**: Currently supports flyway, rgsubset, and rganonymize only
- **Path Management**: Requires appropriate permissions to modify system PATH variables

## Contributing

This is a Red Gate internal tool. Recent enhancements include:
- **Structured Output**: JSON/YAML output for automation (v1.1)
- **Configuration Management**: Custom install location support (v1.1)
- **Improved Error Handling**: Better permission and path management

Future enhancements may include:
- Additional product support (sqlcompare, etc.)
- Advanced version resolution
- Enhanced automation features
- Configuration file support

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
