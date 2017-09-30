using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ControlTargetProvider : MonoBehaviour {

	[SerializeField] 
	private List<ControlTarget> targets = new List<ControlTarget>();


	public List<ControlTarget> GetTargets() {
		return targets;
	}

	public void AddTarget(ControlTarget target) {
		if (!targets.Contains(target)) {
			targets.Add(target);
		}
	}

	public void RemoveTarget(ControlTarget target) {
		if (targets.Contains(target)) {
			targets.Remove(target);
		}
	}

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
