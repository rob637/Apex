#!/bin/bash
# =============================================================================
# APEX CITADELS - C# Validation Script
# Run this before committing to catch compilation errors
# =============================================================================

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
UNITY_DIR="$SCRIPT_DIR/../unity/ApexCitadels"
CSPROJ_FILE="$UNITY_DIR/ApexCitadels.csproj"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "=========================================="
echo "  Apex Citadels - C# Validation"
echo "=========================================="

# Create csproj if it doesn't exist
if [ ! -f "$CSPROJ_FILE" ]; then
    echo -e "${YELLOW}Creating project file for validation...${NC}"
    cat > "$CSPROJ_FILE" << 'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <LangVersion>9.0</LangVersion>
    <Nullable>disable</Nullable>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <NoWarn>CS0649;CS0169;CS0414;NU1701</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="Assets/Scripts/**/*.cs" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Unity3D.SDK" Version="2021.1.14.1" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
  </ItemGroup>
</Project>
EOF
    cd "$UNITY_DIR" && dotnet restore --verbosity quiet 2>/dev/null
fi

echo "Building C# project..."
cd "$UNITY_DIR"

# Run build and capture output
BUILD_OUTPUT=$(dotnet build 2>&1 || true)

# Filter for actionable errors (exclude missing Unity types which we can't resolve)
# CS0234 = namespace doesn't exist
# CS0246 = type not found
# CS0103 = name doesn't exist (TMPro enums etc)
# These are Unity-specific and will be caught by Unity Editor

ACTIONABLE_ERRORS=$(echo "$BUILD_OUTPUT" | grep -E "error CS" | grep -v "CS0234\|CS0246\|CS0103" || true)

# Count errors
ERROR_COUNT=$(echo "$ACTIONABLE_ERRORS" | grep -c "error CS" 2>/dev/null || echo "0")

if [ "$ERROR_COUNT" -gt 0 ] 2>/dev/null && [ -n "$ACTIONABLE_ERRORS" ]; then
    echo ""
    echo -e "${RED}=========================================="
    echo "  VALIDATION FAILED - $ERROR_COUNT error(s) found"
    echo "==========================================${NC}"
    echo ""
    echo "$ACTIONABLE_ERRORS"
    echo ""
    echo -e "${RED}Please fix these errors before pushing.${NC}"
    exit 1
else
    echo ""
    echo -e "${GREEN}=========================================="
    echo "  VALIDATION PASSED"
    echo "==========================================${NC}"
    echo ""
    
    # Show Unity-specific warnings that Unity will catch
    UNITY_ERRORS=$(echo "$BUILD_OUTPUT" | grep -E "error CS" | grep -E "CS0234|CS0246|CS0103" | wc -l || echo "0")
    if [ "$UNITY_ERRORS" -gt 0 ]; then
        echo -e "${YELLOW}Note: $UNITY_ERRORS Unity-specific type errors will be validated by Unity Editor${NC}"
    fi
    
    exit 0
fi
