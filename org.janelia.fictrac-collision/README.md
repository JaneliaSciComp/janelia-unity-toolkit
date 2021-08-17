# Janelia FicTrac Collision-Handling Support

## Summary

This package (org.janelia.fictrac-collision) supports collision detection and response for kinematic motion coming from the FicTrac system ([http://rjdmoore.net/fictrac/](http://rjdmoore.net/fictrac/)).  FicTrac is a "webcam-based method for tracking spherical motion and generating fictive animal paths."  The basic communication with FicTrac is handled by the [org.janelia.io package](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.io).

## Installation

Follow the [installation instructions in the main repository](https://github.com/JaneliaSciComp/janelia-unity-toolkit/blob/master/README.md#installation).

## Details

### Janelia.FicTracSubject

Makes the associated `GameObject` have collision detection and response (per the [org.janelia.collision-handling package](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.collision-handling)) for kinematic motion from the FicTrac system.  Uses `Janelia.FicTracUpdater`.

### Janelia.FicTracUpdater

Uses `Janelia.SocketMessageReader` from the [org.janelia.io package](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.io).
