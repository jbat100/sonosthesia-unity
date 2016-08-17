using UnityEngine;
using System.Collections;


public class SonosthesiaManipulator : MonoBehaviour {

	public GameObject Target { get { return target;} }

	private GameObject target;

	public virtual void Awake() {
		target = gameObject;
	}

}
