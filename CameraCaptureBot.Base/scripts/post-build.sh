#!/bin/bash
LIB_PATH=$OUTPUT_PATH/lib

rm $LIB_PATH/*.so		# remove soft link
rm -r $LIB_PATH/pkgconfig	# remove pkgconfig

for file in $LIB_PATH/*.so.*; do
    if [ -L "$file" ]; then
        target=$(readlink "$file")
        rm "$file"
        mv "$LIB_PATH/$target" "$file"
    fi
done
