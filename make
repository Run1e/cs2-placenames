#!/bin/bash

rm -rf build

for os in linux win; do
    echo "making for ${os}"
    dotnet publish --self-contained --configuration Release --os ${os} --arch x64 --output build
done

echo "cleaning up"
rm -rf bin obj