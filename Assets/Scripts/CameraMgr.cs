//
//  Takes care of debug rotating camera with gamepad and switching between normal + rift cameras
//

using UnityEngine;
using System.Collections;

public class CameraMgr : MonoBehaviour 
{

   public Transform DebugRotateTrans;
   public GameObject RiftCamera;
   public GameObject RiftCamLeft;
   public GameObject RiftCamRight;
   public GameObject NormalCamera;
   public OVRManager RiftMgr;
   public bool StartWithRiftCam = false;
   public float DebugGamepadSensitivity = 2.5f;

   private bool _riftCamActivated = false;
   private Vector3 _debugRotEuler = Vector3.zero;
   private float _minimumRotY = -75F;
   private float _maximumRotY = 75F;

   public static CameraMgr Instance { get; private set; }

   void Awake()
   {
      Instance = this;
   }

	void Start () 
   {
      ActivateRiftCam(StartWithRiftCam);
	}

   public Transform GetCamTrans()
   {
      return _riftCamActivated ? RiftCamLeft.transform : NormalCamera.transform;
   }

   void ActivateRiftCam(bool b)
   {
      _riftCamActivated = b;


      RiftMgr.enabled = _riftCamActivated;
      //RiftCamera.SetActive(_riftCamActivated); //oculus stuff gets unstable if you do this...
      
      NormalCamera.SetActive(!_riftCamActivated);

      if (EightNightsAudioMgr.Instance != null)
         EightNightsAudioMgr.Instance.ShowTestUI = !_riftCamActivated;
   }

   void LateUpdate()
   {
      //toggle rift / normal camera mode
      if (Input.GetKeyDown(KeyCode.C))
         ActivateRiftCam(!_riftCamActivated);
   }


	
	void Update () 
   {
      //toggle rift / normal camera mode
      //if (Input.GetKeyDown(KeyCode.C))
      //   ActivateRiftCam(!_riftCamActivated);

      //recenter rift
      if (Input.GetKeyDown(KeyCode.Space))
         RecenterCamera();

      float lStickX = Input.GetAxis("Horizontal");
      if (!Mathf.Approximately(lStickX, 0.0f))
      {
         _debugRotEuler.y = _debugRotEuler.y + (lStickX * DebugGamepadSensitivity);
      }
      float lStickY = Input.GetAxis("Vertical");
      if (!Mathf.Approximately(lStickY, 0.0f))
      {
         float newVal = _debugRotEuler.x - (lStickY * DebugGamepadSensitivity);
         newVal = Mathf.Clamp(newVal, _minimumRotY, _maximumRotY);
         _debugRotEuler.x = newVal;
      }

      DebugRotateTrans.localEulerAngles = _debugRotEuler;
	}

   void RecenterCamera()
   {
      if(OVRManager.display != null)
         OVRManager.display.RecenterPose();
      _debugRotEuler = Vector3.zero;
   }
}
