using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RotationController : SonosthesiaController {

	public Rotator Rotator = new Rotator(new Vector3(1f, 0f, 0f), 180f);
	public Spinner Spinner = new Spinner(new Vector3(1f, 0f, 0f), 180f);

	private bool spun = false;

	// Use this for initialization
	public override void Start () {

		base.Start();
	
		controlDelegates["rotation"] = delegate(ControlTarget target, ArrayList values) {
			TransformManipulator manipulator = target.GetManipulator<TransformManipulator>();
			Quaternion rotation = Rotator.GetRotation((float)(values[0]));
			manipulator.SetRotation(rotation);
		};

		// spin does not apply to individual targets (hence controlDelegates and not targetDelegate)
		// the effect is applied to each target in the Update method
		responderDelegates["spin"] = delegate(ArrayList values) {
			if ((float)(values[0]) > 1e-6) spun = true;
			else spun = false;
			Spinner.Spin = (float)(values[0]);
		};

	}
	
	// Update is called once per frame
	void Update () {

		List<ControlTarget> targets = TargetProvider.GetTargets();

		// spin involves the rotation being updated every frame
		if (spun) {
			foreach (ControlTarget target in targets) {
				TransformManipulator manipulator = target.GetManipulator<TransformManipulator>();
				Quaternion old = manipulator.GetRotation();
				manipulator.SetRotation(old *= Spinner.GetRotation());
			}	
		}

	}
}
