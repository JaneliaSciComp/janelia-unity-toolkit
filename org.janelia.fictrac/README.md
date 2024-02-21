# Janelia FicTrac Support

## Summary

This package (org.janelia.fictrac) supports the use in Unity of the FicTrac system ([http://rjdmoore.net/fictrac/](http://rjdmoore.net/fictrac/)), a "webcam-based method for tracking spherical motion and generating fictive animal paths."  Since this package provides a basic interface for reading from FicTrac (via a socket), with only one dependency, the [org.janelia.io package](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.io), it should be useful in a variety of Unity applications.  But for use with other packages in [janelia-unity-toolkit](https://github.com/JaneliaSciComp/janelia-unity-toolkit), like the [package for collision handling](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.collision-handling), it may be convenient to use the higher-level [org.janelia.fictrac-collision package](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.fictrac-collision).

## Installation

Follow the [installation instructions in the main repository](https://github.com/JaneliaSciComp/janelia-unity-toolkit/blob/master/README.md#installation).

## Details

### `Janelia.ExampleUsingFicTrac`

A simple `MonoBehaviour` that reads FicTrac messages from a socket and updates the translation and rotation of the `Transform`.  Uses `Janelia.FicTracReader` to read and parse the messages.

### `Janelia.FicTracReader`

Provides the `GetNextMessage` function to copy the next message from FicTrac into a `FicTracReader.Message` struct.  The fields of this struct are filled without creating temporary `string` instances that would trigger garbage collection, using functions of the `IoUtilities` class from the [org.janelia.io package](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.io).  Messages waiting to be retrieved are stored in a ring buffer, to allow a client to consume messages at its own rate.  The buffer is filled by `Janelia.SocketReader` from the [org.janelia.io package](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.io), which reads from a socket asynchronously in a separate thread.

### `FakeTrac.py`

A simple Python script that sends messages in the FicTrac format over a socket, for testing how a Unity application responds to the messages.  Supports UDP and TCP, with UDP being the default to match the real FicTrac code as of late 2020.  Note that with UDP, this scripts starts sending messages immediately (without waiting for a connection, since connections are a TCP concept), so be sure to start the message-receiving application (game) before starting this script.
