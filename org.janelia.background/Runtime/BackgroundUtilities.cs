using UnityEngine;

namespace Janelia
{
    public static class BackgroundUtilities
    {
        public static void SetCylinderTextureOffset(Vector2 offset)
        {
            if (_cylinderMaterial == null)
            {
                _cylinderMaterial = Resources.Load(Janelia.CylinderBackgroundResources.MaterialName, typeof(Material)) as Material;
            }
            if (_cylinderMaterial != null)
            {
                _cylinderMaterial.SetTextureOffset("_MainTex", offset);
            }
            else
            {
                Debug.LogError("Could not load material'" + Janelia.CylinderBackgroundResources.MaterialName + "'");
            }
        }

        private static Material _cylinderMaterial;
    }
}
