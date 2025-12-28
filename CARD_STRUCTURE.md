# 🎴 Card Structure — DECKADENCE

## Architecture

Each card side = **1 main plane** with combined `CardShadow` shader:
- **Shadow** (shadow mesh via `CardShadow.cs`)
- **Tears** (tear effect)
- **Corner cuts** (corner cut)

## Performance
- **10 cards on screen** — OK for mobile
- With **atlasing + batching** — ~10-20 draw calls = excellent
- Single shader per card = minimal overdraw

---

## 🔙 CARD BACK (5 layers)

| # | Layer | GameObject | Description | Animation |
|---|-------|------------|-------------|-----------|
| 1 | **Background** | `CardBack_Background` | CardShadow shader (shadow + tears + corners) | Tears + corners change |
| 2 | **Frame** | `CardBack_Frame` | Decorative frame | Static / hover |
| 3 | **Lower Eyelid** | `CardBack_EyeLower` | Lower eyelid | Blink (scale Y) |
| 4 | **Upper Eyelid** | `CardBack_EyeUpper` | Upper eyelid | Blink (moves down) |
| 5 | **Pupil** | `CardBack_Pupil` | Eye center | Follows cursor |

### Eye Effects:
- 😐 **Idle** — pupil drifts slightly
- 👀 **Hover** — looks at cursor
- 😴 **Sleep** — eyelids close
- 😱 **Alert** — pupil shrinks
- 🔴 **Critical** — pulses

---

## 🎭 CARD FRONT (4 layers)

| # | Layer | GameObject | Description | Animation |
|---|-------|------------|-------------|-----------|
| 1 | **Background** | `CardFront_Background` | Solid color fill (white, can be tinted) | Color change |
| 2 | **Back Frame** | `CardFront_FrameBack` | Back frame (depth) | Static |
| 3 | **Character** | `CardFront_Character` | Character portrait | Idle animation |
| 4 | **Front Frame** | `CardFront_FrameFront` | Front frame (overlay) | Static |

---

## 📊 Total per Card

| Side | Layers |
|------|--------|
| Back | 5 |
| Front | 4 |
| **Total** | **9** |

### Optimizations:
1. Combine static layers into **Sprite Atlas**
2. Use **CanvasGroup** for group transparency
3. **Canvas** batching with single material

