﻿using UnityEngine;

using com.spacepuppy.Utils;

namespace com.spacepuppy.Tween.Curves
{

    [CustomMemberCurve(typeof(Color32))]
    public class Color32MemberCurve : MemberCurve
    {

        #region Fields

        private Color32 _start;
        private Color32 _end;

        #endregion

        #region CONSTRUCTOR

        protected Color32MemberCurve()
        {

        }

        public Color32MemberCurve(string propName, float dur, Color32 start, Color32 end)
            : base(propName, dur)
        {
            _start = start;
            _end = end;
        }

        public Color32MemberCurve(string propName, Ease ease, float dur, Color32 start, Color32 end)
            : base(propName, ease, dur)
        {
            _start = start;
            _end = end;
        }

        protected override void ReflectiveInit(object start, object end, object option)
        {
            _start = ConvertUtil.ToColor32(start);
            _end = ConvertUtil.ToColor32(end);
        }

        #endregion

        #region Properties

        public Color32 Start
        {
            get { return _start; }
            set { _start = value; }
        }

        public Color32 End
        {
            get { return _end; }
            set { _end = value; }
        }

        #endregion

        #region MemberCurve Interface

        protected override object GetValueAt(float dt, float t)
        {
            if (this.Duration == 0f) return _end;
            t = this.Ease(t, 0f, 1f, this.Duration);
            return ColorUtil.Lerp(_start, _end, t);
        }

        #endregion

    }
}
