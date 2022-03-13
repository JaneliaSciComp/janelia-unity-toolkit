using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Janelia
{
    /// <summary>
    /// Automatically gives the Unity editor a menu item for creating an
    /// agent derived from <see cref="EasyMLAgent"/> to be trained in an
    /// arena derived from <see cref="EasyMLArena"/>.  As long as there is
    /// only one such derived agent class and derived arena class, they will be
    /// found and handled automatically, and this code need not be edited.
    /// </summary>
    public static class EasyMLSetup 
    {
        /// <summary>
        /// A menu item for creating a new arena and agent, automatically picking types derived from
        /// <see cref="EasyMLArena"/> and <see cref="EasyMLAgent"/>.
        /// </summary>
        [MenuItem("GameObject/Create Easy ML Arena and Agent", false, 1)]
        static public void CreateEasyMLArena(MenuCommand _)
        {
            // Finds one class derived from `EasyMLArena` to be the class created.
            // Launches an error dialog if there is not exactly one such class.
            Type arenaType = ArenaTypeToCreate();
            if (arenaType != null)
            {
                // Finds one class derived from `EasyMLAgent` to be the class created.
                // Launches an error dialog if there is not exactly one such class.
                Type agentType = AgentTypeToCreate();
                if (agentType != null)
                {
                    Create(arenaType, agentType);
                }
            }
         }

        private static Type ArenaTypeToCreate()
        {
            Type[] arenaSubclasses = EasyMLEditorUtils.GetFinalSubclasses(typeof(EasyMLArena));

            if (arenaSubclasses.Length == 0)
            {
                EditorUtility.DisplayDialog("Cannot create ML arena", "There are no classes derived from Janelia.EasyMLArena", "OK");
                return null;
            }

            if (arenaSubclasses.Length > 1)
            {
                EditorUtility.DisplayDialog("Cannot create ML arena", "There are multiple classes derived from Janelia.EasyMLArena", "OK");
                return null;
            }

            return arenaSubclasses.First();
        }

        private static Type AgentTypeToCreate()
        {
            Type[] agentSubclasses = EasyMLEditorUtils.GetFinalSubclasses(typeof(EasyMLAgent));

            if (agentSubclasses.Length == 0)
            {
                EditorUtility.DisplayDialog("Cannot create ML arena", "There are no classes derived from Janelia.EasyMLAgent", "OK");
                return null;
            }

            if (agentSubclasses.Length > 1)
            {
                EditorUtility.DisplayDialog("Cannot create ML arena", "There are multiple classes derived from Janelia.EasyMLAgent", "OK");
                return null;
            }

            return agentSubclasses.First();
        }

        private static void Create(Type arenaType, Type agentType)
        {
            GameObject[] arenas = new GameObject[0];
            try
            {
                // Find existing arenas to update.
                arenas = GameObject.FindGameObjectsWithTag(EasyMLArena.TAG_ARENA);
            }
            catch (UnityException)
            {
                // The tag was not defined yet, so just proceed to create the first arena.
            }
            if (arenas.Length == 0)
            {
                // There are no existing arenas so prepare to create a new one.
                arenas = new GameObject[] { new GameObject() };
            }

            foreach (GameObject arenaObject in arenas)
            {
                EasyMLArena arena = arenaObject.GetComponent(arenaType) as EasyMLArena;
                if (arena == null)
                {
                    arena = arenaObject.AddComponent(arenaType) as EasyMLArena;
                }

                GameObject agentObject = EasyMLRuntimeUtils.FindChildWithTag(arenaObject, EasyMLAgent.TAG_AGENT);
                if (agentObject == null)
                {
                    // Create a new agent only if none exist already.
                    agentObject = new GameObject();
                }
                agentObject.transform.parent = arenaObject.transform;
                EasyMLAgent agent = agentObject.GetComponent(agentType) as EasyMLAgent;
                if (agent == null)
                {
                    agent = agentObject.AddComponent(agentType) as EasyMLAgent;
                }

                EasyMLSetupHelper helper = new EasyMLSetupHelper();
                arena.Setup(helper);
                agent.Setup(helper);

                arena.PlaceRandomly();

                Undo.RegisterCreatedObjectUndo(arenaObject, "Create " + arenaObject.name);
                Selection.activeObject = arenaObject;
            }
        }
    }
}
