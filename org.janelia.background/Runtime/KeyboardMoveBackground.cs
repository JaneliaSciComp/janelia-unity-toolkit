using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Janelia
{
    public class KeyboardMoveBackground : MonoBehaviour
    {
        public float keyDeltaX = 0.001f;
        public float keyDeltaY = 0.001f;
        public bool keyRepeat = true;
        public bool keyWASD = false;
        public bool headingCompensation = true;

        public void Start()
        {
            _initialEulerAngles = transform.eulerAngles;
        }

        public void LateUpdate()
        {
            if (Input.GetKey(KeyCode.Escape))
            {
                Application.Quit();
            }

            float deltaX = 0;
            float deltaY = 0;
    
            string left = keyWASD ? "a" : "left";
            string right = keyWASD ? "d" : "right";
            string up = keyWASD ? "w" : "up";
            string down = keyWASD ? "s" : "down";

            if (keyRepeat)
            {
                if (Input.GetKey(left))
                {
                    deltaX = keyDeltaX;
                }
                else if (Input.GetKey(right))
                {
                    deltaX = -keyDeltaX;
                }
                if (Input.GetKey(up))
                {
                    deltaY = -keyDeltaY;
                }
                else if (Input.GetKey(down))
                {
                    deltaY = keyDeltaY;
                }
            }
            else
            {
                if (Input.GetKeyDown(left))
                {
                    deltaX = keyDeltaX;
                }
                else if (Input.GetKeyDown(right))
                {
                    deltaX = -keyDeltaX;    
                }
                if (Input.GetKeyDown(up))
                {
                    deltaY = -keyDeltaY;
                }
                else if (Input.GetKeyDown(down))
                {
                    deltaY = keyDeltaY;
                }
            }
            _textureOffset = _textureOffset + new Vector2(deltaX, deltaY);

            float compensationX = (_initialEulerAngles.y - transform.eulerAngles.y) / 360.0f;
            Vector2 compensation = headingCompensation ? new Vector2(compensationX, 0) : Vector2.zero;

            int which = 2;
            Janelia.BackgroundUtilities.SetCylinderTextureOffset(_textureOffset + compensation, which);
        }

        private Vector3 _initialEulerAngles;
        private Vector2 _textureOffset = Vector2.zero;
    }
}
