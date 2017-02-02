# T100k NAL

This project is a simple network abstraction layer for chinese LED Controllers such as the T100k.
It is designed as a library which you can integrate in custom projects, but there is also a standalone terminal 
interface to control individual LEDs.

A description of the network interface is currently being built. 
The development is done using Linux and Monodevelop.

**Note: This project/NAL implementation is not designed to do pixelmapping or similar stuff
but to create a simple wrapper to control specific LEDs**

## Hardware Setup

You need the following hardware to use this software:

- LED Controller T100-k (maybe other models work too, but are however not tested)
- RGB LED stripes which use (or are compatible) to the LPD6803 LED drivers.
  - Please note, that these drivers only support 5 bit resolution per channel.
- A computer with a wired ethernet adapter or a network which is able to transport data using UDP.
  - Every normal computer should be able to do so.

## Quick start guide

- Connect your controller to the computer, setup the computers network adapter accordingly.
  - IP Address: `192.168.60.178`
  - Subnet: `255.255.255.0`
  - Gateway: `none` or `192.168.60.1`
  - DNS Servers: `none`
- Launch the program:
  - Windows: `t100k.exe`
  - Linux/Mono: `t100k.exe`
  - A terminal interface will show up and allow you to control individual LEDs for the controller network.

## Building yourself

- Clone this repository
- Build should work out-of-the-box when using Visual Studio or Monodevelop/Xamarin Studio.
- Some nuget packages are required:
  - log4net