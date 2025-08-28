#!/bin/bash
#
# Initialize PerSpec Framework as a separate git repository
# This script sets up the package folder as an independent git repo
# that can be used as a git submodule or installed via Unity Package Manager
#
# Usage: ./init-package-repo.sh [remote-url]
# Example: ./init-package-repo.sh https://github.com/yourusername/perspec-framework.git

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Get the directory of this script
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
cd "$SCRIPT_DIR"

echo -e "${GREEN}=== PerSpec Framework Package Repository Setup ===${NC}"
echo ""

# Check if already a git repository
if [ -d ".git" ]; then
    echo -e "${YELLOW}Warning: This directory is already a git repository${NC}"
    read -p "Do you want to reinitialize? This will lose git history! (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo -e "${RED}Aborted${NC}"
        exit 1
    fi
    rm -rf .git
fi

# Initialize git repository
echo -e "${GREEN}Initializing git repository...${NC}"
git init

# Configure git for this repository
git config core.autocrlf input
git config core.safecrlf warn

# Add git attributes for proper line ending handling
if [ ! -f ".gitattributes" ]; then
    echo -e "${YELLOW}.gitattributes not found, please create it${NC}"
fi

# Add git ignore
if [ ! -f ".gitignore" ]; then
    echo -e "${YELLOW}.gitignore not found, please create it${NC}"
fi

# Stage all files
echo -e "${GREEN}Adding package files...${NC}"
git add .

# Create initial commit
echo -e "${GREEN}Creating initial commit...${NC}"
git commit -m "Initial commit: PerSpec Framework v1.0.0

- Unity TDD framework with UniTask async patterns
- DOTS/ECS testing support
- SQLite-based test coordination
- Intelligent console log capture
- Background test polling
- Comprehensive test execution management"

# Set up main branch
git branch -M main

# Add remote if provided
if [ ! -z "$1" ]; then
    echo -e "${GREEN}Adding remote origin: $1${NC}"
    git remote add origin "$1"
    
    echo -e "${GREEN}Repository initialized successfully!${NC}"
    echo ""
    echo -e "To push to remote repository, run:"
    echo -e "  ${YELLOW}git push -u origin main${NC}"
else
    echo -e "${GREEN}Repository initialized locally!${NC}"
    echo ""
    echo -e "To add a remote repository later, run:"
    echo -e "  ${YELLOW}git remote add origin https://github.com/yourusername/perspec-framework.git${NC}"
    echo -e "  ${YELLOW}git push -u origin main${NC}"
fi

echo ""
echo -e "${GREEN}=== Setup Complete ===${NC}"
echo ""
echo "Next steps:"
echo "1. Create a repository on GitHub/GitLab/Bitbucket"
echo "2. Push this package to the remote repository"
echo "3. Update the parent project's .gitmodules with the correct URL"
echo "4. In the parent project, run:"
echo "   git submodule add <repository-url> Packages/com.perspec.framework"
echo ""
echo "Users can then install your package via Unity Package Manager:"
echo "   Window > Package Manager > + > Add package from git URL..."
echo "   Enter: <repository-url>#main"