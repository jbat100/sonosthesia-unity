using UnityEngine;
using System.Collections;

public class MidiResponder : SonosthesiaResponder {

	static private string Tag = "MidiResponder";

	// TODO: add responder delegates for pressure, aftertouch, control...

	public delegate void MidiNoteOnDelegate(int channel, int pitch, int velocity);
	public delegate void MidiNoteOffDelegate(int channel, int pitch, int velocity);

	[HideInInspector]
	protected MidiNoteOnDelegate noteOnDelegate;

	[HideInInspector]
	protected MidiNoteOffDelegate noteOffDelegate;

	// Use this for initialization
	public override void Start () {

		base.Start();

		Debug.Log(Tag + " Start, setting midi controlDelegate");

		responderDelegates["midi"] = delegate(ArrayList values) {

			string midiMessageType = (string)values[0];

			switch (midiMessageType) {

			case "note_on":
				if (values.Count == 4) {
					noteOnDelegate((int)values[1], (int)values[2], (int)values[3]);
				}
				break;

			case "note_off":
				if (values.Count == 4) {
					noteOffDelegate((int)values[1], (int)values[2], (int)values[3]);
				}
				break;

			default:
				Debug.Log("Unsupported midi message type " + midiMessageType);
				break;

			}

		};

	}

}
