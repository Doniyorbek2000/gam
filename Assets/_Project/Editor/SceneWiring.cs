using UnityEditor;
using UnityEngine;

namespace BilliardGame.Editor
{
    /// <summary>Assigns private [SerializeField] references from editor code.</summary>
    internal static class SceneWiring
    {
        public static void Set(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null) throw new System.MissingFieldException(target.GetType().Name, field);
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
