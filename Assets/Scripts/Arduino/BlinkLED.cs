//
// set of Uniduino - blink an LED!
//
// NOTE: you have to upload the "StandardFirmata" sketch to the Arduino for this script to work
//

using UnityEngine;
using System.Collections;
using Uniduino;

public class BlinkLED : MonoBehaviour {

   public int ledPin = 9;
   public float blinkSpeed = 1.0f;

   float _curBrightness = 0.0f;
   bool _increase =  true;
   Arduino _arduino;

	// Use this for initialization
	void Start () {
      _arduino = Arduino.global;
      _arduino.Setup(ArduinoSetup);

      StartCoroutine(BlinkLoop());
	}

   void ArduinoSetup()
   {
      _arduino.pinMode(ledPin, PinMode.OUTPUT);
   }

   IEnumerator BlinkLoop()
   {
      while (true)
      {
         _arduino.digitalWrite(ledPin, Arduino.HIGH); // led ON
         yield return new WaitForSeconds(blinkSpeed);
         _arduino.digitalWrite(ledPin, Arduino.LOW); // led OFF
         yield return new WaitForSeconds(blinkSpeed);
      }
   }
	
	// Update is called once per frame
   /*void Update () 
   {
      if (_increase)
         _curBrightness += Time.deltaTime * blinkSpeed;
      else
         _curBrightness -= Time.deltaTime * blinkSpeed;

      if ((_curBrightness <= 0.0f) || (_curBrightness >= 1.0f))
         _increase = !_increase;

      _curBrightness = Mathf.Clamp01(_curBrightness);

      int val = (int)(_curBrightness * 255.0f);
      Debug.Log("val = " + val);
      _arduino.analogWrite(ledPin, val);
   }*/
}
