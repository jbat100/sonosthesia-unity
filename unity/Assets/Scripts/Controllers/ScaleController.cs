using UnityEngine;
using System.Collections;

public class ScaleController : SonosthesiaController {

	public Vector3Mapping scaleMapping = new Vector3Mapping(new Vector3(0.5f, 1f, 1f), new Vector3(5f, 1f, 1f));

	// Use this for initialization
	public override void Start () {

		base.Start();

		controlDelegates["scale"] = delegate(ControlTarget target, ArrayList values) {
			Vector3 scale = scaleMapping.Map((float)(values[0]));
			Debug.Log ("TransformManipulator setting scale " + scale.ToString() );
			TransformManipulator manipulator = target.GetManipulator<TransformManipulator>();
			manipulator.SetScale(scale);
		};
	
	}

}
