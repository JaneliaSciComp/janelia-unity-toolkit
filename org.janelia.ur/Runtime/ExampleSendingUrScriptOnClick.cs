using UnityEngine;
using UnityEngine.SceneManagement;

namespace Janelia
{
    // An example of responding to a mouse click on an object by sending a command
    // to make a Universal Robots device (i.e., robotic arm, a.k.a. controller or server)
    // reach for the object.  The command is a short script in the URScript language,
    // and it is sent to the device via a `UrScriptClient` instance.
    
    public class ExampleSendingUrScriptOnClick : MonoBehaviour
    {
        // This default value is empirically determined for the Janelia arena model, 
        // ca. Oct. 2021.
        public Vector3 urSpaceOffset = new Vector3(-1.65f, 0, 0);

        public void Start()
        {
            _renderer = GetComponent<Renderer>();
            if (_renderer != null)
            {
                _color = _renderer.material.color;
            }
        }

        public void OnMouseEnter()
        {
            if (_renderer != null)
            {
                _renderer.material.color = Color.magenta;
            }
        }

        public void OnMouseExit()
        {
            if (_renderer != null)
            {
                _renderer.material.color = _color;
            }
        }

        public void OnMouseDown()
        {
            Janelia.UrScriptClient sender = Janelia.UrScriptClient.GetCurrent();

            if (sender != null)
            {
                float x = transform.position.z + urSpaceOffset.x;
                float y = -transform.position.x + urSpaceOffset.y;
                float z = transform.position.y + urSpaceOffset.z;

                // The `movej` puts the arm in a relatively neutral position, and reduces the risk
                // that moving to the final position (`x`, `y`, z`) will get the arm into an
                // unsafe configuration.
                string cmd = "def main():\n" +
                    "  movej([0, -1.57, 1.57, -1.57, -1.57, 0], t=2)\n" +
                    "  movel(p[" + x + ", " + y + ", " + z + ", 0, -3.14, 0], t=2)\n" +
                    "end\n";

                sender.Write(cmd);
            }
        }

        private Renderer _renderer;
        private Color _color;
    }
}
