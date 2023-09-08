# Janelia Logging

## Summary

This package (org.janelia.logging) adds functionality for logging what happens during application execution.  The log is structured text in the [JSON](https://en.wikipedia.org/wiki/JSON) format, and the log file is stored in the same [place where Unity stores its own "player" log](https://docs.unity3d.com/Manual/LogFiles.html) (which includes, for example, the output of any `Debug.Log()` calls).  Each entry in the log includes details of when the entry was added: the current frame number (starting at 1) and the elapsed time (in seconds) since the application began running.  Logging starts automatically in any application using this package, and the log is written to the log file automatically when the application finishes or if the log reaches a maximum size.

Any script included in the application can add custom entries to the log.  See the section below for details.  For an example of adding to the log and reading from the log file, see the [`KinematicSubject`](https://github.com/JaneliaSciComp/janelia-unity-toolkit/blob/master/org.janelia.collision-handling/Runtime/KinematicSubject.cs) object in the package [org.janelia.collision-handling](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.collision-handling).

This package also creates a _launcher script_, which first presents a dialog box for configuring the application and then runs the application when the user closes the dialog. The launcher script is created in the main project directory and its name has the suffix "Launcher.hta".  The dialog appears on the "console" display (i.e., the display where the script is run, not the external displays where the application's content appear).  By default, the dialog contains a text input for adding "header notes" to be saved at the beginning of the log.  Optionally, other packages can add more user interface to the launcher with the `Logger.AddLauncherRadioButtonPlugin` and `Logger.AddLauncherOtherPlugin` functions.

There is an example of this plugin usage in the [`ExampleKinematicSubject`](https://github.com/JaneliaSciComp/janelia-unity-toolkit/blob/master/org.janelia.collision-handling/Runtime/ExampleKinematicSubject.cs) class from the [org.janelia.collision-handling](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.collision-handling) package.  The launcher script is implemented as a Microsoft ["HTML Application"](https://en.wikipedia.org/wiki/HTML_Application) ("HTA") using JScript (Javascript) and HTML.  The advantage is that this implementation runs on any modern Windows system without the installation of any additional software.  (If double-clicking on the launcher script raises a dialog asking how to run the script, choose "Microsoft (R) HTML Application host" and check the box to use the choice in the future.)  The disadvantage is that at least currently, there is no implementation on other platforms.

The launcher dialog also has a checkbox to enable the saving of rendered frames, with additional controls for several options:
* include frame numbers
* save only every _n_-th frame
* downsample saved frames to the specified height (and width preserving aspect ratio)

The frames for each session are stored in their own subdirectory of the log directory.  Saving frames reduces performance, so it is most useful during the playback of the log file from an earlier session, as imp
lemented by the [`KinematicSubject`](https://github.com/JaneliaSciComp/janelia-unity-toolkit/blob/master/org.janelia.collision-handling/Runtime/KinematicSubject.cs) object in [org.janelia.collision-handling](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.collision-handling).

## Installation

Follow the [installation instructions in the main repository](https://github.com/JaneliaSciComp/janelia-unity-toolkit/blob/master/README.md#installation).

## Details

### Adding a Custom Log Entry

Use the following steps to make a script add a custom entry to the log:

1. Add to the script a C# class that derives from `Janelia.Loggger.Entry`, like the following:
```csharp
using System;
using UnityEngine;
public class Example : MonoBehaviour
{
    ...
    [Serializable]
    private class ExampleLogEntry : Janelia.Logger.Entry
    {
        public int exampleInteger;
        public Vector3 exampleVector;
    };
}
```
2. The `Janelia.Loggger.Entry` base class adds `frame` and `timeSecs` fields, so there is no need for the custom entry to include the current frame or elapsed time.
3. Note that the class must be tagged with the [`[Serializable]` attribute](https://docs.unity3d.com/ScriptReference/Serializable.html), and adding that attribute requires `using System;`.
4. Note that the class for the log entry may have (and probably should have) the `private` projection level, but its individual fields must be `public`.
5. While not strictly necessary, it is a good practice to add to the script an instance of this new class, to be reused each time a log entry is to be added:
```csharp
public class Example : MonoBehaviour
{
    ...
    private ExampleLogEntry _currentLogEntry = new ExampleLogEntry();
}
```
This practice reduces memory allocation associated with logging, and thus reduces [garbage collection, which can affect application performance](https://docs.unity3d.com/Manual/UnderstandingAutomaticMemoryManagement.html).
6. Add each entry to the log as the application runs with a call to `Janelia.Logger.Log()`:
```csharp
public class Example : MonoBehaviour
{
    ...
    public void Update()
    {
      ...
      _currentLogEntry.exampleInteger = 1234;
      _currentLogFile.exampleVector.Set(1.2, 3.4, 5.6);
      Janelia.Logger.Log(_currentLogEntry);
    }
}
```
7. Remember that logging starts automatically when the application starts, and the log file is written automatically, so there is no need to add code to trigger this activity.
8. An entry in the JSON log file will appear as follows:
```json
[
  ...
  {
      "timeSecs": 0.12810839712619782,
      "frame": 10.0,
      "exampleInteger": 1234,
      "exampleVector": {
          "x": 1.2,
          "y": 3.4,
          "z": 5.6
      }
  },
  ...
]
```

### `Janelia.LogOptions`

By default, installing this package makes logging run automatically, without the need for adding any script to any Unity `GameObject`.  Optionally, the `Janelia.LogOptions` component can be added to a `GameObject` to give additional controls over logging:

- `EnableLogging` [default: `true`]: when `false`, all logging is disabled.
- `LogTotalMemory` [defaut: `false`]: when `true`, the value of `System.GC.GetTotalMemory(false)` is logged at each frame.

### `Janelia.SaveFrames`

This class adds the functionality for saving rendered frames.  It is a static class, so there is no need to manually add it to a scene.  It initiates a [coroutine](https://docs.unity3d.com/Manual/Coroutines.html) for saving the frames when a stand-alone executable is run with the `-saveFrames` commandline option.

The saving of frames can be tuned with several commandline options:

* `-saveFrames N`: save only every _N_-th frame.  If _N_ is omitted, then every frame is saved.

* `-numbers`: include the frame number in red letters in the bottom-left corner of each saved image.

* `-height H`: rescale each saved image to have height _H_ (and width chosen to preserve the original aspect ratio).

* `-output F`: write the saved frames to the folder, _F_, which can be relative to the directory where the executable is launched, or an absolute path.  If this option is omitted, then frames will be saved in the standard log folder, in a subfolder `Frames_D`, where _D_ is the current date and time.

It is particularly useful to save the frames when playing back a log file, and remember that the log file, _L_, to play is specified by the `-playback L` commandline option (as implemented in the 
 [`KinematicSubject`](https://github.com/JaneliaSciComp/janelia-unity-toolkit/blob/master/org.janelia.collision-handling/Runtime/KinematicSubject.cs) object in the package [org.janelia.collision-handling](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.collision-handling) package).

 ### `simplify.py`

 This Python script can be used to post process a log file, making it simpler by combining entries having the same time stamp (i.e., frame).  For example, say the original log file is the following:
 ```json
[
{
    "timeSecs": 0.88304,
    "frame": 100.0,
    "timeSecsAfterSplash": 0.88304,
    "frameAfterSplash": 100.0,
    "a": 1
},
{
    "timeSecs": 0.88304,
    "frame": 100.0,
    "timeSecsAfterSplash": 0.88304,
    "frameAfterSplash": 100.0,
    "b": 1.0
},
{
    "timeSecs": 0.88304,
    "frame": 100.0,
    "timeSecsAfterSplash": 0.88304,
    "frameAfterSplash": 100.0,
    "c": {
        "x": 1.0,
        "y": 1.1,
        "z": 1.2
    }
},
{
    "timeSecs": 0.88612,
    "frame": 101.0,
    "timeSecsAfterSplash": 0.88612,
    "frameAfterSplash": 101.0,
    "a": 2
}
]
 ```
The simplified version would be:
 ```json
[
{
    "timeSecs": 0.88304,
    "frame": 100.0,
    "timeSecsAfterSplash": 0.88304,
    "frameAfterSplash": 100.0,
    "a": 1,
    "b": 1.0,
    "c": {
        "x": 1.0,
        "y": 1.1,
        "z": 1.2
    }
},
{
    "timeSecs": 0.88612,
    "frame": 101.0,
    "timeSecsAfterSplash": 0.88612,
    "frameAfterSplash": 101.0,
    "a": 2
}
]
 ```