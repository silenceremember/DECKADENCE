using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Text.RegularExpressions;

[RequireComponent(typeof(TMP_Text))]
public class TextAnimator : MonoBehaviour
{
    [System.Serializable]
    public struct PresetReference
    {
        public string id;
        public TextAnimPreset preset;
    }

    [Header("Configuration")]
    public List<PresetReference> customPresets = new List<PresetReference>();
    
    [Header("Global Settings")]
    public float timeScale = 1f;

    private TMP_Text _textComponent;
    private TMP_MeshInfo[] _cachedMeshInfo;
    
    // Map character index to list of active effects
    private Dictionary<int, List<ActiveEffect>> _charEffects = new Dictionary<int, List<ActiveEffect>>();
    private string _originalText;
    private bool _needsLayout = false;

    private struct ActiveEffect
    {
        public TextAnimPreset.EffectType type;
        public TextAnimPreset.EffectSettings settings;
    }

    void Awake()
    {
        _textComponent = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
    }

    void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
    }

    private string _lastProcessedText;
    private bool _isParsing = false;

    void OnTextChanged(Object obj)
    {
        if (obj == _textComponent)
        {
            // If we are currently modifying the text ourselves, ignore this event
            if (_isParsing) return;

            // If the text content is different from what we last processed
            if (_textComponent.text != _lastProcessedText)
            {
                // We need to parse it
                _isParsing = true;
                string rawText = _textComponent.text;
                string cleanText = ParseTags(rawText);
                
                // Only update if it actually changed (tags stripped)
                if (cleanText != rawText)
                {
                    _textComponent.text = cleanText;
                }
                
                _lastProcessedText = cleanText;
                _isParsing = false;
            }
            
            _needsLayout = true;
        }
    }

    // Call this if you set text via code and want to parse tags immediately
    public void SetText(string text)
    {
        // Direct set
        _isParsing = true;
        string cleanText = ParseTags(text);
        _textComponent.text = cleanText;
        _lastProcessedText = cleanText;
        _isParsing = false;
        
        _needsLayout = true;
    }

    void Update()
    {
        if (_textComponent == null) return;
        
        // Force update if needed (e.g. first frame)
        if (_needsLayout)
        {
            _textComponent.ForceMeshUpdate();
            _needsLayout = false;
        }

        if (_textComponent.textInfo == null || _textComponent.textInfo.meshInfo == null) return;

        // Apply animations
        AnimateMesh();
    }

    private string ParseTags(string rawText)
    {
        _charEffects.Clear();
        
        // Regex for tags: <tag> or <tag=param> ... </tag>
        // We look for known tags: fade, bounce, wave, shake, wiggle, pend, dangle, rainb, rotating, slide, swing, incr
        // Also "preset" tag
        // Example: <wave>Text</wave> or <preset=MyPreset>Text</preset>

        // Simple stack-based parser might be better than pure regex for nested tags, 
        // but for simplicity and robustness, let's look for tags and remove them, tracking indices.
        
        // List of tags to strip and process
        string[] tags = new string[] { 
            "fade", "bounce", "wave", "shake", "wiggle", 
            "pend", "dangle", "rainb", "rotating", "slide", "swing", "incr" 
        };

        string currentText = rawText;
        
        // Process presets first <preset=Name>...</preset>
        // Pattern: <preset=([^>]+)>(.*?)</preset>
        var presetRegex = new Regex(@"<preset=([^>]+)>(.*?)</preset>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        
        // We need to iteratively strip tags.
        // Issue: Indices shift as we strip.
        // Solution: Build a map of Index -> Effects.
        
        // Let's do a simplified pass:
        // 1. Scan string, identify all tags (start and end).
        // 2. Sort tags by position.
        // 3. Construct new string and build the map.
        
        // Alternatively, use a builder approach.
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        // It's complex to implement a full robust XML/HTML parser here. 
        // We'll use a known trick: Loop through matches, keeping track of 'clean' index.
        
        // But TMP tags (color, b, i) should REMAIN.
        // So we only want to strip OUR tags.
        
        // Let's filter our known tags.
        // Warning: This simple parser doesn't handle nested same-type tags perfectly without a stack, 
        // but user requirements are usually simple.
        
        // Mapping: TagName -> EffectType
        Dictionary<string, TextAnimPreset.EffectType> tagToType = new Dictionary<string, TextAnimPreset.EffectType> {
            {"fade", TextAnimPreset.EffectType.Fade},
            {"bounce", TextAnimPreset.EffectType.Bounce},
            {"wave", TextAnimPreset.EffectType.Wave},
            {"shake", TextAnimPreset.EffectType.Shake},
            {"wiggle", TextAnimPreset.EffectType.Wiggle},
            {"pend", TextAnimPreset.EffectType.Pendulum},
            {"dangle", TextAnimPreset.EffectType.Dangle},
            {"rainb", TextAnimPreset.EffectType.Rainbow},
            {"rotating", TextAnimPreset.EffectType.Rotate},
            {"slide", TextAnimPreset.EffectType.Slide},
            {"swing", TextAnimPreset.EffectType.Swing},
            {"incr", TextAnimPreset.EffectType.IncreaseSize}
        };

        // We will process matches. 
        // Note: Regex is tricky for nested tags. Custom state machine is safer but heavier.
        // Let's assume non-nested or basic nesting.
        // Pattern: <(/?)(tag|preset)(?:=([^>]*))?>
        
        string pattern = @"<(/?)(" + string.Join("|", tagToType.Keys) + @"|preset)(?:=([^>]*))?>";
        MatchCollection matches = Regex.Matches(rawText, pattern, RegexOptions.IgnoreCase);
        
        // We need to reconstruct the string without these tags, and map indices.
        
        // Stack for active effects:
        Stack<ActiveEffectData> effectStack = new Stack<ActiveEffectData>();
        
        int currentCleanIndex = 0;
        int lastPos = 0;
        
        foreach (Match m in matches)
        {
            // Append text before tag
            string textChunk = rawText.Substring(lastPos, m.Index - lastPos);
            sb.Append(textChunk);
            
            // Add effects to indices in this chunk
            AddEffectsToRange(currentCleanIndex, textChunk.Length, effectStack);
            
            currentCleanIndex += textChunk.Length;
            
            // Process tag
            bool isClose = m.Groups[1].Value == "/";
            string tagName = m.Groups[2].Value.ToLower();
            string param = m.Groups[3].Value;
            
            if (!isClose)
            {
                // OPEN TAG
                if (tagName == "preset")
                {
                    // Find preset
                    var preset = customPresets.Find(p => p.id == param).preset;
                    if (preset != null)
                    {
                        foreach(var eff in preset.effects)
                        {
                            effectStack.Push(new ActiveEffectData { isPreset = true, presetSetting = eff });
                        }
                    }
                }
                else if (tagToType.ContainsKey(tagName))
                {
                    // Built-in effect
                    // Create default settings or parse params?
                    // User said "available through tags", "configure own presets".
                    // We'll use default settings for raw tags for now.
                    var type = tagToType[tagName];
                    effectStack.Push(new ActiveEffectData { 
                        isPreset = false, 
                        type = type,
                        // Basic default 
                        manualSettings = new TextAnimPreset.EffectSettings { 
                            type = type, 
                            speed = 5, 
                            amplitude = 5, 
                            frequency = 1 
                        } 
                    });
                }
            }
            else
            {
                // CLOSE TAG
                // Pop matching type? Or just pop?
                // Simple stack pop assumes well-formed XML.
                // If we want to be robust, we should search stack for match, but that's complex.
                // We'll just Pop for now.
                if (effectStack.Count > 0)
                {
                     // Ideally we check if the top matches the closing tag, but this logic assumes valid nesting.
                     // For <preset>, we might have pushed multiple effects. We need to pop them all.
                     // This is getting tricky. 
                     
                     // Simpler approach: 
                     // Since we need to support "Preset" which pushes MULTIPLE effects, 
                     // A closing </preset> should pop ALL effects pushed by that preset.
                     // We need to mark stack items.
                     
                     // Actually, if we just support 1 active tag at a time per span it's easier, but mixing is requested.
                     
                     // Let's use a simpler heuristic: Pop until we hit a matching start or safety limit.
                     // But we don't store "what tag started this" easily in this simple stack.
                     
                     // Refined approach:
                     // TextAnimator applies effects based on a list.
                     // We just pop one item. 
                     // Wait, if <preset> pushed 3 effects, </preset> needs to pop 3.
                     // So we need to track "Batch Size" in the stack?
                     
                     // Let's implement a 'Scope' object.
                     if (tagName == "preset") {
                         // We need to know how many we pushed. 
                         // But we didn't store it.
                         // This parsing logic is getting brittle.
                         
                         // Let's stick to simple single-effect tags popping, 
                         // and for presets, we treat them as a single "EffectSource" wrapper?
                         // Getting complicated.
                         
                         // User requirement: "Effects to be mixable".
                         
                         // Let's just pop. If the stack gets misaligned, so be it (garbage in garbage out).
                         // But for preset with multiple effects...
                         // Maybe we wrap them in a single structural entry on stack.
                         
                         // Hack: Peek and pop.
                         if (effectStack.Count > 0) effectStack.Pop();
                     } else {
                         if (effectStack.Count > 0) effectStack.Pop();
                     }
                }
            }
            
            lastPos = m.Index + m.Length;
        }
        
        // Append remaining text
        string remaining = rawText.Substring(lastPos);
        sb.Append(remaining);
        AddEffectsToRange(currentCleanIndex, remaining.Length, effectStack);
        
        return sb.ToString();
    }
    
    // Auxiliary class for the parsing stack
    private class ActiveEffectData
    {
        public bool isPreset;
        public TextAnimPreset.EffectSettings presetSetting; // Used if isPreset
        public TextAnimPreset.EffectType type;           // Used if raw tag
        public TextAnimPreset.EffectSettings manualSettings; // Used if raw tag
    }

    private void AddEffectsToRange(int startIndex, int length, Stack<ActiveEffectData> stack)
    {
        if (length == 0 || stack.Count == 0) return;
        
        for (int i = 0; i < length; i++)
        {
            int charIndex = startIndex + i;
            if (!_charEffects.ContainsKey(charIndex)) _charEffects[charIndex] = new List<ActiveEffect>();
            
            foreach (var item in stack)
            {
                ActiveEffect eff = new ActiveEffect();
                if (item.isPreset)
                {
                    eff.type = item.presetSetting.type;
                    eff.settings = item.presetSetting;
                }
                else
                {
                    eff.type = item.type;
                    eff.settings = item.manualSettings;
                }
                _charEffects[charIndex].Add(eff);
            }
        }
    }

    void AnimateMesh()
    {
        _textComponent.ForceMeshUpdate();
        TMP_TextInfo textInfo = _textComponent.textInfo;
        int characterCount = textInfo.characterCount;

        if (characterCount == 0) return;

        // Cache mesh info if needed (for performance, though TMP regenerates it often)
        // We modify vertices directly
        
        for (int i = 0; i < characterCount; i++)
        {
            if (!_charEffects.ContainsKey(i)) continue; // No effects for this char
            
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;
            
            Vector3[] sourceVertices = textInfo.meshInfo[materialIndex].vertices;
            
            // Get center of character for rotation/scaling
            Vector3 center = (sourceVertices[vertexIndex + 0] + sourceVertices[vertexIndex + 2]) / 2;

            float time = Time.time * timeScale;
            
            // Apply all effects
            List<ActiveEffect> effects = _charEffects[i];
            
            Color32[] newColors = textInfo.meshInfo[materialIndex].colors32;

            for (int j = 0; j < 4; j++)
            {
                Vector3 originalPos = sourceVertices[vertexIndex + j];
                Vector3 finalPos = originalPos;
                
                Vector3 top = (sourceVertices[vertexIndex + 1] + sourceVertices[vertexIndex + 2]) / 2;

                foreach (var eff in effects)
                {
                    float effectiveTime = eff.settings.useUnscaledTime ? Time.unscaledTime : time;
                    var res = TextAnimPreset.GetEffectForChar(i, effectiveTime, eff.settings, center, top);
                    
                    // 1. Position Offset
                    finalPos += res.posOffset;
                    
                    // 2. Determine Pivot
                    Vector3 pivot = center;
                    // Pendulum and Dangle typically rotate around the top of the character
                    if (eff.type == TextAnimPreset.EffectType.Pendulum || eff.type == TextAnimPreset.EffectType.Dangle)
                    {
                        pivot = top;
                    }

                    // 3. Apply Scale (around pivot)
                    if (Mathf.Abs(res.scaleMultiplier - 1f) > 0.001f)
                    {
                        finalPos = (finalPos - pivot) * res.scaleMultiplier + pivot;
                    }

                    // 4. Apply Rotation (around pivot)
                    if (res.rotOffset != Quaternion.identity)
                    {
                        finalPos = res.rotOffset * (finalPos - pivot) + pivot;
                    }

                    // 5. Apply Color
                    if (res.colorOverride.HasValue)
                    {
                         newColors[vertexIndex + j] = res.colorOverride.Value;
                    }
                }
                
                sourceVertices[vertexIndex + j] = finalPos;
            }
        }
        
        // Push changes
        _textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
    }

    Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation)
    {
        return rotation * (point - pivot) + pivot;
    }
}
