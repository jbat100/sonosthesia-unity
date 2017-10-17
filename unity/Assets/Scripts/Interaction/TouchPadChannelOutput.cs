using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sonosthesia
{
    public class TouchPadChannelOutput : PadChannelOutput
    {

        protected virtual Touch? GetTouchWithId(int touchId)
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

        protected override ContactInfo? GetContactInfo(int touchId)
        {
            Touch? touch = GetTouchWithId(touchId);

            if (touch != null)
            {
                ContactInfo info = new ContactInfo();
                info.position = new Vector3(touch.Value.position.x, touch.Value.position.y, touch.Value.pressure);
                info.time = Time.unscaledTime;
                return info;
            }

            return null;
        }

        protected override void GetStartingContacts(List<int> list)
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

        protected override void GetEndingContacts(List<int> list)
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

