#!/usr/bin/env bash
# Clone the upstream C++ Turbo Vision (magiblot/tvision) at the pinned commit into
# .upstream/tvision (git-ignored) so it can be read side by side with the port.
set -euo pipefail

COMMIT="b5fc1e50dcc2da062d72f43eb3662894d25aaf30"   # keep in sync with Directory.Build.props
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
DEST="$ROOT/.upstream/tvision"

if [ ! -d "$DEST/.git" ]; then
    git clone --quiet https://github.com/magiblot/tvision "$DEST"
fi
git -C "$DEST" fetch --quiet origin "$COMMIT" 2>/dev/null || git -C "$DEST" fetch --quiet origin
git -C "$DEST" -c advice.detachedHead=false checkout --quiet "$COMMIT"
echo "upstream tvision at $(git -C "$DEST" rev-parse --short HEAD) in $DEST"
