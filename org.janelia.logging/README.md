# Janelia Logging

## Summary

This package (org.janelia.logging) adds functionality for logging what happens during application execution.  The log is structured text in the [JSON](https://en.wikipedia.org/wiki/JSON) format, and the log file is stored in the same [place where Unity stores its own "player" log](https://docs.unity3d.com/Manual/LogFiles.html) (which includes, for example, the output of any `Debug.Log()` calls).  Each entry in the log includes details of when the entry was added: the current frame number (starting at 1) and the elapsed time (in seconds) since the application began running.  Logging starts automatically in any application using this package, and the log is written to the log file automatically when the application finishes or if the log reaches a maximum size.

Any script included in the application can add custom entries to the log.  See the section below for details.  For an example of adding to the log and reading from the log file, see the `KinematicSubject` object in the package [org.janelia.collision-handling](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.collision-handling).

## Installation

Follow the [installation instructions in the main repository](https://github.com/JaneliaSciComp/janelia-unity-toolkit/blob/master/README.md#installation).

## Details

### Adding a Custom Log Entry

Use the following steps to make a script add a custom entry to the log:

1. Add to the script a C# class that derives from `Janelia.Loggger.Entry`, like the following:
```csharp
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
3. Note that the class must be tagged with the [`[Serializable]` attribute](https://docs.unity3d.com/ScriptReference/Serializable.html).
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

### Janelia.LogOptions

By default, installing this package makes logging run automatically, without the need for adding any script to any Unity `GameObject`.  Optionally, the `Janelia.LogOptions` component can be added to a `GameObject` to give additional controls over logging:

- `EnableLogging` [default: `true`]: when `false`, all logging is disabled.
- `LogTotalMemory` [defaut: `false`]: when `true`, the value of `System.GC.GetTotalMemory(false)` is logged at each frame.
