using UnityEngine;
using System.Collections;

public class MidiScaleModifier : MidiModifier {

	public enum Foundation { Identity, Vector, Transform };

	public Foundation foundation;

	public Vector3Mapping vectorMapping;
	public TransformMapping transformMapping;

	public override void NoteOn(ControlTarget target, int channel, int pitch, int velocity) {

		float val = valueGenerator.GenerateNoteValue(channel, pitch, velocity);

		TransformManipulator manipulator = target.GetManipulator<TransformManipulator>();
		if (manipulator == null) return;

		switch(foundation) {
		case Foundation.Identity:
			manipulator.SetScale(Vector3.one);
			break;
		case Foundation.Vector:
			manipulator.SetScale(vectorMapping.Map(val));
			break;
		case Foundation.Transform:
			manipulator.SetScale(transformMapping.MapScale(val));
			break;
		}

	}
}
