using System.Collections.Generic;
using UnityEngine;

namespace Janelia
{
    /// <summary>
    /// Utilities used in the Runtime assembly.
    /// </summary>
    public static class EasyMLRuntimeUtils
    {
        /// <summary>
        /// Finds one descendant (child, grandchild, etc.) with the specified tag.
        /// </summary>
        /// <param name="parent">The parent object where the search starts</param>
        /// <param name="tag">The tag to search for</param>
        /// <returns>The child or null if no tagged descendant exists</returns>
        public static GameObject FindChildWithTag(GameObject parent, string tag)
        {
            List<GameObject> tagged = FindChildrenWithTag(parent, tag);
            return (tagged.Count > 0) ? tagged[0] : null;
        }

        /// <summary>
        /// Finds all descendants (children, grandchildren, etc.) with the specified tag.
        /// </summary>
        /// <param name="parent">The parent object where the search starts</param>
        /// <param name="tag">The tag to search for</param>
        /// <returns>The array of tagged descendants, empty if none are tagged</returns>
        public static List<GameObject> FindChildrenWithTag(GameObject parent, string tag)
        {
            GameObject[] tagged = GameObject.FindGameObjectsWithTag(tag);
            List<GameObject> result = new List<GameObject>();
            foreach (GameObject obj in tagged)
            {
                Transform current = obj.transform;
                while (current != null)
                {
                    if (current == parent.transform)
                    {
                        result.Add(obj);
                        break;
                    }
                    current = current.parent;
                }
            }
            return result;
        }
    }
}