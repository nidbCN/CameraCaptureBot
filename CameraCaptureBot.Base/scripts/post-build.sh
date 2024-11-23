#!/bin/bash
rm $BUILD_DIR/lib/*.so
rm -r $BUILD_DIR/lib/pkgconfig

for file in $BUILD_DIR/lib/*.so.*; do
    if [ -L "$file" ]; then
        target=$(readlink "$file")
        rm "$file"
        mv "$BUILD_DIR/lib/$target" "$file"
    fi
done
