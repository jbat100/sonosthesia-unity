﻿using System.Collections;
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
        
        protected override ContactInfo? GetContactInfo(int touchId)
        {
            ContactInfo info = new ContactInfo();
            info.position = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 1f);
            info.time = Time.unscaledTime;
            return info;
        }

        protected override void GetStartingContacts(List<int> list)
        {
            if (ScreenPointIsInPanel(Input.mousePosition))
            {
                InteractionHelpers.GetMouseButtonDowns(list, useLeft, useRight, useMiddle);
            }
        }

        protected override void GetEndingContacts(List<int> list)
        {
            InteractionHelpers.GetMouseButtonUps(list, useLeft, useRight, useMiddle);
        }


    }

}


