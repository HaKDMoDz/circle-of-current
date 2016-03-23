# Wii Drum Controller #

[My Website Link](http://frank.circleofcurrent.com/wiidrum.php)

Rock Band 2 (RB2) and Guitar Hero World Tour (GHWT) both work with the drum set controller that is packaged with GHWT. The GHWT drums, unlike the RB2 drums, is plugged into a Wiimote's expansion port. This expansion port is a I2C bus and the drum is a I2C slave device. An AVR microcontroller can be connected on this bus as a I2C slave with the same slave address to trick the Wii into thinking that it's connected to a drum controller.

The code inside \wii\_drum is firmware source code for an AVR microcontroller. It uses the [Wiimote Extension Library](WiiExtensionLibrary.md)

[First Hardware Picture](http://i156.photobucket.com/albums/t31/frank26080115/DSC03116Small.jpg)

[Proof of Concept Youtube Video](http://www.youtube.com/watch?v=Kzl0qaKv-Wg&fmt=18)

# Details #

## Code ##

[Link to source code folder](http://code.google.com/p/circle-of-current/source/browse/#svn/wii_drum)

The code is written in C and compiled with AVR-GCC (WinAVR is used if compiling in Windows). The project file is called "wiidrum.aps" and is opened with AVR Studio. The code is currently configured for use with an ATmega168 AVR microcontroller running on a 8 MHz internal RC oscillator.

It uses the [Wiimote Extension Library](WiiExtensionLibrary.md)

### main.c ###

This file handles initialization and everything associated with the interaction with the drum pad sensors. Some configuration is done with pre-processor defines and array tables at the top of the file. If you want to modify the behaviour of the controller, modify this file.

### pindef.h ###

This is where connections on the microcontroller are defined. Change this file to suit your own hardware. Make sure the TWI and UART port are properly defined.

### Other Files ###

These files are not specific to the drum set controller, they can be used to make any Wii extension controller ("classic", guitar, "Nunchuk", etc).

#### wiimote.c , wm\_crypto.h ####

These two files handle everything that deals with the Wiimote. The functions inside handle encryption and decides how to respond to various commands sent from the Wiimote. wm\_crypto.h contains key tables used for encrypting the data sent to the Wiimote.

Thanks to Hector Martin for the decryption and encryption method. He wrote about it here:

[PDF Link](http://www.derkeiler.com/pdf/Newsgroups/sci.crypt/2008-11/msg00110.pdf)

## Hardware ##

[First Hardware Picture](http://i156.photobucket.com/albums/t31/frank26080115/DSC03116Small.jpg)

![http://c.statcounter.com/4037095/0/d833116e/1/&nonsense=something_that_ends_with.png](http://c.statcounter.com/4037095/0/d833116e/1/&nonsense=something_that_ends_with.png)