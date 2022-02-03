# Collision Handling for Janelia Experimental Technology's Trackball (jETTrac)

## Summary

This package (org.janelia.jettrac-collision) supports collision detection and response for kinematic motion coming from jETTrac.  The jETTrac system tracks an animal walking on a ball, and is built by  [Janelia Experimental Technologies (jET)](https://www.janelia.org/support-team/janelia-experimental-technology).  The basic communication with jETTrac is handled by the [org.janelia.io package](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.io).

## Installation

Follow the [installation instructions in the main repository](https://github.com/JaneliaSciComp/janelia-unity-toolkit/blob/master/README.md#installation).

## Details

### `Janelia.JetTracSubject`

Makes the associated `GameObject` have collision detection and response (per the [org.janelia.collision-handling package](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.collision-handling)) for kinematic motion from the jETTrac system.  Uses `Janelia.JetTracUpdater`.

### `Janelia.JetTracUpdater`

Uses `Janelia.JetTracReader` and `Janelia.JetTracTransformer` rather like `Janelia.ExampleUsingJetTrac` from the [org.janelia.janelia package](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.jettrac).

### `Janelia.SetupJetTracSubject`

Adds the `Window/jETTrac/Setup jETTrac Collision Subject` menu item to the Unity editor, which launches a simple user interface for creating a new `GameObject` with the appropriate hierarchy and scripts.

