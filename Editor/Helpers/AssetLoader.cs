using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GraphToolsFSM.Editor.Helpers {
    public static class AssetLoader
    {
        public static StyleSheet LoadStyleSheetByName(string fileName)
        {
            var guids = AssetDatabase.FindAssets($"{fileName} t:StyleSheet");
            if (guids.Length == 0) return null;

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
        }
        
        public static Texture2D LoadIcon(string iconName) {
            var guids = AssetDatabase.FindAssets($"{iconName} t:Texture2D");
            if (guids.Length == 0) return null;
    
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}