// https://frarees.github.io/default-gist-license

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(MinMaxAttribute))]
class MinMaxSliderDrawer : PropertyDrawer
{
	const string kVectorMinName = "x";
	const string kVectorMaxName = "y";
	const float kFloatFieldWidth = 30f;
	const float kSpacing = 2f;
	const float kRoundingValue = 100f;

	float Round(float value, float roundingValue)
	{
		if (roundingValue == 0)
		{
			return value;
		}

		return Mathf.Round(value * roundingValue) / roundingValue;
	}

	bool SetVectorValue(SerializedProperty property, float min, float max)
	{
		if (property.propertyType == SerializedPropertyType.Vector2)
		{
			min = Round(min, kRoundingValue);
			max = Round(max, kRoundingValue);
			property.vector2Value = new Vector2(min, max);
		}
		else if (property.propertyType == SerializedPropertyType.Vector2Int)
		{
			property.vector2IntValue = new Vector2Int((int)min, (int)max);
		}
		else
		{
			return false;
		}

		return true;
	}

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		float min = 0f;
		float max = 0f;

		if (property.propertyType == SerializedPropertyType.Vector2)
		{
			var v = property.vector2Value;
			min = v.x;
			max = v.y;
		}
		else if (property.propertyType == SerializedPropertyType.Vector2Int)
		{
			var v = property.vector2IntValue;
			min = v.x;
			max = v.y;
		}
		else
		{
			var c = new GUIContent("Unsupported field type " + property.type);
			EditorGUI.LabelField(position, label, c);
			return;
		}

		float ppp = EditorGUIUtility.pixelsPerPoint;
		float spacing = kSpacing * ppp;
		float fieldWidth = kFloatFieldWidth * ppp;

		var indent = EditorGUI.indentLevel;

		var attr = attribute as MinMaxAttribute;

		var r = EditorGUI.PrefixLabel(position, label);

		Rect sliderPos = r;

		sliderPos.x += fieldWidth + spacing;
		sliderPos.width -= (fieldWidth + spacing) * 2;

		EditorGUI.BeginChangeCheck();
		EditorGUI.indentLevel = 0;
		EditorGUI.MinMaxSlider(sliderPos, ref min, ref max, attr.min, attr.max);
		EditorGUI.indentLevel = indent;
		if (EditorGUI.EndChangeCheck())
		{
			SetVectorValue(property, min, max);
		}

		Rect minPos = r;
		minPos.width = fieldWidth;

		var vectorMinProp = property.FindPropertyRelative(kVectorMinName);
		EditorGUI.showMixedValue = vectorMinProp.hasMultipleDifferentValues;
		EditorGUI.BeginChangeCheck();
		EditorGUI.indentLevel = 0;
		min = EditorGUI.DelayedFloatField(minPos, min);
		EditorGUI.indentLevel = indent;
		if (EditorGUI.EndChangeCheck())
		{
			min = Mathf.Max(min, attr.min);
			min = Mathf.Min(min, max);
			SetVectorValue(property, min, max);
		}

		Rect maxPos = position;
		maxPos.x += maxPos.width - fieldWidth;
		maxPos.width = fieldWidth;

		var vectorMaxProp = property.FindPropertyRelative(kVectorMaxName);
		EditorGUI.showMixedValue = vectorMaxProp.hasMultipleDifferentValues;
		EditorGUI.BeginChangeCheck();
		EditorGUI.indentLevel = 0;
		max = EditorGUI.DelayedFloatField(maxPos, max);
		EditorGUI.indentLevel = indent;
		if (EditorGUI.EndChangeCheck())
		{
			max = Mathf.Min(max, attr.max);
			max = Mathf.Max(max, min);
			SetVectorValue(property, min, max);
		}

		EditorGUI.showMixedValue = false;
	}
}

#endif
