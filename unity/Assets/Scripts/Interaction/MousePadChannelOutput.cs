using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Sonosthesia
{

    public class MousePadChannelOutput : PadChannelOutput
    {
        bool useLeft = true;
        bool useRight = true;
        bool useMiddle = true;
        
        protected override TouchInfo GetTouchInfo(int touchId)
        {
            TouchInfo info = new TouchInfo();
            info.position = Input.mousePosition;
            info.time = Time.unscaledTime;
            return info;
        }

        protected override void GetStartingTouches(List<int> list)
        {
            if (ScreenPointIsInPanel(Input.mousePosition))
            {
                if (useLeft && Input.GetMouseButtonDown(0))
                {
                    list.Add(0);
                }
                if (useRight && Input.GetMouseButtonDown(1))
                {
                    list.Add(1);
                }
                if (useMiddle && Input.GetMouseButtonDown(2))
                {
                    list.Add(2);
                }
            }
        }

        protected override void GetEndingTouches(List<int> list)
        {
            if (Input.GetMouseButtonDown(0))
            {
                list.Add(0);
            }
            if (Input.GetMouseButtonDown(1))
            {
                list.Add(1);
            }
            if (Input.GetMouseButtonDown(2))
            {
                list.Add(2);
            }
        }


    }

}


