#!/bin/bash
set -e
set -u
set -x
 
dotnet restore
dotnet build --no-restore
dotnet test --no-restore --no-build
