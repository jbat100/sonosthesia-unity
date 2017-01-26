using UnityEngine;
using System.Collections;


// most of this stuff should probably go in the hub

[System.Serializable]
public class ValueGenerator : System.Object {

	public enum GeneratorMode { Linear, Random, Drunk };
	public enum OverflowMode { Unbound, Clamp, Repeat };

	public GeneratorMode mappingMode;
	public OverflowMode overflowMode;

	public float offset = 0.0f;
	public float scale = 1.0f;

	public float drunkStep = 0.01f;

	private float random = 0.0f;
	private float drunk = 0.5f;
	private float input = 0.0f;

	public void SetInput(float value) {
		input = value;
	}

	public float ApplyOverflow(float value) {

		switch(overflowMode) {
		case OverflowMode.Clamp:
			value = Mathf.Clamp(value, 0.0f, 1.0f);
			break;
		case OverflowMode.Repeat:
			value = Mathf.Repeat(value, 1.0f);
			break;
		}
		return value;

	}

	// ususally called from a mono behaviour's update, with the time delta as argument
	// this is because some mapping generators may be time dependent (like drunk for example)

	public void Update(float time) {

		switch (mappingMode) {
		case GeneratorMode.Drunk:
			drunk += (drunkStep * (UnityEngine.Random.value > 0.5f ? 1.0f : -1.0f) ) * time;
			drunk = ApplyOverflow(drunk);
			break;
		case GeneratorMode.Random:
			random = UnityEngine.Random.value;
			break;
		default:
			break;
		}

	}

	// if we are using a random / drunk modes (for example) and want the corresponding randomly generated
	// values to be locked accross multiple mappings then call Update on one of them, then call SyncTo 
	// on the others giving the first as argument

	public void SyncTo(ValueGenerator other) {
		drunk = other.drunk;
		random = other.random;
	}

	public float Generate() {

		float value = 0.0f;

		switch(mappingMode) {
		case GeneratorMode.Drunk:
			value = drunk;
			break;
		case GeneratorMode.Random:
			value = random;
			break;
		case GeneratorMode.Linear:
			value = input;
			break;
		default:
			break;
		}

		return ApplyOverflow((value*scale)+offset);

	}

};



[System.Serializable]
public class FloatMapping : System.Object
{
	public float Zero = 0f;
	public float Unit = 1f;

	public FloatMapping(float _zero, float _unit) {
		Zero = _zero;
		Unit = _unit;
	}

	public float Map(float val) {
		return (val * (Unit - Zero)) + Zero;
	}
}

[System.Serializable]
public class Vector3Mapping : System.Object
{
	public Vector3 Zero = Vector3.zero;
	public Vector3 Unit = Vector3.one;

	public Vector3Mapping(Vector3 _zero, Vector3 _unit) {
		Zero = _zero;
		Unit = _unit;
	}

	public Vector3 Map(float val) {
		return Vector3.Lerp(Zero, Unit, val);
	}
}

[System.Serializable]
public class QuaternionMapping : System.Object
{
	public Quaternion Zero = Quaternion.identity;
	public Quaternion Unit = Quaternion.LookRotation(Vector3.forward);

	public Quaternion Map(float val) {
		return Quaternion.Slerp(Zero, Unit, val);
	}
}
	

[System.Serializable]
public class TransformMapping : System.Object
{
	public Transform Zero;
	public Transform Unit;

	public bool SlerpRotation = true; // slerp is slower but better

	public Vector3 MapScale(float val) {
		return Vector3.Lerp(Zero.localScale, Unit.localScale, val);
	}

	public Vector3 MapPosition(float val) {
		return Vector3.Lerp(Zero.localPosition, Unit.localPosition, val);
	}

	public Quaternion MapRotation(float val) {
		if (SlerpRotation) return Quaternion.Slerp(Zero.localRotation, Unit.localRotation, val);
		return Quaternion.Lerp(Zero.localRotation, Unit.localRotation, val);
	}

}

[System.Serializable]
public class MeshMapping : System.Object
{
	public Mesh mesh;

	// val will determine which vertex on the mesh will be selected, then map vertex position and normal

	public Vector3 MapPosition(float val) { 
		return Vector3.zero;
	}

	public Quaternion MapRotation(float val) { 
		return Quaternion.identity;
	}
}

[System.Serializable]
public class Rotator : System.Object
{
	public Vector3 Axis = Vector3.forward;
	public float Scale = 180f;

	public Rotator(Vector3 _axis, float _scale) {
		Axis = _axis;
		Scale = _scale;
	}

	public Quaternion GetRotation(float val) {
		return Quaternion.AngleAxis ( Scale * val, Axis );
	}
}

[System.Serializable]
public class Spinner : System.Object
{
	public Vector3 Axis = Vector3.forward;
	public float Scale = 180f;
	public float Spin = 0f;

	public Spinner(Vector3 _axis, float _scale) {
		Axis = _axis;
		Scale = _scale;
	}

	public Quaternion GetRotation() {
		return Quaternion.AngleAxis ((float)(Scale * Spin * Time.deltaTime * 60f), Axis);
	}
}