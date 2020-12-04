# Janelia FicTrac Support

## Summary

This package (org.janelia.fictrac) supports the use in Unity of the FicTrac system ([http://rjdmoore.net/fictrac/](http://rjdmoore.net/fictrac/)), a "webcam-based method for tracking spherical motion and generating fictive animal paths."  Since this package provides a basic interface for reading from FicTrac (via a socket), with no dependencies on other packages in [janelia-unity-toolkit](https://github.com/JaneliaSciComp/janelia-unity-toolkit), it should be useful in a variety of Unity applications.  But for use with other packages in [janelia-unity-toolkit](https://github.com/JaneliaSciComp/janelia-unity-toolkit), like the [package for collision handling](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.collision-handling), it may be convenient to use the higher-level
[org.janelia.fictrac-collision package](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.fictrac-collision).

## Installation

Follow the [installation instructions in the main repository](https://github.com/JaneliaSciComp/janelia-unity-toolkit/blob/master/README.md#installation).

## Details

### Janelia.ExampleUsingFicTrac

A simple `MonoBehaviour` that reads FicTrac messages from a socket and updates the translation and rotation of the `Transform`.  Uses `Janelia.FicTracReader` to obtain the messages and `Janelia.FicTracUtilities` to parse the messages.

### Janelia.FicTracReader

Reads FicTrac messages from a socket into a buffer, so the messages can be consumed by a client at its own rate.  Avoids creating temporary `string` instances that would trigger garbage collection, with an API asking the client to pass in a `byte[]` that will get a copy of the FicTrac message.  Uses `Janelia.SocketReader` to read from the socket asynchronously in a separate thread.

### Janelia.SocketReader

Reads from a socket, reusing a `byte[]` to avoid temporary `string` instances and garbage collection.  Reads asynchronously into a buffer, using a separate thread, with a `Take` function for obtaining all the messages currently in the buffer.  Supports UDP (the default) and TCP.

### Janelia.FicTracUtilities

A static class with functions to parse `long` and `double` values from FicTrac messages, without creating any temporary `string` instances that would trigger garbage collection.

### FakeTrac.py

A simple Python script that sends messages in the FicTrac format over a socket, for testing how a Unity application responds to the messages.  Supports UDP and TCP, with UDP being the default to match the real FicTrac code as of late 2020.  Note that with UDP, this scripts starts sending messages immediately (without waiting for a connection, since connections are a TCP concept), so be sure to start the message-receiving application (game) before starting this script.

## Testing

To run this package's unit tests, use the following steps:
1. Create a new Unity project and add this package.
2. In the directory for the new project, in its `Packages` subdirectory, edit the `manifest.json` file to add a `"testables"` section as follows:
    ```
    {
      "dependencies": {
       ...
      },
      "testables": ["org.janelia.fictrac"]
    }
    ```
    Note the comma separating the `"dependencies"` and `"testables"` sections.
3. In the Unity editor's "Window" menu, under "General", choose "Test Runner".
4. In the new "Test Runner" window, choose the "PlayMode" tab.
5. There should be an item for the new project, with items underneath it for "Janelia.Fictrac.RuntimeTests.dll", etc.
6. Press the "Run All" button.
7. All the items under the new project will have green check marks if the tests succeed.
