﻿//
//  Implements gaze-activated buttons in the rift portion of the installation
//

using UnityEngine;
using System.Collections;
using Uniduino;

public class RiftButtonMgr : MonoBehaviour 
{
   [Space(10)]
   public RiftButton[] RiftButtons;

   [Space(10)]

   [Header("Progress Thresholds")]
   public float ProgressAngleThreshMin = 10.0f;
   public float ProgressAngleThreshMax = 60.0f;

   [Header("Press")]
   public KeyCode PressCheat = KeyCode.KeypadEnter;
   public int ButtonArduinoPin = 6;

   private Arduino _arduino;
   private bool _buttonPressed = false;
   private RiftButton _selectedButton = null;

   public static RiftButtonMgr Instance { get; private set; }

   void Awake()
   {
      Instance = this;
   }

	void Start () 
   {
      _arduino = Arduino.global;
      if(_arduino != null)
         _arduino.Setup(ArduinoSetup);	
	}

   void ArduinoSetup()
   {
      _arduino.pinMode(ButtonArduinoPin, PinMode.INPUT);
   }

	
	void Update () 
   {
      if (CameraMgr.Instance == null)
         return;

      Transform camTrans = CameraMgr.Instance.GetCamTrans();
      if (camTrans == null)
         return;

      //update progress feedback on buttons
      RiftButton closestButton = null;
      float smallestAngle = float.MaxValue;
      foreach (RiftButton b in RiftButtons)
      {
         Vector3 toMe = b.transform.position - camTrans.position;
         toMe.Normalize();
         Vector3 camForward = camTrans.forward;
         float dot = Vector3.Dot(camForward, toMe);
         float angle = Mathf.Acos(dot) * Mathf.Rad2Deg;

         float progressU = 1.0f - Mathf.InverseLerp(ProgressAngleThreshMin, ProgressAngleThreshMax, angle);
         b.SelectionProgress = progressU;

         if (angle < smallestAngle)
         {
            smallestAngle = angle;
            closestButton = b;
         }
      }

      //select the closest button you are looking at
      if (closestButton != null)
         _selectedButton = closestButton;
      
      //update selection state
      foreach (RiftButton b in RiftButtons)
      {
         b.Selected = (b == _selectedButton);
      }

      //handle keyboard cheat button press
      if (Input.GetKeyDown(PressCheat))
         PressButton();

      //handle arduino button press
      if ((_arduino != null) && _arduino.Connected)
      {
         int buttonState = _arduino.digitalRead(ButtonArduinoPin);

         bool newButtonPressed = (buttonState == Arduino.LOW);

         if (newButtonPressed != _buttonPressed)
         {
            _buttonPressed = newButtonPressed;

            if (_buttonPressed && (EightNightsAudioMgr.Instance != null))
            {
               PressButton();
            }
         }
      }
	}

   void PressButton()
   {
      if (_selectedButton != null)
         _selectedButton.TriggerPress();
   }
}
