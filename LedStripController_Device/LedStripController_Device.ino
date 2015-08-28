
#include "LedStrip.h"
#include "SerialCommand.h"
#include <EEPROM.h>

#define LED_STRIP_INVERT_PWM false

LedStrip ledStrip(9, 10, 11, LED_STRIP_INVERT_PWM);
#define LedEnablePin1 6
#define LedEnablePin2 7

#define ButtonPin 3
#define LedPin 13
#define TurnOnOffFadeTime 2000


SerialCommand serialCommand;


bool lastButtonState;
bool buttonState;
long lastDebounceTime = 0;
#define DebounceDelay 50
bool buttonSwitch;


uint8_t preset_r, preset_g, preset_b;


void setup() {

	Serial.begin(9600);
	Serial.println("Led strip is ready");

	pinMode(LedEnablePin1, OUTPUT);
	pinMode(LedEnablePin2, OUTPUT);
	digitalWrite(LedEnablePin1, HIGH);
	digitalWrite(LedEnablePin2, HIGH);

	serialCommand.addCommand("on", LED_on);
	serialCommand.addCommand("off", LED_off);
	serialCommand.addCommand("fade", fadecolor);
	serialCommand.addCommand("strobe", strobe);
	serialCommand.addCommand("stopstrobe", stopstrobe);
	serialCommand.addCommand("color", setcolor);
	serialCommand.addCommand("state", getState);
	serialCommand.addCommand("fadetopreset", fade_to_preset);
	serialCommand.addCommand("storepreset", storePreset);
	serialCommand.setDefaultHandler(unrecognized);


	pinMode(ButtonPin, INPUT_PULLUP);

	pinMode(LedPin, OUTPUT);
	lastButtonState = digitalRead(ButtonPin);
	buttonState = lastButtonState;
	buttonSwitch = false;



	ledStrip.TurnOff();
	readPreset();
	ledStrip.SetColor(preset_r, preset_g, preset_b);
	switchLedState();
}



void loop() {

	serialCommand.readSerial();
	ledStrip.Loop();
	readButton();
	digitalWrite(LedPin, ledStrip.IsOn());
}



void readButton()
{
	bool reading = digitalRead(ButtonPin);


	if (reading != lastButtonState)
		lastDebounceTime = millis();


	if ((millis() - lastDebounceTime) > DebounceDelay) {
		if (reading != buttonState) {
			buttonState = reading;

			if (buttonState) 
				switchLedState();
		}
	}


	lastButtonState = reading;
}

void switchLedState()
{
	if (!buttonSwitch)
		ledStrip.TurnOn(TurnOnOffFadeTime);
	else
		ledStrip.TurnOff(TurnOnOffFadeTime);

	buttonSwitch = !buttonSwitch;

	Serial.print("Switch: ");
	Serial.println(buttonSwitch);
}

void storePreset() {
	Serial.println("Preset stored in EEPROM");
	EEPROM.update(0, preset_r);
	EEPROM.update(1, preset_g);
	EEPROM.update(2, preset_b);
}

void readPreset() {
	preset_r = EEPROM.read(0);
	preset_g = EEPROM.read(1);
	preset_b = EEPROM.read(2);
}



void LED_on() {
	Serial.print("Led ON: ");

	char *arg;
	unsigned int fade_time;

	arg = serialCommand.next();
	if (arg != NULL) {
		fade_time = atoi(arg);
		Serial.println(fade_time);
		ledStrip.TurnOn(fade_time);
	}
	else
	{
		Serial.println("OK");
		ledStrip.TurnOn();
	}

	buttonSwitch = true;
}


void LED_off() {
	Serial.print("Led OFF: ");

	char *arg;
	unsigned int fade_time;

	arg = serialCommand.next();
	if (arg != NULL) {
		fade_time = atoi(arg);
		Serial.println(fade_time);
		ledStrip.TurnOff(fade_time);
	}
	else
	{
		Serial.println("OK");
		ledStrip.TurnOff();
	}

	buttonSwitch = false;
}



void fade_to_preset()
{
	readPreset();

	Serial.print("Fade to preset: ");

	char *arg;
	unsigned int fade_time;

	arg = serialCommand.next();
	if (arg != NULL) {
		fade_time = atoi(arg);
		Serial.println(fade_time);
	}
	else
	{
		Serial.println("bad arguments");
		return;
	}

	Serial.println(preset_r);
	Serial.println(preset_g);
	Serial.println(preset_b);

	ledStrip.FadeToColor(preset_r, preset_g, preset_b, fade_time);
}








void getState() {
	Serial.print("State: ");
	Serial.print(ledStrip.Get_r());
	Serial.print(" ");
	Serial.print(ledStrip.Get_g());
	Serial.print(" ");
	Serial.print(ledStrip.Get_b());
	Serial.print(" ");
	Serial.println(ledStrip.IsOn());
}

void setcolor() {

	Serial.print("Set color: ");

	int r = 0, g = 0, b = 0;
	char *arg;

	arg = serialCommand.next();
	if (arg != NULL) {
		r = atoi(arg);
	}
	else
	{
		Serial.println("bad arguments");
		return;
	}

	arg = serialCommand.next();
	if (arg != NULL) {
		g = atoi(arg);
	}
	else
	{
		Serial.println("bad arguments");
		return;
	}

	arg = serialCommand.next();
	if (arg != NULL) {
		b = atoi(arg);
	}
	else
	{
		Serial.println("bad arguments");
		return;
	}

	Serial.print(r);
	Serial.print(" ");
	Serial.print(g);
	Serial.print(" ");
	Serial.println(b);

	preset_r = r;
	preset_g = g;
	preset_b = b;

	ledStrip.SetColor(r, g, b);
}


void fadecolor() {

	Serial.print("Fade color: ");

	int r = 0, g = 0, b = 0, time = 0;
	char *arg;

	arg = serialCommand.next();
	if (arg != NULL) {
		r = atoi(arg);
	}
	else
	{
		Serial.println("bad arguments");
		return;
	}

	arg = serialCommand.next();
	if (arg != NULL) {
		g = atoi(arg);
	}
	else
	{
		Serial.println("bad arguments");
		return;
	}

	arg = serialCommand.next();
	if (arg != NULL) {
		b = atoi(arg);
	}
	else
	{
		Serial.println("bad arguments");
		return;
	}

	arg = serialCommand.next();
	if (arg != NULL) {
		time = atoi(arg);
	}
	else
	{
		Serial.println("bad arguments");
		return;
	}

	Serial.print(r);
	Serial.print(" ");
	Serial.print(g);
	Serial.print(" ");
	Serial.print(b);
	Serial.print(" ");
	Serial.println(time);

	preset_r = r;
	preset_g = g;
	preset_b = b;

	ledStrip.FadeToColor(r, g, b, time);
}



void strobe() {

	Serial.print("Strobe: ");

	unsigned int on_duration, off_duration, times;

	char *arg;

	arg = serialCommand.next();
	if (arg != NULL) {
		on_duration = atoi(arg);
	}
	else
	{
		Serial.println("bad arguments");
		return;
	}

	arg = serialCommand.next();
	if (arg != NULL) {
		off_duration = atoi(arg);
	}
	else
	{
		Serial.println("bad arguments");
		return;
	}

	arg = serialCommand.next();
	if (arg != NULL) {
		times = atoi(arg);
	}
	else
	{
		Serial.println("bad arguments");
		return;
	}


	Serial.print(on_duration);
	Serial.print(" ");
	Serial.print(off_duration);
	Serial.print(" ");
	Serial.println(times);


	ledStrip.Strobe(on_duration, off_duration, times);
}

void stopstrobe() {
	Serial.print("Stop strobe");
	ledStrip.StopStrobe();
}



void unrecognized(const char *command) {
	Serial.println("Available commands:");
	Serial.println("on - turn on leds");
	Serial.println("off - turn off leds");
	Serial.println("on 2000 - turn on leds, fade time = 2000");
	Serial.println("off 2000 - turn off leds, fade time = 2000");
	Serial.println("fade 150 100 50 1000 - fade color to R:150,G:100,B:50, fade time = 1000");
	Serial.println("fadetopreset 1000 - fade to color stored in EEPROM, fade time = 1000");
	Serial.println("color 150 100 50 - change color to R:150,G:100,B:50");
	Serial.println("strobe 100 900 3 - strobe 3 times, 100 ms - on, 900 ms - off");
	Serial.println("stopstrobe - stop strobing");
	Serial.println("storepreset - store current color to EEPROM");
	Serial.println("state - get leds state (R,G,B,isOn)");

}



