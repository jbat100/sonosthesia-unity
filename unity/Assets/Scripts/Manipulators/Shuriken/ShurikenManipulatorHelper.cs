using UnityEngine;
using System.Collections;

public class ShurikenManipulatorHelper : ManipulatorHelper {

	// according to a 153k (god level) stack overflow user a property named the same as a class is fine
	// http://stackoverflow.com/questions/1095644/should-a-property-have-the-same-name-as-its-type

	public ParticleSystem ParticleSystem { get { return particleSystem; } }

	private ParticleSystem particleSystem;

	public ShurikenManipulatorHelper(GameObject _target) : base (_target) {
		particleSystem = Target.GetComponent<ParticleSystem> ();
	}

}
