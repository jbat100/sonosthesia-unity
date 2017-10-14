using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Sonosthesia
{

    public class MousePadChannelOutput : PadChannelOutput
    {
        public bool useLeft = true;
        public bool useRight = true;
        public bool useMiddle = true;
        
        protected override TouchInfo GetTouchInfo(int touchId)
        {
            TouchInfo info = new TouchInfo();
            info.position = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 1f);
            info.time = Time.unscaledTime;
            return info;
        }

        protected override void GetStartingTouches(List<int> list)
        {
            if (ScreenPointIsInPanel(Input.mousePosition))
            {
                //Debug.Log("GetStartingTouches mouse in panel");
                if (useLeft && Input.GetMouseButtonDown(0))
                {
                    //Debug.Log("GetStartingTouches starting left");
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
            if (Input.GetMouseButtonUp(0))
            {
                //Debug.Log("GetStartingTouches end left");
                list.Add(0);
            }
            if (Input.GetMouseButtonUp(1))
            {
                list.Add(1);
            }
            if (Input.GetMouseButtonUp(2))
            {
                list.Add(2);
            }
        }


    }

}


