**Note: this page is outdated**

## Decisions ##

[Walkera UFO board](http://www.walkera.com.hk/walkera-dragonfly-ufo-4-spare-parts-ufo4z16-pcb-receiver-p-2064.html) purchased (backordered), should include three ENC-03J gyros and a 3 axis [MMA7260 accelerometer](http://www.freescale.com/webapp/sps/site/prod_summary.jsp?code=MMA7260QT&fsrch=1). Pictures of board:
[RC Groups Post 1](http://www.rcgroups.com/forums/showthread.php?p=11232259#post11232259)
[RC Groups Post 2](http://www.rcgroups.com/forums/showthread.php?p=11238320#post11238320)
[RC Groups Post 3](http://www.rcgroups.com/forums/showthread.php?p=9987793#post9987793)
[Some European Store (confirms that sensors are included)](http://ckado.com/UFO-004-Z-16-B-pcb_product_1805_fr.html)
[SparkFun's new 6 DOF IMU](http://www.sparkfun.com/commerce/product_info.php?products_id=9431)

Also purchased [Panasonic EWTS82](http://www.seeedstudio.com/depot/angular-rate-sensor-ewts82-p-193.html) to test.

It looks like the ATmega168 will be capable of stabilizing the quadrotor helicopter with its built in ADC, judging by the success of the Walkera UFO and the `MikroQuad` project.

2200 mAh 3S1P (11.1V) 20C lithium polymer battery, this calculates to a maximum of 44A continuous current draw ([link](http://www.hobbycity.com/hobbycity/store/uh_viewItem.asp?idProduct=6306&Product_Name=ZIPPY_Flightmax_2200mAh_3S1P_20C))

The microcontrollers on the ESCs are ATmega8, I2C firmware may be used, but the original firmware is under lock and cannot be backed up, therefor we will not attempt to use I2C.

Frame will be built with 3/8" aluminum tubing, platform will be polycarbonate or aluminum

2410-9 brushless motors ([link](http://www.hobbycity.com/hobbycity/store/uh_viewItem.asp?idProduct=6549)) and 18A ESCs ([link](http://www.hobbycity.com/hobbycity/store/uh_viewItem.asp?idProduct=6456))

[This battery monitor](http://www.hobbycity.com/hobbycity/store/uh_viewItem.asp?idProduct=4175) will be used, perhaps aid the secondary MCU to warn the pilot. The actual battery voltage can also be monitored with the AVR's internal ADC. We would rather not have the BEC cut power to the motors for obvious reasons.

[These EPP1045 propellers (10" diameter X 4.5 pitch)](http://www.tech-mp.com/accessories.php) will be used


## Old Void Decisions ##

The 10 bit ADC in AVRs may not be enough, will use 12 bit ADCs with good sample rate from TI, either a single channel ADC with multiplexer ([ADS7822U](http://focus.ti.com/docs/prod/folders/print/ads7822.html) and [CD74HC4067](http://search.digikey.com/scripts/DkSearch/dksus.dll?Detail&name=296-9225-5-ND)) or an 8 channel ADC ([TLV2548IDW](http://focus.ti.com/docs/prod/folders/print/tlv2548.html#samples)).

IDG300 gyros will be used for pitch and roll because of it's profile and it's low noise characteristics. ([link](http://www.sparkfun.com/commerce/product_info.php?products_id=698))

LISY300AL gyro for yaw ([link](http://www.sparkfun.com/commerce/product_info.php?products_id=8955))

MMA7260Q for accelerometer ([link](http://www.sparkfun.com/commerce/product_info.php?products_id=252))

## Notes ##

Flight control software structure is written and the three main systems are tested, RC input works, ESC output works, and sensor input works. All systems run on interrupts and timing has 0.05 microsecond resolution. ESC output will use a serial to parallel shift register using the hardware PWM pin, which is more accurate since it does not depend on software response time.

Looking at the chart on the quadrotor topic on RC Groups, a 30 ounce (850 grams) craft on a 2100 mAH battery will fly for 14.3 minutes, which equates to 8.8A average current draw, which is within our limits. We are building our craft using nearly identical motors and propellers, this is our benchmark.

Lower center of gravity means lower angular acceleration from the motors, dangling a mass perpendicular to the craft below the center of gravity will help stabilize pitch and yaw.

ATmega644 should be used for the flight controller, and a co-pilot can be attached but is optional. SPI, I2C and UART are all still available.

Auto-landing and minimum altitude should be done with an ultrasonic sensor.

A scrolling light bar will be added to indicate forward direction.

Torque will want the X frame to want to "scissor", I hope polycarbonate can brace against this well, or else sheet aluminum will be used. The force is likely small as the blades are not heavy.

Use two O-rings on the propellers, because of [this](http://www.rcgroups.com/forums/showpost.php?p=9681777&postcount=1456)

The two radio choices are now the VEX radio or a pair of serial data radios. The VEX radio cannot be modified to send serial data, but it has more range. The serial data radios are bigger and heavier and has less range, but provides more data flexibility. The VEX transmitter can be modified to interface with a computer or Nunchuks with a DAC controlled by an AVR.

It sounds like a low pass filter with a cutoff at about 30-40 Hz should be used on all sensor outputs.

Research into differential amplifiers, shouldn't be hard to make a circuit that takes one range to to a wider one with an offset.

## Notable Sources ##

[RC Groups thread](http://www.rcgroups.com/forums/showthread.php?t=768115)

[Quix's blog post about SuperSimple ESC programming](http://diydrones.ning.com/profiles/blogs/supersimple-escs-and)

[Dimensions for 2410 motor mount](http://www.rcgroups.com/forums/showpost.php?p=9107167&postcount=633)

Aluminum examples:
  * http://www.rcgroups.com/forums/showpost.php?p=8740938&postcount=226
  * http://www.rcgroups.com/forums/showpost.php?p=9081658&postcount=610
  * http://www.rcgroups.com/forums/showthread.php?t=811550
  * http://www.rcgroups.com/forums/attachment.php?attachmentid=2022829



[Info on li-po batteries](http://www.rcgroups.com/forums/showthread.php?t=209187)


[More props](http://www.rcgroups.com/forums/showthread.php?t=1005930)


[MikroQuad, built on the Arduino](http://mikroquad.com/bin/view/Main/WebHome)