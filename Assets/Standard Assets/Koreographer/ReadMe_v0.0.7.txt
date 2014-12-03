----------------------------------------------
            Koreographer™
 Copyright © 2014 Sonic Bloom, LLC
            Version 0.0.9
----------------------------------------------

Thank you for downloading Koreographer™!

PLEASE NOTE that Koreographer™ can only be legally downloaded from the Koreographer™ Developer Preview site (https://sites.google.com/a/sonicbloomgames.com/koreographer-devpreview/home)

If you've obtained Koreographer™ via some other means then note that your license is effectively invalid, as Sonic Bloom, LLC cannot provide support for pirated and/or potentially modified software.

Use of the Koreographer™ software is bound to terms set forth in the End User License Agreement, a copy of which is included below.

---------------------------------------
 Support, Documentation, and Tutorials
---------------------------------------

All can be found here:
https://sites.google.com/a/sonicbloomgames.com/koreographer-devpreview/home

If you have any questions, suggestions, comments or feature requests, please drop by the Koreographer™ Developer Preview forum, found here:
https://groups.google.com/forum/#!forum/koreographer-developer-preview/

---------------------------------------
End User License Agreement
---------------------------------------

CAREFULLY READ THE FOLLOWING LICENSE AGREEMENT. YOU ACCEPT AND AGREE TO BE BOUND BY THIS LICENSE AGREEMENT BY CHOOSING TO INSTALL THE CONTENTS OF THE ATTACHMENT. IF YOU DO NOT AGREE TO THIS LICENSE, PLEASE DISCARD THIS AND ALL CONTENTS. WHEREIN THE EULA IS FOUND INSUFFICIENT, THE USER AGREEMENT WILL FALL BACK ON THE STANDARD UNITY ASSET STORE USER AGREEMENT.* 

*http://unity3d.com/legal/as_terms 

License Grant

"You" means the person or company who is being licensed to use the Software. "We," "us" and "our" means Sonic Bloom, LLC. "Software" means the Developer Preview version of Koreographer™ and the files distributed by us.
We hereby grant you a nonexclusive, worldwide, and perpetual license to use the Developer Preview Software for personal non-profit use. "Non-profit use" means that you do not charge or accept compensation for the use of the Software or any services or products that you provide with it, without meeting additional conditions*.
*License to use the Software for commercial use is granted on the grounds that the Software is credited in either the post-credit sequence or introduction credits sequence, the choice is at the users discretion. Use of the Koreographer™ logo is to be implemented when credited, using the full color logo provided, at a size no smaller than 399px by 100px, in the logo’s native aspect ratio. “Commercial use” means that you charge or accept compensation for the use of the Software for any services or products you provide.
The Software is "in use" on a computer when it is installed as a Unity editor extension, and used in conjunction with the Unity game engine to compile any game code.

Title

We remain the owner of all rights, title and interest in the Software and related explanatory written materials ("Documentation").

Things You May Not Do

The Software and Documentation are protected by copyright laws and international treaties. You must treat the Software and Documentation like any other copyrighted material, for example a book. You may not:
	- copy the Documentation,
	- copy the Software except to make archival or backup copies as provided above,
	- reverse engineer, adapt, disassemble, decompile, or make any attempt to replicate the source code of the Software for use outside of embedded components of electronic games and interactive media without special, mutually agreed upon, additional license,
	- place the Software onto a server so that it is accessible via a public network such as the Internet, 
	- sublicense, rent, lease or lend any portion of the Software Product or Documentation,
	- share the costs related to purchasing an Asset and then let any third party that has contributed to such purchase use such Asset (forum pooling).

Transfers

You may not transfer any of your rights to use the Software Product and Documentation to another person or legal entity.

Copyright

All title and copyrights in and to the Software (including but not limited to any images, photographs, clip art, libraries, and examples incorporated into the Software), the accompanying printed materials, and any copies of the Software are owned by Sonic Bloom, LLC. The Software is protected by copyright laws and international treaty provisions. Therefore, you must treat the Software like any other copyrighted material. The licensed users or licensed company can use all functions, example, templates, clipart, libraries and symbols in the Software to create new diagrams and distribute the diagrams.

Disclaimer of Warranty

The Software is provided on an AS IS basis, without warranty of any kind, including without limitation the warranties of merchantability, fitness for a particular purpose and non-infringement.  
The entire risk as to the quality and performance of the Software is borne by you.  
Should the Software prove defective, you and not Sonic Bloom, LLC assume the entire cost of any service and repair.  
 
SONIC BLOOM, LLC IS NOT RESPONSIBLE FOR ANY INDIRECT, SPECIAL, INCIDENTAL, OR CONSEQUENTIAL DAMAGES OF ANY CHARACTER INCLUDING, WITHOUT LIMITATION, DAMAGES FOR LOSS OF GOODWILL, WORK STOPPAGE, COMPUTER FAILURE OR MALFUNCTION, OR ANY AND ALL OTHER COMMERCIAL DAMAGES OR LOSSES.  
 
Title, ownership rights, and intellectual property rights in and to the Software shall remain in Sonic Bloom, LLC. The Software is protected by international copyright treaties.  

Term and Termination

This license agreement takes effect upon your use of the software and remains effective until terminated. You may terminate it at any time by destroying all copies of the Software and Documentation in your possession. It will also automatically terminate if you fail to comply with any term or condition of this license agreement. You agree on termination of this license to destroy all copies of the Software and Documentation in your possession.
Confidentiality
The Software contains trade secrets and proprietary know-how that belong to us and it is being made available to you in strict confidence. ANY USE OR DISCLOSURE OF THE SOFTWARE, OR OF ITS ALGORITHMS, PROTOCOLS OR INTERFACES, OTHER THAN IN STRICT ACCORDANCE WITH THIS LICENSE AGREEMENT, MAY BE ACTIONABLE AS A VIOLATION OF OUR TRADE SECRET RIGHTS.

General Provisions

	1. This written license agreement is the exclusive agreement between you and us concerning the Software and Documentation and supersedes any prior purchase order, communication, advertising or representation concerning the Software.
	2. This license agreement may be modified only by a writing signed by you and us.
	3. In the event of litigation between you and us concerning the Software or Documentation, the prevailing party in the litigation will be entitled to recover attorney fees and expenses from the other party.
	4. This license agreement is governed by the laws of The Commonwealth of Massachusetts.
	5. You agree that the Software will not be shipped, transferred or exported into any country or used in any manner prohibited by the United States Export Administration Act or any other export laws, restrictions or regulations.

-----------------
 Version History
-----------------

0.0.9 Developer Preview - Fifth Release!
- NEW: Clone Tool support!  This mode, enabled by pressing the “d” key or clicking the “Clone” button above the Waveform Display, enables fast duplication of a selection of events.  Here’s how it works:
  - If a selection doesn’t yet exist, the mouse will act exactly as it does in “Select” mode, allowing you to select events.
  - If a selection exists when the mouse is pressed, those events will be duplicated (cloned) and added to the Koreography Track at the current mouse position.  This operation adheres to Snap to Beat settings.
- NEW: The WaveDisplay now maintains focus after the user scrolls via the scrollbar.
- NEW: The “Snap to Beat” setting can be temporarily inverted by holding down the Shift key for most interactions.
- NEW: Pressing the Escape key while the Waveform Display has focus will clear the active selection of events.
- NEW: Type-specific Payload accessors directly off of KoreographyEvent object instances!  Accessors currently include:
  - CurvePayload: HasCurvePayload(), GetCurveValue(), GetValueOfCurveAtTime()
  - FloatPayload: HasFloatPayload(), GetFloatValue()
  - TextPayload: HasTextPayload(), GetTextValue()
- NEW: Added an UnregisterForAllEvents() convenience function to the Koreographer class that unregisters for all possible EventIDs.  Useful for objects that have lots of potential Event ID Registrations.
- NEW: Pulled the update logic out of SimpleMusicPlayer into a new class called AudioVisor with an extremely simple API.  This makes custom Music Player creation much easier and provides the foundation for future players (multi-layer, FMOD, WWise, etc.).
- FIX: Actually break the publisher-subscriber link when unregistering events with the Koreographer.  This stops a whole bunch of memory from leaking/sitting around until the Koreographer was cleaned up!
- FIX: Don’t globally swallow Cut/Copy/Paste/SelectAll commands to allow all fields proper chances to respond.
- FIX: More reasonable default zoom level on Audio/Koreography load.
- FIX: In the SimplePlayer component, properly handle updating the sample time when changing the playing clip.
- FIX: Major overhaul of SimplePlayer component timing system.  Should improve reliability and remove bugs, particularly in high frame-rate scenarios.
- FIX: Restored compatibility with latest Unity 5ß (up to date as of 5b14).
- FIX: Removed use of AudiClip.isReadyToPlay flag that was deprecated in Unity 5b13.
- FIX: The Koreography Editor window can once again be flexibly resized down to a default size of 100x100px.  It will now only be resized on first-launch.  A new file “KoreographerSettings” will appear in the <ProjectDirectory>/Library folder.  This is currently only used to determine if the Koreography Editor has been opened in this project before or not.
- FIX: Handled a Null Reference exception that would occur when trying to draw/drag a span event with click position sufficiently close to left edge of the WaveForm display.
- FIX: Small precision enhancements to work around internal floating point errors.  Many internal calculations now occur with the 128-bit Decimal value type.

0.0.8 Developer Preview - Fourth Release!
- NEW: Faster Waveform rendering thanks to using Unity's built-in line renderer over the custom community created one.
- NEW: Unity 5 Support!  Koreograhper can now be opened in the latest Unity 5 Beta!  AudioClips set to not preload and load in the background are also supported!
  - NOTE: A known bug exists in the latest Unity 5 Beta (currently b11) that causes extreme slowdown when rendering (only affects Unity's built-in line renderer).  Unity is aware of the issue.
- NEW: We've been surreptitiously listening to you all.  "Instantaneous" events are now renamed to "OneOff".  Not only is it shorter but it appears to be far more natural!
- NEW: OneOff events and Span events 12px or less in size will now only move with the mouse by default.  In order to resize these events, hold down the ALT key [or Option key on a Mac] to switch to resize mode!
- NEW: Sample time estimation during audio playback while in the Koreography Editor!  The SimplePlayer and MusicPlayer runtime classes already did this.  We now estimate sample positions when the underlying AudioSource reports that the playhead hasn't moved even though we expect that it has.  This should result in more responsive event creation and playhead updates in scenarios with enough speed to benefit (fully cached Waveform display, etc.).
- NEW: "Play From Here" option added to the Waveform Display context menu (right-click).
- NEW: Support for skipping the audio playhead forward and back by 1 measure at a time with the Left and Right Arrow keys!
- NEW: Increase/Decrease the playback speed (pitch) of the audio in the Koreography Editor with the Up and Down Arrow keys!
- NEW: A new toggle called "Auto Scroll" exists in the Koreography Editor (on by default).  When this is on the Waveform display will move along with the music as it does currently.  When the toggle is off, the Waveform display will not automatically scroll (the playhead will continue to move, however).
- NEW: Keyboard Controls Overhaul!
  - E, Return, or Enter - Create New Event at the playhead position [during audio playback].
  - Spacebar - Play/Pause audio.
  - Shift+Spacebar - Stop audio [clears playhead, sets audio to start at beginning].
  - Left/Right Arrows - Scan ahead/back by one measure.
  - Up/Down Arrows - Speed up/slow down audio playback speed (pitch).
  - Escape - Remove focus from current button/control and focus the Waveform display (may require several presses depending on state of the current 'hot' control).
- NEW: Waveform display LCD timing readouts now default to showing MusicTime rather than sample position.
- FIX: Properly clear the selection box when the mouse is released outside of the Waveform Display or the Koreography Editor window.
- FIX: Properly handle clearing out the internal selection tracking list when committing an Undo/Redo action that modifies it.  No more Exception spam [from this]!
- FIX: If a selection exists when the Koreography Editor is in Draw mode, the selection will clear with the next mouse click rather than begin drawing events.
- FIX: Refactored the initial AudioClip processing to slightly speed up the initial AudioClip lodaing process.
- FIX: Logic surrounding the KLUDGE from the previous release regarding selecting the Koreographer internally hidden object has been updated.  The selection now changes to the currently loaded Koreography and updates to new Koreography as long as the user does not select another object in the scene or project.  It is not necessary in Unity 5.x at all and running in Unity 4.x will now not override Project Folder asset selections.
- FIX: Pasting events from the context menu (right-click) now respects the "Snap to Beat" setting.
- FIX: Scrubbing the audio during playback by scrolling with a track-pad or mouse no longer causes a slingshot effect (which would frequently send the playhead further backwards than where started even though the scroll direction was forward).
- FIX: Buttons/controls around the Waveform display now don't steal the focus away when interracted with, returning or pushing the control to the Waveform display.
- FIX: Koreography Editor window now has a valid minSize.  This new minimum size allows all potential controls to be seen.
- FIX: New/Load buttons now remember the chosen asset path, making subsequent load/save operations less aggravating.

0.0.7 Developer Preview - Third Release!
- NEW: Mouse input for event creation (drawing).
- NEW: Mouse input for event modification (event resizing/moving).
- NEW: Keyboard controls for Select/Draw mode switching (the Display must be 'focused'):
  - A - Select
  - S - Draw
- NEW: When multiple events are selected, global changes to payload and position may be effected.  NOTE: Edits to curves do not properly propagate to all selected events.  The workaround currently is to copy the first event and do a "Paste Payload" action across the entire group ([Control/⌘]+V).
- FIX: Remove other control focus when Waveform Display is 'focused'.
- FIX: When a user initiates a selection by dragging a selection box in the Waveform Display but then releases the mouse outside the Koreography Editor's window extents we now clear the selection when the mouse returns to the window.
- FIX: [KLUDGE] Select the Koreographer internally hidden object when no other object is selected in the scene.  This keeps Unity's curve editor from spewing exceptions into the console.  It also ensures that the gear icon with saved curves appears and is usable.
- FIX: Other bugfixes and optimizations.

0.0.6 Developer Preview - Second Release!
- NEW: Koreography Editor supports multiple standard keyboard commands!  The following functions are now supported:
  - [Control/⌘]+A - Select All Events
  - [Control/⌘]+X - Cut Selected Event(s) to clipboard
  - [Control/⌘]+C - Copy Selected Event(s) to clipboard
  - [Control/⌘]+V - Paste Event(s) from clipboard
  - [Control/⌘]+Shift+V - Paste earliest Payload from clipboard into selected Event(s)
- NEW: Context menu for the Waveform Display!  Paste from this menu allows placing copies of events in the clipboard to the waveform at the location of the mouse.


0.0.5 Developer Preview - First Release!
- NEW: Koreography Editor opened by clicking “Audio Tools->Koreography Editor”.  The Koreography Editor allows the generation and modification of Koreography.
- NEW: Koreographer component.  Load Koreography into the Koreographer and register with it for events.  This component also provides a Music Time interface.  This is currently set up to be a simple Singleton.
- NEW: Generic and extensible Event Payload system.  Currently included payload options include No Payload, Curve, Float, or Text.
- NEW: The MusicPlayer component - a music player that supports sample locked music layer playback, synchronization, and Koreographer integration.
- NEW: The SimpleMusicPLayer component - a single AudioClip music player that supports Koreographer integration.
