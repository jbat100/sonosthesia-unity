using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// for color utils
using com.spacepuppy.Utils;

namespace Sonosthesia
{
    // encapsulated in a data structure to be more easily passed beteen the modifier and the implementation
    [Serializable ]
    public class ColorModifierSettings
    {
        public Color initial = Color.black;
        public Gradient gradient1;
        public Gradient gradient2;
    }

    // moved to seperate class so that the implementation can be static
    public class ColorModifierImplementation
    {
        private Dictionary<string, Func<ColorModifierSettings, Color, IList<float>, Color>> _modifiers;

        public Color Modify(string key, ColorModifierSettings settings, Color current, IList<float> parameter)
        {
            if (_modifiers.ContainsKey(key))
            {
                return _modifiers[key](settings, current, parameter);
            }
            return current;
        }

        public ColorModifierImplementation()
        {
            Setup();
        }

        protected virtual void Setup()
        {
            _modifiers = new Dictionary<string, Func<ColorModifierSettings, Color, IList<float>, Color>>();

            // single a settings

            _modifiers["color_alpha"] = (settings, color, parameter) =>
            {
                color.SetAlpha(parameter[0]);
                return Color.black;
            };

            // single rgb settings

            _modifiers["color_red"] = (settings, color, parameter) =>
            {
                color.SetRed(parameter[0]);
                return Color.black;
            };

            _modifiers["color_green"] = (settings, color, parameter) =>
            {
                color.SetGreen(parameter[0]);
                return Color.black;
            };

            _modifiers["color_blue"] = (settings, color, parameter) =>
            {
                color.SetBlue(parameter[0]);
                return Color.black;
            };

            // single hsv settings

            _modifiers["color_hue"] = (settings, color, parameter) =>
            {
                HSBColor hsb = HSBColor.FromColor(color);
                hsb.h = parameter[0];
                return hsb.ToColor();
            };

            _modifiers["color_saturation"] = (settings, color, parameter) =>
            {
                HSBColor hsb = HSBColor.FromColor(color);
                hsb.s = parameter[0];
                return hsb.ToColor();
            };
            
            _modifiers["color_brightness"] = (settings, color, parameter) =>
            {
                HSBColor hsb = HSBColor.FromColor(color);
                hsb.b = parameter[0];
                return hsb.ToColor();
            };

            // full settings

            _modifiers["color_rgba"] = (settings, color, parameter) =>
            {
                return new Color(parameter[0], parameter[1], parameter[2], parameter[3]);
            };

            _modifiers["color_hsba"] = (settings, color, parameter) =>
            {
                return new HSBColor(parameter[0], parameter[1], parameter[2], parameter[3]).ToColor();
            };

            // grad settings

            _modifiers["color_grad_1"] = (settings, color, parameter) =>
            {
                return settings.gradient1.Evaluate(parameter[0]);
            };

            _modifiers["color_grad_2"] = (settings, color, parameter) =>
            {
                return settings.gradient2.Evaluate(parameter[0]);
            };

            _modifiers["color_grad_mix"] = (settings, color, parameter) =>
            {
                Color color1 = settings.gradient1.Evaluate(parameter[0]);
                Color color2 = settings.gradient2.Evaluate(parameter[1]);
                if (parameter.Count > 2) return Color.Lerp(color1, color2, parameter[2]);
                return Color.Lerp(color1, color2, 0.5f);
            };
        }
    }


    public class ColorModifier : BaseChannelModifier
    {

        public ColorAccessor ColorAccessor;

        public ColorModifierSettings settings;
        

        public override IEnumerable<ChannelParameterDescription> ParameterDescriptions { get { return _parameterDescriptions; } }

        protected virtual Color InitialColor
        {
            get
            {
                return (settings != null && settings.initial != null) ? settings.initial : Color.black;
            }
        }

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

        private static ColorModifierImplementation _implementation = new ColorModifierImplementation();

        
        public override void Initialise(GameObject representation)
        {
            if (ColorAccessor != null)
            {
                ColorAccessor.SetColor(representation, InitialColor);
            }
        }

        public override void ApplyParameter(GameObject representation, string key, IList<float> parameter)
        {
            if (ColorAccessor != null)
            {
                Color current = ColorAccessor.GetColor(representation);
                Color modified = _implementation.Modify(key, settings, current, parameter);
                ColorAccessor.SetColor(representation, modified);
            }

        }

        
    }


}


