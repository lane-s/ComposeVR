# ComposeVR

The goal of this project is to provide a VR interface for Bitwig Studio (or any other DAW which provides an extensive API).

The project will support both Oculus and SteamVR, though it is currently being developed using only the Rift CV1.

There are currently no builds available for end users, but anyone who is interested can build the project with Unity.

ComposeVR depends the [ComposeVRExtension](https://github.com/lane-s/ComposeVRExtension) Bitwig extension. This extension should be installed before attempting to use the Unity application.

To configure the system from the Unity editor, find the local IP address of the machine which is running Bitwig. Set the HostIP field of the TCPClientController to this address. Set the port to TCP Port listed in Bitwig -> Settings -> Controllers -> ComposeVR. 

To pull in updates from submodules (i.e. changes to the protocol) use:

```git submodule foreach git pull origin master```

