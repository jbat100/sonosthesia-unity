﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sonosthesia
{
    public class TouchPadChannelOutput : PadChannelOutput
    {
        
        private Touch? GetTouchWithId(int touchId)
        {
            for(int i = 0; i < Input.touchCount; ++i)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.fingerId == touchId)
                {
                    return touch; 
                }
            }
            return null;
        } 

        protected override TouchInfo GetTouchInfo(int touchId)
        {
            TouchInfo info = new TouchInfo();

            Touch? touch = GetTouchWithId(touchId);

            if (touch != null)
            {
                info.position = new Vector3(touch.Value.position.x, touch.Value.position.y, touch.Value.pressure);
            }
            
            info.time = Time.unscaledTime;
            return info;
        }

        protected override void GetStartingTouches(List<int> list)
        {
            for (int i = 0; i < Input.touchCount; ++i)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began && ScreenPointIsInPanel(touch.position))
                {
                    list.Add(touch.fingerId);
                }
            }
        }

        protected override void GetEndingTouches(List<int> list)
        {
            for (int i = 0; i < Input.touchCount; ++i)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    list.Add(touch.fingerId);
                }
            }
        }
    }

}
