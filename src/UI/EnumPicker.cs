using System;
using System.Linq;
using UnityEngine;

#nullable enable

namespace TrainerKit.UI;

public class EnumPicker<T>(T value) : Picker<T>(value) where T : struct, IConvertible
{
	private Rect _windowRect = new(20, 20, 200, 500);
	private float _scrollOffset = 0f;

	public T[] Candidates
	{
		get
		{
			return field ??=
			[
				.. Enum
					.GetValues(typeof(T))
					.OfType<T>()
					.OrderBy(i => i)
			];
		}
	} = null;

	public override void SetWindowPosition(float x, float y)
	{
		_windowRect.x = x;
		_windowRect.y = y;
	}

	private bool _isDragging;
	private Vector2 _dragOffset;
	private const float TitleBarHeight = 24f;
	private const float RowHeight = 20f;
	private const float Padding = 6f;
	private const float MaxVisibleHeight = 460f;

	public override void DrawWindow(int id, string title)
	{
		var evt = Event.current;
		var titleRect = new Rect(_windowRect.x, _windowRect.y, _windowRect.width, TitleBarHeight);

		switch (evt.type)
		{
			case EventType.MouseDown when titleRect.Contains(evt.mousePosition):
				_isDragging = true;
				_dragOffset = evt.mousePosition - new Vector2(_windowRect.x, _windowRect.y);
				evt.Use();
				break;
			case EventType.MouseDrag when _isDragging:
				_windowRect.x = evt.mousePosition.x - _dragOffset.x;
				_windowRect.y = evt.mousePosition.y - _dragOffset.y;
				evt.Use();
				break;
			case EventType.MouseUp when _isDragging:
				_isDragging = false;
				evt.Use();
				break;
		}

		if (evt.type == EventType.ScrollWheel && _windowRect.Contains(evt.mousePosition))
		{
			_scrollOffset += evt.delta.y * RowHeight;
			_scrollOffset = Mathf.Max(0, _scrollOffset);
			float maxScroll = Candidates.Length * RowHeight - MaxVisibleHeight;
			if (maxScroll > 0)
				_scrollOffset = Mathf.Min(_scrollOffset, maxScroll);
			else
				_scrollOffset = 0;
			evt.Use();
		}

		float contentHeight = Candidates.Length * RowHeight;
		float visibleHeight = Mathf.Min(contentHeight, MaxVisibleHeight);
		_windowRect.height = TitleBarHeight + Padding + visibleHeight + Padding;

		GUI.Box(_windowRect, string.Empty);
		var titleStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, normal = { textColor = Color.white }, fontSize = 12 };
		GUI.Label(titleRect, title, titleStyle);

		var listArea = new Rect(_windowRect.x + Padding, _windowRect.y + TitleBarHeight + Padding,
			_windowRect.width - Padding * 2, visibleHeight);
		GUI.BeginGroup(listArea);

		float y = -_scrollOffset;
		foreach (var candidate in Candidates)
		{
			if (y + RowHeight > 0 && y < visibleHeight)
			{
				var itemRect = new Rect(0, y, listArea.width, RowHeight);
				bool clicked = evt is { type: EventType.MouseDown, button: 0, mousePosition.x: >= 0 } && evt.mousePosition.x <= listArea.width
				                                                                                      && evt.mousePosition.y >= y && evt.mousePosition.y < y + RowHeight;

				GUI.Label(itemRect, candidate.ToString(), GUI.skin.label);

				if (clicked)
				{
					IsSelected = true;
					Value = candidate;
					evt.Use();
				}
			}
			y += RowHeight;
		}

		GUI.EndGroup();
	}
}
