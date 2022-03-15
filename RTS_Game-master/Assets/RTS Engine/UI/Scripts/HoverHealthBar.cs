using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/* Hover Health Bar script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class HoverHealthBar : MonoBehaviour
    {
        //make sure the hover health bar is always looking at the camera
        //确保悬停健康栏始终注视着摄像头
        Transform CamTransform;

        void Start ()
        {
            CamTransform = GameManager.Instance.CamMov.transform;
        }

        void Update()
        {
            //move the canvas in order to face the camera and look at it
            //移动画布以面对相机并观察它 
            transform.LookAt(transform.position + CamTransform.rotation * Vector3.forward,
                CamTransform.rotation * Vector3.up);
        }
    }
}
