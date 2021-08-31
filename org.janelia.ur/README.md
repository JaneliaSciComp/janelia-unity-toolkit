# Janelia Universal Robots Support

## Summary

This package (org.janelia.ur) supports the use in Unity of [Univeral Robots](https://www.universal-robots.com) devices, like the [UR10e](https://www.universal-robots.com/products/ur10-robot/) arm.  In particular, this package supports communication with a UR controller (robot) using the [Real Time Data Exchange (RTDE)](https://www.universal-robots.com/articles/ur/interface-communication/real-time-data-exchange-rtde-guide/) interface. The communication occurs over a socket using TCP, as supported by the [org.janelia.io package](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.io).

## Installation

Follow the [installation instructions in the main repository](https://github.com/JaneliaSciComp/janelia-unity-toolkit/blob/master/README.md#installation).

## Details

### `fake-rtde-controller.py`

A Python script that simulates how a controller responds to a simple sequnce of RTDE commands.  The script successfully interacts with the `record.py` example client from the 
[RTDE guide](https://www.universal-robots.com/articles/ur/interface-communication/real-time-data-exchange-rtde-guide/).  In that example, the client checks the protocol and controller version numbers, sets up an "output recipe" of values (e.g., actual joint angles) to be sent from the controller to the client, and then requests the start of output sending.  The client then receives an ongoing sequence of output values at a specified rate.  The `fake-rtde-controller.py` script is a useful way of testing a RTDE client without using an actual UR device.

### `Janelia.ExampleUsingRtde`

A simple example of a RTDE client that runs in Unity.  This client sets up an "output recipe" involving only the actual joint angles of a robotic arm.  Note that multiple instances of this script can receive output from multiple robotic arms independently.  Uses the `Janelia.RtdeClient` class.

### `Janelia.RtdeClient`

Sets up the "output recipe" to receive actual joint angles from a robotic arm, and makes the angles available at each update step via a `GetNextMessage` function.  Uses `Janelia.SocketReader` from the [org.janelia.io package](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.io).