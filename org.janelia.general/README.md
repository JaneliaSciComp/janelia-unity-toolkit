# Janelia General

## Summary

This package (`org.janelia.general`) contains some useful code not related to the topics supported by other packages.


## Installation

Follow the [installation instructions in the main repository](https://github.com/JaneliaSciComp/janelia-unity-toolkit/blob/master/README.md#installation).

## Details

### `Janelia.SessionParameters`

A static class supporting parameters that might need to be changed for different runs of a standalone application.  The user can change these parameters at the start of a run without rebuilding the application.

Simply adding the `org.janelia.general` package to a project causes the building of the project to generate a launcher script (see the [`org.janelia.logging` package](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.logging)).  Running the launcher script shows a simple user interface before running the standalone application.  The user interface has a "Session parameters" section at the bottom, for changing the values of parameters to be used for the upcoming run of the application.  An example of such a parameter is the timeout period after which the application automatically ends, added by the `Janelia.Timeout` class.  The "Session parameters" section is an editable block of text listing the parameters, like the following:
```
{
  "timeoutSecs": 0
}
```
Editing this text updates a [JSON](https://en.wikipedia.org/wiki/JSON) file, stored in the [place where Unity stores its own "player" log](https://docs.unity3d.com/Manual/LogFiles.html).  The `SessionParameters` class loads this JSON file at the start of each application run, and makes the parameter values available to scripts through `GetFloatParameter` and `GetStringParameter` methods.

It is simple for scripts to add custom session parameters.  See the `Janelia.Timeout` class for an example.

### `Janelia.Timeout`

A static class that adds a session parameter for a timeout period.  When this period ends, a standalone application automatically ends itself.  An automatic ending of this sort can be useful to limit an animal study.  The parameter is named `timeoutSecs`, and its value should be in seconds; a value of `0` (or less) means there is no timeout and the application will continue running until ended manually.

Running the application from a shell with the command-line argument `-timeoutSecs N` overrides the `"timeoutSecs"` session parameter.  For example, if the application is run once from the launcher script with `"timeoutSecs": 10` as a session parameter, then that timeout value is used when running the application from a shell with no command-line arguments.  But running from the shell with `-timeoutSecs 0` as command-line arguments means there is no timeout and the application will continue running until ended manually.

### `Janelia.DistanceTeleporter`

A `GameObject` with this component will teleport to a new position whenever the object moves to more than a specified distance from designated location.

The component's `thresholdDistance` field specifies the distance. This value can be overridden at the start of a session with a session parameter named `"teleportAtDistance"`.

The component's `distanceFrom` field is another `GameObject` (which can be an "empty" serving only to mark a position), and teleportation is triggered when the distance between the two `GameObjects` exceeds `thresholdDistance` (or `"teleportAtDistance"`).

When teleporting is triggered, the component's `GameObject` moves to the position and orientation of its `teleportTo` field, another `GameObject` (which, again, can be an "empty").