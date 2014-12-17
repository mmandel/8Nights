//
// Represents a button in the installation space
//
// NOTE: you have to upload the "AllInputsFirmata" sketch to the Arduino for this script to work
//

using UnityEngine;
using System.Collections;
using Uniduino;

public class EightNightsArduinoButton : MonoBehaviour
{

   public int buttonPin = 2;
   public EightNightsMgr.GroupID Group;

   [Header("Outputs")]
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

   void Update()
   {
      //if (!_arduino.Connected)
      //   return;

      int buttonState = _arduino.Connected ? _arduino.digitalRead(buttonPin) : Arduino.HIGH;

      bool newButtonPressed = (buttonState == Arduino.LOW) || EightNightsMgr.Instance.CheatStateForGroup(Group);

      //update persistent pressed state
      EightNightsMgr.Instance.SetButtonPressedState(Group, newButtonPressed);

      if (newButtonPressed != ButtonPressed)
      {
         ButtonPressed = newButtonPressed;

         if (ButtonPressed && (EightNightsAudioMgr.Instance != null))
         {
            EightNightsAudioMgr.Instance.TriggerGroup(Group);
         }
      }

   }
}
