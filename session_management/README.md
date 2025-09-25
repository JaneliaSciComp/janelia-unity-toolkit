# Janelia Session Manager


## Summary

This Python script runs an experiment consisting of a number of sessions, with each session involving the running of a stand-alone executable built with Unity.  The details of the sessions are described in a "spec" file in the [JSON format](https://en.wikipedia.org/wiki/JSON).

## Installation

The Python script itself has no dependencies and requires no special installation (assuming that Python is installed).  It does assume that the stand-alone executables built with Unity that it is running have certain capabilities, though, that require the [org.janelia.logging package](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.logging) and the [org.janelia.general package](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.general).  To install those packages, follow the [installation instructions in the main repository](https://github.com/JaneliaSciComp/janelia-unity-toolkit/blob/master/README.md#installation).

## Usage

To run an experiment described by the file `experiment.json`:
```
python session-manager.py --input-paradigm experiment.json
```
The `--input-paradigm` argument can be abbreviated as `-ip`.

To make trial-specific additions to the log filename and/or header:
```
python session-manager.py --input-paradigm experiment.json --input-trial trial.json
```
The `--input-trial` argument can be abbreviated as `-it`.

To start in the middle of an experiment, at session 3 for example (where the first experiment is 1):
```
python session-manager.py -ip experiment.json --start 3
```
The `--start` argument can be abbreviated as `-s`.

To print what would be done in an experiment without actually doing it:
```
python session-manager.py -ip experiment.json --dry-run
```

## JSON Spec File

Here is an example of the JSON file describing a simple three-session experiment:
```json
{
    // The executable is a shortcut (link), as produced by the
    // AdjoiningDisplaysCamera in org.janelia.camera-utilities.
    "exe": "C:/Users/scientist/SessionA/unity-standalone.lnk",
    "logDir": "C:/Users/scientist/AppData/LocalLow/institution/SessionA",
    // Specifies the session length.
    "sessionParams": {
        "timeoutSecs": 120
    },
    // Pause between sessions.
    "pauseSecs": 5,
    "sessions": [
        {
            // Suffix for the log file name.
            "logFilenameExtra": "_session1.1",
            // Extra text at the start of the log file.
            "logHeader": "First experiment, first session."
        },
        {
            "log_filename_extra": "_session1.2",
            "logHeader": "First experiment, second session: longer initial delay.",
            "pauseSecs": 10
        },
        {
            "logFilenameExtra": "_session1.3",
            "logHeader": "First experiment, third session: uses a different executable.",
            "exe": "C:/Users/scientist/SessionB/unity-standalone.lnk",
            // The log directory will be different, too.
            "logDir": "C:/Users/scientist/AppData/LocalLow/institution/SessionB"
        }
    ]
}
```

The general structure of the file is:
* a group of zero or more "global" parameters that apply in all sessions unless overridden in a particular session; 
* a key `"sessions"` whose value is a list of objects, one for each session, with optional parmeters specific to that session.

In the example, above, there is a global parameter `"exe"` that is overridden in the third session.

The supported parameters are:
* `"exe"` [required]: The path to the stand-alone executable built with Unity.  For the other parameters to work, this executable's project must include the [org.janelia.logging package](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.logging) and the [org.logging.general package](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.general). The executable can be a link (e.g., on Windows, a "shortcut" file with the extension `.lnk`, though that extension is often hidden), like the one produced when using the `AdjoiningDisplaysCamera` class from the [org.janelia.camera-utilities package](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.camera-utilities).
* `"logDir"` [required]: The log directory corresponding to the executable.  For now, at least, this parameter cannot be used to _change_ the log directory that the executable has been built to use; this parameter merely makes that directory known to the session manager script.
* `"pauseSecs"` [optional, default: 0]: The number of seconds to pause before starting a session.
* `"sessionParams"` [optional, default: `{}`]: A JSON object of special parameters to use in the session, as implemented by the [org.janelia.general package](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.general).
  - `"timeoutSecs"` [optional, default: unlimited]: probably the most useful session parameter, specifying the length of time for the session.
* `"logFilenameExtra"` [optional, default: `""`]: An extra string to go at the end of the log file name (e.g., a standard log file name would be something like `Log_2022-09-15_14-28-54.json`, and `"logFilenameExtra": "_extra"` would change it to `Log_2022-09-15_14-28-54_extra.json`).
* `"logHeader"` [optional, default: `""`]: Text to go in a special header record at the start of the log, as described in the [org.janelia.logging package](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.logging).

The keys for the parameters are "normalized" when the JSON file is read, meaning they are converted to lowercase and underscore characters (i.e., `_`) are removed, and only first part is tested for a match.  Hence, various altneratives keys are equivalent, such as:
* `"exe"`, `"executable"`, `"Exe"` (and also `"prog"`, `"Program"`, or `"bin"`, `"binary"`)
* `"logDir"`, `"logDirectory"`, `"log_dir"`, `"logdir"`
* `"pauseSecs"`, `"pauseSeconds"`, `"pauseseconds"`, `"pause_secs"`, `"pause"`
* `"sessionParams"`, `"session_parameters`"
  - But note that the session parameters themselves do not support alternatives (e.g., `"timeoutSec"` is supported but not `"timeout"`, `"timeout_seconds"`, etc.).
* `"logFilenameExtra"`, `"log_filename_extra"`, `"logfilenameextra"`
* `"logHeader"`, `"log_header"`, `"logheader"`, `"loghead"`

If the sessions do not override any of the global parameters, the `"sessions"` list can contain empty JSON objects (i.e., `{}`) to indicate the number of sessions in the experiment:
```json
{
    "exe": "C:/Users/scientist/SessionA/unity-standalone.lnk",
    "logDir": "C:/Users/scientist/AppData/LocalLow/institution/SessionA",
    "sessionParameters": {
        "timeoutSecs": 5
    },
    "pauseSecs": 2,
    // Four sessions with no overrriding parameters.
    "sessions": [{}, {}, {}, {}]
}
```

Unlike traditional JSON, comments lines starting with `//` or `#` are allowed, and are removed before the file is processed.  Not all structured text editors handle JSON with comments, but [Visual Studio Code](https://code.visualstudio.com) does, when the "Select Language Mode" control in the bottom right is changed to "JSON with Comments".
