#!/bin/bash
#Usage: bash sync_public.sh

SOURCE_DIR="../Dungeon"
PUBLIC_DIR="../Dungeon/Dungeon_public/"

echo "Safely syncing code files from $SOURCE_DIR to $PUBLIC_DIR..."

# Define safe file extensions to sync
EXTENSIONS=("*.cs" "*.meta" "*.asset" "*.shader" "*.prefab" "*.mat" "*.controller")

for ext in "${EXTENSIONS[@]}"; do
    find "$SOURCE_DIR" -type f -name "$ext" ! -path "$PUBLIC_DIR/*" | while read -r src_file; do
        filename=$(basename "$src_file")
        match=$(find "$PUBLIC_DIR" -type f -name "$filename")

        if [[ -n "$match" ]]; then
            echo "Copying $filename → $match"
            cp "$src_file" "$match"
        fi
    done
done

echo "Safe sync complete."