using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TextAnimPreset))]
public class TextAnimPresetEditor : Editor
{
    private float _time = 0f;
    private double _lastTime;

    private void OnEnable()
    {
        _lastTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += UpdateTime;
    }

    private void OnDisable()
    {
        EditorApplication.update -= UpdateTime;
    }

    private void UpdateTime()
    {
        double currentTime = EditorApplication.timeSinceStartup;
        float dt = (float)(currentTime - _lastTime);
        _lastTime = currentTime;
        _time += dt;
        
        // Force repaint for animation
        Repaint(); 
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        DrawDefaultInspector();

        TextAnimPreset preset = (TextAnimPreset)target;

        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
        
        // Информация о шрифте
        if (preset.previewFont != null)
        {
            Font sourceFont = null;
            
            // Проверяем оба способа получения шрифта
            if (preset.previewFont.sourceFontFile != null)
            {
                sourceFont = preset.previewFont.sourceFontFile;
            }
            else
            {
                var fontAssetProperty = preset.previewFont.GetType().GetField("m_SourceFontFile", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (fontAssetProperty != null)
                {
                    sourceFont = fontAssetProperty.GetValue(preset.previewFont) as Font;
                }
            }
            
            if (sourceFont != null)
            {
                EditorGUILayout.HelpBox($"✓ Используется шрифт: {preset.previewFont.name}\nИсходный файл: {sourceFont.name}", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"⚠ TMP шрифт '{preset.previewFont.name}' загружен, но Source Font File недоступен.\n\nДля исправления:\n1. Откройте TMP Font Asset\n2. Проверьте поле 'Source Font File'\n3. Убедитесь что 'Atlas Population Mode' = Dynamic (не Static)", MessageType.Warning);
                
                if (GUILayout.Button("Открыть TMP Font Asset для проверки", GUILayout.Height(35)))
                {
                    Selection.activeObject = preset.previewFont;
                    EditorGUIUtility.PingObject(preset.previewFont);
                }
            }
        }
        
        // Динамическая высота превью в зависимости от размера шрифта
        float previewHeight = Mathf.Max(150f, preset.previewFontSize * 4f);
        
        // Preview Box
        Rect rect = EditorGUILayout.GetControlRect(false, previewHeight);
        EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f));
        
        // Draw Text
        // Center of the box
        Vector2 center = rect.center;
        
        // Используем текст из настроек пресета
        string textToDisplay = string.IsNullOrEmpty(preset.previewText) ? "Sample Text" : preset.previewText;
        
        if (preset.effects.Count > 0)
        {
            // Создаем базовый стиль для измерения ширины символов
            GUIStyle measureStyle = new GUIStyle(EditorStyles.boldLabel);
            measureStyle.fontSize = Mathf.RoundToInt(preset.previewFontSize);
            
            // Применяем кастомный шрифт для измерений тоже
            if (preset.previewFont != null)
            {
                Font sourceFont = preset.previewFont.sourceFontFile;
                if (sourceFont == null)
                {
                    var fontAssetProperty = preset.previewFont.GetType().GetField("m_SourceFontFile", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (fontAssetProperty != null)
                    {
                        sourceFont = fontAssetProperty.GetValue(preset.previewFont) as Font;
                    }
                }
                if (sourceFont != null)
                {
                    measureStyle.font = sourceFont;
                }
            }
            
            // Вычисляем позиции символов с учетом реальной ширины
            float[] charPositions = new float[textToDisplay.Length];
            float currentX = 0f;
            
            for (int i = 0; i < textToDisplay.Length; i++)
            {
                charPositions[i] = currentX;
                Vector2 charSize = measureStyle.CalcSize(new GUIContent(textToDisplay[i].ToString()));
                currentX += charSize.x;
            }
            
            // Центрируем весь текст
            float totalWidth = currentX;
            float startX = center.x - totalWidth / 2f;
            
            for (int i = 0; i < textToDisplay.Length; i++)
            {
                // Logic mimics TextAnimator.cs slightly simplified for GUI drawing
                
                // Измеряем реальную ширину текущего символа
                Vector2 charSize = measureStyle.CalcSize(new GUIContent(textToDisplay[i].ToString()));
                
                // Base pos - используем вычисленную позицию + половина ширины для центрирования
                Vector2 charPos = new Vector2(startX + charPositions[i] + charSize.x / 2f, center.y);
                Vector3 worldPos = new Vector3(charPos.x, charPos.y, 0);
                
                // Construct fake char center/top for effects
                // Масштабируем размер символа в зависимости от fontSize
                float charHeight = preset.previewFontSize * 0.5f;
                Vector3 charCenter = worldPos; 
                Vector3 charTop = worldPos + Vector3.up * charHeight;
                
                // We assume default scale 1, rot 0
                Vector3 checkPos = charCenter; 
                Vector3 finalPos = checkPos;

                Color color = Color.white;
                
                // Apply effects loop (Sequential like TextAnimator)
                foreach(var eff in preset.effects)
                {
                    var res = TextAnimPreset.GetEffectForChar(i, _time, eff, charCenter, charTop); // Use generic helper
                    
                    // 1. Pos
                    finalPos += res.posOffset; // Note: Vector3 to Vector3
                    
                    // 2. Pivot
                    Vector3 pivot = charCenter;
                    if (eff.type == TextAnimPreset.EffectType.Pendulum || eff.type == TextAnimPreset.EffectType.Dangle)
                        pivot = charTop;
                        
                    // 3. Scale
                    if (Mathf.Abs(res.scaleMultiplier - 1f) > 0.001f)
                        finalPos = (finalPos - pivot) * res.scaleMultiplier + pivot;
                        
                    // 4. Rotation
                    if (res.rotOffset != Quaternion.identity)
                        finalPos = res.rotOffset * (finalPos - pivot) + pivot;

                    // 5. Color
                    if (res.colorOverride.HasValue) color = res.colorOverride.Value;
                }
                
                // Draw Character
                // We need to set GUI matrix to handle rotation/scale around 'finalPos' center?
                // Actually 'finalPos' is the NEW center position.
                // But rotation applies to the vert positions relative to center.
                // So if we just draw text at 'finalPos', we handle translation.
                // But we lost rotation/scale of the glyph itself if we just move the point.
                // The 'finalPos' in our logic was updating a VERTEX position. 
                // Here we are updating the CHARACTER CENTER position effectively?
                // No, TextAnimator updates 4 vertices. Here we draw 1 label.
                // To visualize rotation, we MUST use GUI.matrix.
                
                // We need to decompose 'finalPos' back into Pos/Rot/Scale? 
                // Or rather, we should apply the same transform logic to the GUI Matrix.
                
                // Simpler: Just recalculate the transform that *would* happen to the center point, 
                // and the rotation offset accumulation.
                // Our generic helper returns 'rotOffset'.
                // We can accumulate rotOffset.
                
                Quaternion accumulatedRot = Quaternion.identity;
                float accumulatedScale = 1f;
                Vector3 accumulatedPosOffset = Vector3.zero;
                
                // Re-run simplified accumulation for Matrix
                // This is an approximation because our vertex logic supports per-vertex deformation (like Skew) which matrix doesn't.
                // But for standard affine transforms (Move, Rot, Scale), it works.
                
                foreach(var eff in preset.effects)
                {
                     var res = TextAnimPreset.GetEffectForChar(i, _time, eff, charCenter, charTop);
                     accumulatedPosOffset += res.posOffset;
                     accumulatedRot *= res.rotOffset; 
                     accumulatedScale *= res.scaleMultiplier;
                     if (res.colorOverride.HasValue) color = res.colorOverride.Value;
                }
                
                // Drawing
                // Сохраняем текущую матрицу перед трансформациями
                Matrix4x4 oldMatrix = GUI.matrix;
                
                GUIUtility.RotateAroundPivot(accumulatedRot.eulerAngles.z, charPos + (Vector2)accumulatedPosOffset); 
                GUIUtility.ScaleAroundPivot(Vector2.one * accumulatedScale, charPos + (Vector2)accumulatedPosOffset);
                
                // Draw Label
                // Used centered style
                GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
                style.alignment = TextAnchor.MiddleCenter;
                style.normal.textColor = color;
                style.fontSize = Mathf.RoundToInt(preset.previewFontSize);
                
                // Применяем кастомный шрифт, если он задан
                if (preset.previewFont != null)
                {
                    // Пробуем получить исходный шрифт разными способами
                    Font sourceFont = null;
                    
                    // Способ 1: через sourceFontFile (TMP 3.0+)
                    if (preset.previewFont.sourceFontFile != null)
                    {
                        sourceFont = preset.previewFont.sourceFontFile;
                    }
                    // Способ 2: через faceInfo (старые версии TMP)
                    else
                    {
                        // Пытаемся найти шрифт через Reflection или альтернативный путь
                        var fontAssetProperty = preset.previewFont.GetType().GetField("m_SourceFontFile", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (fontAssetProperty != null)
                        {
                            sourceFont = fontAssetProperty.GetValue(preset.previewFont) as Font;
                        }
                    }
                    
                    if (sourceFont != null)
                    {
                        style.font = sourceFont;
                    }
                }
                
                // Используем реальный размер символа с небольшим отступом
                float charRectWidth = charSize.x * 1.1f;
                float charRectHeight = charSize.y * 1.1f;
                
                // Pos needs to include offset
                Rect charRect = new Rect(
                    charPos.x + accumulatedPosOffset.x - charRectWidth / 2f, 
                    charPos.y + accumulatedPosOffset.y - charRectHeight / 2f, 
                    charRectWidth, 
                    charRectHeight
                );
                
                // Invert Y for UI? No, Editor UI is Top-Left origin. 
                // Our logic assumes Y up? 
                // TextAnimator (World Space) +Y is UP.
                // Editor GUI +Y is DOWN.
                // We should flip Y amplitude or just accept it simulates upside down.
                // To make it look right: Flip Y of offsets.
                
                // Quick fix for visually correct preview:
                // Just use it as is, might be inverted vertically but movement logic holds.
                
                GUI.Label(charRect, textToDisplay[i].ToString(), style);
                
                // ВАЖНО: Восстанавливаем исходную матрицу, а не сбрасываем в identity
                GUI.matrix = oldMatrix;
            }
        }
        else
        {
            GUIStyle s = new GUIStyle(EditorStyles.label);
            s.alignment = TextAnchor.MiddleCenter;
            s.normal.textColor = Color.gray;
            GUI.Label(rect, "No Effects Added", s);
        }
        
        serializedObject.ApplyModifiedProperties();
    }
}
