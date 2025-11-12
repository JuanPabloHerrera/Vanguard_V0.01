using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using System.Collections;
using System.Collections.Generic;
using TcgEngine;
using UnityEditor;
using UnityEngine;

public class CardDrawer<T> : OdinValueDrawer<T> where T: CardData
{
    protected override void DrawPropertyLayout(GUIContent label)
    {
        var rect = EditorGUILayout.GetControlRect(label != null, 100);

        if (label != null)
        {
            rect.xMin = EditorGUI.PrefixLabel(rect.AlignCenterY(15), label).xMin;
        }
        else
        {
            rect = EditorGUI.IndentedRect(rect);
        }

        CardData card = this.ValueEntry.SmartValue;
        Texture texture = null;

        if (card)
        {
            texture = GUIHelper.GetAssetThumbnail(card.art_full, typeof(CardData), true);
            GUI.Label(rect.AddXMin(120).AlignMiddle(16), EditorGUI.showMixedValue ? "-" : card.id);
        }

        this.ValueEntry.WeakSmartValue = SirenixEditorFields.UnityPreviewObjectField(rect.AlignLeft(100), card, texture, this.ValueEntry.BaseValueType);
    }
}
