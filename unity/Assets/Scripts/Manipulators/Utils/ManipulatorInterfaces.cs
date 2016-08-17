using UnityEngine;
using System.Collections;

// interfaces would be nice if they didn't play so poorly with GetComponent, basically if we don't 
// want to have to cast to an interface on the client side we have to use the GetComponent<T> generic

public interface IColorManipulator {

	Color GetColor(GameObject target);

	void SetColor(GameObject target, Color color);

}

public interface IPositionManipulator {

	Vector3 GetPosition(GameObject target);

	void SetPosition(GameObject target, Vector3 position);

}

public interface IRotationManipulator {

	Quaternion GetRotation(GameObject target);

	void SetRotation(GameObject target, Quaternion rotation);

}

public interface IScaleManipulator {

	Vector3 GetScale(GameObject target);

	void SetScale(GameObject target, Vector3 scale);

}

public interface IParticleManipulator {

	void SetVelocity(GameObject target, float velocity);

	void SetSize(GameObject target, float size);

}