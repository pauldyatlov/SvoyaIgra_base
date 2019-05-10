using System;
using JetBrains.Annotations;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class TextReplacerUtility
{
  [MenuItem("CONTEXT/Text/Change [Text] to [TextMeshPro]")]
  private static void ChangeToTmp()
  {
    var selectedObject = Selection.activeGameObject;
    var textComponent = selectedObject.GetComponent<Text>();

    ChangeToTextMeshPro(selectedObject, textComponent);
  }

  [CanBeNull] private static TextMeshProUGUI ChangeToTextMeshPro(GameObject selectedObject, Text textComponent)
  {
    var selectedTransform = selectedObject.transform as RectTransform;
    if (selectedTransform == null)
      return null;

    var offsetMin = selectedTransform.offsetMin;
    var offsetMax = selectedTransform.offsetMax;

    Object.DestroyImmediate(textComponent);

    var meshProComponent = selectedObject.AddComponent<TextMeshProUGUI>();
    var asset = Resources.Load<TMP_FontAsset>("UI/Fonts/Jovanny Lemonad - Bender SDF");

    var meshTransform = meshProComponent.transform as RectTransform;
    if (meshTransform == null)
      return null;

    meshProComponent.font = asset;
    meshProComponent.fontStyle = ConvertStyle(textComponent.fontStyle);
    meshProComponent.text = textComponent.text;
    meshProComponent.fontSize = textComponent.fontSize;
    meshProComponent.color = textComponent.color;
    meshProComponent.alignment = ConvertAlignment(textComponent.alignment);

    meshTransform.offsetMin = offsetMin;
    meshTransform.offsetMax = offsetMax;

    return meshProComponent;
  }

  private static FontStyles ConvertStyle(FontStyle style)
  {
    switch (style)
    {
      case FontStyle.Normal:
        return FontStyles.Normal;
      case FontStyle.Bold:
        return FontStyles.Bold;
      case FontStyle.Italic:
      case FontStyle.BoldAndItalic:
        return FontStyles.Italic;
      default:
        throw new ArgumentOutOfRangeException("style", style, null);
    }
  }

  private static TextAlignmentOptions ConvertAlignment(TextAnchor anchor)
  {
    switch (anchor)
    {
      case TextAnchor.UpperLeft:
        return TextAlignmentOptions.TopLeft;
      case TextAnchor.UpperCenter:
        return TextAlignmentOptions.Top;
      case TextAnchor.UpperRight:
        return TextAlignmentOptions.TopRight;
      case TextAnchor.MiddleLeft:
        return TextAlignmentOptions.MidlineLeft;
      case TextAnchor.MiddleCenter:
        return TextAlignmentOptions.Midline;
      case TextAnchor.MiddleRight:
        return TextAlignmentOptions.MidlineRight;
      case TextAnchor.LowerLeft:
        return TextAlignmentOptions.BottomLeft;
      case TextAnchor.LowerCenter:
        return TextAlignmentOptions.Bottom;
      case TextAnchor.LowerRight:
        return TextAlignmentOptions.BottomRight;
      default:
        throw new ArgumentOutOfRangeException("anchor", anchor, null);
    }
  }
}