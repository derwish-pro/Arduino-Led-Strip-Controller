# Arduino Led Strip Controller

The controller is able to smoothly turn on/off RGB-backlight, fading colors, strobing/blinking.<br>
You can turn on/off the backlight by using the hardware button or from widows app.<br>
Favorite color may be written in EEPROM.<br><br>
<img alt="" src="https://github.com/derwish-pro/Arduino-Led-Strip-Controller/blob/master/LedStripController_Windows/screenshot.JPG" style="max-width:100%;">

## Connections
Connect On/Off button to Arduino pin 3 and GND (the resistor is not required).<br><br>
If you use L298N, connect led strip as follows:<br>
IN1 > Arduino pin 9<br>
IN2 > Arduino pin 10<br>
IN3 > Arduino pin 11<br>
ENA > Arduino pin 6<br>
ENB > Arduino pin 6<br><br>
GND > Arduino pin GND<br><br>
The Arduino can be powered from L298N (5V pin), but you can`t control the led-strip using windows app.<br>
Set LED_STRIP_INVERT_PWM to true.
If you use transistors instead of L298N, connect transistors to pins 9,10,11.<br><br>

