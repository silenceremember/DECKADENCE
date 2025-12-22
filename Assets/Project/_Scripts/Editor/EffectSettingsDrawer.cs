using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(TextAnimPreset.EffectSettings))]
public class EffectSettingsDrawer : PropertyDrawer
{
    private const float BUTTON_HEIGHT = 18f;
    private const float SPACING = 2f;
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight; // Foldout line
        
        if (property.isExpanded)
        {
            // Считаем высоту всех дочерних свойств
            SerializedProperty typeProp = property.FindPropertyRelative("type");
            SerializedProperty speedProp = property.FindPropertyRelative("speed");
            SerializedProperty amplitudeProp = property.FindPropertyRelative("amplitude");
            SerializedProperty frequencyProp = property.FindPropertyRelative("frequency");
            SerializedProperty noiseScaleProp = property.FindPropertyRelative("noiseScale");
            SerializedProperty useUnscaledTimeProp = property.FindPropertyRelative("useUnscaledTime");
            
            height += EditorGUI.GetPropertyHeight(typeProp) + EditorGUIUtility.standardVerticalSpacing;
            
            // Проверяем, нужна ли кнопка
            TextAnimPreset.EffectType effectType = (TextAnimPreset.EffectType)typeProp.enumValueIndex;
            var recommended = TextAnimPreset.GetRecommendedSettings(effectType);
            
            bool isDifferent = !Mathf.Approximately(speedProp.floatValue, recommended.speed) ||
                              !Mathf.Approximately(amplitudeProp.floatValue, recommended.amplitude) ||
                              !Mathf.Approximately(frequencyProp.floatValue, recommended.frequency) ||
                              !Mathf.Approximately(noiseScaleProp.floatValue, recommended.noiseScale);
            
            if (isDifferent)
            {
                height += BUTTON_HEIGHT + SPACING; // Кнопка только если настройки отличаются
            }
            
            height += EditorGUI.GetPropertyHeight(speedProp) + EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUI.GetPropertyHeight(amplitudeProp) + EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUI.GetPropertyHeight(frequencyProp) + EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUI.GetPropertyHeight(noiseScaleProp) + EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUI.GetPropertyHeight(useUnscaledTimeProp) + EditorGUIUtility.standardVerticalSpacing;
        }
        
        return height;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        // Foldout
        Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);
        
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            
            float y = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            // Получаем свойства
            SerializedProperty typeProp = property.FindPropertyRelative("type");
            SerializedProperty speedProp = property.FindPropertyRelative("speed");
            SerializedProperty amplitudeProp = property.FindPropertyRelative("amplitude");
            SerializedProperty frequencyProp = property.FindPropertyRelative("frequency");
            SerializedProperty noiseScaleProp = property.FindPropertyRelative("noiseScale");
            SerializedProperty useUnscaledTimeProp = property.FindPropertyRelative("useUnscaledTime");
            
            // Type
            float typeHeight = EditorGUI.GetPropertyHeight(typeProp);
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, typeHeight), typeProp);
            y += typeHeight + EditorGUIUtility.standardVerticalSpacing;
            
            // Проверяем, отличаются ли текущие настройки от рекомендуемых
            TextAnimPreset.EffectType effectType = (TextAnimPreset.EffectType)typeProp.enumValueIndex;
            var recommended = TextAnimPreset.GetRecommendedSettings(effectType);
            
            bool isDifferent = !Mathf.Approximately(speedProp.floatValue, recommended.speed) ||
                              !Mathf.Approximately(amplitudeProp.floatValue, recommended.amplitude) ||
                              !Mathf.Approximately(frequencyProp.floatValue, recommended.frequency) ||
                              !Mathf.Approximately(noiseScaleProp.floatValue, recommended.noiseScale);
            
            // Кнопка "Recommended" показывается только если настройки отличаются
            if (isDifferent)
            {
                float indent = EditorGUI.indentLevel * 15f;
                Rect buttonRect = new Rect(position.x + indent, y, position.width - indent, BUTTON_HEIGHT);
                
                if (GUI.Button(buttonRect, "↻ Apply Recommended Settings"))
                {
                    speedProp.floatValue = recommended.speed;
                    amplitudeProp.floatValue = recommended.amplitude;
                    frequencyProp.floatValue = recommended.frequency;
                    noiseScaleProp.floatValue = recommended.noiseScale;
                    
                    property.serializedObject.ApplyModifiedProperties();
                }
                y += BUTTON_HEIGHT + SPACING;
            }
            
            // Speed
            float speedHeight = EditorGUI.GetPropertyHeight(speedProp);
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, speedHeight), speedProp);
            y += speedHeight + EditorGUIUtility.standardVerticalSpacing;
            
            // Amplitude
            float amplitudeHeight = EditorGUI.GetPropertyHeight(amplitudeProp);
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, amplitudeHeight), amplitudeProp);
            y += amplitudeHeight + EditorGUIUtility.standardVerticalSpacing;
            
            // Frequency
            float frequencyHeight = EditorGUI.GetPropertyHeight(frequencyProp);
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, frequencyHeight), frequencyProp);
            y += frequencyHeight + EditorGUIUtility.standardVerticalSpacing;
            
            // Noise Scale
            float noiseHeight = EditorGUI.GetPropertyHeight(noiseScaleProp);
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, noiseHeight), noiseScaleProp);
            y += noiseHeight + EditorGUIUtility.standardVerticalSpacing;
            
            // Use Unscaled Time
            float unscaledHeight = EditorGUI.GetPropertyHeight(useUnscaledTimeProp);
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, unscaledHeight), useUnscaledTimeProp);
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUI.EndProperty();
    }
}
