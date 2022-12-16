using System;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.Animations;

namespace Janelia
{
    public class SetupCylinderBackground : EditorWindow
    {
        [MenuItem("Window/Set Up Background Cylinder")]
        public static void ShowWindow()
        {
            InitializeResourceFolders();
            GetWindow(typeof(SetupCylinderBackground));
        }

        public void OnEnable()
        {
            Load();
        }

        public void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            // It does not work to store a GameObject reference in the window state.  Unity cannot serialize such references
            // very well, so the window state would not be reliably recreated in the following necessary situations: 
            // when a Unity editor session is started; when this window is closed and reopened in a session; and (most insidiously)
            // when the game is built, a process which causes all GameObject references to become obsolete.  An alternative is
            // to store an identifier for the GameObject int the window state, and to translate from this identifer and back again
            // within the current routine, so the user interface works in terms of GameObject references.  The "path name" for
            // a GameObject works well enough as this identifier.  In theory, it could fail, because Unity does not require that
            // GameObject names be unique.  But a reasonable user will make names unique, so this approach works well enough.

            GameObject subject = GameObject.Find(_subjectPath);
            subject = EditorGUILayout.ObjectField("Subject", subject, typeof(GameObject), true) as GameObject;
            if (subject != null)
            {
                _subjectPath = PathName(subject);
            }

            _radius = EditorGUILayout.FloatField("Cylinder radius", _radius);
            _height = EditorGUILayout.FloatField("Cylinder height", _height);

            if (GUILayout.Button("Update"))
            {
                SetUp(subject);
                Save();
            }

            EditorGUILayout.EndVertical();
        }

        public void OnDestroy()
        {
            Save();
        }

        [PostProcessBuildAttribute(Janelia.SessionParameters.POST_PROCESS_BUILD_ORDER + 2)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            Janelia.SessionParameters.AddStringParameter("backgroundCylinderTexture", "");
        }

        private void SetUp(GameObject subject)
        {
            if (subject)
            {
                const string modelPath = "Packages/org.janelia.background/Assets/Models/cylinder_4096_upY_flipped.obj";
                GameObject master = AssetDatabase.LoadAssetAtPath(modelPath, typeof(GameObject)) as GameObject;
                if (master)
                {
                    const string cylinderName = "BackgroundCylinder";
                    GameObject cylinder = GameObject.Find(cylinderName);
                    if (cylinder != null)
                    {
                        DestroyImmediate(cylinder);
                    }

                    cylinder = Instantiate(master);
                    cylinder.name = cylinderName;

                    if (AddMaterial(cylinder))
                    {
                        AddConstraint(cylinder, subject);
                    }
                }
            }
        }

        private bool AddMaterial(GameObject cylinder)
        {
            string shaderName = "Unlit/Texture";
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError("org.janelia.background: Cannot find shader '" + shaderName + "'");
                return false;
            }

            // In order for the shader to be built into a standalone executable, it must be used in a material
            // that is stored as a resource asset.

            Material material = new Material(shader)
            {
                name = CylinderBackgroundResources.MaterialName
            };
            if (material == null)
            {
                Debug.LogError("org.janelia.background: Cannot make material for shader '" + shaderName + "'");
                return false;
            }

            if (!AssetDatabase.IsValidFolder(CylinderBackgroundResources.AssetDatabaseFolder))
            {
                Debug.LogError("org.janelia.background: No resource folder for shader '" + shaderName + "'");
                return false;
            }

            AssetDatabase.CreateAsset(material, CylinderBackgroundResources.MaterialCreationName);

            const string texturePath = "Packages/org.janelia.background/Assets/Textures/default.png";
            Texture2D texture = AssetDatabase.LoadAssetAtPath(texturePath, typeof(Texture2D)) as Texture2D;
            if (texture == null)
            {
                Debug.LogError("org.janelia.background: Cannot load texture '" + texturePath + "'");
                return false;
            }

            material.SetTexture("_MainTex", texture);
            Transform childTransform = cylinder.transform.GetChild(0);
            if (childTransform == null)
            {
                Debug.LogError("org.janelia.background: Cannot find expected cylinder child");
                return false;
            }

            GameObject child = childTransform.gameObject;
            MeshRenderer renderer = child.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                Debug.LogError("org.janelia.background: Cannot find cylinder MeshRenderer");
                return false;
            }

            renderer.material = material;

            // Negate the `x` scale to flip the texture, since it will be on the inside
            // of the cyclinder.
            childTransform.localScale = new Vector3(-_radius, _height, _radius);

            return true;
        }

        private void AddConstraint(GameObject cylinder, GameObject subject)
        {
            if (subject != null)
            {
                cylinder.transform.position = subject.transform.position;
                PositionConstraint constraint = cylinder.AddComponent<PositionConstraint>();
                ConstraintSource source = new ConstraintSource
                {
                    sourceTransform = subject.transform,
                    weight = 1
                };
                constraint.AddSource(source);
                constraint.constraintActive = true;
            }
        }

        private static void InitializeResourceFolders()
        {
            if (!AssetDatabase.IsValidFolder(CylinderBackgroundResources.AssetDatabaseFolder))
            {
                AssetDatabase.CreateFolder(CylinderBackgroundResources.RequiredParentFolder,
                    CylinderBackgroundResources.RequiredResourceFolder);
            }

            if (!AssetDatabase.IsValidFolder(CylinderBackgroundResources.EditorAssetDatabaseFolder))
            {
                AssetDatabase.CreateFolder(CylinderBackgroundResources.AssetDatabaseFolder,
                    CylinderBackgroundResources.RequiredEditorFolder);
            }
        }

        private void Save()
        {
            _saved.subjectPath = _subjectPath;
            _saved.radius = _radius;
            _saved.height = _height;
            AssetDatabase.Refresh();
            EditorUtility.SetDirty(_saved);
            AssetDatabase.SaveAssets();
        }

        private void Load()
        {
            _saved = Resources.Load<SetupCylinderBackgroundSaved>(CylinderBackgroundResources.WindowStateLoadingName);
            if (_saved != null)
            {
                _subjectPath = _saved.subjectPath;
                _radius = _saved.radius;
                _height = _saved.height;
            }
            else
            {
                _saved = CreateInstance<SetupCylinderBackgroundSaved>();
                AssetDatabase.CreateAsset(_saved, CylinderBackgroundResources.WindowStateCreationName);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private string PathName(GameObject o)
        {
            string path = o.name;
            while (o.transform.parent != null)
            {
                o = o.transform.parent.gameObject;
                path = o.name + "/" + path;
            }
            return path;
        }

        private string _subjectPath;
        private float _radius = 1.0f;
        private float _height = 0.6f;

        private SetupCylinderBackgroundSaved _saved;
    }
}
