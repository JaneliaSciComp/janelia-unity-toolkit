using UnityEngine;

namespace Janelia
{
    public static class BackgroundUtilities
    {
        public static void SetCylinderTextureOffset(Vector2 offset, int which = 0)
        {
            if (_cylinderMaterial == null)
            {
                _cylinderMaterial = Resources.Load(Janelia.CylinderBackgroundResources.MaterialName, typeof(Material)) as Material;
            }
            if (_cylinderMaterial != null)
            {
                switch (which)
                {
                    case 0:
                    case 1:
                        _cylinderMaterial.SetVector("_MainTex_ST", offset);
                        break;
                    case 2:
                        _cylinderMaterial.SetVector("_SecondTex_ST", offset);
                        break;
                    default:
                        Debug.LogError("SetCylinderTextureOffset, which = " + which + " not supported");
                        break;
                }
            }
            else
            {
                Debug.LogError("Could not load material '" + Janelia.CylinderBackgroundResources.MaterialName + "'");
            }
        }

        private static Material _cylinderMaterial;
    }
}
