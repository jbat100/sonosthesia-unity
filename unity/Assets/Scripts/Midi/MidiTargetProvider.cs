using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

[System.Serializable]
public class MidiNoteTarget : System.Object {

	private ControlTarget target;
	private int channel;
	private int pitch;
	private int velocity;

	public MidiNoteTarget(ControlTarget _target, int _channel, int _pitch, int _velocity) {
		target = _target;
		channel = _channel;
		pitch = _pitch;
		velocity = _velocity;
	}

	public ControlTarget Target { get { return target; } }

	public int Channel { get { return channel; } }

	public int Pitch { get { return pitch; } }

	public int Velocity { get { return velocity; } }

}


public class MidiTargetProvider : ControlTargetProvider {
	
	private List<MidiNoteTarget> noteTargets;

	public void RegisterNoteTarget(ControlTarget target, int channel, int pitch, int velocity) {
		AddTarget(target);
		MidiNoteTarget noteTarget = new MidiNoteTarget(target, channel, pitch, velocity);
		noteTargets.Add(noteTarget);
	}
		
	public void DestroyNoteTarget(int channel, int pitch) {
		MidiNoteTarget noteTarget = GetNoteTarget(channel, pitch);
		if (noteTarget != null) {
			noteTargets.Remove(noteTarget);
			RemoveTarget(noteTarget.Target);
			Destroy(noteTarget.Target);
		};
	}

	public MidiNoteTarget GetNoteTarget (int channel, int pitch) {
		return noteTargets.Find(i => i.Channel == channel && i.Pitch == pitch);
	}
		
}
