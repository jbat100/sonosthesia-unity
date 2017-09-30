using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Midi7BitRange : System.Object {
	
	[Range(0,127)] public int Min = 0;

	[Range(0,127)] public int Max = 127;

	public Boolean Contains(int value) {
		return (value >= Min) && (value <= Max);
	}

}

[System.Serializable]
public class Midi7BitDomain : System.Object {

	// use SerializeField to show private variables in the Editor 
	// https://unity3d.com/learn/tutorials/modules/beginner/tips/private-variables-in-the-inspector

	[SerializeField]
	private Boolean All = true;

	[SerializeField]
	private List<int> Values;

	[SerializeField]
	private List<Midi7BitRange> Ranges;

	public Boolean Contains(int test) {

		if (All) {
			return true;
		}

		bool found = false;
		foreach(int v in Values) {
			if (v == test) {
				found = true;
				break;
			}
		}

		if (found) return true;

		foreach(Midi7BitRange range in Ranges) {
			if (range.Contains(test)) {
				found = true;
				break;
			}
		}

		return found;

	}

	public void AddValue(int value) {
		//TODO add checks
		Values.Add(value);
	}

	public void AddRange(Midi7BitRange range) {
		Ranges.Add(range);
	}
}

[System.Serializable]
public class MidiNoteDomain : System.Object {

	public Midi7BitDomain ChannelDomain;
	public Midi7BitDomain PitchDomain;
	public Midi7BitDomain VelocityDomain;

	public Boolean ContainsNote(int channel, int pitch, int velocity) {
		return ChannelDomain.Contains(channel) && PitchDomain.Contains(pitch) && VelocityDomain.Contains(velocity);
	} 

}
