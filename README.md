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
rgupdate use [product] [--version 1.2.3]
```

**Examples:**
```bash
rgupdate use flyway --version 8.1.23
rgupdate use rgsubset --version latest
```

### list
Lists available versions of a product, showing the most recent 10 versions by default, along with installation and active status.

```bash
rgupdate list [product] [--all]
```

**Examples:**
```bash
rgupdate list flyway
rgupdate list flyway --all
```

**Sample Output:**
```
Version         | Release date  | Status
----------------|---------------|--------
8.1.23          | 2025-07-04    | -
7.1.23          | 2025-06-04    | -
6.15.23         | 2025-05-04    | ACTIVE
6.1.23          | 2025-04-04    | -
5.1.23          | 2025-03-04    | installed
4.1.23          | 2025-02-04    | -
3.1.23          | 2025-01-04    | installed

78 older versions (To see all versions, run: rgupdate list flyway --all)
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
- **.NET 8.0**: For cross-platform compatibility
- **System.CommandLine**: For modern CLI parsing and help generation
- **Async/await**: For responsive command execution

### Project Structure
```
rgupdate/
├── Program.cs          # Main application and command definitions
├── rgupdate.csproj     # Project configuration
├── rgupdate.sln        # Solution file
└── README.md           # This file
```

## Limitations

- **Licensing**: rgupdate explicitly does NOT handle licensing in any way
- **Product Support**: Currently supports flyway, rgsubset, and rganonymize only
- **Path Management**: Requires appropriate permissions to modify system PATH variables

## Contributing

This is a Red Gate internal tool. Future enhancements may include:
- Additional product support (sqlcompare, etc.)
- Advanced version resolution
- Improved PATH management
- Configuration file support
