﻿
using com.spacepuppy.Utils;

namespace com.spacepuppy.Tween.Accessors
{

    [CustomTweenMemberAccessor(typeof(ITimeSupplier), "Scale")]
    public class TimeScaleMemberAccessor : ITweenMemberAccessor
    {

        public const string DEFAULT_TIMESCALE_ID = "SPTween.TimeScale";

        #region Fields

        private string _id = DEFAULT_TIMESCALE_ID;

        #endregion

        #region ITweenMemberAccessor Interface

        public System.Type GetMemberType()
        {
            return typeof(float);
        }

        public System.Type Init(object target, string propName, string args)
        {
            _id = (string.IsNullOrEmpty(args)) ? DEFAULT_TIMESCALE_ID : args;

            return typeof(float);
        }

        public object Get(object target)
        {
            var supplier = target as IScalableTimeSupplier;
            if (supplier != null && supplier.HasScale(_id))
            {
                return supplier.GetScale(_id);
            }
            return 1f;
        }

        public void Set(object target, object value)
        {
            var supplier = target as IScalableTimeSupplier;
            if (supplier != null)
            {
                supplier.SetScale(_id, ConvertUtil.ToSingle(value));
            }
        }

        #endregion

    }

}
