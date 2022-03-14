#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(LogRangeAttribute))]
class LogRangeDrawer : PropertyDrawer
{
	const float FLOAT_FIELD_WIDTH = 70.0f;
	const float SPACING = 2.0f;
	
	public override void OnGUI(Rect rect, SerializedProperty prop, GUIContent label)
	{
		if(prop.propertyType != SerializedPropertyType.Float) {
			GUIContent c = new GUIContent("Unsupported field type " + prop.type);
			EditorGUI.LabelField(rect, label, c);
			return;
		}

		float value = prop.floatValue;
		float logValue = Mathf.Log10(value);
		LogRangeAttribute attr = attribute as LogRangeAttribute;

		rect = EditorGUI.PrefixLabel(rect, label);
		rect.width -= (SPACING + FLOAT_FIELD_WIDTH) * EditorGUIUtility.pixelsPerPoint;

		int prevIndent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;
		EditorGUI.BeginChangeCheck();
		logValue = GUI.HorizontalSlider(rect, logValue, attr.min, attr.max);
		value = Mathf.Pow(10.0f, logValue);

		if(logValue < 0.0f) {
			float mult = Mathf.Pow(10.0f, Mathf.Ceil(-attr.min) + 1.0f);
			value = Mathf.Floor(value * mult) / mult;
		}

		rect.x += rect.width + SPACING * EditorGUIUtility.pixelsPerPoint;
		rect.width = FLOAT_FIELD_WIDTH * EditorGUIUtility.pixelsPerPoint;

		EditorGUI.DelayedFloatField(rect, value);
		EditorGUI.indentLevel = prevIndent;

		if(EditorGUI.EndChangeCheck())
			prop.floatValue = value;
	}
}

#endif
