----------------------------------------------
            Koreographer™
 Copyright © 2014 Sonic Bloom, LLC
            Version 0.0.7
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
