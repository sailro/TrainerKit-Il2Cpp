using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TrainerKit.Configuration;
using TrainerKit.Extensions;
using TrainerKit.Properties;
using TrainerKit.UI;
using UnityEngine;

#nullable enable

namespace TrainerKit.Features;

internal abstract class FeatureRenderer : ToggleFeature
{
	public abstract float X { get; set; }
	public abstract float Y { get; set; }

	protected const float DefaultX = 40f;
	protected const float DefaultY = 20f;

	private static RectOffset MakeRectOffset(int left, int right, int top, int bottom) => new() { left = left, right = right, top = top, bottom = bottom };

	private static GUIStyle LabelStyle
	{
		get => field ??= new GUIStyle { wordWrap = false, normal = { textColor = Color.white }, margin = MakeRectOffset(8, 0, 8, 0), fixedWidth = 150f, stretchWidth = false };
	}

	private static GUIStyle DescriptionStyle
	{
		get => field ??= new GUIStyle { wordWrap = true, normal = { textColor = Color.white }, margin = MakeRectOffset(8, 0, 8, 0), stretchWidth = true };
	}

	private static GUIStyle ColorButtonFullStyle
	{
		get => field ??= new GUIStyle { normal = { background = Texture2D.whiteTexture, textColor = Color.white }, fixedHeight = 22f };
	}

	private static GUIStyle TitleStyle
	{
		get => field ??= new GUIStyle { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, normal = { textColor = Color.white }, fontSize = 13 };
	}

	private static GUIStyle TextFieldStyle
	{
		get => field ??= new GUIStyle { normal = { textColor = new Color(0.9f, 0.9f, 0.9f, 1f) }, fontSize = 12, alignment = TextAnchor.MiddleLeft, padding = { left = 4, right = 4 } };
	}

	protected void SetupWindowCoordinates()
	{
		bool needfix = false;
		X = FixCoordinate(X, Screen.width, DefaultX, ref needfix);
		Y = FixCoordinate(Y, Screen.height, DefaultY, ref needfix);

		if (needfix)
			SaveSettings();
	}

	private static float FixCoordinate(float coord, float maxValue, float defaultValue, ref bool needfix)
	{
		if (coord < 0 || coord >= maxValue)
		{
			coord = defaultValue;
			needfix = true;
		}

		return coord;
	}

	internal enum SelectionContextType { Color = 1, KeyCode = 2 }

	internal class SelectionContext
	{
		public SelectionContext(IFeature feature, OrderedProperty orderedProperty, float parentX, float parentY, Func<object, IPicker> builder, SelectionContextType contextType)
		{
			Feature = feature;
			OrderedProperty = orderedProperty;
			Picker = builder(orderedProperty.Property.GetValue(feature)!);
			ContextType = contextType;

			var position = Event.current.mousePosition;
			Picker.SetWindowPosition(parentX + LabelStyle.fixedWidth * 3 + LabelStyle.margin.left * 6, position.y + parentY - 32f);
		}

		public IFeature Feature { get; }
		public OrderedProperty OrderedProperty { get; }
		public IPicker Picker { get; }
		public SelectionContextType ContextType { get; }
	}

	private Rect _clientWindowRect;
	private readonly Dictionary<SelectionContextType, SelectionContext> _selectionContexts = [];

	private bool _isDragging;
	private Vector2 _dragOffset;
	private const float TitleBarHeight = 24f;

	protected override void OnGUIWhenEnabled()
	{
		_clientWindowRect = new Rect(X, Y, 490, Math.Max(_clientWindowRect.height, 256));

		foreach (var key in _selectionContexts.Keys)
		{
			if (HandleSelectionContext(_selectionContexts[key]))
				_selectionContexts.Remove(key);
		}

		HandleDrag();

		GUI.Box(_clientWindowRect, string.Empty);
		var titleRect = new Rect(_clientWindowRect.x, _clientWindowRect.y, _clientWindowRect.width, TitleBarHeight);
		GUI.Label(titleRect, Strings.FeatureCommandsTitle, TitleStyle);

		RenderWindowContent();

		X = _clientWindowRect.x;
		Y = _clientWindowRect.y;
	}

	private void HandleDrag()
	{
		var evt = Event.current;
		var titleRect = new Rect(_clientWindowRect.x, _clientWindowRect.y, _clientWindowRect.width, TitleBarHeight);

		if (evt.type == EventType.MouseDown && titleRect.Contains(evt.mousePosition))
		{
			_isDragging = true;
			_dragOffset = evt.mousePosition - new Vector2(_clientWindowRect.x, _clientWindowRect.y);
			evt.Use();
		}
		else if (evt.type == EventType.MouseDrag && _isDragging)
		{
			_clientWindowRect.x = evt.mousePosition.x - _dragOffset.x;
			_clientWindowRect.y = evt.mousePosition.y - _dragOffset.y;
			evt.Use();
		}
		else if (evt.type == EventType.MouseUp && _isDragging)
		{
			_isDragging = false;
			evt.Use();
		}
	}

	private bool HandleSelectionContext(SelectionContext? context)
	{
		if (context == null)
			return false;

		var property = context.OrderedProperty.Property;
		var picker = context.Picker;

		picker.DrawWindow((int)context.ContextType, GetPropertyDisplay(property.Name));
		property.SetValue(context.Feature, picker.RawValue);

		return picker.IsSelected;
	}

	private const float TabWidth = 150f;
	private const float ContentMargin = 8f;
	private const float WindowPadding = 10f;
	private const float RowHeight = 22f;
	private const float PropertyLabelWidth = 150f;

	private int _selectedTabIndex = 0;
	private void RenderWindowContent()
	{
		var wx = _clientWindowRect.x;
		var wy = _clientWindowRect.y;

		var fixedTabs = new[] { Strings.FeatureRendererSummary };

		var tabs = fixedTabs
			.Concat
			(
				Context
					.Features
					.Value
					.Select(RenderFeatureText)
			)
			.ToArray();

		var tabY = wy + WindowPadding + TitleBarHeight;
		var lastIndex = _selectedTabIndex;
		for (int i = 0; i < tabs.Length; i++)
		{
			var tabRect = new Rect(wx + WindowPadding, tabY, TabWidth, RowHeight);
			if (Il2CppButton(tabRect, tabs[i]))
				_selectedTabIndex = i;
			tabY += RowHeight + 2;
		}

		if (lastIndex != _selectedTabIndex)
			_selectionContexts.Clear();

		var contentX = wx + WindowPadding + TabWidth + ContentMargin;
		var contentWidth = 490 - WindowPadding - TabWidth - ContentMargin - WindowPadding;
		var layout = new IMGuiLayout(contentX, wy + WindowPadding + TitleBarHeight + 4, contentWidth);

		switch (_selectedTabIndex)
		{
			case 0:
				RenderSummary(layout);
				break;
			default:
				var feature = Context.Features.Value[_selectedTabIndex - fixedTabs.Length];
				RenderFeature(feature, layout);
				break;
		}

		var neededHeight = Math.Max(tabY - wy, layout.CurrentY - wy) + WindowPadding;
		_clientWindowRect.height = Math.Max(neededHeight, 256);
	}

	private static bool RectContains(Rect rect, Vector2 point)
	{
		return point.x >= rect.x && point.x <= rect.x + rect.width
			&& point.y >= rect.y && point.y <= rect.y + rect.height;
	}

	private static bool Il2CppButton(Rect rect, string text)
	{
		var evt = Event.current;
		bool clicked = evt.type == EventType.MouseDown && evt.button == 0 && RectContains(rect, evt.mousePosition);
		GUI.Button(rect, text);
		if (clicked)
			evt.Use();
		return clicked;
	}

	private static bool Il2CppButton(Rect rect, string text, GUIStyle style)
	{
		var evt = Event.current;
		bool clicked = evt.type == EventType.MouseDown && evt.button == 0 && RectContains(rect, evt.mousePosition);
		GUI.Button(rect, text, style);
		if (clicked)
			evt.Use();
		return clicked;
	}

	private static bool Il2CppToggle(Rect rect, bool value, string text)
	{
		var evt = Event.current;
		bool clicked = evt.type == EventType.MouseDown && evt.button == 0 && RectContains(rect, evt.mousePosition);
		GUI.Toggle(rect, value, text);
		if (clicked)
			return !value;
		return value;
	}

	private static string RenderFeatureText(Feature feature)
	{
		if (feature is not ToggleFeature toggleFeature || ConfigurationManager.IsSkippedProperty(feature, nameof(Enabled)))
			return feature.Name;

		return string.Format(Strings.CommandStatusTextFormat, feature.Name, toggleFeature.Enabled ? Strings.TextOn.Green() : Strings.TextOff.Red(), string.Empty);
	}

	private void RenderSummary(IMGuiLayout layout)
	{
		GUI.Label(layout.NextRect(30), $"<i><b>{Strings.FeatureRendererWelcome}</b></i>", DescriptionStyle);
		layout.Space();

		if (Il2CppButton(layout.NextRect(), Strings.CommandLoadDescription))
			LoadSettings();

		if (Il2CppButton(layout.NextRect(), Strings.CommandSaveDescription))
			SaveSettings();
	}

	protected static void SaveSettings()
	{
		ConfigurationManager.Save(Context.ConfigFile, Context.Features.Value);
	}

	protected void LoadSettings(bool warnIfNotExists = true)
	{
		var cx = X;
		var cy = Y;

		ConfigurationManager.Load(Context.ConfigFile, Context.Features.Value, warnIfNotExists);
		_controlValues.Clear();

		if (!Enabled)
			return;

		X = cx;
		Y = cy;
	}

	private void RenderFeature(Feature feature, IMGuiLayout layout)
	{
		var orderedProperties = ConfigurationManager.GetOrderedProperties(feature.GetType());

		GUI.Label(layout.NextRect(30), $"<i><b>{feature.Description}</b></i>", DescriptionStyle);
		layout.Space();

		foreach (var property in orderedProperties)
			RenderFeatureProperty(feature, property, layout);
	}

	private static readonly Dictionary<string, string> _controlValues = [];
	private void RenderFeatureProperty(Feature feature, OrderedProperty orderedProperty, IMGuiLayout layout)
	{
		if (!orderedProperty.Attribute.Browsable)
			return;

		var property = orderedProperty.Property;

		layout.BeginHorizontal(PropertyLabelWidth);
		GUI.Label(layout.NextRect(), GetPropertyDisplay(property.Name), LabelStyle);

		layout.FlexRemaining();

		var currentValue = property.GetValue(feature);
		var currentBackgroundColor = GUI.backgroundColor;

		if (currentValue == null)
		{
			layout.EndHorizontal();
			return;
		}

		var controlName = $"{feature.Name}.{property.Name}-{property.PropertyType.Name}";
		GUI.SetNextControlName(controlName);

		var newValue = RenderControl(feature, orderedProperty, currentValue, controlName, layout);

		if (currentValue != newValue && property.CanWrite)
			property.SetValue(feature, newValue);

		var focused = GUI.GetNameOfFocusedControl();

		foreach (var key in _selectionContexts.Keys)
		{
			if (ShouldResetSelectionContext(focused, _selectionContexts[key]))
				_selectionContexts.Remove(key);
		}

		GUI.backgroundColor = currentBackgroundColor;
		layout.EndHorizontal();
	}

	protected abstract string GetPropertyDisplay(string propertyName);

	private object RenderControl(IFeature feature, OrderedProperty orderedProperty, object currentValue, string controlName, IMGuiLayout layout)
	{
		var property = orderedProperty.Property;
		var propertyType = property.PropertyType;
		var newValue = currentValue;
		var rect = layout.NextRect();

		switch (propertyType.Name)
		{
			case nameof(Boolean):
				var boolValue = (bool)currentValue;
				var newBool = Il2CppToggle(rect, boolValue, string.Empty);
				if (newBool != boolValue) _selectionContexts.Clear();
				newValue = newBool;
				break;

			case nameof(KeyCode):
				if (Il2CppButton(rect, currentValue.ToString()!))
				{
					_selectionContexts[SelectionContextType.KeyCode] = new SelectionContext(feature, orderedProperty, X, Y, o => new EnumPicker<KeyCode>((KeyCode)o), SelectionContextType.KeyCode);
					GUI.FocusControl(controlName);
				}
				break;

			case nameof(Single):
				newValue = RenderFloatControl(rect, currentValue, controlName);
				break;

			case nameof(Int32):
				newValue = RenderIntControl(rect, currentValue, controlName);
				break;

			case nameof(Color):
				GUI.backgroundColor = (Color)currentValue;
				if (Il2CppButton(rect, string.Empty, ColorButtonFullStyle))
				{
					_selectionContexts[SelectionContextType.Color] = new SelectionContext(feature, orderedProperty, X, Y, o => new ColorPicker((Color)o), SelectionContextType.Color);
					GUI.FocusControl(controlName);
				}
				break;

			case nameof(String):
				newValue = Il2CppTextField(rect, currentValue.ToString()!, controlName);
				break;

			default:
				GUI.Label(rect, string.Format(Strings.ErrorUnsupportedTypeFormat, propertyType.FullName));
				break;
		}

		return newValue;
	}

	private static bool ShouldResetSelectionContext(string focused, SelectionContext? context)
	{
		return !string.IsNullOrEmpty(focused)
			   && context != null
			   && !focused.EndsWith($"-{context.ContextType}");
	}

	private static string? _activeControlName;

	private string Il2CppTextField(Rect rect, string text, string controlName)
	{
		bool isActive = _activeControlName == controlName;
		var editText = _controlValues.GetValueOrDefault(controlName, text);

		var tmp = GUI.backgroundColor;
		GUI.backgroundColor = isActive ? new Color(1f, 1f, 1f, 0.3f) : new Color(1f, 1f, 1f, 0.15f);
		GUI.Box(rect, string.Empty);
		GUI.backgroundColor = tmp;

		GUI.Label(rect, isActive ? editText + "|" : editText, TextFieldStyle);

		var evt = Event.current;

		if (evt.type == EventType.MouseDown && evt.button == 0)
		{
			if (RectContains(rect, evt.mousePosition))
			{
				_activeControlName = controlName;
				_selectionContexts.Clear();
				_controlValues[controlName] = editText;
				evt.Use();
			}
			else if (isActive)
			{
				_activeControlName = null;
			}
		}

		if (isActive && evt.type == EventType.KeyDown)
		{
			if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
			{
				_activeControlName = null;
				evt.Use();
			}
			else if (evt.keyCode == KeyCode.Escape)
			{
				_controlValues[controlName] = text;
				_activeControlName = null;
				evt.Use();
				return text;
			}
			else if (evt.keyCode == KeyCode.Backspace)
			{
				if (editText.Length > 0)
					editText = editText[..^1];
				_controlValues[controlName] = editText;
				evt.Use();
			}
			else if (evt.character != 0 && evt.character != '\n' && evt.character != '\r')
			{
				editText += evt.character;
				_controlValues[controlName] = editText;
				evt.Use();
			}
		}

		return _controlValues.GetValueOrDefault(controlName, text);
	}

	private object RenderFloatControl(Rect rect, object currentValue, string controlName)
	{
		var culture = CultureInfo.InvariantCulture;
		var text = Il2CppTextField(rect, ((float)currentValue).ToString("G", culture), controlName);
		text = text.Replace(",", ".");
		return float.TryParse(text, NumberStyles.Float, culture, out var floatVal) ? floatVal : currentValue;
	}

	private object RenderIntControl(Rect rect, object currentValue, string controlName)
	{
		var text = Il2CppTextField(rect, currentValue.ToString()!, controlName);
		return int.TryParse(text, out var intVal) ? intVal : currentValue;
	}
}
