#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"

echo "=== Building Extension ==="
cd "$REPO_ROOT/extension"
if [ -f package.json ]; then
  npm ci
  npm run build
else
  echo "  (no package.json yet — skipping)"
fi

echo ""
echo "=== Building .NET Agent ==="
cd "$REPO_ROOT/agent"
if ls *.sln 1>/dev/null 2>&1; then
  dotnet build --configuration Release
  dotnet test --configuration Release --no-build
else
  echo "  (no .sln yet — skipping)"
fi

echo ""
echo "=== Build complete ==="
