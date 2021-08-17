# Janelia Basic Input/Output

## Summary

This package (org.janelia.io) provides basic support for reading messages from external devices.  Messages can be read over a socket using either UDP or TCP protocols, or over a serial port.  The code to read messages runs in separate threads, copying the data into ring buffers that can be read asynchronously by the main Unity thread.  The code works with byte arrays to avoid creating temporary strings (which would affect performance by triggering garbage collection), and there are utility functions to assist in extracting data from the byte arrays.

## Installation

Follow the [installation instructions in the main repository](https://github.com/JaneliaSciComp/janelia-unity-toolkit/blob/master/README.md#installation).

Note that Unity's `Api Compatibility Level` must be set to `.NET 4.x` in the project using this package (to support reading from serial ports):
* In the Unity editor's `Edit` menu, choose `Project Settings...` to raise the `Project Settings` window.
* In the `Player` tab, open the `Other Settings` bellow.
* In the `Configuration` section, set `API Compatibility Level` to `.NET 4.x`.

Without this change, `System.IO.Ports` will not be found, and the Unity editor will give errors like: `type or namespace name 'SerialPort' could not be found`.

The `org.janelia.package-installer` system for installing packages with dependencies attempts to detect this error and fix it automatically.

## Details

### Janelia.SocketReader

Reads from a socket, reusing a `byte[]` to avoid temporary `string` instances and garbage collection.  Reads asynchronously into a buffer, using a separate thread, with a `Take` function for obtaining unprocessed messages currently in the buffer.  Supports UDP (the default) and TCP.

### Janelia.SocketMessageReader

Uses `SocketReader` and adds a notion of "messages".  Messages can be delimited by a header character (e.g., the `'F'` character for [FicTrac](http://rjdmoore.net/fictrac/) messages) or a terminator character (e.g., a newline at the end of a string representing JSON).  The `GetNextMessage` function allows looping over all the messages that have been read and are waiting to be processed.  The `byte[]` content of each message can be parsed with functions from `IoUtilities`.

### Janelia.ExampleReadingSocket

A simple example that uses `SocketMessageReader` to update the position and rotation of a `GameObject` based on messages sent by `ExampleWritingSocket.py` over a socket.  Supports UDP or TCP, with messages in either JSON or an ad hoc format. The JSON version demonstrates how to use a type indicator in the JSON to pick the appropriate C# class type to use for deserialization; this approach works around a limitation of [Unity's JsonUtility class](https://docs.unity3d.com/Manual/JSONSerialization.html), that the resulting type of the deserialzation must be known when deserialization starts.

### ExampleWritingSocket.py

A simple Python script that sends messages to update the position and rotation of a `GameObject`.  Supports UDP or TCP, with messages in either JSON or an ad hoc format.

### Janelia.IoUtilities

A static class of utility functions.  Examples include functions to parse `long` or `double` values from the `byte[]` content of a message, without creating any temporary `string` instances that would trigger garbage collection.

### Janelia.SerialReader

Reads from a serial port, reusing a `byte[]` to avoid temporary `string` instances and garbage collection.  Reads asynchronously into a buffer, using a separate thread, with a `Take` function for obtaining unprocessed messages currently in the buffer.  The functions from `Janelia.IoUtilities` are useful to parsing values from the `byte[]` content of a message.

## Testing

To run this package's unit tests, use the following steps:
1. Create a new Unity project and add this package.
2. In the directory for the new project, in its `Packages` subdirectory, edit the `manifest.json` file to add a `"testables"` section as follows:
    ```
    {
      "dependencies": {
       ...
      },
      "testables": ["org.janelia.io"]
    }
    ```
    Note the comma separating the `"dependencies"` and `"testables"` sections.
3. In the Unity editor's "Window" menu, under "General", choose "Test Runner".
4. In the new "Test Runner" window, choose the "PlayMode" tab.
5. There should be an item for the new project, with items underneath it for "Janelia.Io.RuntimeTests.dll", etc.
6. Press the "Run All" button.
7. All the items under the new project will have green check marks if the tests succeed.
