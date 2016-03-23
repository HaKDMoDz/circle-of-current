# Wiimote Extension Library for AVR Microcontroller #

[My Website Link](http://frank.circleofcurrent.com/wiiextensionlib.php)

This is a library that gives an AVR microcontroller the ability to act as a Wiimote extension controller (Nunchuk, Classic Controller, Guitar Hero 3 controller, Guitar Hero World Tour guitar controller, Guitar Hero World Tour drum controller, etc). The library has two parts: the I2C slave device and Wiimote protocol handling. It can be used with AVR-GCC, and it is possible to adapt it to work with Arduino if you know how to include the files into your sketch properly.

wiimote.c and wiimote.h handles the interface between the application software and the I2C bus, it also handles the encryption of the data sent to the Wiimote. The encryption look-up tables are in wm\_crypto.h .

wiimote.c has code to make the microcontroller a register based I2C slave device. It has 256 registers in an array, these registers are written to and read by the Wiimote via the I2C bus using a fairly standard method.

## Using the Library ##

You need to connect the Wiimote's extension port to your microcontroller's I2C bus. There is also one pin on the extension port that is used to detect whether or not something is connected, this should be connected to Vcc, or if you want to control this detection, connect it to an I/O pin.

The pin-out of the extension port is shown below, the order from left to right is the same order looking at the back of the Wiimote.

| Ground | No Connection | SCL |
|:-------|:--------------|:----|
| SDA    | Device Detect | Vcc (3.3V) |

Define your hardware connections in wiimote.h like so:

```
#define twi_port PORTC
#define twi_ddr DDRC
#define twi_scl_pin 5
#define twi_sda_pin 4
#define dev_detect_port PORTD
#define dev_detect_ddr DDRD
#define dev_detect_pin 4
```

This depends on the chip you use and your circuit.

Calling wm\_init will start up the I2C hardware and prepare the AVR for communication with the Wiimote. wm\_init has 4 parameters: device ID, initial data, calibration data, and an application function. The device ID depends on what type of controller you want to make, it's an array of 6 bytes. The initial data is the button press data that is sampled during game play, which should be set as a default state. The calibration data is an array of 32 bytes containing calibration data for the controller, this is composed of things like maximum/minimum joystick positions and things like that, I have no more details about this. The user application will link a function in the application, and it will be called every time the Wiimote requests a sample of button press data.

If you have the device detection pin of the extension port connected to an I/O on your microcontroller, then the wm\_init function will simulate a disconnection and reconnection every time it is called. This is useful if you don't want your microcontroller to be powered by the extension port of the Wiimote.

Once wm\_init has been called, button press data can be set by calling wm\_newaction. The parameter is an array of 6 bytes containing the button press data. You can continuously call this function in your main application loop, when the Wiimotes samples the data, the latest data sent to wm\_newaction will be read.

For various controller IDs, and information on the button press data array, visit http://wiibrew.org/wiki/Wiimote/Extension_Controllers .

## Example of Use ##

WiiDrumController
http://code.google.com/p/circle-of-current/wiki/WiiDrumController

## Source Code Download ##

Go to http://code.google.com/p/circle-of-current/source/browse/#svn/wii_drum and download:
wiimote.c
wiimote.h
wm\_crypto.h