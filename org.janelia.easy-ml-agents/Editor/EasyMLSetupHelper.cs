using UnityEditor;
using UnityEngine;

namespace Janelia
{
    /// <summary>
    /// The <see cref="EasyMLArena"/> and <see cref="EasyMLAgent"/> classes are in the
    /// runtime assembly, but their setup functions need to be able call functionality
    /// (e.g., to create tags, or add materials to the asset database) available only in
    /// editor assembly.  So the editor passes an instance of this helper class to the
    /// runtime code, using the <see cref="IEasyMLSetupHelper"/> interface.
    /// </summary>
    public class EasyMLSetupHelper : IEasyMLSetupHelper
    {
        /// <summary>
        /// Displays an editor dialog.  
        /// </summary>
        /// <param name="title">The dialog's title</param>
        /// <param name="message">The dialog's message</param>
        /// <param name="ok">The label for the button with the "ok" role</param>
        /// <param name="cancel">The label for the button with the "cancel" role</param>
        /// <returns>True if the user presses the ok button</returns>
        public bool DisplayDialog(string title, string message, string ok = "OK", string cancel = "Cancel")
        {
            return EditorUtility.DisplayDialog(title, message, ok, cancel);
        }

        /// <summary>
        /// Gives the GameObject a polyhedral mesh asset.
        /// </summary>
        /// <param name="assignedTo">The object the mesh is assigned to</param>
        /// <param name="objFilename">The filename for the OBJ file describing the mesh</param>
        public void CreateMeshFilter(GameObject assignedTo, string objFilename)
        {
            string path = objFilename;
            if (!path.Contains("/"))
            {
                path = "Packages/org.janelia.easy-ml-agents/Assets/Models/" + path;
            }
            Mesh mesh = AssetDatabase.LoadAssetAtPath(path, typeof(Mesh)) as Mesh;
            if (mesh != null)
            {
                MeshFilter meshFilter = assignedTo.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = mesh;

                assignedTo.AddComponent<MeshRenderer>();
            }
        }

        /// <summary>
        /// Adds a string as one of the available tags.
        /// </summary>
        /// <param name="tag">The tag to create</param>
        public void CreateTag(string tag)
         {
            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath("ProjectSettings/TagManager.asset");
            if (asset != null)
            {
                SerializedObject so = new SerializedObject(asset);
                SerializedProperty tags = so.FindProperty("tags");
                int tagCount = tags.arraySize;
                for (int i = 0; i < tagCount; ++i)
                {
                    SerializedProperty oldTag = tags.GetArrayElementAtIndex(i);
                    if (oldTag.stringValue == tag)
                    {
                        return;
                    }
                }

                tags.InsertArrayElementAtIndex(tagCount);
                tags.GetArrayElementAtIndex(tagCount).stringValue = tag;
                so.ApplyModifiedProperties();
                so.Update();
            }
        }

        /// <summary>
        /// Gives the GameObject a new material asset that describes the object's color,
        /// or updates an existing one.
        /// </summary>
        /// <param name="obj">The object the material is assigned to</param>
        /// <param name="colorStr">The color specified by the material</param>
        public void CreateColorMaterial(GameObject obj, string colorStr)
        {
            Color color;
            if (!ColorUtility.TryParseHtmlString(colorStr, out color))
            {
                Debug.Log("Cannot parse color '" + colorStr + "'");
                return;
            }
            string colorName = UsingURP() ? "_BaseColor" : "_Color";

            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                Debug.Log("Cannot find MeshRenderer for '" + obj.name + "'");
                return;
            }

            Material mat = renderer.sharedMaterial;
            if ((mat != null) && (mat.name == obj.name))
            {
                mat.SetColor(colorName, color);
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }

            string shaderName = UsingURP() ? "Universal Render Pipeline/Lit" : "Standard";
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.Log("Cannot find shader '" + shaderName + "'");
                return;
            }

            mat = new Material(shader);
            if (mat == null)
            {
                Debug.Log("Cannot create Material for shader '" + shaderName + "'");
                return;
            }

            mat.name = obj.name;
            mat.SetColor(colorName, color);
            renderer.material = mat;

            string path = "Assets/Materials/" + obj.name + ".mat";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mat, path);
        }

        /// <summary>
        /// Gives the collider an asset that describes a few physics properties (i.e., friction),
        /// or updates an existing one.  Note that the file extension for this asset is 
        /// ".physicMaterial" without an "s", per Unity conventions.
        /// </summary>
        /// <param name="collider">The collider the material is assigned to</param>
        /// <param name="staticFriction">The static friction (0 to 1)</param>
        /// <param name="dynamicFriction">The dynamics friction (0 to 1)</param>
        public void CreatePhysicsMaterial(Collider collider, float staticFriction, float dynamicFriction)
        {
            PhysicMaterial mat = collider.sharedMaterial;
            if ((mat != null) && (mat.name == collider.gameObject.name))
            {
                mat.staticFriction = staticFriction;
                mat.dynamicFriction = dynamicFriction;
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/PhysicsMaterials"))
            {
                AssetDatabase.CreateFolder("Assets", "PhysicsMaterials");
            }

            mat = new PhysicMaterial();
            if (mat == null)
            {
                Debug.Log("Cannot create PhysicMaterial for GameObject '" + collider.gameObject.name + "'");
                return;
            }

            mat.name = collider.gameObject.name;
            mat.staticFriction = staticFriction;
            mat.dynamicFriction = dynamicFriction;
            collider.material = mat;

            string path = "Assets/PhysicsMaterials/" + collider.gameObject.name + ".physicMaterial";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mat, path);
        }

        /// <summary>
        /// Checks whether the URP (uiversal rendering pipeline) is being used.
        /// </summary>
        /// <returns>True if URP is in use</returns>

        public bool UsingURP()
        {
            return (UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset.GetType().Name == "UniversalRenderPipelineAsset");
        }
    }
}
