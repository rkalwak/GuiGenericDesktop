#include <Arduino.h>

#ifndef FLAG_STRING
#define FLAG_STRING 0
#endif

#ifndef FLAG_STRING_TEXT
#define FLAG_STRING_TEXT "fallback"
#endif

#ifndef FLAG_NUMERIC_COUNT
#define FLAG_NUMERIC_COUNT 0
#endif

#ifndef FLAG_NUMERIC_GPIO
#define FLAG_NUMERIC_GPIO 0
#endif

#if FLAG_STRING != 1
#error FLAG_STRING was not passed to the compiler
#endif

#if FLAG_NUMERIC_COUNT != 7
#error FLAG_NUMERIC_COUNT was not passed to the compiler
#endif

#if FLAG_NUMERIC_GPIO != 12
#error FLAG_NUMERIC_GPIO was not passed to the compiler
#endif

static_assert(sizeof(FLAG_STRING_TEXT) == sizeof("alpha beta"), "FLAG_STRING_TEXT was not passed as the expected string literal");

void setup()
{
	Serial.begin(115200);
	delay(2000);

	Serial.println("FLAG_VALIDATION_BEGIN");
	Serial.print("FLAG_STRING_ACTIVE=");
	Serial.println(FLAG_STRING ? "1" : "0");
	Serial.print("FLAG_STRING_TEXT=");
	Serial.println(FLAG_STRING_TEXT);
	Serial.print("FLAG_NUMERIC_COUNT=");
	Serial.println(FLAG_NUMERIC_COUNT);
	Serial.print("FLAG_NUMERIC_GPIO=");
	Serial.println(FLAG_NUMERIC_GPIO);
	Serial.println("FLAG_VALIDATION_PASS");
}

void loop()
{
	delay(1000);
}
