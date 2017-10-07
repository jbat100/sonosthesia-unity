using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


using com.spacepuppy.Geom;

namespace Sonosthesia
{

    [Serializable]
    public class TransModifierSettings
    {
        public Transform initial;
    }

    // moved to seperate class so that the implementation can be static
    public class TransModifierImplementation
    {
        private Dictionary<string, Func<TransModifierSettings, Trans, IList<float>, Trans>> _modifiers;

        public Trans Modify(string key, TransModifierSettings settings, Trans current, IList<float> parameter)
        {
            if (_modifiers.ContainsKey(key))
            {
                return _modifiers[key](settings, current, parameter);
            }
            return current;
        }

        public TransModifierImplementation()
        {
            Setup();
        }

        protected virtual void Setup()
        {
            _modifiers = new Dictionary<string, Func<TransModifierSettings, Trans, IList<float>, Trans>>();

            // position settings

            _modifiers["position_x"] = (settings, trans, parameter) =>
            {
                Vector3 position = trans.Position;
                trans.Position = new Vector3(parameter[0], position.y, position.z);
                return trans;
            };

            _modifiers["position_y"] = (settings, trans, parameter) =>
            {
                Vector3 position = trans.Position;
                trans.Position = new Vector3(position.x, parameter[0], position.z);
                return trans;
            };

            _modifiers["position_z"] = (settings, trans, parameter) =>
            {
                Vector3 position = trans.Position;
                trans.Position = new Vector3(position.x, position.y, parameter[0]);
                return trans;
            };

            _modifiers["position_xyz"] = (settings, trans, parameter) =>
            {
                trans.Position = new Vector3(parameter[0], parameter[1], parameter[2]);
                return trans;
            };

            // rotation settings

            _modifiers["rotation_x"] = (settings, trans, parameter) =>
            {
                Vector3 rotation = trans.Rotation.eulerAngles;
                trans.Rotation = Quaternion.Euler(new Vector3(parameter[0], rotation.y, rotation.z));
                return trans;
            };

            _modifiers["rotation_y"] = (settings, trans, parameter) =>
            {
                Vector3 rotation = trans.Rotation.eulerAngles;
                trans.Rotation = Quaternion.Euler(new Vector3(rotation.x, parameter[0], rotation.z));
                return trans;
            };

            _modifiers["rotation_z"] = (settings, trans, parameter) =>
            {
                Vector3 rotation = trans.Rotation.eulerAngles;
                trans.Rotation = Quaternion.Euler(new Vector3(rotation.x, rotation.y, parameter[0]));
                return trans;
            };

            _modifiers["rotation_xyz"] = (settings, trans, parameter) =>
            {
                trans.Rotation = Quaternion.Euler(new Vector3(parameter[0], parameter[1], parameter[2]));
                return trans;
            };

            // scale settings

            _modifiers["scale_uniform"] = (settings, trans, parameter) =>
            {
                trans.Scale = new Vector3(parameter[0], parameter[0], parameter[0]);
                return trans;
            };

            _modifiers["scale_x"] = (settings, trans, parameter) =>
            {
                Vector3 scale = trans.Scale;
                trans.Scale = new Vector3(parameter[0], scale.y, scale.z);
                return trans;
            };

            _modifiers["scale_y"] = (settings, trans, parameter) =>
            {
                Vector3 scale = trans.Scale;
                trans.Scale = new Vector3(scale.x, parameter[0], scale.z);
                return trans;
            };

            _modifiers["scale_z"] = (settings, trans, parameter) =>
            {
                Vector3 scale = trans.Scale;
                trans.Scale = new Vector3(scale.x, scale.y, parameter[0]);
                return trans;
            };

            _modifiers["scale_xyz"] = (settings, trans, parameter) =>
            {
                trans.Scale = new Vector3(parameter[0], parameter[1], parameter[2]);
                return trans;
            };

        }
    }

    public class TransModifier : BaseChannelModifier
    {
        public TransAccessor transAccessor;

        public TransModifierSettings settings;

        protected virtual Trans InitialTrans
        {
            get
            {
                return (settings == null && settings.initial == null) ? Trans.GetGlobal(settings.initial) : Trans.Identity;
            }
        }

        public override IEnumerable<ChannelParameterDescription> ParameterDescriptions { get { return _parameterDescriptions; } }

        static private IEnumerable<ChannelParameterDescription> _parameterDescriptions = new List<ChannelParameterDescription>()
        {
            new ChannelParameterDescription("color_alpha"),

            new ChannelParameterDescription("color_red"),
            new ChannelParameterDescription("color_green"),
            new ChannelParameterDescription("color_blue"),

            new ChannelParameterDescription("color_hue"),
            new ChannelParameterDescription("color_saturation"),
            new ChannelParameterDescription("color_brightness"),

            new ChannelParameterDescription("color_grad_1"),
            new ChannelParameterDescription("color_grad_2"),

            new ChannelParameterDescription("color_grad_mix", 0f, 1f, 0f, 2),

            new ChannelParameterDescription("color_rgba", 0f, 1f, 0f, 4),
            new ChannelParameterDescription("color_hsva", 0f, 1f, 0f, 4)
        };

        private static TransModifierImplementation _implementation = new TransModifierImplementation();

        public override void Initialise(GameObject representation)
        {
            if (transAccessor != null)
            {
                transAccessor.SetTrans(representation, InitialTrans);
            }
        }

        public override void ApplyParameter(GameObject representation, string key, IList<float> parameter)
        {
            if (transAccessor != null)
            {
                Trans current = transAccessor.GetTrans(representation);
                Trans modified = _implementation.Modify(key, settings, current, parameter);
                transAccessor.SetTrans(representation, modified);
            }
        }
    }

}


