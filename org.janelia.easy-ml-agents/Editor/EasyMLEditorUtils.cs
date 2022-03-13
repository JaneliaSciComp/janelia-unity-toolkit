using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Janelia
{
    /// <summary>
    /// Utilities used in the Editor assembly.
    /// </summary>
    public static class EasyMLEditorUtils
    {
        /// <summary>
        /// Gets the most-derived subclasses of the specified type.
        /// </summary>
        /// <param name="type">The base class type</param>
        /// <returns>An array of the most-derived subclass types</returns>
        public static Type[] GetFinalSubclasses(Type type)
        {
            IEnumerable<Type> finalsEnumerable =
                from sub in GetSubclasses(type)
                where GetSubclasses(sub).Length == 0
                select sub;
            return finalsEnumerable.ToArray();
        }

        /// <summary>
        /// Gets all classes that have the specified type as an ancestor.
        /// </summary>
        /// <param name="type">The base class type</param>
        /// <returns>An array of all the subclass types</returns>
        public static Type[] GetSubclasses(Type type)
        {
            IEnumerable<Type> subclassesEnumerable =
                from assembly in AppDomain.CurrentDomain.GetAssemblies()
                from assemblyType in assembly.GetTypes()
                where assemblyType.IsSubclassOf(type)
                select assemblyType;
            return subclassesEnumerable.ToArray();
        }
    }
}