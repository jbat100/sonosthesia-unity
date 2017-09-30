using UnityEngine;
using System.Collections;
using System.Linq;
using System;

namespace Sonosthesia.Obsolete
{
    public class ColorController : SonosthesiaController
    {

        public FloatMapping hueMapping = new FloatMapping(0f, 1f);
        public FloatMapping saturationMapping = new FloatMapping(0f, 1f);
        public FloatMapping brightnessMapping = new FloatMapping(0f, 1f);

        // Use this for initialization
        public override void Start()
        {

            base.Start();

            controlDelegates["hue"] = delegate (ControlTarget target, ArrayList values) {
                ColorManipulator manipulator = target.GetManipulator<ColorManipulator>();
                if (manipulator == null)
                {
                    Debug.LogError(this.GetType().Name + " could not find color manipulator");
                    return;
                }
                HSBColor hsbColor = HSBColor.FromColor(manipulator.GetColor());
                hsbColor.h = hueMapping.Map((float)(values[0]));
                manipulator.SetColor(hsbColor.ToColor());
            };

            controlDelegates["saturation"] = delegate (ControlTarget target, ArrayList values) {
                ColorManipulator manipulator = target.GetManipulator<ColorManipulator>();
                HSBColor hsbColor = HSBColor.FromColor(manipulator.GetColor());
                hsbColor.s = saturationMapping.Map((float)(values[0]));
                manipulator.SetColor(hsbColor.ToColor());
            };

            controlDelegates["brightness"] = delegate (ControlTarget target, ArrayList values) {
                ColorManipulator manipulator = target.GetManipulator<ColorManipulator>();
                HSBColor hsbColor = HSBColor.FromColor(manipulator.GetColor());
                hsbColor.b = brightnessMapping.Map((float)(values[0]));
                manipulator.SetColor(hsbColor.ToColor());
            };

            //Debug.Log(this.GetType().Name + " set control delegates for keys : " + String.Join(", ", controlDelegates.Keys.ToArray()));

        }

    }

}
