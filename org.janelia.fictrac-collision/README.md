# Janelia FicTrac Collision-Handling Support

## Summary

This package (org.janelia.fictrac-collision) supports collision detection and response for kinematic motion coming from the FicTrac system ([http://rjdmoore.net/fictrac/](http://rjdmoore.net/fictrac/)).  FicTrac is a "webcam-based method for tracking spherical motion and generating fictive animal paths."  The basic communication with FicTrac is handled by the [org.janelia.io package](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.io).

## Installation

Follow the [installation instructions in the main repository](https://github.com/JaneliaSciComp/janelia-unity-toolkit/blob/master/README.md#installation).

## Details

### `Janelia.FicTracSubject`

Makes the associated `GameObject` have collision detection and response (per the [org.janelia.collision-handling package](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.collision-handling)) for kinematic motion from the FicTrac system.  Uses `Janelia.FicTracUpdater`.

### `Janelia.FicTracUpdater`

Uses `Janelia.SocketMessageReader` from the [org.janelia.io package](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.io).

### `Janelia.FicTracSubjectIntegrated`

A drop-in replacement for `Janelia.FicTracSubject`, with a few behavioral differences:
* it uses the "integrated animal heading (lab)" sent by FicTrac, as mentioned in the [FicTrac data_header.txt]( https://github.com/rjdmoore/fictrac/blob/master/doc/data_header.txt);
* it does not add collision handling;
* it does not support data smoothing or the `smoothingCount` field.