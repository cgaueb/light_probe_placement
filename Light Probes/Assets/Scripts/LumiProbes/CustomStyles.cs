using UnityEditor;
using UnityEngine;

public class CustomStyles
{
    public static Color redErrorLabel;
    public static Color colorHeader;
    public static Color colorMainAction;
    public static Color colorSubAction;
    public static GUIStyle EditorErrorRed;
    public static GUIStyle EditorStylesHeader;
    public static GUIStyle EditorStylesMainAction;
    public static GUIStyle EditorStylesFoldoutMainAction;
    public static GUIStyle EditorStylesSubAction;
    public static GUILayoutOption[] defaultGUILayoutOption = new GUILayoutOption[] { GUILayout.ExpandWidth(false), GUILayout.MinWidth(50), GUILayout.MaxWidth(1500) };
    public static void setStyles() {
        float scale = 0.9f;
        redErrorLabel = new Color(0.95f, 0.25f, 0.0f);
        colorHeader = new Color(1.0f, 0.65f, 0.0f);
        colorMainAction = new Color(1.0f, 0.65f, 0.0f);
        colorSubAction = new Color(0.95f, 0.95f, 0.0f);

        EditorErrorRed = new GUIStyle(EditorStyles.whiteBoldLabel);
        EditorErrorRed.normal.textColor = new Color(redErrorLabel.r * scale, redErrorLabel.g * scale, redErrorLabel.b * scale);
        EditorStylesHeader = new GUIStyle(EditorStyles.boldLabel);
        EditorStylesHeader.normal.textColor = new Color(colorHeader.r * scale, colorHeader.g * scale, colorHeader.b * scale);
        EditorStylesMainAction = new GUIStyle(EditorStyles.boldLabel);
        EditorStylesMainAction.normal.textColor = new Color(colorMainAction.r * scale, colorMainAction.g * scale, colorMainAction.b * scale);
        EditorStylesSubAction = new GUIStyle(EditorStyles.boldLabel);
        EditorStylesSubAction.normal.textColor = new Color(colorSubAction.r * scale, colorSubAction.g * scale, colorSubAction.b * scale);
        EditorStylesFoldoutMainAction = new GUIStyle(EditorStyles.foldoutHeader);
        EditorStylesFoldoutMainAction.normal.textColor = new Color(colorMainAction.r * scale, colorMainAction.g * scale, colorMainAction.b * scale);
        EditorStylesFoldoutMainAction.active.textColor = new Color(colorMainAction.r * scale, colorMainAction.g * scale, colorMainAction.b * scale);
        EditorStylesFoldoutMainAction.hover.textColor = new Color(colorMainAction.r * scale, colorMainAction.g * scale, colorMainAction.b * scale);
        EditorStylesFoldoutMainAction.focused.textColor = new Color(colorMainAction.r * scale, colorMainAction.g * scale, colorMainAction.b * scale);
        EditorStylesFoldoutMainAction.onNormal.textColor = new Color(colorMainAction.r * scale, colorMainAction.g * scale, colorMainAction.b * scale);
        EditorStylesFoldoutMainAction.onActive.textColor = new Color(colorMainAction.r * scale, colorMainAction.g * scale, colorMainAction.b * scale);
        EditorStylesFoldoutMainAction.onHover.textColor = new Color(colorMainAction.r * scale, colorMainAction.g * scale, colorMainAction.b * scale);
        EditorStylesFoldoutMainAction.onFocused.textColor = new Color(colorMainAction.r * scale, colorMainAction.g * scale, colorMainAction.b * scale);
    }
    public static void addStyles() {
        System.Collections.Generic.List<GUIStyle> mystyles = new System.Collections.Generic.List<GUIStyle> {
            EditorStyles.numberField,
            EditorStyles.objectField,
            EditorStyles.objectFieldThumb,
            EditorStyles.objectFieldMiniThumb,
            EditorStyles.colorField,
            EditorStyles.layerMaskField,
            EditorStyles.toggle,
            EditorStyles.foldout,
            EditorStyles.foldoutPreDrop,
            EditorStyles.foldoutHeader,
            EditorStyles.foldoutHeaderIcon,
            EditorStyles.toggleGroup,
            EditorStyles.toolbar,
            EditorStyles.toolbarButton,
            EditorStyles.toolbarPopup,
            EditorStyles.toolbarDropDown,
            EditorStyles.toolbarTextField,
            EditorStyles.inspectorDefaultMargins,
            EditorStyles.inspectorFullWidthMargins,
            EditorStyles.popup,
            EditorStyles.toolbarSearchField,
            EditorStyles.miniTextField,
            EditorStyles.label,
            EditorStyles.miniLabel,
            EditorStyles.largeLabel,
            EditorStyles.boldLabel,
            EditorStyles.miniBoldLabel,
            EditorStyles.centeredGreyMiniLabel,
            EditorStyles.wordWrappedMiniLabel,
            EditorStyles.wordWrappedLabel,
            EditorStyles.linkLabel,
            EditorStyles.helpBox,
            EditorStyles.whiteLabel,
            EditorStyles.whiteLargeLabel,
            EditorStyles.whiteBoldLabel,
            EditorStyles.radioButton,
            EditorStyles.miniButton,
            EditorStyles.miniButtonLeft,
            EditorStyles.miniButtonMid,
            EditorStyles.miniButtonRight,
            EditorStyles.miniPullDown,
            EditorStyles.textField,
            EditorStyles.textArea,
            EditorStyles.whiteMiniLabel
    };

        foreach (GUIStyle s in mystyles) {
            addStyle(s);
        }
    }
    private static void addStyle(GUIStyle style) {
        GUIStyle s = new GUIStyle(style);
        EditorGUILayout.BeginVertical(style);
        GUILayout.Button(new GUIContent(style.ToString(), ""));
        EditorGUILayout.EndVertical();
    }
}