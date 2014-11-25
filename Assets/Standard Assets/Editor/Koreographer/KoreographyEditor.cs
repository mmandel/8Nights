//----------------------------------------------
//            	   Koreographer                 
//      Copyright © 2014 Sonic Bloom, LLC      
//----------------------------------------------

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class WaveDisplayState
{
	public int samplesPerPixel = 1;
	public int drawStartOffsetInPixels = 0;
	public int firstSamplePackToDraw = 0;
	public int playheadSamplePosition = 0;
	public WaveDisplayType displayType = WaveDisplayType.Both;
}

public class KoreographyEditor : EditorWindow
{
	#region Fields

	bool bIsWaveDisplayFocused = false;

	AudioSource audioSrc = null;
	int estimatedSampleTime = 0;

	// Cached Asset Path
	string assetPath = string.Empty;

	Koreography editKoreo = null;
	
	TempoSectionDef editTempoSection = null;
	
	KoreographyTrack editTrack = null;
	
	WaveDisplayState displayState = new WaveDisplayState();

	WaveDisplay waveDisplay = null;

	int maxSamplesPerPixel = 1;

	bool bShowBPM = true;			// Tempo display switch: [BPM, Samples Per Beat]
	bool bShowPlayhead = false;
	
	List<System.Type> payloadTypes = new List<System.Type>();
	List<string> payloadTypeNames = new List<string>();
	int currentPayloadTypeIdx = -1;

	bool bCreateOneOff = true;

	Vector2 scrollPosition = Vector2.zero;
	bool bScrollWithPlayhead = true;
	
	bool bSnapTimingToBeat = true;
	int snapSubBeatCount = 0;

	// Data!
	KoreographyEvent buildEvent = null;
	List<KoreographyEvent> selectedEvents = new List<KoreographyEvent>();
	List<KoreographyEvent> eventsToHighlight = new List<KoreographyEvent>();

	// Cut/Copy/Paste!
	List<KoreographyEvent> clippedEvents = new List<KoreographyEvent>();
	
	// Mouse dragging related.
	EventEditMode eventEditMode = EventEditMode.None;
	float eventEditClickX = 0f;
	Vector2 dragStartPos = Vector2.zero;
	Vector2 dragEndPos = Vector2.zero;
	List<KoreographyEvent> dragSelectedEvents = new List<KoreographyEvent>();

	// GUI Skin stuff.
	GUISkin koreographyEditorSkin = null;
	GUIStyle lcdStyleLeft = null;
	GUIStyle lcdStyleRight = null;
	GUIStyle lcdStyleCenter = null;

	// Button textures.
	Texture playTex = null;
	Texture stopTex = null;
	Texture pauseTex = null;
	Texture prevBeatTex = null;
	Texture nextBeatTex = null;
	Texture beatTex = null;

	List<Rect> lcdRects = new List<Rect>();

	enum LCDDisplayMode
	{
		SampleTime,
		MusicTime,
		SolarTime,
	}

	LCDDisplayMode lcdMode = LCDDisplayMode.MusicTime;

	enum ControlMode
	{
		Select,
		Author,
	}

	ControlMode controlMode = ControlMode.Select;

	#endregion
	#region Properties

	private AudioClip EditClip
	{
		get
		{
			return editKoreo == null ? null : editKoreo.SourceClip;
		}
		set
		{
			if (editKoreo != null && editKoreo.SourceClip != value)
			{
				Undo.RecordObject(editKoreo, "Set Audio Clip");
				editKoreo.SourceClip = value;
				EditorUtility.SetDirty(editKoreo);
			}
		}
	}

	#endregion
	#region Static Methods

	[MenuItem("Audio Tools/Koreography Editor")]
	static void ShowWindow()
	{
		EditorWindow win = EditorWindow.GetWindow(typeof(KoreographyEditor), false, "Koreography Editor");
		win.minSize = new Vector2(900f, 710f);
	}

	#endregion
	#region Methods

	void OnEnable()
	{
		wantsMouseMove = true;
		EditorApplication.modifierKeysChanged += Repaint;
		
		// Initialize the AudioSource for playback and editing.
		if (audioSrc == null)
		{
			GameObject go = EditorUtility.CreateGameObjectWithHideFlags("__KOREOGRAPHER__", HideFlags.HideAndDontSave, new System.Type[]{typeof(AudioSource)});
			audioSrc = go.GetComponent<AudioSource>();
			audioSrc.volume = 0.75f;
		}

		// Load in the GUIStyles.
		if (koreographyEditorSkin == null)
		{
			koreographyEditorSkin = EditorGUIUtility.Load("Koreographer/GUI/KoreographyEditorSkin.guiskin") as GUISkin;
			lcdStyleLeft = koreographyEditorSkin.GetStyle("LCDLeft");
			lcdStyleRight = koreographyEditorSkin.GetStyle("LCDRight");
			lcdStyleCenter = koreographyEditorSkin.GetStyle("LCDCenter");

			playTex = EditorGUIUtility.Load("Koreographer/Textures/Play.png") as Texture;
			stopTex = EditorGUIUtility.Load("Koreographer/Textures/Stop.png") as Texture;
			pauseTex = EditorGUIUtility.Load("Koreographer/Textures/Pause.png") as Texture;
			prevBeatTex = EditorGUIUtility.Load("Koreographer/Textures/PrevBeat.png") as Texture;
			nextBeatTex = EditorGUIUtility.Load("Koreographer/Textures/NextBeat.png") as Texture;
			beatTex = EditorGUIUtility.Load("Koreographer/Textures/Beat.png") as Texture;
		}
		
		if (payloadTypes.Count == 0)
		{
			payloadTypes = KoreographyEvent.GetPayloadTypes().ToList();

			// Create the list of class names for use in the GUI.
			payloadTypeNames.Clear();
			payloadTypeNames.Add("No Payload");
			payloadTypeNames.AddRange(payloadTypes.Select(x => GetFriendlyNameOfPayloadType(x)));
		}

		if (buildEvent != null)
		{
			//Debug.LogWarning("The Build Event was not properly cleared during the last use.  Did you just recompile scripts?");
			buildEvent = null;
		}

		// Clean out all the lists.
		selectedEvents.Clear();
		dragSelectedEvents.Clear();
		eventsToHighlight.Clear();
	}

	void OnDisable()
	{
		EditorApplication.modifierKeysChanged -= Repaint;
	}

	void Update()
	{
		// Force a Repaint() while audio is playing to update the playhead.
		if (IsPlaying())
		{
			Repaint();

			int rawSampleTime = GetCurrentRawMusicSample();

			if (rawSampleTime == estimatedSampleTime)
			{
				estimatedSampleTime += (int)((float)EditClip.frequency * audioSrc.pitch * Time.deltaTime);
			}
			else
			{
				estimatedSampleTime = rawSampleTime;
			}
		}
		else
		{
			estimatedSampleTime = GetCurrentRawMusicSample();
		}
	}

	void OnDestroy()
	{
		// Stop and delete the AudioSource.
		StopAudio();
		GameObject.DestroyImmediate(audioSrc.gameObject);
	}

	void OnGUI()
	{
		// Sanity checking.
		{
			// Make sure that we don't have any empty entries or otherwise bad data to worry about.
			ValidateKoreographyAndTrackData();

			// Check that our editTrack is okay.  It can be deleted out from underneath us by direct deletion in the asset library or operating system.
			if (editTrack != null && (editKoreo == null || editKoreo.GetIndexOfTrack(editTrack) < 0))
			{
				KoreographyTrack newEditTrack = null;
				if (editKoreo != null)
				{
					newEditTrack = editKoreo.GetTrackAtIndex(0);
				}
				SetNewEditTrack(newEditTrack);
			}

			// Check that the editTempoSection is okay.  Can be deleted out from underneath by compiling from the editor...
			if (editTempoSection == null || (editKoreo == null || editKoreo.GetIndexOfTempoSection(editTempoSection) < 0))
			{
				editTempoSection = null;
				if (editKoreo != null)
				{
					editTempoSection = editKoreo.GetTempoSectionAtIndex(0);
				}
			}

			// Use for-loops below: Cannot do destructive tasks (Remove()) within foreach loops.

			// Check that our selectedEvents are okay.  They can be deleted out from underneath us by inspecting the event in the editor.
			for (int i = selectedEvents.Count - 1; i >= 0; --i)
			{
				KoreographyEvent evt = selectedEvents[i];
				if (evt == null || (editTrack == null || editTrack.GetIDForEvent(evt) < 0))
				{
					selectedEvents.RemoveAt(i);
				}
			}

			// Also check dragSelectedEvents.
			for (int i = dragSelectedEvents.Count - 1; i >= 0; --i)
			{
				KoreographyEvent evt = dragSelectedEvents[i];
				if (evt == null || (editTrack == null || editTrack.GetIDForEvent(evt) < 0))
				{
					dragSelectedEvents.RemoveAt(i);
				}
			}

			// Also check clippedEvents(??).
			for (int i = clippedEvents.Count - 1; i >= 0; --i)
			{
				if (clippedEvents[i] == null)
				{
					clippedEvents.RemoveAt(i);
				}
			}
		}

		// Input checking - This usually changes State.  Do this work BEFORE or AFTER all controls are drawn so that we don't change the layout mid-call!
		//  Doing this "before" allows us to Use the input events before the controls get a chance at them.
		{
			if (Event.current.type == EventType.ValidateCommand)
			{

				switch (Event.current.commandName)
				{
				case "Cut":
					if (selectedEvents.Count > 0 && bIsWaveDisplayFocused)
					{
						Event.current.Use();
					}
					break;
				case "Copy":
					if (selectedEvents.Count > 0 && bIsWaveDisplayFocused)
					{
						Event.current.Use();
					}
					break;
				case "Paste":
					if (clippedEvents.Count > 0 && selectedEvents.Count > 0 && bIsWaveDisplayFocused && waveDisplay != null)
					{
						Event.current.Use();
					}
					break;
				case "SelectAll":
					if (editTrack != null && bIsWaveDisplayFocused)
					{
						Event.current.Use();
					}
					break;
				case "UndoRedoPerformed":
					Event.current.Use();	// For now, just Use() it so we don't have to go into the rest of the function unnecessarily.
					break;
				default:
//					Debug.Log("Unknown command name encountered for Event ValidateCommand: " + Event.current.commandName);
					break;
				}
			}

			if (Event.current.type == EventType.ExecuteCommand)
			{
				switch (Event.current.commandName)
				{
				case "Cut":
					CutSelectedEvents();
					Event.current.Use();
					break;
				case "Copy":
					CopySelectedEvents();
					Event.current.Use();
					break;
				case "Paste":
					PasteOverSelectedEvents();
					Event.current.Use();
					break;
				case "SelectAll":
					SelectAll();
					Event.current.Use();
					break;
				case "UndoRedoPerformed":
					Event.current.Use();
					break;
				default:
//					Debug.Log("Unhandled command name encountered for Event ExecuteCommand: " + Event.current.commandName);
					break;
				}
			}

			if (Event.current.isKey)
			{
				HandleKeyInput();
			}

			if (Event.current.isMouse ||
			    Event.current.type == EventType.Ignore && Event.current.rawType == EventType.MouseUp)
			{
				HandleMouseInput();
			}

			// This happens before the mouse-down button (at least on Mac(?)).
			if (Event.current.type == EventType.ContextClick)
			{
				Vector2 mousePos = Event.current.mousePosition;
				if (waveDisplay != null && waveDisplay.ContainsPoint(mousePos))
				{
					// If an event is being hovered over and is not otherwise already in the
					//  selection, replace the selection.
					KoreographyEvent evt = waveDisplay.GetEventAtLoc(mousePos);
					if (evt != null && ! selectedEvents.Contains(evt))
					{
						selectedEvents.Clear();
						selectedEvents.Add(evt);
					}

					GenericMenu menu = new GenericMenu();

					// Cut/Copy
					{
						if (selectedEvents.Count > 0)
						{
							menu.AddItem(new GUIContent("Cut"), false, CutSelectedEvents);
							menu.AddItem(new GUIContent("Copy"), false, CopySelectedEvents);
						}
						else
						{
							menu.AddDisabledItem(new GUIContent("Cut"));
							menu.AddDisabledItem(new GUIContent("Copy"));
						}
					}

					// Paste.
					{
						if (clippedEvents.Count > 0)
						{
							// Paste either over the selection or where the mouse is.
							if (selectedEvents.Count > 0)
							{
								menu.AddItem(new GUIContent("Paste"), false, PasteOverSelectedEvents);
								menu.AddItem(new GUIContent("Paste Payload Only"), false, PastePayloadToSelectedEvents);
							}
							else
							{
								int samplePos = waveDisplay.GetSamplePositionOfPoint(mousePos, displayState);

								if (bSnapTimingToBeat)
								{
									samplePos = editKoreo.GetSampleOfNearestBeat(samplePos, snapSubBeatCount);
								}

								menu.AddItem(new GUIContent("Paste"), false, PasteEventsAtLocation, samplePos);
								menu.AddDisabledItem(new GUIContent("Paste Payload Only"));
							}
						}
						else
						{
							menu.AddDisabledItem(new GUIContent("Paste"));
							menu.AddDisabledItem(new GUIContent("Paste Payload Only"));
						}
					}

					menu.AddSeparator(string.Empty);

					// Playback
					{
						int samplePos = waveDisplay.GetSamplePositionOfPoint(mousePos, displayState);
						menu.AddItem(new GUIContent("Play From Here"), false, PlayFromSampleLocation, samplePos);
					}
					
					menu.ShowAsContext();
					
					Event.current.Use();
				}
			}

			// Unlike other input, we test for scrolling before drawing anything.  This needs to occur prior to the ScrollView because otherwise the
			//  ScrollView will swallow the input.  This is safe because currently it only affects zoom.
			HandleScrollInput();

			// Early out!  No need to further process a Used event.
			if (Event.current.type == EventType.Used)
			{
				return;
			}
		}

		EditorGUILayout.BeginHorizontal();

		{
			// Edit Koreography.
			Koreography newKoreo = EditorGUILayout.ObjectField("Koreography", editKoreo, typeof(Koreography), false) as Koreography;

			if (newKoreo != editKoreo)
			{
				SetNewEditKoreo(newKoreo);
			}
		}

		if (GUILayout.Button("New Koreography", GUILayout.MaxWidth(110f)))
		{
			// Get the save location and file type.  Then massage it for the AssetDatabase functions.
			string path = string.Empty;
			if (assetPath == string.Empty)
			{
				path = EditorUtility.SaveFilePanelInProject("Save New Koreography Asset...", "NewKoreography",
			    	                                               "asset", "Select a location and file name for the new Koreography.");
			}
			else
			{
				path = EditorUtility.SaveFilePanel("Save New Koreography Asset...", assetPath, "NewKoreography", "asset");
			}

			if (path != string.Empty)
			{
				assetPath = path;		// Store off valid path.
			}
			path = path.Replace(Application.dataPath, "Assets");
			
			if (!string.IsNullOrEmpty(path) && Path.GetExtension(path) == ".asset" && Path.GetDirectoryName(path).Contains("Assets"))
			{
				// Instantiate and init the new Track object.
				Koreography newKoreo = ScriptableObject.CreateInstance<Koreography>();

				// Create the new Track asset and save it!
				AssetDatabase.CreateAsset(newKoreo, path);
				AssetDatabase.SaveAssets();

				// Handle all of the associated setup.
				SetNewEditKoreo(newKoreo);
			}
		}

		EditorGUILayout.EndHorizontal();

		GUI.enabled = editKoreo != null;

		// Audio Clip.
		{
			AudioClip newClip = EditorGUILayout.ObjectField("Audio Clip", EditClip, typeof(AudioClip), false) as AudioClip;
			if (newClip == null)
			{
				EditClip = null;
				// TODO: Clear any pre-existing data from the waveDisplay.
			}
			else if (EditClip != newClip || waveDisplay == null)
			{	
				if (!IsAudioClipValid(newClip)) // Check that we have a valid clip for the tool.
				{
					EditorUtility.DisplayDialog("Incompatible Settings Detected", "If the AudioClip is Compressed (OGG), please ensure its Load Type is not set to 'Compressed In Memory' in the clip settings.", "Okay");
				}
				else
				{
					EditClip = newClip;
					InitNewClip(EditClip);
				}
			}
		}

		EditorGUILayout.LabelField("Tempo Section Settings:", EditorStyles.boldLabel);
		EditorGUI.indentLevel++;

		EditorGUILayout.BeginHorizontal();

		// Section list.
		{
			string[] sectionNames = new string[]{""};

			int selectedIdx = 0;
			if (editKoreo != null)
			{
				sectionNames = editKoreo.GetTempoSectionNames();

				// Section names need to be unique to properly show up in the Popup.
				for (int i = 0; i < sectionNames.Length; ++i)
				{
					sectionNames[i] = (i + 1) + ") " + sectionNames[i];
				}

				if (editTempoSection != null)
				{
					selectedIdx = editKoreo.GetIndexOfTempoSection(editTempoSection);
				}
			}

			int newSelectedIdx = EditorGUILayout.Popup("Tempo Section to Edit", selectedIdx, sectionNames, GUILayout.MaxWidth(300f));

			if (selectedIdx != newSelectedIdx || (editKoreo != null && editTempoSection == null))
			{
				editTempoSection = editKoreo.GetTempoSectionAtIndex(newSelectedIdx);
			}
		}

		GUI.enabled = editTempoSection != null;

		// Allow editing of tempo section name.
		{
			EditorGUIUtility.labelWidth = 92f;
			string oldName = editTempoSection != null ? editTempoSection.SectionName : string.Empty;
			string newName = EditorGUILayout.TextField("Section Name", oldName, GUILayout.MaxWidth(200f));
			EditorGUIUtility.labelWidth = 0f;
			
			if (newName != oldName)
			{
				Undo.RecordObject(editKoreo, "Set Tempo Section Name");
				editTempoSection.SectionName = newName;
				EditorUtility.SetDirty(editKoreo);
			}
		}

		// Add Section Before/After buttons.
		{
			GUILayout.FlexibleSpace();

			GUI.enabled = (editTempoSection != null) && (editKoreo != null) && (editKoreo.GetNumTempoSections() > 1);

			if (GUILayout.Button("Delete", GUILayout.MaxWidth(50f)))
		    {
				Undo.RecordObject(editKoreo, "Delete Tempo Section " + editTempoSection.SectionName);

				int editSectionIdx = editKoreo.GetIndexOfTempoSection(editTempoSection);
				// Grab the prior section unless we deleted the first one!
				// TODO: verify that the GetTempoSectionAtIndex() doesn't return -1?
				editTempoSection = editKoreo.GetTempoSectionAtIndex(editSectionIdx == 0 ? 1 : editSectionIdx - 1);

				editKoreo.RemoveTempoSectionAtIndex(editSectionIdx);

				EditorUtility.SetDirty(editKoreo);
				Repaint();
			}

			GUI.enabled = (editTempoSection != null);

			if (GUILayout.Button("Insert New Before", GUILayout.MaxWidth(108f)))
			{
				Undo.RecordObject(editKoreo, "Insert New Tempo Section");

				TempoSectionDef newSection = editKoreo.InsertTempoSectionAtIndex(editKoreo.GetIndexOfTempoSection(editTempoSection));

				newSection.StartSample = editTempoSection.StartSample;
				newSection.SamplesPerBeat = editTempoSection.SamplesPerBeat;

				editTempoSection = newSection;

				EditorUtility.SetDirty(editKoreo);
				Repaint();
			}

			if (GUILayout.Button("Insert New After", GUILayout.MaxWidth(108f)))
			{
				Undo.RecordObject(editKoreo, "Insert New Tempo Section");

				TempoSectionDef newSection = editKoreo.InsertTempoSectionAtIndex(editKoreo.GetIndexOfTempoSection(editTempoSection) + 1);

				newSection.StartSample = editTempoSection.StartSample;
				newSection.SamplesPerBeat = editTempoSection.SamplesPerBeat;

				editTempoSection = newSection;

				EditorUtility.SetDirty(editKoreo);
				Repaint();
			}
		}

		EditorGUILayout.EndHorizontal();
		EditorGUILayout.BeginHorizontal();

		// StartSample, SamplesPerBeat/BPM
		{
			// Disallow editing the first section's StartSample.
			GUI.enabled = (editKoreo != null && editKoreo.GetIndexOfTempoSection(editTempoSection) > 0);

			int newStartSample = EditorGUILayout.IntField("Start Sample", (editTempoSection != null) ? editTempoSection.StartSample : 0);
			if (editTempoSection != null && editTempoSection.StartSample != newStartSample)
			{
				Undo.RecordObject(editKoreo, "Set Tempo Section Start Sample");

				editTempoSection.StartSample = newStartSample;

				// Verify that we have the correct ordering.
				editKoreo.EnsureTempoSectionOrder();

				EditorUtility.SetDirty(editKoreo);
			}

			// Add some space to separate the two float fields.
			GUILayout.Space(95f);

			if (bShowBPM)
			{
				GUI.enabled = (EditClip != null) && (editTempoSection != null);
				// BPM Settings
				{
					// Default to 44100hz if no editClip exists.
					int frequency = (EditClip != null) ? EditClip.frequency : 44100;
					EditorGUIUtility.labelWidth = 42f;
					float bpm = (editTempoSection != null) ? ((float)frequency / editTempoSection.SamplesPerBeat * 60f) : 0f;
					float newBPM = EditorGUILayout.FloatField(bpm, GUILayout.MaxWidth(80f));
					if (newBPM != bpm && newBPM > 0f && editTempoSection != null)
					{
						Undo.RecordObject(editKoreo, "Set Tempo Section BPM");
						editTempoSection.SamplesPerBeat = (float)frequency / (newBPM / 60f);
						EditorUtility.SetDirty(editKoreo);
					}
					EditorGUIUtility.labelWidth = 0f;
				}
			}
			else
			{
				GUI.enabled = editTempoSection != null;
				
				EditorGUIUtility.labelWidth = 110f;
				float newSamplesPerBeat = EditorGUILayout.FloatField((editTempoSection != null) ? editTempoSection.SamplesPerBeat : 0f, GUILayout.MaxWidth(80f));
				EditorGUIUtility.labelWidth = 0f;
				if (editTempoSection != null && editTempoSection.SamplesPerBeat != newSamplesPerBeat)
				{
					Undo.RecordObject(editKoreo, "Set Tempo Section Samples Per Beat");
					editTempoSection.SamplesPerBeat = newSamplesPerBeat;
					EditorUtility.SetDirty(editKoreo);
				}
			}

			if (GUILayout.Toggle(bShowBPM, new GUIContent("BPM", "Beats Per Minute"), EditorStyles.radioButton, GUILayout.Width(40f)))
			{
				bShowBPM = true;
			}
			if (GUILayout.Toggle(!bShowBPM, new GUIContent("Samples Per Beat", "Number of samples that span a single beat"), EditorStyles.radioButton, GUILayout.Width(110f)))
			{
				bShowBPM = false;
			}
		}

		GUILayout.FlexibleSpace();

		EditorGUILayout.EndHorizontal();

		// Beats Per Measure
		{
			GUI.enabled = editTempoSection != null;
			int newBeatsPerMeasure = EditorGUILayout.IntField("Beats Per Measure", (editTempoSection != null) ? editTempoSection.BeatsPerMeasure : 0, GUILayout.MaxWidth(180f));
			if (editTempoSection != null && editTempoSection.BeatsPerMeasure != newBeatsPerMeasure)
			{
				Undo.RecordObject(editKoreo, "Set Tempo Section Beats Per Measure");
				editTempoSection.BeatsPerMeasure = newBeatsPerMeasure;
				EditorUtility.SetDirty(editKoreo);
			}
		}

		GUI.enabled = editKoreo != null && editKoreo.GetNumTracks() > 0;

		EditorGUI.indentLevel--;
		EditorGUILayout.LabelField("Track Settings:", EditorStyles.boldLabel);
		EditorGUI.indentLevel++;

		EditorGUILayout.BeginHorizontal();

		// Track list.
		{
			string[] trackNames = new string[]{""};

			int selectedIdx = 0;
			if (editKoreo != null)
			{
				trackNames = editKoreo.GetEventIDs();
				if (editTrack != null)
				{
					// TODO: Check that the selectedIdx isn't -1?
					selectedIdx = editKoreo.GetIndexOfTrack(editTrack);
				}
			}

			int newSelectedIdx = EditorGUILayout.Popup("Track to Edit", selectedIdx, trackNames);

			if (selectedIdx != newSelectedIdx ||
			    // This next line ensures the editor will work again after Unity compiles scripts.
			    selectedIdx >= 0 && (waveDisplay != null && waveDisplay.GetEventTrack() == null))
			{
				// For these to be different we MUST have an editKoreo.
				SetNewEditTrack(editKoreo.GetTrackAtIndex(newSelectedIdx));
			}
		}

		GUI.enabled = editTrack != null;

		// Make the track name adjustable.
		{
			EditorGUIUtility.labelWidth = 96f;
			string oldID = editTrack != null ? editTrack.EventID : string.Empty;
			string newID = EditorGUILayout.TextField("Track Event ID", oldID, GUILayout.MaxWidth(300f));
			EditorGUIUtility.labelWidth = 0f;

			if (newID != oldID)
			{
				if (!editKoreo.DoesTrackWithEventIDExist(newID))
				{
					Undo.RecordObject(editTrack, "Set Event ID");
					// Won't get here unless editTrack is valid (GUI is disabled otherwise).
					editTrack.EventID = newID;
					EditorUtility.SetDirty(editTrack);
				}
			}
		}

		GUI.enabled = editKoreo != null;

		// Load an existing track into the Koreography.
		if (GUILayout.Button("Load Track", GUILayout.MaxWidth(80f)))
		{
			// Get an asset path and massage it to work with the loading mechanisms.
			string path = EditorUtility.OpenFilePanel("Select an KoreographyTrack Asset...", assetPath, "asset");
			if (path != string.Empty)
			{
				assetPath = path;		// Store off valid path.
			}
			path = path.Replace(Application.dataPath, "Assets");

			KoreographyTrack loadTrack = AssetDatabase.LoadAssetAtPath(path, typeof(KoreographyTrack)) as KoreographyTrack;

			if (loadTrack != null)
			{
				// Won't get here unless the editKoreo is around.  The GUI button won't work otherwise.
				if (loadTrack.SourceClip != editKoreo.SourceClip)
				{
					if (EditorUtility.DisplayDialog("Track/Koreography Audio Clip Mismatch", "The AudioClip assigned to the " +
						"selected Track does not match that in the Koreography.\n\nForce the Track setting to match the" +
						"Koreography?  This will break other Koreography that this Track is in!", "Okay", "Cancel"))
					{
						// A bit dissonant but this gets combined with the next Undo operation at the end of the frame.
						//  The problem is that we need to set this before seeing if it can be added!
						Undo.RecordObject(loadTrack, "Change Audio Clip in Loaded Track");
						loadTrack.SourceClip = editKoreo.SourceClip;

						if (editKoreo.CanAddTrack(loadTrack))
						{
							// Change can happen!  Record the object and then perform the operation!
							Undo.RecordObject(editKoreo, "Load Track");
							editKoreo.AddTrack(loadTrack);
							EditorUtility.SetDirty(editKoreo);
							SetNewEditTrack(loadTrack);
						}
					}
				}
				else if (!editKoreo.CanAddTrack(loadTrack))
				{
					EditorUtility.DisplayDialog("Load Error Occurred!", "The selected Track could not be loaded.  Is the track already in the Koreography?  " +
						"Or is there another Track with the same Event ID (" + loadTrack.EventID + ") already in the Koreography?", "Okay");
				}
				else
				{
					// Record the object and then commit!
					Undo.RecordObject(editKoreo, "Load Track");
					editKoreo.AddTrack(loadTrack);
					EditorUtility.SetDirty(editKoreo);
					SetNewEditTrack(loadTrack);
				}
			}
		}
		// Create a new track and add to the Koreography.
		if (GUILayout.Button("New Track", GUILayout.MaxWidth(80f)))
		{
			// Get the save location and file type.  Then massage it for the AssetDatabase functions.
			string path = string.Empty;
			if (assetPath == string.Empty)
			{
				path = EditorUtility.SaveFilePanelInProject("Save New KoreographyTrack Asset...", "NewKoreographyTrack",
			    	                                             "asset", "Select a location and file name for the new Track.");
			}
			else
			{
				path = EditorUtility.SaveFilePanel("Save New KoreographyTrack Asset...", assetPath, "NewKoreographyTrack", "asset");
			}

			if (path != string.Empty)
			{
				assetPath = path;		// Store off valid path.
			}
			path = path.Replace(Application.dataPath, "Assets");

			if (!string.IsNullOrEmpty(path) && Path.GetExtension(path) == ".asset" && Path.GetDirectoryName(path).Contains("Assets"))
			{
				// Instantiate and init the new Track object.
				KoreographyTrack newTrack = ScriptableObject.CreateInstance<KoreographyTrack>();
				newTrack.SourceClip = editKoreo.SourceClip;
				newTrack.EventID = Path.GetFileNameWithoutExtension(path);
				
				// File a bug?  Does not work as expected: files are not properly uncreated.
				// See: http://answers.unity3d.com/questions/674429/calling-registercreatedobjectundo-on-an-asset-is-b.html
				//Undo.RegisterCreatedObjectUndo(newTrack, "New Track");

				if (editKoreo.CanAddTrack(newTrack))
				{
					Undo.RecordObject(editKoreo, "New Track");

					editKoreo.AddTrack(newTrack);
					
					EditorUtility.SetDirty(editKoreo);
				}

				SetNewEditTrack(newTrack);

				// Create the new Track asset and save it!
				AssetDatabase.CreateAsset(newTrack, path);
				AssetDatabase.SaveAssets();
			}
		}
		
		EditorGUILayout.EndHorizontal();

		EditorGUI.indentLevel--;

		GUI.enabled = EditClip != null;

		// Handle markup generation.
		{
			// Start by checking if an event exists and, if so, continue it!
			if (EditClip != null && buildEvent != null && IsPlaying())
			{
				ContinueNewEvent(GetCurrentEstimatedMusicSample());
			}
		}

		// Buttons.
		EditorGUILayout.BeginHorizontal();

		// Playback controls.
		if (IsPlaying())
		{
			if (GUILayout.Button(pauseTex))
			{
				PauseAudio();

				// Without this the Play/Pause Button itself takes focus.
				//  Which is meaningless.
				FocusWaveDisplayWindow();
			}
		}
		else
		{
			if (GUILayout.Button(playTex))
			{
				// Playback always causes window focus.
				PlayAudio();
			}
		}

		if (GUILayout.Button(stopTex))
		{
			StopAudio();

			// Without this the Stop Button itself takes focus.  Which is meaningless.
			FocusWaveDisplayWindow();
		}
		
		GUILayout.FlexibleSpace();

		// Used for display below here.
		GUI.changed = false;
		{
			if (GUILayout.Toggle(controlMode == ControlMode.Select, new GUIContent("Select", "(a) Put the editor in Selection mode"), EditorStyles.miniButtonLeft, GUILayout.Width(45), GUILayout.Height(20)))
			{
				controlMode = ControlMode.Select;
			}
			if (GUILayout.Toggle(controlMode == ControlMode.Author, new GUIContent("Draw", "(s) Put the editor in Draw mode"), EditorStyles.miniButtonRight, GUILayout.Width(45), GUILayout.Height(20)))
			{
				controlMode = ControlMode.Author;
			}

			GUILayout.FlexibleSpace();

			// Zoom.
			{
				EditorGUILayout.LabelField("Zoom", GUILayout.MaxWidth(40f));
				int newSamplesPerPixel = (int)GUILayout.HorizontalSlider((float)displayState.samplesPerPixel, 1f, (float)maxSamplesPerPixel, GUILayout.MinWidth(250f));

				// Update the scroll position if the samplesPerPixel (zoom factor) changes.
				if (newSamplesPerPixel != displayState.samplesPerPixel)
				{
					SetNewSamplesPerPixel(newSamplesPerPixel);
				}
			}

			GUILayout.FlexibleSpace();

			// Payload selection.
			EditorGUIUtility.labelWidth = 50f;
			EditorGUIUtility.fieldWidth = 100f;

			// +1 to string index input and -1 from string index output.  This keeps the index properly
			//  in the range of the actual Type list which DOESN'T have the "No Payload" option.
			currentPayloadTypeIdx = EditorGUILayout.Popup("Payload", currentPayloadTypeIdx + 1, payloadTypeNames.ToArray()) - 1;

			// Runtime creation mode.
			if (GUILayout.Toggle(bCreateOneOff, new GUIContent("OneOff", "(z) Created events are mapped to a single sample"), EditorStyles.miniButtonLeft, GUILayout.Width(45), GUILayout.Height(20)))
			{
				bCreateOneOff = true;
			}
			if (GUILayout.Toggle(!bCreateOneOff, new GUIContent("Span", "(x) Created events span multiple samples"), EditorStyles.miniButtonRight, GUILayout.Width(45), GUILayout.Height(20)))
			{
				bCreateOneOff = false;
			}
		}
		if (GUI.changed)
		{
			FocusWaveDisplayWindow();
			GUI.changed = false;
		}

		EditorGUILayout.EndHorizontal();


		{
			// Wave Display Area.
			if (waveDisplay != null)
			{
				displayState.playheadSamplePosition = GetCurrentEstimatedMusicSample();

				// Handle metadata.
				if (IsPlaying() && bScrollWithPlayhead)
				{
					int sampleDistanceToPlayhead = WaveDisplay.pixelDistanceToPlayheadMarker * displayState.samplesPerPixel;
					
					// Determine where the first sample should be drawn and which is the first sample.
					//  This is necessary as we can have a starting gap.
					if (sampleDistanceToPlayhead > displayState.playheadSamplePosition)
					{
						displayState.firstSamplePackToDraw = 0;
						displayState.drawStartOffsetInPixels = WaveDisplay.pixelDistanceToPlayheadMarker - (displayState.playheadSamplePosition / displayState.samplesPerPixel);
					}
					else
					{
						displayState.drawStartOffsetInPixels = 0;
						displayState.firstSamplePackToDraw = displayState.playheadSamplePosition - sampleDistanceToPlayhead;
					}

					scrollPosition.x = (float)(displayState.firstSamplePackToDraw / displayState.samplesPerPixel);
				}
				else
				{
					displayState.firstSamplePackToDraw = (int)scrollPosition.x * displayState.samplesPerPixel;
					displayState.drawStartOffsetInPixels = 0;
				}

				// Handle display.
				{
					// Add some space for the LCD readouts.
					GUILayout.Space(8f);
					Rect lcdRect = GUILayoutUtility.GetLastRect();

					int windowHeight = 400;
					int windowWidth = GetWidthOfWaveDisplayWindow();
					int contentWidth = windowWidth;

					if (EditClip != null)
					{
						contentWidth = EditClip.samples / displayState.samplesPerPixel;

						// Allow us to scrub all the way to the end, given the pixelDistanceToPlayheadMarker
						if (IsPlaying() && bScrollWithPlayhead)
						{
							contentWidth += (windowWidth - WaveDisplay.pixelDistanceToPlayheadMarker);
						}
					}

					GUI.skin.scrollView.margin = GUI.skin.box.margin;

					Vector2 newScrollPos = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Width(windowWidth - 1f), GUILayout.Height(windowHeight + GUI.skin.horizontalScrollbar.fixedHeight + GUI.skin.box.padding.vertical + 2f));
					GUILayout.Box("", GUILayout.Width(contentWidth), GUILayout.Height(windowHeight));
					EditorGUILayout.EndScrollView();

					if (newScrollPos.x != scrollPosition.x)
					{
						scrollPosition = newScrollPos;

						if (IsPlaying() && bScrollWithPlayhead &&
						    scrollPosition.x < contentWidth - windowWidth && 		// If we get to full don't continuously reset the timeSamples.
						    scrollPosition.x > 256 / displayState.samplesPerPixel)	// If we get to the beginning, don't continuously reset the timeSamples.
						{
							// If we're auto scrolling, we need to keep the distToPlayheadMarker in mind, otherwise it will rewind over time.
							audioSrc.timeSamples = (int)((scrollPosition.x + WaveDisplay.pixelDistanceToPlayheadMarker) * displayState.samplesPerPixel);
						}
					}

					Rect waveBoxRect = GUILayoutUtility.GetLastRect();
					waveBoxRect.width = windowWidth;
					waveBoxRect.height = windowHeight;
					waveBoxRect.y += 4f;

					// Integer division.  This keeps us "samples-per-pixel" aligned so we don't get odd reshaping of the waveform as the read head moves.
					displayState.firstSamplePackToDraw = (displayState.firstSamplePackToDraw / displayState.samplesPerPixel) * displayState.samplesPerPixel;
#if UNITY_5_0
					// Make sure we fill up with AudioData if it's finally loaded.
					if (Event.current.type == EventType.Repaint &&
					    !waveDisplay.HasAudioData() &&
					    EditClip != null &&
					    EditClip.isReadyToPlay)  // Background loading is finished!
					{
						waveDisplay.SetAudioData(EditClip);
					}
#endif
					// Send a COPY of the displayState so other folks can't change it?!
					waveDisplay.Draw(waveBoxRect, displayState, editKoreo, bShowPlayhead, (dragStartPos == dragEndPos) ? selectedEvents : eventsToHighlight);

					// LCD Readouts.
					{
						if (Event.current.type == EventType.Repaint)
						{
							lcdRects.Clear();
						}

						float shiftDownAmount = GUI.skin.scrollView.margin.top + 1f;
						lcdRect.yMin += shiftDownAmount;
						lcdRect.height += shiftDownAmount + GUI.skin.scrollView.margin.top;
						lcdRect.width = position.width;
						lcdRect.xMin = 0f;
						GUI.BeginGroup(lcdRect);
						
						Rect lcd = new Rect();

						// Left-most-sample LCD.
						lcd.width = 150f;
						lcd.height = lcdRect.height;
						lcd.xMin = lcdStyleLeft.margin.left - 1f;

						//string solarTimeFmt = @"{0:d\.hh\:mm\:ss\.fff}";	// For TimeSpan.ToString(fmt) - not supported by Unity!
						string solarTimeFmt = "{0:00':'}{1:00':'}{2:00'.'}{3:000}";
						System.TimeSpan solarTime;
						string output = string.Empty;

						switch (lcdMode)
						{
						case LCDDisplayMode.SampleTime:
							output = string.Format("sample {0:0}", displayState.firstSamplePackToDraw);
							break;
						case LCDDisplayMode.MusicTime:
							output = GetMusicTimeForDisplayFromSample(displayState.firstSamplePackToDraw);
							break;
						case LCDDisplayMode.SolarTime:
							solarTime = System.TimeSpan.FromSeconds((double)displayState.firstSamplePackToDraw / (double)EditClip.frequency);
							output = string.Format(solarTimeFmt, solarTime.Hours, solarTime.Minutes, solarTime.Seconds, solarTime.Milliseconds);
							break;
						default:
							break;
						}

						GUI.Label(lcd, output, lcdStyleLeft);
						if (Event.current.type == EventType.Repaint)
						{
							lcdRects.Add(lcdRect);
						}

						// Currently playing sample.
						if (bShowPlayhead)
						{
							lcd.x = (lcdRect.width - lcd.width) * 0.5f;

							switch (lcdMode)
							{
							case LCDDisplayMode.SampleTime:
								output = string.Format("sample {0:0}", displayState.playheadSamplePosition);
								break;
							case LCDDisplayMode.MusicTime:
								output = GetMusicTimeForDisplayFromSample(displayState.playheadSamplePosition);
								break;
							case LCDDisplayMode.SolarTime:
								solarTime = System.TimeSpan.FromSeconds((double)displayState.playheadSamplePosition / (double)EditClip.frequency);
								output = string.Format(solarTimeFmt, solarTime.Hours, solarTime.Minutes, solarTime.Seconds, solarTime.Milliseconds);
								break;
							default:
								break;
							}

							GUI.Label(lcd, output, lcdStyleCenter);
							if (Event.current.type == EventType.Repaint)
							{
								lcdRects.Add(lcdRect);
							}
						}

						// Right-most-sample LCD.
						lcd.x = lcdRect.width - (lcd.width + lcdStyleRight.margin.right);

						int endSample = (displayState.firstSamplePackToDraw + ((waveDisplay.GetChannelPixelWidthForWindow((int)waveBoxRect.width) - displayState.drawStartOffsetInPixels) * displayState.samplesPerPixel));
						endSample = (EditClip != null && endSample > EditClip.samples) ? EditClip.samples : endSample;

						switch (lcdMode)
						{
						case LCDDisplayMode.SampleTime:
							output = string.Format("sample {0:0}", endSample);
							break;
						case LCDDisplayMode.MusicTime:
							output = GetMusicTimeForDisplayFromSample(endSample);
							break;
						case LCDDisplayMode.SolarTime:
							 solarTime = System.TimeSpan.FromSeconds((double)endSample / (double)EditClip.frequency);
	  	                     output = string.Format(solarTimeFmt, solarTime.Hours, solarTime.Minutes, solarTime.Seconds, solarTime.Milliseconds);
							break;
						default:
							break;
						}

						GUI.Label(lcd, output, lcdStyleRight);
						if (Event.current.type == EventType.Repaint)
						{
							lcdRects.Add(lcdRect);
						}

						GUI.EndGroup();
					}
				}

				// Draw the selection box.
				if (dragStartPos != dragEndPos)
				{
					Color bgColor = GUI.backgroundColor;
					Color newBGColor = Color.gray;
					newBGColor.a = 0.25f;
					GUI.backgroundColor = newBGColor;

					GUI.Box(GetDragAreaRect(), "");

					GUI.backgroundColor = bgColor;
				}
			}

			GUILayout.BeginHorizontal();

			GUI.changed = false;
			bSnapTimingToBeat = EditorGUILayout.ToggleLeft("Snap To Beat", bSnapTimingToBeat, GUILayout.Width(84));
			if (!bSnapTimingToBeat && buildEvent != null && buildEvent.IsOneOff() && IsPlaying())
			{
				// Clear the one off guy!  This will allow us to not do the silly every-frame repeating!
				buildEvent = null;
			}
			if (GUI.changed)
			{
				FocusWaveDisplayWindow();
				GUI.changed = false;
			}
			
			EditorGUIUtility.labelWidth = 30;
			EditorGUIUtility.fieldWidth = 20;
			snapSubBeatCount = EditorGUILayout.IntField(new GUIContent("with", "The number of beats that appear between each beat in a measure."), snapSubBeatCount);
			snapSubBeatCount = (snapSubBeatCount < 0) ? 0 : snapSubBeatCount;
			EditorGUILayout.LabelField(new GUIContent("Sub Beats", "The number of beats that appear between each beat in a measure."), GUILayout.Width(60));
			EditorGUIUtility.labelWidth = 0;
			EditorGUIUtility.fieldWidth = 0;

			// This will push the toggles to the right!
			GUILayout.FlexibleSpace();

			GUI.changed = false;
			EditorGUIUtility.labelWidth = 68f;
			bScrollWithPlayhead = EditorGUILayout.Toggle(new GUIContent("Auto Scroll", "Whether or not the view automatically scrolls with audio playback."), bScrollWithPlayhead);
			if (GUI.changed)
			{
				FocusWaveDisplayWindow();
				GUI.changed = false;
			}

			EditorGUIUtility.labelWidth = 50f;
			audioSrc.volume = EditorGUILayout.Slider(new GUIContent("Volume", "Volume of the Audio Clip during playback."), audioSrc.volume, 0.0f, 1f);
			EditorGUIUtility.labelWidth = 0f;
			EditorGUIUtility.labelWidth = 40f;
			audioSrc.pitch = EditorGUILayout.Slider(new GUIContent("Speed", "How fast the Audio Clip should play. Changes pitch."), audioSrc.pitch, 0.05f, 5f);
			EditorGUIUtility.labelWidth = 0f;

			GUI.enabled = displayState.samplesPerPixel != 1;
			if (displayState.samplesPerPixel == 1)
			{
				displayState.displayType = WaveDisplayType.Line;
			}
			else if (displayState.displayType == WaveDisplayType.Line)
			{
				displayState.displayType = WaveDisplayType.Both;
			}

			GUI.changed = false;
			// Display toggles (this could alternatively be done with a SelectionGrid).
			if (GUILayout.Toggle(displayState.displayType == WaveDisplayType.MinMax,
			                     new GUIContent("MinMax", "Vertical lines depict the minimum and maximum values of the sample range of a given pixel location."),
			                     GUI.skin.GetStyle("radio"), GUILayout.Width(60)))
			{
				displayState.displayType = WaveDisplayType.MinMax;
			}
			if (GUILayout.Toggle(displayState.displayType == WaveDisplayType.RMS,
			                     new GUIContent("RMS", "Vertical lines depict the result of taking the Root Mean Square of the sample range of a given pixel location.  Vertically symmetrical."),
			                     GUI.skin.GetStyle("radio"), GUILayout.Width(40)))
			{
				displayState.displayType = WaveDisplayType.RMS;
			}
			if (GUILayout.Toggle(displayState.displayType == WaveDisplayType.Both,
			                     new GUIContent("Both", "Overlays RMS over MinMax."),
			                     GUI.skin.GetStyle("radio"), GUILayout.Width(50)))
			{
				displayState.displayType = WaveDisplayType.Both;
			}
			if (GUI.changed)
			{
				FocusWaveDisplayWindow();
				GUI.changed = false;
			}

			GUI.enabled = true;

			GUILayout.EndHorizontal();

			// Fine tuning of currently selected event settings.
			if (waveDisplay != null && editTrack != null)
			{
				bool bEventDeletionScheduled = false;

				if (selectedEvents.Count == 1)		// When only a single event is selected!
				{
					KoreographyEvent selectedEvent = null;

					// Only runs once (see if-condition).  This is the only way I've found to pull an element out of the
					//  HashSet without dumping the contents into a list first (though this may be doing that internally.
					foreach (KoreographyEvent e in selectedEvents)
					{
						selectedEvent = e;
					}

					EditorGUILayout.BeginHorizontal();

					EditorGUILayout.LabelField("Selected Event Settings (" + editTrack.GetIDForEvent(selectedEvent) + ")", EditorStyles.boldLabel);
					GUILayout.FlexibleSpace();

					// Handle the deletion of the event AFTER we have finished drawing everything related to it.  This is necessary for
					//  the Layout and Repaint passes of OnGUI to work.
					bEventDeletionScheduled = GUILayout.Button("Delete Event", GUILayout.MaxWidth(90), GUILayout.MaxHeight(15));

					EditorGUILayout.EndHorizontal();
					EditorGUILayout.BeginHorizontal();

					// Used at the bottom of this block to determine whether or not we need to set the dirty bit for saving.
					GUI.changed = false;

					EditorGUIUtility.labelWidth = 120f;
					EditorGUIUtility.fieldWidth = 100f;

					int evtPayloadTypeIdx = (selectedEvent.Payload == null) ? 0 : payloadTypes.IndexOf(selectedEvent.Payload.GetType()) + 1;
					evtPayloadTypeIdx = EditorGUILayout.Popup("Payload", evtPayloadTypeIdx, payloadTypeNames.ToArray());
					if (GUI.changed)
					{
						GUI.changed = false;
						Undo.RecordObject(editTrack, "Change Payload Type");
						AttachPayloadToEvent(selectedEvent, (evtPayloadTypeIdx == 0) ? null : payloadTypes[evtPayloadTypeIdx - 1]);
						// SetDirty is called in AttachPayloadToEvent
					}

					if (selectedEvent.Payload != null)
					{
						// Undo operations handled within DoGUI.
						if (selectedEvent.Payload.DoGUI(EditorGUILayout.GetControlRect(false, GUILayout.Width(142f)), editTrack, false))
						{
							// Reset.
							GUI.changed = false;
							EditorUtility.SetDirty(editTrack);
						}
					}

					GUILayout.FlexibleSpace();
					EditorGUILayout.EndHorizontal();

					// In the following sections, the following thing may LOOK redundant:
					//
					//    GetSampleOfNearestBeat(sampleLoc +- (int)GetSamplesPerSnapSection())
					//
					// but it isn't.  SamplesPerBeat is a float.  The truncating occurs in GetSamplesPerSection.
					// To make sure things truly align, we use the GetSampleOfNearestBeat() function to shift it
					// to the correct location.  This avoids compounding errors when moving the beat with the 
					// buttons below.

					{
						// START SAMPLE
						EditorGUILayout.BeginHorizontal();

						// Disallow negative sample positions.
						int startSample = Mathf.Max(0, EditorGUILayout.IntField("Start Sample Location", selectedEvent.StartSample));
						if (selectedEvent.StartSample != startSample)
						{
							Undo.RecordObject(editTrack, "Adjust Start Sample Location");
							selectedEvent.StartSample = startSample;
						}

						// Snapping.
						EditorGUILayout.LabelField("Snap to:", GUILayout.MaxWidth(50));
						// Disallow negative sample positions.
						GUI.enabled = selectedEvent.StartSample > 0;
						if (GUILayout.Button(prevBeatTex))
						{
							Undo.RecordObject(editTrack, "Snap Start to Previous Beat");

							int sampleLoc = editKoreo.GetSampleOfNearestBeat(selectedEvent.StartSample, snapSubBeatCount);
							selectedEvent.StartSample = (sampleLoc < selectedEvent.StartSample) ? sampleLoc : editKoreo.GetSampleOfNearestBeat(sampleLoc - (int)editKoreo.GetSamplesPerBeat(sampleLoc, snapSubBeatCount), snapSubBeatCount);
						}
						GUI.enabled = true;
						if (GUILayout.Button(beatTex, GUILayout.MinWidth(30)))
						{
							Undo.RecordObject(editTrack, "Snap Start to Nearest Beat");

							selectedEvent.StartSample = editKoreo.GetSampleOfNearestBeat(selectedEvent.StartSample, snapSubBeatCount);
						}
						if (GUILayout.Button(nextBeatTex))
						{
							Undo.RecordObject(editTrack, "Snap Start to Next Beat");

							int sampleLoc = editKoreo.GetSampleOfNearestBeat(selectedEvent.StartSample, snapSubBeatCount);
							selectedEvent.StartSample = (sampleLoc > selectedEvent.StartSample) ? sampleLoc : editKoreo.GetSampleOfNearestBeat(sampleLoc + (int)editKoreo.GetSamplesPerBeat(sampleLoc, snapSubBeatCount), snapSubBeatCount);
						}
						GUILayout.FlexibleSpace();
						EditorGUILayout.EndHorizontal();

						// END SAMPLE
						EditorGUILayout.BeginHorizontal();
						int endSample = EditorGUILayout.IntField("End Sample Location", selectedEvent.EndSample);
						if (selectedEvent.EndSample != endSample)
						{
							Undo.RecordObject(editTrack, "Adjust End Sample Location");
							selectedEvent.EndSample = endSample;
						}

						// Snapping.
						EditorGUILayout.LabelField("Snap to:", GUILayout.MaxWidth(50));
						if (GUILayout.Button(prevBeatTex))
						{
							Undo.RecordObject(editTrack, "Snap End to Previous Beat");

							int sampleLoc = editKoreo.GetSampleOfNearestBeat(selectedEvent.EndSample, snapSubBeatCount);
							selectedEvent.EndSample = (sampleLoc < selectedEvent.EndSample) ? sampleLoc : editKoreo.GetSampleOfNearestBeat(sampleLoc - (int)editKoreo.GetSamplesPerBeat(sampleLoc, snapSubBeatCount), snapSubBeatCount);
						}
						if (GUILayout.Button(beatTex, GUILayout.MinWidth(30)))
						{
							Undo.RecordObject(editTrack, "Snap End to Nearest Beat");

							selectedEvent.EndSample = editKoreo.GetSampleOfNearestBeat(selectedEvent.EndSample, snapSubBeatCount);
						}
						if (GUILayout.Button(nextBeatTex))
						{
							Undo.RecordObject(editTrack, "Snap End to Next Beat");

							int sampleLoc = editKoreo.GetSampleOfNearestBeat(selectedEvent.EndSample, snapSubBeatCount);
							selectedEvent.EndSample = (sampleLoc > selectedEvent.EndSample) ? sampleLoc : editKoreo.GetSampleOfNearestBeat(sampleLoc + (int)editKoreo.GetSamplesPerBeat(sampleLoc, snapSubBeatCount), snapSubBeatCount);
						}
						GUILayout.FlexibleSpace();
						EditorGUILayout.EndHorizontal();
					}
				}
				else if (selectedEvents.Count > 1)		// When a group of events is selected!
				{
					EditorGUILayout.BeginHorizontal();
					
					EditorGUILayout.LabelField("Selected Group Options", EditorStyles.boldLabel);
					GUILayout.FlexibleSpace();
					
					// Handle the deletion of the event AFTER we have finished drawing everything related to it.  This is necessary for
					//  the Layout and Repaint passes of OnGUI to work.
					bEventDeletionScheduled = GUILayout.Button("Delete Events", GUILayout.MaxWidth(90), GUILayout.MaxHeight(15));
					
					EditorGUILayout.EndHorizontal();

					// Used for the next two lines.
					EditorGUIUtility.labelWidth = 120f;
					EditorGUIUtility.fieldWidth = 100f;

					EditorGUILayout.BeginHorizontal();

					// Group payload
					{
						List<System.Type> groupTypes = new List<System.Type>();

						foreach (KoreographyEvent evt in selectedEvents)
						{
							if (evt.Payload == null)
							{
								if (!groupTypes.Contains(null))
								{
									groupTypes.Add(null);
								}
							}
							else if (!groupTypes.Contains(evt.Payload.GetType()))
							{
								groupTypes.Add(evt.Payload.GetType());
							}

							if (groupTypes.Count > 1)
							{
								break;
							}
						}

						List<string> tempPayloadNames = new List<string>(payloadTypeNames);

						int evtPayloadTypeIdx = 0;
						if (groupTypes.Count > 1)
						{
							tempPayloadNames.Insert(0, "[Mixed]");
						}
						else
						{
							System.Type groupType = groupTypes.First();
							evtPayloadTypeIdx = (groupType == null) ? 0 : payloadTypes.IndexOf(groupType) + 1;
						}

						int newTypeIdx = EditorGUILayout.Popup("Payload", evtPayloadTypeIdx, tempPayloadNames.ToArray());
						if (newTypeIdx != evtPayloadTypeIdx)
						{
							GUI.changed = false;
							Undo.RecordObject(editTrack, "Change Payload Type");

							newTypeIdx -= (groupTypes.Count > 1) ? 1 : 0;

							System.Type newType = (newTypeIdx == 0) ? null : payloadTypes[newTypeIdx - 1];

							foreach (KoreographyEvent evt in selectedEvents)
							{
								AttachPayloadToEvent(evt, newType);
								// SetDirty is called in AttachPayloadToEvent
							}
						}

						selectedEvents.Sort(KoreographyEvent.CompareByStartSample);
						KoreographyEvent firstEvt = selectedEvents.First();

						if (firstEvt.Payload != null && groupTypes.Count < 2)
						{
							// Undo operations handled within DoGUI.
							if (firstEvt.Payload.DoGUI(EditorGUILayout.GetControlRect(false, GUILayout.Width(142f)), editTrack, false))
							{
								Undo.RecordObject(editTrack, "Group Adjust Payload");

								foreach (KoreographyEvent evt in selectedEvents)
								{
									if (evt != firstEvt)
									{
										// Figure out why this doesn't work with Curves.  Is it possible that
										//  Curves don't copy correctly with Layout(?) events?
										evt.Payload = firstEvt.Payload.GetCopy();
									}
								}

								// Reset.
								GUI.changed = false;
								EditorUtility.SetDirty(editTrack);
							}
						}
					}

					GUILayout.FlexibleSpace();

					EditorGUILayout.EndHorizontal();
					EditorGUILayout.BeginHorizontal();

					// Group position.
					{
						// Only need to convert the selectedEvents set.  Doesn't matter otherwise.
						List<KoreographyEvent> selEventGroup = new List<KoreographyEvent>(selectedEvents);
						selEventGroup.Sort(KoreographyEvent.CompareByStartSample);

						// Disallow negative sample positions.
						int startOffset = selEventGroup.First().StartSample;
						int newPos = Mathf.Max(0, EditorGUILayout.IntField("Event Location", startOffset));

						if (newPos != startOffset)
						{
							Undo.RecordObject(editTrack, "Move Events");

							foreach (KoreographyEvent movEvt in selectedEvents)
							{
								movEvt.MoveTo(newPos + (movEvt.StartSample - startOffset));
							}

							EditorUtility.SetDirty(editTrack);
						}
					}

					GUILayout.FlexibleSpace();

					EditorGUILayout.EndHorizontal();

					// FUTURE: snapping?  How would this work?  Move by first event?
					//  "Snap All Start" / "Snap All End" ?
				}

				// Change detected.  Mark dirty.
				if (GUI.changed)
				{
					EditorUtility.SetDirty(editTrack);
				}

				// We're done doing selectedEvent-based tasks.  Safe to remove it!
				if (bEventDeletionScheduled)
				{
					DeleteSelectedEvents();
				}

				// Ensure that no changes screw up the order of the events.
				editTrack.EnsureEventOrder();
			}
		}

		// Wrap up.  All other controls have had their chance.  Left-over keys can be used here.
		if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
		{
			// Use the escape key to push focus to the WaveDisplay!
			FocusWaveDisplayWindow();
			Event.current.Use();
		}
	}
	
	bool IsAudioClipValid(AudioClip clip)
	{
		// We need to verify that we can call AudioClip.GetData().  According to the
		//  documentaiton (http://docs.unity3d.com/ScriptReference/AudioClip.GetData.html),
		//  only assets set to "DecompressOnLoad" should be viable.
		AudioImporter clipImporter = AudioImporter.GetAtPath(AssetDatabase.GetAssetPath(clip)) as AudioImporter;

#if UNITY_5_0
		// Internal testing shows that AudioClips with Format "PCM" and LoadType
		//  "CompressedInMemory" also works.  Allow this as well.
		return clipImporter.loadType == AudioClipLoadType.DecompressOnLoad ||
			  (clipImporter.loadType == AudioClipLoadType.CompressedInMemory && clipImporter.format == AudioClipFormat.PCM);
#else
		//  Internal testing shows that LoadType "StreamFromDisc" also works.  The Native 
		//   format (WAV) only supports those two options so we simply allow them.
		//   Otherwise, simply make sure we're not set to Compressed.
		return clipImporter.format == AudioImporterFormat.Native ||
			clipImporter.loadType != AudioImporterLoadType.CompressedInMemory;
#endif
	}

	void SetNewEditKoreo(Koreography newKoreo)
	{
#if UNITY_4_5 || UNITY_4_6
		// This ensures that we don't force lots of errors to be thrown by
		//  loading up the Curve Editor.  Fixed in Unity 5!
		if (Selection.activeObject == null || Selection.activeObject == editKoreo)
		{
			Selection.activeObject = newKoreo;
		}
#endif
		editKoreo = newKoreo;
		
		if (editKoreo == null)
		{
			// Clear out related objects.
			editTempoSection = null;
			editTrack = null;
			waveDisplay = null;
		}
		else		// Load in associated metadata.
		{
			// TODO: Assert that the SourceClip is valid (correct Format/Load Type)?
			if (editKoreo.SourceClip != null)
			{
				InitNewClip(editKoreo.SourceClip);
			}
			else
			{
				waveDisplay = null;
			}
			
			// Load in an Edit Track if one exists!
			KoreographyTrack firstTrack = editKoreo.GetTrackAtIndex(0);
			if (firstTrack != null)
			{
				SetNewEditTrack(firstTrack);
			}
			else
			{
				// No track in this Koreography.  Clear out, including selections!
				editTrack = null;
				selectedEvents.Clear();
				dragSelectedEvents.Clear();
			}

			// All Koreography objects have an editTempoSection.
			editTempoSection = editKoreo.GetTempoSectionAtIndex(0);
		}
	}
	
	void SetNewEditTrack(KoreographyTrack newTrack)
	{
		editTrack = newTrack;

		if (waveDisplay != null)
		{
			waveDisplay.SetEventTrack(editTrack);
		}

		// Clear the event selections.
		selectedEvents.Clear();
		dragSelectedEvents.Clear();
	}

	void InitNewClip(AudioClip newClip)
	{
		StopAudio();

		// New clip.  Reinitialize the WaveDisplay.
		waveDisplay = new WaveDisplay();

#if UNITY_5_0
		if (!newClip.preloadAudioData)
		{
			newClip.LoadAudioData();
		}

		if (newClip.isReadyToPlay)
		{
			waveDisplay.SetAudioData(newClip);
		}
#else
		waveDisplay.SetAudioData(newClip);
#endif

		maxSamplesPerPixel = waveDisplay.GetMaximumSamplesPerPixel(newClip.samples, GetWidthOfWaveDisplayWindow());
	}

	void PlayAudio(int sampleTime = -1)
	{
		if (audioSrc.clip != EditClip)
		{
			audioSrc.clip = EditClip;
		}

		if (sampleTime >= 0)
		{
			audioSrc.timeSamples = sampleTime;
		}
		
		audioSrc.Play();
		
		bShowPlayhead = true;

		FocusWaveDisplayWindow();
	}

	void PauseAudio()
	{
		audioSrc.Pause();
	}

	void StopAudio()
	{
		audioSrc.Stop();

		audioSrc.timeSamples = 0;

		bShowPlayhead = false;
	}

	void ScanToSample(int sample)
	{
		if (EditClip != null)
		{
			sample = Mathf.Clamp(sample, 0, EditClip.samples);
			audioSrc.timeSamples = sample;
		}
	}

	void ScanAheadOneMeasure()
	{
		if (editKoreo != null)
		{
			int curSample = GetCurrentEstimatedMusicSample();

			int measureNum = Mathf.FloorToInt(editKoreo.GetMeasureTimeFromSampleTime(curSample));

			float beatInMeasure = editKoreo.GetBeatCountInMeasureFromSampleTime(curSample);
			if (beatInMeasure <= editKoreo.GetTempoSectionForSample(curSample).BeatsPerMeasure - 1)
			{
				// Get the sample time from the measure of +1.
				ScanToSample(editKoreo.GetSampleTimeFromMeasureTime(measureNum + 1));
			}
			else
			{
				// Skip beyond the next measure to the one following.
				ScanToSample(editKoreo.GetSampleTimeFromMeasureTime(measureNum + 2));
			}

			bShowPlayhead = true;	// Potentially converts from stopped to paused.
		}
	}

	void ScanBackOneMeasure()
	{
		if (editKoreo != null)
		{
			int curSample = GetCurrentEstimatedMusicSample();
			
			int measureNum = Mathf.FloorToInt(editKoreo.GetMeasureTimeFromSampleTime(curSample));
			
			float beatInMeasure = editKoreo.GetBeatCountInMeasureFromSampleTime(curSample);
			if (beatInMeasure >= 1)
			{
				// Get the sample time from the current measure.
				ScanToSample(editKoreo.GetSampleTimeFromMeasureTime(measureNum));
			}
			else
			{
				// Skip completely to the previous measure.
				ScanToSample(editKoreo.GetSampleTimeFromMeasureTime(measureNum - 1));
			}

			bShowPlayhead = true;	// Potentially converts from stopped to paused.
		}
	}

	bool IsPlaying()
	{
		return audioSrc.isPlaying;
	}

	bool IsStopped()
	{
		return !IsPlaying() && (audioSrc.timeSamples == 0) && !bShowPlayhead;
	}

	bool IsPaused()
	{
		return !IsPlaying() && bShowPlayhead;
	}

	bool IsSelecting()
	{
		return dragStartPos != dragEndPos;
	}

	void FocusWaveDisplayWindow()
	{
		bIsWaveDisplayFocused = true;
		GUI.FocusControl("");
	}

	int GetWidthOfWaveDisplayWindow()
	{
		return (int)position.width - GUI.skin.box.margin.horizontal;
	}

	// Music is consumed in discreet chunks by the audio driver.  If updates occur so quickly that
	//  multiple frames report the same audio position but the music is still "playing" then we will
	//  do our best to extrapolate how many samples have been read between "now" and the last
	//  reported change (based on bitrate of music and CPU clock time differences).
	// The actual calculation for this is done in Update().  This ensures that we don't get 
	//  recalculated multiple times within a single frame (which could have consequences for drawing
	//  a concistent view of the WaveForm.
	int GetCurrentEstimatedMusicSample()
	{
		return estimatedSampleTime;
	}

	// Gets the data from the AudioSource itself.
	int GetCurrentRawMusicSample()
	{
		return audioSrc.timeSamples;
	}

	void SetNewSamplesPerPixel(int samplesPerPixel, int offsetInPixels = 0)
	{
		// Get sample centered on zoomOffset.
		int centerSample = displayState.firstSamplePackToDraw + (int)(((float)(offsetInPixels - displayState.drawStartOffsetInPixels) + 0.5f) * (float)displayState.samplesPerPixel);

		// Update the settings.
		displayState.samplesPerPixel = samplesPerPixel;

		// Recalculate the starting sample.
		int samplesToCenter = offsetInPixels * samplesPerPixel;
		displayState.firstSamplePackToDraw = centerSample - samplesToCenter;

		// Update the first sample offset as necessary.
		if (displayState.firstSamplePackToDraw < 0)
		{
			displayState.drawStartOffsetInPixels = -displayState.firstSamplePackToDraw / samplesPerPixel;
			displayState.firstSamplePackToDraw = 0;
		}
		else
		{
			displayState.drawStartOffsetInPixels = 0;
		}

		// Then ensure that our scroll position remains sane!
		scrollPosition.x = displayState.firstSamplePackToDraw / samplesPerPixel;
	}

	KoreographyEvent GetNewEvent(int startSampleLoc)
	{
		KoreographyEvent newEvt = new KoreographyEvent();
		AttachPayloadToEvent(newEvt);
		newEvt.StartSample = startSampleLoc;

		if (!bCreateOneOff)
		{
			newEvt.EndSample += 1;			// Spans have a span of at least 1.
		}

		return newEvt;
	}

	void BeginNewEvent(int samplePos)
	{
		if (bSnapTimingToBeat)
		{
			samplePos = editKoreo.GetSampleOfNearestBeat(samplePos, snapSubBeatCount);
		}

		buildEvent = GetNewEvent(samplePos);

		Undo.RecordObject(editTrack, "Add New Event");

		// Might not actually add it.  Don't worry for now.
		if (editTrack.AddEvent(buildEvent))
		{
			// This only needs to happen here for OneOff events.
			EditorUtility.SetDirty(editTrack);
		}

		if (bCreateOneOff && !bSnapTimingToBeat)
		{
			buildEvent = null;
		}
	}

	void ContinueNewEvent(int samplePos)
	{
		// TODO: Check for beat overlap?

		// EndSample is set with StartSample.  If StartSample > curSampleTime, so is EndSample.
		//  Update EndSample if we're beyond StartSample and *NOT* in OneOff mode.
		if (buildEvent.StartSample < samplePos)
		{
			// In the OneOff case, we should add events we may have missed since the last event was added!
			if (bCreateOneOff)
			{
				AddBeatAlignedOneOffEventsToRange(buildEvent.StartSample, samplePos, snapSubBeatCount);
			}
			else
			{
				buildEvent.EndSample = samplePos;
			}
		}
		else if (buildEvent.StartSample > samplePos)
		{
			if (bCreateOneOff)
			{
				AddBeatAlignedOneOffEventsToRange(samplePos, buildEvent.StartSample, snapSubBeatCount);
			}
			else
			{
				// Don't do this in playmode.  It will break the startSample beat snapping.
				if (Event.current.isMouse)
				{
					buildEvent.StartSample = samplePos;
				}
			}
		}
	}

	void EndNewEvent(int rawSamplePos)
	{
		// End and commit the current event!
		if (bSnapTimingToBeat)
		{
			int beatSample = editKoreo.GetSampleOfNearestBeat(rawSamplePos, snapSubBeatCount);

			if (bCreateOneOff)
			{
				// Add intermediary OneOff events!
				AddBeatAlignedOneOffEventsToRange(buildEvent.StartSample, rawSamplePos, snapSubBeatCount);
			}
			else
			{
				// Do things a bit differently if we're using the mouse to draw.
				if (!Event.current.isMouse)
				{
					if (buildEvent.StartSample > rawSamplePos || buildEvent.StartSample == beatSample)
					{
						buildEvent.EndSample = editKoreo.GetSampleOfNearestBeat(buildEvent.StartSample + (int)editKoreo.GetSamplesPerBeat(rawSamplePos, snapSubBeatCount));
					}
					else
					{
						buildEvent.EndSample = beatSample;
					}
				}
				else
				{
					// Set the StartSample and then ensure that we've quantized the EndSample.
					//  Inverse the operation based on how the *raw* sample pos compares to
					//  the StartSample.
					if (rawSamplePos <= buildEvent.StartSample)
					{
						buildEvent.StartSample = beatSample;
						buildEvent.EndSample = editKoreo.GetSampleOfNearestBeat(buildEvent.EndSample, snapSubBeatCount);
					}
					else
					{
						buildEvent.StartSample = editKoreo.GetSampleOfNearestBeat(buildEvent.StartSample, snapSubBeatCount);
						buildEvent.EndSample = beatSample;
					}

					// We may have snapped ourselves into OneOff.  Detect and adjust for this.
					if (buildEvent.IsOneOff())
					{
						buildEvent.EndSample = editKoreo.GetSampleOfNearestBeat(buildEvent.StartSample + (int)editKoreo.GetSamplesPerBeat(buildEvent.EndSample, snapSubBeatCount));
					}
				}
			}
		}
		else
		{
			// This should only happen with mouse input.
			if (rawSamplePos <= buildEvent.StartSample)
			{
				buildEvent.StartSample = rawSamplePos;
			}
			else
			{
				buildEvent.EndSample = rawSamplePos;
			}
		}

		// All above code flows end in actually updating the event's EndSample.  Mark this as dirty.
		EditorUtility.SetDirty(editTrack);
		
		buildEvent = null;
	}

	string GetFriendlyNameOfPayloadType(System.Type payType)
	{
		return (payType.GetMethod("GetFriendlyName") != null) ? (string)payType.GetMethod("GetFriendlyName").Invoke(null, null) : payType.ToString();
	}

	void AttachPayloadToEvent(KoreographyEvent koreoEvent)
	{
		AttachPayloadToEvent(koreoEvent, currentPayloadTypeIdx < 0 ? null : payloadTypes[currentPayloadTypeIdx]);
	}

	void AttachPayloadToEvent(KoreographyEvent koreoEvent, System.Type payloadType)
	{
		if (payloadType == null)
		{
			// No payload for this sucker!
			koreoEvent.Payload = null;
		}
		else if (koreoEvent.Payload == null || koreoEvent.Payload.GetType() != payloadType)
		{
			// GameObjects or Components can only be properly created with Object.Instantiate and require
			//  a base object to clone anyway.
			// This isn't actually true.  We *could* use EditorUtility.CreateGameObjectWithHideFlags.  Not
			//  sure of the implications of this, however.

			if (payloadType.IsSubclassOf(typeof(ScriptableObject)))
			{
				koreoEvent.Payload = ScriptableObject.CreateInstance(payloadType) as KoreographyPayload;
			}
			else
			{
				koreoEvent.Payload = System.Activator.CreateInstance(payloadType) as KoreographyPayload;
			}
		}
	}

	// Adds a new beat-aligned OneOff event to the current track in the following manner: (startSample, endSample].
	void AddBeatAlignedOneOffEventsToRange(int startSample, int endSample, int subBeats = 0)
	{
		bool bDidAddAnEvent = false;

		for (int i = startSample + (int)editKoreo.GetSamplesPerBeat(startSample, subBeats); i <= endSample; i = i + (int)editKoreo.GetSamplesPerBeat(i, subBeats))
		{
			// This also iterates our current "buildEvent" to the most recent OneOff made!
			buildEvent = GetNewEvent(editKoreo.GetSampleOfNearestBeat(i, subBeats));

			if (editTrack.AddEvent(buildEvent))
			{
				bDidAddAnEvent = true;
			}
		}

		if (bDidAddAnEvent)
		{
			EditorUtility.SetDirty(editTrack);
		}
	}

	void DeleteSelectedEvents()
	{
		Undo.RecordObject(editTrack, selectedEvents.Count > 1 ? "Delete Events" : "Delete Event");

		// editTrack is valid when there are selected events.
		foreach (KoreographyEvent evt in selectedEvents)
		{
			if (evt != null)
			{
				// Delete selected event.
				editTrack.RemoveEvent(evt);

				EditorUtility.SetDirty(editTrack);
				Repaint();
			}
		}

		selectedEvents.Clear();
	}

	void HandleKeyInput()
	{
		if ((Event.current.type == EventType.KeyDown || Event.current.type == EventType.KeyUp) &&
		    Event.current.keyCode != KeyCode.None &&
		    bIsWaveDisplayFocused)
		{
			if (Event.current.keyCode == KeyCode.KeypadEnter ||
			    Event.current.keyCode == KeyCode.Return ||
			    Event.current.keyCode == KeyCode.E)
			{
				if (editTrack != null && IsPlaying())
				{
					if (Event.current.type == EventType.KeyDown)
					{
						if (buildEvent == null)
						{
							BeginNewEvent(GetCurrentEstimatedMusicSample());
						}

						Event.current.Use();
					}
					else if (Event.current.type == EventType.KeyUp)
					{
						if (buildEvent != null)
						{
							EndNewEvent(GetCurrentEstimatedMusicSample());
							Event.current.Use();
						}
					}
				}
			}
			else if (Event.current.keyCode == KeyCode.Space && Event.current.type == EventType.KeyDown)
			{
				if (Event.current.shift)
				{
					StopAudio();
				}
				else if (IsPlaying())
				{
					PauseAudio();
				}
				else
				{
					PlayAudio();
				}
				Event.current.Use();
			}
			else if (Event.current.keyCode == KeyCode.A && Event.current.type == EventType.KeyDown)
			{
				controlMode = ControlMode.Select;
				Event.current.Use();
			}
			else if (Event.current.keyCode == KeyCode.S && Event.current.type == EventType.KeyDown)
			{
				controlMode = ControlMode.Author;
				Event.current.Use();
			}
			else if (Event.current.keyCode == KeyCode.Z && Event.current.type == EventType.KeyDown)
			{
				bCreateOneOff = true;
				Event.current.Use();
			}
			else if (Event.current.keyCode == KeyCode.X && Event.current.type == EventType.KeyDown)
			{
				bCreateOneOff = false;
				Event.current.Use();
			}
			else if ((Event.current.keyCode == KeyCode.Delete || Event.current.keyCode == KeyCode.Backspace) &&
			         Event.current.type == EventType.KeyDown &&
			         selectedEvents.Count > 0)
			{
				DeleteSelectedEvents();
				Event.current.Use();
			}
			else if (Event.current.keyCode == KeyCode.V && Event.current.type == EventType.KeyDown &&
			         Event.current.shift &&
#if UNITY_STANDALONE_OSX
			         Event.current.command)
#else
					 Event.current.control)
#endif
			{
				if (clippedEvents.Count > 0 && selectedEvents.Count > 0 && bIsWaveDisplayFocused && waveDisplay != null)
				{
					PastePayloadToSelectedEvents();
					Event.current.Use();
				}
			}
			else if (Event.current.keyCode == KeyCode.LeftArrow && Event.current.type == EventType.KeyDown)
			{
				ScanBackOneMeasure();
				Event.current.Use();
			}
			else if (Event.current.keyCode == KeyCode.RightArrow && Event.current.type == EventType.KeyDown)
			{
				ScanAheadOneMeasure();
				Event.current.Use();
			}
			else if (Event.current.keyCode == KeyCode.DownArrow && Event.current.type == EventType.KeyDown)
			{
				audioSrc.pitch = Mathf.Max(0.1f, Mathf.Round((audioSrc.pitch - 0.1f) * 10f) / 10f);
				Event.current.Use();
			}
			else if (Event.current.keyCode == KeyCode.UpArrow && Event.current.type == EventType.KeyDown)
			{
				audioSrc.pitch = Mathf.Min(5f, Mathf.Round((audioSrc.pitch + 0.1f) * 10f) / 10f);
				Event.current.Use();
			}
		}
	}

	void HandleMouseInput()
	{
		if (Event.current.rawType == EventType.MouseMove)
		{
			if (IsSelecting())
			{
				// MouseDrag occurs while selecting.  If we are now getting
				//  MouseMove, a mouse up has occurred offscreen.  Clear the
				//  selection.
				dragStartPos = Vector2.zero;
				dragEndPos = Vector2.zero;

				Repaint();
				Event.current.Use();
			}
		}
		else if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
		{
			// Make sure we handle the wave display focusing.
			bIsWaveDisplayFocused = false;

			Vector2 mousePos = Event.current.mousePosition;
			if (waveDisplay != null && waveDisplay.IsClickableAtLoc(mousePos))
			{
				FocusWaveDisplayWindow();

				KoreographyEvent clickEvt = waveDisplay.GetEventAtLoc(mousePos);

				// Ensure a clean starting point.
				eventEditMode = EventEditMode.None;
				eventEditClickX = 0;

				if (clickEvt != null || controlMode == ControlMode.Select)
				{
					MouseDownEditMode();

					if (selectedEvents.Contains(clickEvt))
					{
						eventEditMode = waveDisplay.GetEventEditModeAtLoc(mousePos);

						if (eventEditMode == EventEditMode.Move)
						{
							selectedEvents.Sort(KoreographyEvent.CompareByStartSample);
							eventEditClickX = mousePos.x - waveDisplay.GetHorizontalLocOfSample(selectedEvents.First().StartSample, displayState);
						}
						else if (eventEditMode != EventEditMode.None)
						{
							// Resizing.  Make sure we're the only one selected.
							selectedEvents.Clear();
							selectedEvents.Add(clickEvt);
						}
					}
				}
				else if (editKoreo != null && editTrack != null && buildEvent == null &&
				         eventEditMode == EventEditMode.None)
				{
					MouseDownDrawMode();
				}
			}
			else if (IsPointInLCD(mousePos))
			{
				// SampleTime->MusicTime->SolarTime->SampleTime...
				lcdMode = (lcdMode == LCDDisplayMode.SampleTime) ? LCDDisplayMode.MusicTime : (lcdMode == LCDDisplayMode.MusicTime) ? LCDDisplayMode.SolarTime : LCDDisplayMode.SampleTime; 
				Event.current.Use();
				Repaint();
			}
		}
		else if ((Event.current.type == EventType.MouseUp || Event.current.rawType == EventType.MouseUp) && Event.current.button == 0)
		{
			// Clear the edit mode, no matter where we are.
			eventEditMode = EventEditMode.None;
			eventEditClickX = 0;
			
			if(controlMode == ControlMode.Select)
			{
				MouseUpEditMode();
			}
			else if (buildEvent != null)	// For this to be the case we must have valid Koreography and at least one KoreographyTrack.
			{
				MouseUpDrawMode();
			}
		}
		else if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
		{
			Vector2 mousePos = Event.current.mousePosition;
			if (waveDisplay != null && waveDisplay.ContainsPoint(mousePos))
			{
				if (eventEditMode != EventEditMode.None)
				{
					MouseEditEvent();
				}
				else if (controlMode == ControlMode.Select)
				{
					MouseDragEditMode();
				}
				else if (buildEvent != null)	// For this to be the case we must have valid Koreography and at least one KoreographyTrack.
				{
					MouseDragDrawMode();
				}
			}
		}
	}

	bool IsPointInLCD(Vector2 point)
	{
		bool bInLCD = false;
		foreach (Rect lcdRect in lcdRects)
		{
			if (lcdRect.Contains(point))
			{
				bInLCD = true;
				break;
			}
		}
		return bInLCD;
	}

	string GetMusicTimeForDisplayFromSample(int sample)
	{
		float beat = editKoreo.GetBeatCountInMeasureFromSampleTime(sample) + 1f;
		float measure = Mathf.Floor(editKoreo.GetMeasureTimeFromSampleTime(sample) + 1f);

		// This appears to be necessary because of floating point rounding errors with sample
		//  math precision (probably?).  May need to use a double to get this precisely correct.
		//  Or some other math is entirely off...
		{
			TempoSectionDef curSection = editKoreo.GetTempoSectionForSample(sample);
			float nextSampleBeat = Mathf.Floor(beat + (1f / curSection.SamplesPerBeat));
			// Normally we don't care about this because rounding handles things.  However, when we're less than one
			//  sample away from the next measure boundary we need to be super careful.  Currently rounding rules
			//  work in such a way as to set the measure boundary to the sample that closest to the boundary, even if
			//  that sample is *below* the measure boundary.  This should probably be changed such that the the *next*
			//  sample after the between-sample measure boundary is used as the first measure sample, rather than the
			//  'closest'.
			if (nextSampleBeat > Mathf.Floor(beat) && nextSampleBeat > curSection.BeatsPerMeasure)
			{
				beat = 1f;
				measure++;
			}
			
			// Handle String format rounding rules.  This will ensure that we have a precision of
			//  exactly 3 decimal positions, forestalling rounding.
			beat = Mathf.Round(beat * 1000f) / 1000f;
		}

		return string.Format("{0:0'm | '}", measure) +
			   string.Format("{0:0.000'b'}", beat);
	}
	
	Rect GetDragAreaRect()
	{
		float minX = Mathf.Min(dragStartPos.x, dragEndPos.x);
		float minY = Mathf.Min(dragStartPos.y, dragEndPos.y);
		float maxX = Mathf.Max(dragStartPos.x, dragEndPos.x);
		float maxY = Mathf.Max(dragStartPos.y, dragEndPos.y);
		return Rect.MinMaxRect(minX, minY, maxX, maxY);
	}

	void HandleScrollInput()
	{
		if (Event.current.type == EventType.scrollWheel)
		{
			if (waveDisplay != null &&
				waveDisplay.ContainsPoint(Event.current.mousePosition) &&
				Mathf.Abs(Event.current.delta.y) > Mathf.Abs(Event.current.delta.x))
			{
				// Vertical scroll over the wave display detected.  Do zoom!
				int scrollFactor = 2;

				int zoomOffsetInPixels = waveDisplay.GetPixelOffsetInChannelAtLoc(Event.current.mousePosition);

				SetNewSamplesPerPixel(Mathf.Clamp(displayState.samplesPerPixel + ((int)Event.current.delta.y * scrollFactor), 1, maxSamplesPerPixel), zoomOffsetInPixels);

				Event.current.Use();
			}
		}
	}
		
	void ValidateKoreographyAndTrackData()
	{
		if (editKoreo != null)
		{
			// This is done to avoid short-circuiting as cleanup logic could happen in both in the same frame!
			bool bNeedsTrackListSave = editKoreo.CheckTrackListIntegrity();
			bool bNeedsTempoListSave = editKoreo.CheckTempoSectionListIntegrity();

			if (bNeedsTrackListSave || bNeedsTempoListSave)
			{
				EditorUtility.SetDirty(editKoreo);
			}
		}

		if (editTrack != null && editTrack.CheckEventListIntegrity())
		{
			EditorUtility.SetDirty(editTrack);
		}
	}

	#endregion
	#region Mouse Methods

	void ResetDrag()
	{
		dragStartPos = Vector2.zero;
		dragEndPos = Vector2.zero;
	}

	void MouseEditEvent()
	{
		Vector2 mouseLoc = Event.current.mousePosition;
		if (waveDisplay != null && waveDisplay.ContainsPoint(mouseLoc) && editKoreo != null)
		{
			int samplePos = 0;
			if (eventEditMode == EventEditMode.Move)
			{
				Vector2 edgeLoc = mouseLoc;
				edgeLoc.x -= eventEditClickX;
				samplePos = waveDisplay.GetSamplePositionOfPoint(edgeLoc, displayState);

				if (bSnapTimingToBeat)
				{
					samplePos = editKoreo.GetSampleOfNearestBeat(samplePos, snapSubBeatCount);
				}

				selectedEvents.Sort(KoreographyEvent.CompareByStartSample);

				// Used for offsetting.  We are going to move all events by the amount
				//  done on the first event.  This could be weird.  Wait for
				//  suggestions/experience/complaints to adjust further.
				int startOffset = selectedEvents.First().StartSample;

				Undo.RecordObject(editTrack, (selectedEvents.Count == 1) ? "Move Event" : "Move Events");
				
				foreach (KoreographyEvent movEvt in selectedEvents)
				{
					movEvt.MoveTo(samplePos + (movEvt.StartSample - startOffset));
				}

				EditorUtility.SetDirty(editTrack);
			}
			else
			{
				samplePos = waveDisplay.GetSamplePositionOfPoint(mouseLoc, displayState);
				
				if (bSnapTimingToBeat)
				{
					samplePos = editKoreo.GetSampleOfNearestBeat(samplePos, snapSubBeatCount);
				}

				if (eventEditMode == EventEditMode.ResizeLeft)
				{
					Undo.RecordObject(editTrack, "Adjust Event Start");

					KoreographyEvent evt = selectedEvents.First();
					evt.StartSample = samplePos;

					EditorUtility.SetDirty(editTrack);
				}
				else if (eventEditMode == EventEditMode.ResizeRight)
				{
					Undo.RecordObject(editTrack, "Adjust Event End");

					KoreographyEvent evt = selectedEvents.First();
					evt.EndSample = samplePos;

					EditorUtility.SetDirty(editTrack);
				}
			}
			
			Event.current.Use();
			Repaint();
		}
	}

	void MouseDownEditMode()
	{
		Vector2 mousePos = Event.current.mousePosition;

		KoreographyEvent selectedEvent = waveDisplay.GetEventAtLoc(mousePos);

		if (selectedEvent != null)
		{
			if (Event.current.clickCount < 2)	// Double clicks should fall through to the system to enable editing!
			{
				if (Event.current.shift)	// Shift is add [up to].
				{
					if (!selectedEvents.Contains(selectedEvent))
					{
						selectedEvents.Add(selectedEvent);
					}
				}
#if UNITY_STANDALONE_OSX
				else if (Event.current.command)	// Command (Mac) is add/remove unique.
#else
				else if (Event.current.control)	// Control (other) is add/remove unique.
#endif
				{
					if (selectedEvents.Contains(selectedEvent))
					{
						selectedEvents.Remove(selectedEvent);
					}
					else
					{
						selectedEvents.Add(selectedEvent);
					}
				}
				else if (selectedEvents.Count == 0 || !selectedEvents.Contains (selectedEvent))		// Replace selection.
				{
					selectedEvents.Clear();
					selectedEvents.Add(selectedEvent);
				}
				
				Event.current.Use();
				Repaint();
			}
			else
			{
				// Drop focus (as the edit field should be focused now).
				bIsWaveDisplayFocused = false;

				// Clear out the selection to enable [Delete/Backspace] keys to work without
				//  deleting the events first.
				selectedEvents.Clear();
			}
		}
		else
		{
			if (Event.current.clickCount < 2)
			{
				// Remove the selection.
				if (!(Event.current.shift ||
#if UNITY_STANDALONE_OSX
				      Event.current.command)
#else
				      Event.current.control)
#endif
				   )
				{
					selectedEvents.Clear();
				}

				// Start dragging!
				dragStartPos = mousePos;
				dragEndPos = dragStartPos;

				Event.current.Use();
				Repaint();
			}
			else if (Event.current.clickCount == 2)
			{
				// Double click to add a new event, even in Edit Mode.
				if (editKoreo != null && editTrack != null)
				{
					KoreographyEvent newEvt = GetNewEvent(waveDisplay.GetSamplePositionOfPoint(mousePos, displayState));

					// Only create OneOff events on Double click.
					newEvt.EndSample = newEvt.StartSample;

					if (bSnapTimingToBeat)
					{
						newEvt.MoveTo(editKoreo.GetSampleOfNearestBeat(newEvt.StartSample, snapSubBeatCount));
					}

					Undo.RecordObject(editTrack, "Add New Event");

					if (editTrack.AddEvent(newEvt))
					{
						// This only needs to happen here for OneOff events.
						EditorUtility.SetDirty(editTrack);
					}
				}

				// Clear selected events?

				Event.current.Use();
				Repaint();
			}
		}
	}

	void MouseDragEditMode()
	{
		if (dragStartPos != Vector2.zero)
		{
			dragEndPos = Event.current.mousePosition;
			
			// Replace the currently selected area.
			dragSelectedEvents.Clear();
			dragSelectedEvents.AddRange(waveDisplay.GetEventsTouchedByArea(GetDragAreaRect()));
			
			// Set up the highlight set.
			eventsToHighlight.Clear();
			eventsToHighlight.AddRange(selectedEvents);
			
			// And adjust the highlight set based on user control.
			if (Event.current.shift)
			{
				eventsToHighlight = eventsToHighlight.Union(dragSelectedEvents).ToList();
			}
#if UNITY_STANDALONE_OSX
			else if (Event.current.command)	// Command (Mac) is add/remove unique.
#else
			else if (Event.current.control)	// Control (other) is add/remove unique.
#endif
			{
				eventsToHighlight = eventsToHighlight.Except(dragSelectedEvents).ToList();
			}
			else
			{
				eventsToHighlight.Clear();
				eventsToHighlight.AddRange(dragSelectedEvents);
			}
			
			Event.current.Use();
			Repaint();
		}
	}

	void MouseUpEditMode()
	{
		if (dragStartPos != dragEndPos)
		{
			if (dragSelectedEvents.Count > 0)
			{
				// Commit the changes.
				if (Event.current.shift)
				{
					selectedEvents = selectedEvents.Union(dragSelectedEvents).ToList();
				}
#if UNITY_STANDALONE_OSX
				else if (Event.current.command)	// Command (Mac) is add/remove unique.
#else
				else if (Event.current.control)	// Control (other) is add/remove unique.
#endif
				{
					selectedEvents = selectedEvents.Except(dragSelectedEvents).ToList();
				}
				else
				{
					// Replace the events.
					selectedEvents.Clear();
					selectedEvents.AddRange(dragSelectedEvents);
				}
				
				dragSelectedEvents.Clear();
			}
			
			ResetDrag();
			
			Event.current.Use();
			Repaint();
		}
	}

	void MouseDownDrawMode()
	{
		// Clear out the selected events in draw mode before drawing.
		if (selectedEvents.Count > 0)
		{
			selectedEvents.Clear();
		}
		else
		{
			BeginNewEvent(waveDisplay.GetSamplePositionOfPoint(Event.current.mousePosition, displayState));
		}

		Event.current.Use();
		Repaint();
	}

	void MouseDragDrawMode()
	{
		ContinueNewEvent(waveDisplay.GetSamplePositionOfPoint(Event.current.mousePosition, displayState));

		Event.current.Use();
		Repaint();
	}

	void MouseUpDrawMode()
	{
		int samplePos = buildEvent.EndSample;

		if (waveDisplay.ContainsPoint(Event.current.mousePosition))
		{
			samplePos = waveDisplay.GetSamplePositionOfPoint(Event.current.mousePosition, displayState);
		}

		EndNewEvent(samplePos);

		Event.current.Use();
		Repaint();
	}

	#endregion
	#region Commands

	void SelectAll()
	{
		if (editTrack != null)
		{
			selectedEvents.Clear();
			selectedEvents = editTrack.GetAllEvents();
		}
	}

	void CutSelectedEvents()
	{
		if (selectedEvents.Count > 0)
		{
			CopySelectedEvents();

			Undo.RecordObject(editTrack, selectedEvents.Count > 1 ? "Cut Events" : "Cut Event");
			DeleteSelectedEvents();
			EditorUtility.SetDirty(editTrack);
		}
	}

	void CopySelectedEvents()
	{
		// Clear previously copied events.
		clippedEvents.Clear();

		// TODO: Sort the selected list (once we change from a Set to a List).

		foreach (KoreographyEvent evt in selectedEvents)
		{
			clippedEvents.Add(evt.GetCopy());
		}
	}

	void PasteOverSelectedEvents()
	{
		clippedEvents.Sort(KoreographyEvent.CompareByStartSample);

		KoreographyEvent evtToOverwrite = (selectedEvents.Count == 1) ? selectedEvents.First() : null;

		if (selectedEvents.Count > 1)
		{
			// Store off references to the events before they're deleted.
			selectedEvents.Sort(KoreographyEvent.CompareByStartSample);
			evtToOverwrite = selectedEvents.First();
		}

		// Delete the selected events.  We have to do this because we're replacing exact events and
		//  sample location collisions may not be allowed.
		Undo.RecordObject(editTrack, clippedEvents.Count > 1 ? "Paste Events" : "Paste Event");
		DeleteSelectedEvents();
		EditorUtility.SetDirty(editTrack);

		// Used for offsetting.
		int startOffset = clippedEvents.First().StartSample;

		if (evtToOverwrite != null)
		{
			foreach (KoreographyEvent addEvt in clippedEvents)
			{
				KoreographyEvent newEvt = addEvt.GetCopy();
				newEvt.MoveTo(evtToOverwrite.StartSample + (addEvt.StartSample - startOffset));

				editTrack.AddEvent(newEvt);
			}
		}
	}

	void PastePayloadToSelectedEvents()
	{
		Undo.RecordObject(editTrack, "Paste Payload");
		EditorUtility.SetDirty(editTrack);

		clippedEvents.Sort(KoreographyEvent.CompareByStartSample);

		KoreographyPayload payload = clippedEvents.First().Payload;

		if (payload == null)
		{
			foreach (KoreographyEvent evt in selectedEvents)
			{
				evt.Payload = null;
			}
		}
		else
		{
			foreach (KoreographyEvent evt in selectedEvents)
			{
				evt.Payload = payload.GetCopy();
			}
		}
	}

	void PasteEventsAtLocation(System.Object samplePosAsObj)
	{
		int samplePos = (int)samplePosAsObj;

		clippedEvents.Sort(KoreographyEvent.CompareByStartSample);
		
		Undo.RecordObject(editTrack, clippedEvents.Count > 1 ? "Paste Events" : "Paste Event");
		EditorUtility.SetDirty(editTrack);
		
		// Used for offsetting.
		int startOffset = clippedEvents.First().StartSample;
		
		foreach (KoreographyEvent addEvt in clippedEvents)
		{
			KoreographyEvent newEvt = addEvt.GetCopy();
			newEvt.MoveTo(samplePos + (addEvt.StartSample - startOffset));

			editTrack.AddEvent(newEvt);
		}
	}

	void PlayFromSampleLocation(System.Object samplePosAsObj)
	{
		PlayAudio((int)samplePosAsObj);
	}

	#endregion
}
