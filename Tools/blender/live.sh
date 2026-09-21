#!/bin/zsh
# Opens the Gotchi cat in a live Blender window that Claude Code drives through the "MCP for Blender" add-on.
#
#   Tools/blender/live.sh              # rebuild the cat from build_cat2.py into /tmp/gotchi-cat2/cat2.blend, then open it
#   Tools/blender/live.sh path/to.blend # open an existing .blend instead (e.g. a hand-edited one)
#
# The add-on (installed in ~/Library/Application Support/Blender/5.2/scripts/addons/blender_mcp.py) auto-starts its
# socket server on localhost:9876 when Blender loads; Claude Code reaches it through the "blender" MCP server registered
# for this repo (`claude mcp list`). Sidebar: press N in the 3D viewport → "MCP for Blender" tab.
set -euo pipefail
BLENDER=/Applications/Blender.app/Contents/MacOS/Blender
ROOT=$(cd "$(dirname "$0")/../.." && pwd)

if [[ $# -ge 1 ]]; then
  BLEND=$(cd "$(dirname "$1")" && pwd)/$(basename "$1")
else
  OUT=/tmp/gotchi-cat2
  "$BLENDER" -b -P "$ROOT/Tools/blender/build_cat2.py" -- --preview "$OUT" --blend "$OUT/cat2.blend" | grep '^\[build_cat2\]'
  BLEND="$OUT/cat2.blend"
fi

if nc -z localhost 9876 2>/dev/null; then
  echo "note: something already listens on localhost:9876 (another Blender with the add-on?) — the new window will skip auto-start."
fi
open -n -a Blender --args "$BLEND"
echo "Opened $BLEND in a new Blender window; MCP server on localhost:9876 starts automatically."
echo "In Claude Code (this repo), ask for a viewport screenshot to confirm the link."
