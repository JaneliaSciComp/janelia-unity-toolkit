# Janelia Logging

## Summary
This package (org.janelia.logging) adds some functionality for logging what happens during application execution.

For an example of writing to and reading from the log, see the `KinematicSubject` object in the package [org.janelia.collision-handling](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.collision-handling).

## Installation
Follow the [installation instructions in the main repository](https://github.com/JaneliaSciComp/janelia-unity-toolkit/blob/master/README.md#installation).

## Details

### Janelia.LogOptions

By default, installing this package makes logging run automatically, without the need for adding any script to any Unity `GameObject`.  Optionally, the `Janelia.LogOptions` component can be added to a `GameObject` to give additional controls over logging:

- `EnableLogging` [default: `true`]: when `false`, all logging is disabled.
- `LogTotalMemory` [defaut: `false`]: when `true`, the value of `System.GC.GetTotalMemory(false)` is logged at each frame.
