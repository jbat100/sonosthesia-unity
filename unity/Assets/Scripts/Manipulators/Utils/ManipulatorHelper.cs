using UnityEngine;
using System.Collections;

public class ManipulatorHelper : System.Object {

	public GameObject Target { get { return target; } }

	private GameObject target;

	public ManipulatorHelper(GameObject _target) {
		target = _target;
	}
}
