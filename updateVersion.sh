#!/bin/bash

echo "new_release_version=$1" >> "$GITHUB_OUTPUT"
sed -i '' "s#<Version>.*#<Version>$1</Version>#" "$2"
