#!/usr/bin/env bash
# Build script for BlazorFastMarquee Demo
# Usage: ./build-demo.sh [local|github]

set -e  # Exit on error

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
DEMO_PROJECT="BlazorFastMarquee.Demo.csproj"
OUTPUT_DIR="./bin/Release/net9.0/publish"

print_header() {
    echo -e "${BLUE}===================================================${NC}"
    echo -e "${BLUE}  $1${NC}"
    echo -e "${BLUE}===================================================${NC}"
}

print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

build_local() {
    print_header "Building Demo for Local Development"
    
    dotnet publish "$DEMO_PROJECT" \
        --configuration Release \
        -p:BaseHref="/"
    
    print_success "Local build completed"
    echo ""
    echo "To run locally:"
    echo "  cd $OUTPUT_DIR/wwwroot"
    echo "  python3 -m http.server 8080"
}

build_github() {
    print_header "Building Demo for GitHub Pages"
    
    dotnet publish "$DEMO_PROJECT" \
        --configuration Release \
        -p:BaseHref="/BlazorFastMarquee/"
    
    print_success "GitHub Pages build completed"
    echo ""
    echo "Published to: $OUTPUT_DIR"
    echo ""
    echo "Deploy to GitHub Pages:"
    echo "  1. Copy contents of $OUTPUT_DIR/wwwroot to your gh-pages branch"
    echo "  2. Or use a GitHub Action to deploy automatically"
}

show_usage() {
    cat << EOF
Usage: $0 [target]

Targets:
    local       Build for local development (base href: /)
    github      Build for GitHub Pages (base href: /BlazorFastMarquee/)

Examples:
    $0 local    # Build for local testing
    $0 github   # Build for GitHub Pages deployment

Note: The base href is set at build time using MSBuild properties.
EOF
}

# Main script
case "${1:-local}" in
    local)
        build_local
        ;;
    github)
        build_github
        ;;
    help|--help|-h)
        show_usage
        exit 0
        ;;
    *)
        echo "Unknown target: $1"
        echo ""
        show_usage
        exit 1
        ;;
esac
