using UnityEngine;
using System.Collections;

public class MidiModifier : MonoBehaviour {

	public MidiValueGenerator valueGenerator;

	// perhaps have a sync (source/target) switch here for the value generator

	private MidiModifier syncSource;

	// Use this for initialization
	public virtual void Start () {

	}
	
	// Update is called once per frame
	public virtual void Update () {
	
	}

	public virtual void NoteOn(ControlTarget target, int channel, int pitch, int velocity) {
		
	}

	public virtual void NoteOff(ControlTarget target, int channel, int pitch, int velocity) {
		
	}

	// add methods for control, pressure, aftertouch midi messages
}
