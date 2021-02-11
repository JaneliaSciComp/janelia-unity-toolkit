# Janelia NI DAQmx Support

## Summary

This package (`org.janelia.ni-daq-mx`) supports reading from and writing to National Instruments data acquisition (DAQ) hardware, using the second-generation NI-DAQmx driver.  DAQ hardware is a convenient way of sending control voltages to and reading sample voltages from external hardware devices like microscope controllers.

## Installation

Outside Unity, download the NI-DAQmx driver software from https://www.ni.com/en-us/support/downloads/drivers/download.ni-daqmx.html and install it.  The DLL files like `nicaiu.dll` likely will end up in the `C:\Windows\System32` folder.

Inside Unity, follow the [installation instructions in the main repository](https://github.com/JaneliaSciComp/janelia-unity-toolkit/blob/master/README.md#installation).

## Details

### Janelia.NiDaqMx

A static class that controls communication with a NI DAQ device: initializing the device, setting up the details of the input and output channels, reading from those channels into an array, writing from an array to those channels, shutting down the device, and reporting any errors that may occur.

### Janelia.ExampleUsingNiDaqMx

A `MonoBehaviour` subclass that shows how to use `Janelia.NiDaqMx` to read voltages from and write voltages to NI DAQ hardware.
