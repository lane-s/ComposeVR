#!/bin/bash
# Generates C# source files from .proto files.

# Location of the protocol buffer compiler
PROTOC=Packages/Google.Protobuf.Tools.3.5.1/tools/windows_x64/protoc

#Location of proto files
PDIR=ComposeVRProtocol

#Location of C# source
CDIR=Assets/ComposeVR/Scripts/Networking/Protocol

for f in $PDIR/*.proto; do
	$PROTOC "-I=$PDIR" "--csharp_out=$CDIR" "$f";
done

