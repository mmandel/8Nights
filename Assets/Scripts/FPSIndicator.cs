//
// An FPS indicator appropriate for use in the Rift (i.e works on 3D text)
//

using UnityEngine;
using System.Collections;

public class FPSIndicator : MonoBehaviour 
{
	
	// Attach this to a 3D Text Mesh to make a frames/second indicator.
	//
	// It calculates frames/second over each updateInterval,
	// so the display does not keep changing wildly.
	//
	// It is also fairly accurate at very low FPS counts (<10).
	// We do this not by simply counting frames per interval, but
	// by accumulating FPS for each frame. This way we end up with
	// correct overall FPS even if the interval renders something like
	// 5.5 frames.
	
	public  float updateInterval = 0.5F;
	public  bool startShowing = false;
	
	private float accum   = 0; // FPS accumulated over the interval
	private int   frames  = 0; // Frames drawn over the interval
	private float timeleft; // Left time for current interval
	private TextMesh _textMesh = null;
	private bool _showingMesh = true;
	
	void Start()
	{
		_showingMesh = startShowing;
		_textMesh = this.gameObject.GetComponent<TextMesh>();
		if( !_textMesh )
		{
			Debug.Log("FPSIndicator needs a TextMesh component!");
			enabled = false;
			return;
		}
		timeleft = updateInterval;  
	}
	
	void Update()
	{

		//toggle on/off with 'f' key
      if (Input.GetKeyDown(KeyCode.F))
         _showingMesh = !_showingMesh;
	
		this.renderer.enabled = _showingMesh;
		if(!_showingMesh)
			return;
		timeleft -= Time.deltaTime;
		accum += Time.timeScale/Time.deltaTime;
		++frames;
		
		// Interval ended - update GUI text and start new interval
		if( timeleft <= 0.0 )
		{
			// display two fractional digits (f2 format)
			float fps = accum/frames;
			string format = System.String.Format("{0:F2} FPS",fps);
			_textMesh.text = format;
			
			if(fps < 55)
				_textMesh.renderer.material.color = Color.yellow;
			else if(fps < 30)
				_textMesh.renderer.material.color = Color.red;
			else
				_textMesh.renderer.material.color = Color.green;
			//	DebugConsole.Log(format,level);
			timeleft = updateInterval;
			accum = 0.0F;
			frames = 0;
		}
	}
}