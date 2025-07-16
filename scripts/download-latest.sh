#!/bin/bash
# Download latest rgupdate release for Linux/macOS
# Usage: ./download-latest.sh [output-directory]

set -e

OUTPUT_DIR="${1:-.}"
REPO_URL="https://github.com/alex-yates-redgate/rgupdate"
DOWNLOAD_URL="$REPO_URL/releases/latest/download/rgupdate-linux"
OUTPUT_PATH="$OUTPUT_DIR/rgupdate"

show_help() {
    cat << EOF
Download Latest rgupdate Release

Usage: 
    ./download-latest.sh [OutputDir]

Parameters:
    OutputDir    Directory to save the downloaded executable (default: current directory)
    -h, --help   Show this help message

Examples:
    ./download-latest.sh                    # Download to current directory
    ./download-latest.sh /usr/local/bin     # Download to /usr/local/bin
    ./download-latest.sh --help             # Show this help
EOF
}

if [[ "$1" == "-h" || "$1" == "--help" ]]; then
    show_help
    exit 0
fi

echo "🔄 Downloading latest rgupdate release..."
echo "   From: $DOWNLOAD_URL"
echo "   To:   $OUTPUT_PATH"

# Create output directory if it doesn't exist
mkdir -p "$(dirname "$OUTPUT_PATH")"

# Download the file
if command -v curl >/dev/null 2>&1; then
    curl -L -o "$OUTPUT_PATH" "$DOWNLOAD_URL"
elif command -v wget >/dev/null 2>&1; then
    wget -O "$OUTPUT_PATH" "$DOWNLOAD_URL"
else
    echo "❌ Error: Neither curl nor wget is available"
    echo "💡 Please install curl or wget, or download manually from:"
    echo "   $REPO_URL/releases/latest"
    exit 1
fi

# Make executable
chmod +x "$OUTPUT_PATH"

echo "✅ Download completed successfully!"
echo "   Saved to: $OUTPUT_PATH"

# Show file info
if [[ -f "$OUTPUT_PATH" ]]; then
    SIZE=$(du -h "$OUTPUT_PATH" | cut -f1)
    echo "   Size: $SIZE"
fi

echo ""
echo "🚀 You can now run rgupdate:"
echo "   $OUTPUT_PATH --help"

# If downloaded to a directory in PATH, mention it
if [[ ":$PATH:" == *":$(dirname "$OUTPUT_PATH"):"* ]]; then
    echo ""
    echo "💡 Since $(dirname "$OUTPUT_PATH") is in your PATH, you can also run:"
    echo "   rgupdate --help"
fi
