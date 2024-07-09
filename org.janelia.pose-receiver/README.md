# Janelia Pose Receiver

## Summary

Receives poses (positions, and orientations expressed as Euler angles) for designated objects via UDP packets.

## Installation

Follow the [installation instructions in the main repository](https://github.com/JaneliaSciComp/janelia-unity-toolkit/blob/master/README.md#installation).

## Usage

The `Janelia.PoseReceiver` component acts as a controller, receiving packets and applying the poses in those packets to the `GameObject`s listed in its `controlled` array.

To receive and apply poses:
1. Create the objects whose poses should be received. Say there are two, `Subject` and `Object`.
1. Add the `Janelia.PoseReceiver` component to some `GameObject`.  It can be an empty object.
1. In the `Janelia.PoseReceiver`'s `controlled` field, set the first element to `Subject` and the second to `Object`.  With this configuration, packets with ID 0 control `Subject` and packets with ID 1 control `Object`.  By default, `controlled` has two elements, but it can be reset to have more.  In the Unity editor's Inspector, change the number to the right of the "Controlled" item (with the triangle button to expand the list of elements).  In a script, assign to `controlled` a `new GameObject[n]` where `n` is the desired element count.
1. Set the `Janelia.PoseReceiver`'s `scale` field to convert the received positions from normalized coordinates (-1 to 1) to the desired coordinates.
1. Set the `Janelia.PoseReceiver`'s `angleOffsetDegs` field to offset the received Euler angles if desired (e.g., perhaps add 90 to the _Y_ angle to make _Z_ the forward direction).
1. Press play.
1. The `Janelia.PoseReceiver` component's `Update` function automatically applies poses that it receives.

## Details

_Incomplete_

### UDP Packets

Packets should be UTF-8 encoded strings of the following format:
```
P,t,i,px,py,pz,ex,ey,ez,
```
* The literal `P` is the header.
* _t_ is a timestamp, expected to be milliseconds since the epoch
* _i_ is the ID of the object being posed, from 0 to _N_ - 1
* _px_, _py_, _pz_ is the 3D position, in normalized coordinates between -1 and 1
* _ex_, _ey_, _ez_ are the Euler angles, in radians

### Logging

Packets are logged with the [org.janelia.logging](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.logging) package:
```json
{
    "timeSecs": 0.20404289662837983,
    "frame": 10.0,
    "timeSecsAfterSplash": 0.20404289662837983,
    "frameAfterSplash": 9.0,
    "poseReceiverId": 0,
    "sentTimestampMs": 1720532217861,
    "processedTimestampMs": 1720532217865,
    "position": {
        "x": 0.22028809785842896,
        "y": 0.0,
        "z": -0.2619714140892029
    },
    "eulerAngles": {
        "x": 0.0,
        "y": -1.1811800003051758,
        "z": 0.0
    }
}
```
(To find such entries in the log file, search for `poseReceiver`.)