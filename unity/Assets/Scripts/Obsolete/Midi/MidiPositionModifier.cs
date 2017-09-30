using UnityEngine;
using System.Collections;

public class MidiPositionModifier : MidiModifier {

	public enum Foundation { Identity, Vector, Transform, Mesh, Spline };

	public Foundation foundation;

	public Vector3Mapping vectorMapping;
	public TransformMapping transformMapping;
	public MeshMapping meshMapping;
	// public SplineMapping splineMapping;


	public override void NoteOn(ControlTarget target, int channel, int pitch, int velocity) {

		float val = valueGenerator.GenerateNoteValue(channel, pitch, velocity);

		TransformManipulator manipulator = target.GetManipulator<TransformManipulator>();
		if (manipulator == null) return;

		switch(foundation) {
		case Foundation.Identity:
			manipulator.SetPosition(Vector3.zero);
			break;
		case Foundation.Vector:
			manipulator.SetPosition(vectorMapping.Map(val));
			break;
		case Foundation.Transform:
			manipulator.SetPosition(transformMapping.MapPosition(val));
			break;
		case Foundation.Mesh:
			manipulator.SetPosition(meshMapping.MapPosition(val));
			break;
		case Foundation.Spline:
			break;
		}

	}
}
