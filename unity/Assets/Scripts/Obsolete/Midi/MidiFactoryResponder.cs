using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;


public class MidiFactoryResponder : MidiResponder {

	public MidiNoteDomain NoteDomain { get { return noteDomain;} }

	public ControlTarget prefab;

	private MidiNoteDomain noteDomain;
	private List<MidiModifier> modifiers;
	private MidiTargetProvider targetProvider;

	public override void Awake () {

		base.Awake ();

		targetProvider = GetComponent<MidiTargetProvider>();
		modifiers = GetComponents<MidiModifier>().ToList();

		Debug.Log(this.GetType().Name + " Awake, got " + modifiers.Count + " modifiers");
	}

	// Use this for initialization
	public override void Start () {

		base.Start();

		Debug.Log(this.GetType().Name + " Start, setting midi on and off delegates");


		noteOnDelegate = delegate(int channel, int pitch, int velocity) {

			Debug.Log(this.GetType().Name + " note_on : " + channel + " " + pitch + " " + velocity);

			if (NoteDomain.ContainsNote(channel, pitch, velocity) == false) {
				Debug.Log(this.GetType().Name + " midi note is not in the relevant midi domain");
				return;
			}

			if (prefab == null) {
				Debug.Log(this.GetType().Name + " no prefab");
				return; 
			}

			// set the instance as a child of the spawner so that the overall spawning structure can be moved/rotated
			ControlTarget target = Instantiate(prefab);
			target.transform.parent = transform;

			foreach(MidiModifier modifier in modifiers) {
				modifier.NoteOn(target, channel, pitch, velocity);
			}

			targetProvider.RegisterNoteTarget(target, channel, pitch, velocity);

		};

		noteOffDelegate = delegate(int channel, int pitch, int velocity) {

			Debug.Log(this.GetType().Name + " note_off : " + channel + " " + pitch + " " + velocity);

			MidiNoteTarget noteTarget = targetProvider.GetNoteTarget(channel, pitch);
			if (noteTarget != null) {
				foreach(MidiModifier modifier in modifiers) {
					modifier.NoteOff(noteTarget.Target, channel, pitch, velocity);
				}
			}

			targetProvider.DestroyNoteTarget(channel, pitch);

		};

	}

}
