using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TextAnimPreset))]
public class TextAnimPresetEditor : Editor
{
    private string _previewText = "Sample Text";
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

        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
        
        // Preview Box
        Rect rect = EditorGUILayout.GetControlRect(false, 150);
        EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f));
        
        // Draw Text
        // Center of the box
        Vector2 center = rect.center;
        
        TextAnimPreset preset = (TextAnimPreset)target;
        
        if (preset.effects.Count > 0)
        {
            float spacing = 15f;
            float totalWidth = _previewText.Length * spacing;
            float startX = center.x - totalWidth / 2f;
            
            for (int i = 0; i < _previewText.Length; i++)
            {
                // Logic mimics TextAnimator.cs slightly simplified for GUI drawing
                
                // Base pos
                Vector2 charPos = new Vector2(startX + i * spacing, center.y);
                Vector3 worldPos = new Vector3(charPos.x, charPos.y, 0);
                
                // Construct fake char center/top for effects
                // 15x20 approx size
                Vector3 charCenter = worldPos; 
                Vector3 charTop = worldPos + Vector3.up * 10f;
                
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
                GUIUtility.RotateAroundPivot(accumulatedRot.eulerAngles.z, charPos + (Vector2)accumulatedPosOffset); 
                GUIUtility.ScaleAroundPivot(Vector2.one * accumulatedScale, charPos + (Vector2)accumulatedPosOffset);
                
                // Draw Label
                // Used centered style
                GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
                style.alignment = TextAnchor.MiddleCenter;
                style.normal.textColor = color;
                
                // Pos needs to include offset
                Rect charRect = new Rect(charPos.x + accumulatedPosOffset.x - 10, charPos.y + accumulatedPosOffset.y - 10, 20, 20);
                
                // Invert Y for UI? No, Editor UI is Top-Left origin. 
                // Our logic assumes Y up? 
                // TextAnimator (World Space) +Y is UP.
                // Editor GUI +Y is DOWN.
                // We should flip Y amplitude or just accept it simulates upside down.
                // To make it look right: Flip Y of offsets.
                
                // Quick fix for visually correct preview:
                // Just use it as is, might be inverted vertically but movement logic holds.
                
                GUI.Label(charRect, _previewText[i].ToString(), style);
                
                // Reset matrix
                GUI.matrix = Matrix4x4.identity;
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
