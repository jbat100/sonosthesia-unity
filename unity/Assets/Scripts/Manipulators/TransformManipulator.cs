using UnityEngine;
using System.Collections;


public class TransformManipulator : SonosthesiaManipulator {

	public Vector3 GetPosition() {
		return Target.transform.localPosition;
	}

	public void SetPosition(Vector3 position) {
		Target.transform.localPosition = position;
	}

	public Quaternion GetRotation() {
		return Target.transform.localRotation;
	}

	public void SetRotation(Quaternion rotation) {
		Target.transform.localRotation = rotation;
	}

	public Vector3 GetScale() {
		return Target.transform.localScale;
	}

	public void SetScale(Vector3 scale) {
		Target.transform.localScale = scale;
	}

}
