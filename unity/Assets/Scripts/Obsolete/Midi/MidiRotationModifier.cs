using UnityEngine;
using System.Collections;

public class MidiRotationModifier : MidiModifier {

	public enum Foundation { Identity, Quaternion, Transform, Mesh, Spline };

	public Foundation foundation;

	public QuaternionMapping quaternionMapping;
	public TransformMapping transformMapping;
	public MeshMapping meshMapping;
	// public SplineMapping splineMapping;

	public override void NoteOn(ControlTarget target, int channel, int pitch, int velocity) {

		float val = valueGenerator.GenerateNoteValue(channel, pitch, velocity);

		TransformManipulator manipulator = target.GetManipulator<TransformManipulator>();
		if (manipulator == null) return;

		switch(foundation) {
		case Foundation.Identity:
			manipulator.SetRotation(Quaternion.identity);
			break;
		case Foundation.Quaternion:
			manipulator.SetRotation(quaternionMapping.Map(val));
			break;
		case Foundation.Transform:
			manipulator.SetRotation(transformMapping.MapRotation(val));
			break;
		case Foundation.Mesh:
			manipulator.SetRotation(meshMapping.MapRotation(val));
			break;
		case Foundation.Spline:
			break;
		}

	}
}
