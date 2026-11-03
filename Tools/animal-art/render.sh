#!/bin/zsh
# Draws the lab's animals and renders every pose into Assets/_Project/Art/Animals with headless Chrome.
# Afterwards, return to Unity so it imports them. A new animal or pose also needs DigitalStory/Build Lab Scene,
# which links the pictures to the roster. Set OUT to render somewhere else, and CHROME if Chrome lives elsewhere.
set -e
HERE=${0:A:h}
REPO=${HERE:h:h}
OUT=${OUT:-"$REPO/Assets/_Project/Art/Animals"}
CHROME=${CHROME:-"/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"}
WORK=$(mktemp -d)
trap 'rm -rf "$WORK"' EXIT

mkdir -p "$OUT"
python3 -I "$HERE/draw.py" "$WORK/html"

for page in "$WORK"/html/*.html; do
  "$CHROME" --headless=new --disable-gpu --hide-scrollbars --user-data-dir="$WORK/profile" \
    --force-device-scale-factor=1 --default-background-color=00000000 --window-size=512,512 \
    --screenshot="$OUT/${page:t:r}.png" "file://$page" >/dev/null 2>&1
done

echo "rendered $(ls "$WORK"/html | wc -l | tr -d ' ') pictures into $OUT"
