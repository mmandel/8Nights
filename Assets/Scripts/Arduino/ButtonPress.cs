//
// bool on this script goes true when button is pressed
//
// NOTE: you have to upload the "AllInputsFirmata" sketch to the Arduino for this script to work
//

using UnityEngine;
using System.Collections;
using Uniduino;

public class ButtonPress : MonoBehaviour
{

   public int buttonPin = 2;
   int ledPin = 13;

   public bool ButtonPressed = false;

   Arduino _arduino;

   // Use this for initialization
   void Start()
   {
      _arduino = Arduino.global;
      _arduino.Setup(ArduinoSetup);

   }

   void ArduinoSetup()
   {
      _arduino.pinMode(buttonPin, PinMode.INPUT);
   }

   void Update () 
   {
      int buttonState = _arduino.digitalRead(buttonPin);

      ButtonPressed = (buttonState == Arduino.LOW);

      if (buttonState == Arduino.HIGH)
         _arduino.digitalWrite(ledPin, Arduino.LOW);
     else
         _arduino.digitalWrite(ledPin, Arduino.HIGH);
   }
}
