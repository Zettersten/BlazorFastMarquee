#!/usr/bin/env bash
# Build script for BlazorFastMarquee
# Usage: ./build.sh [clean|restore|build|test|pack|all]

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
PROJECT="BlazorFastMarquee.csproj"
TEST_PROJECT="tests/BlazorFastMarquee.Tests/BlazorFastMarquee.Tests.csproj"
OUTPUT_DIR="./artifacts"
CONFIGURATION="${CONFIGURATION:-Release}"

# Functions
print_header() {
    echo -e "${BLUE}===================================================${NC}"
    echo -e "${BLUE}  $1${NC}"
    echo -e "${BLUE}===================================================${NC}"
}

print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

check_dotnet() {
    if ! command -v dotnet &> /dev/null; then
        print_error ".NET SDK not found. Please install .NET 9.0 or later."
        exit 1
    fi
    
    DOTNET_VERSION=$(dotnet --version)
    print_success ".NET SDK version: $DOTNET_VERSION"
}

clean() {
    print_header "Cleaning Build Artifacts"
    
    rm -rf "$OUTPUT_DIR"
    dotnet clean "$PROJECT" --configuration "$CONFIGURATION" --verbosity quiet
    
    if [ -f "$TEST_PROJECT" ]; then
        dotnet clean "$TEST_PROJECT" --configuration "$CONFIGURATION" --verbosity quiet
    fi
    
    print_success "Clean completed"
}

restore() {
    print_header "Restoring Dependencies"
    
    dotnet restore "$PROJECT"
    
    if [ -f "$TEST_PROJECT" ]; then
        dotnet restore "$TEST_PROJECT"
    fi
    
    print_success "Restore completed"
}

build() {
    print_header "Building Project ($CONFIGURATION)"
    
    dotnet build "$PROJECT" \
        --configuration "$CONFIGURATION" \
        --no-restore
    
    print_success "Build completed"
}

test() {
    print_header "Running Tests"
    
    if [ ! -f "$TEST_PROJECT" ]; then
        print_warning "Test project not found, skipping tests"
        return 0
    fi
    
    dotnet test "$TEST_PROJECT" \
        --configuration "$CONFIGURATION" \
        --no-build \
        --verbosity normal \
        --logger "console;verbosity=detailed"
    
    print_success "Tests completed"
}

pack() {
    print_header "Creating NuGet Package"
    
    mkdir -p "$OUTPUT_DIR"
    
    # Pack the project (MinVer automatically calculates version)
    dotnet pack "$PROJECT" \
        --configuration "$CONFIGURATION" \
        --no-build \
        --output "$OUTPUT_DIR" \
        -p:IncludeSymbols=true \
        -p:SymbolPackageFormat=snupkg
    
    # Extract and display version from package filename
    PACKAGE=$(ls "$OUTPUT_DIR"/*.nupkg 2>/dev/null | grep -v symbols | head -n 1)
    if [ -n "$PACKAGE" ]; then
        VERSION=$(basename "$PACKAGE" | sed 's/BlazorFastMarquee\.\(.*\)\.nupkg/\1/')
        print_success "Package version: $VERSION"
    fi
    
    print_success "Pack completed"
    
    # List packages
    echo ""
    echo -e "${BLUE}Created packages:${NC}"
    ls -lh "$OUTPUT_DIR"/*.nupkg 2>/dev/null || print_warning "No packages found"
}

validate_package() {
    print_header "Validating NuGet Package"
    
    # Basic validation - check package structure
    for pkg in "$OUTPUT_DIR"/*.nupkg; do
        if [[ "$pkg" != *".symbols.nupkg" ]] && [[ "$pkg" != *".snupkg" ]]; then
            echo "Validating: $(basename "$pkg")"
            # Check if package can be extracted
            if unzip -t "$pkg" > /dev/null 2>&1; then
                print_success "Package structure is valid"
            else
                print_error "Package structure validation failed"
                return 1
            fi
        fi
    done
    
    print_success "Validation completed"
}

list_contents() {
    print_header "Package Contents"
    
    for pkg in "$OUTPUT_DIR"/*.nupkg; do
        if [[ "$pkg" != *".symbols.nupkg" ]] && [[ "$pkg" != *".snupkg" ]]; then
            echo ""
            echo -e "${BLUE}=== $(basename "$pkg") ===${NC}"
            unzip -l "$pkg" | grep -E '\.(dll|razor|css|js|json|md|xml)$' || true
        fi
    done
}

show_usage() {
    cat << EOF
Usage: $0 [command]

Commands:
    clean       Clean build artifacts
    restore     Restore NuGet dependencies
    build       Build the project (default: Release)
    test        Run unit tests
    pack        Create NuGet package
    validate    Validate NuGet package
    list        List package contents
    all         Run all steps (clean, restore, build, test, pack)
    help        Show this help message

Environment Variables:
    CONFIGURATION   Build configuration (default: Release)

Examples:
    $0 all                      # Full build pipeline
    $0 build                    # Build only
    CONFIGURATION=Debug $0 all  # Debug build
EOF
}

# Main script
main() {
    check_dotnet
    
    case "${1:-all}" in
        clean)
            clean
            ;;
        restore)
            restore
            ;;
        build)
            restore
            build
            ;;
        test)
            test
            ;;
        pack)
            pack
            ;;
        validate)
            validate_package
            ;;
        list)
            list_contents
            ;;
        all)
            clean
            restore
            build
            test
            pack
            validate_package
            list_contents
            ;;
        help|--help|-h)
            show_usage
            exit 0
            ;;
        *)
            print_error "Unknown command: $1"
            echo ""
            show_usage
            exit 1
            ;;
    esac
    
    echo ""
    print_success "All operations completed successfully! 🎉"
}

# Run main
main "$@"
