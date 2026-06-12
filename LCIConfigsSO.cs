using UnityEngine;
using UnityEngine.UIElements;

namespace LumieComponentInspector
{
    [CreateAssetMenu(fileName = "LCIConfigs", menuName = "SO/LCIConfigsSO")]
    internal class LCIConfigsSO : ScriptableObject
    {
        public const string DefaultAssetPath = "LCIConfigs";

        public StyleSheet ComponentInspectorsStyleSheet;
        public StyleSheet ComponentToolbarStyleSheet;
        public StyleSheet ComponentHeaderStyleSheet;
    }
}