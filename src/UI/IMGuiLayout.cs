using UnityEngine;

#nullable enable

namespace TrainerKit.UI;

/// <summary>
/// Simple layout helper for IL2CPP IMGUI rendering.
/// </summary>
internal class IMGuiLayout(float x, float y, float width)
{
	private float _x = x;
	private float _y = y;
	private float _width = width;
	private readonly float _startX = x;
	private readonly float _startY = y;
	private readonly float _totalWidth = width;
	private const float RowHeight = 22f;
	private const float Spacing = 4f;

	// Horizontal mode
	private float _hCursorX;
	private bool _horizontal;

	public float CurrentY => _y;

	public Rect NextRect(float height = RowHeight)
	{
		Rect rect;
		if (_horizontal)
		{
			rect = new Rect(_hCursorX, _y, _width, height);
			_hCursorX += _width + Spacing;
		}
		else
		{
			rect = new Rect(_x, _y, _width, height);
			_y += height + Spacing;
		}
		return rect;
	}

	public void Space(float pixels = Spacing)
	{
		if (!_horizontal)
			_y += pixels;
		else
			_hCursorX += pixels;
	}

	public void BeginHorizontal(int columns)
	{
		_horizontal = true;
		_hCursorX = _x;
		_width = (_totalWidth - Spacing * (columns - 1)) / columns;
	}

	public void BeginHorizontal(float leftWidth)
	{
		_horizontal = true;
		_hCursorX = _x;
		_width = leftWidth;
	}

	public void SetColumnWidth(float width)
	{
		_width = width;
	}

	public void FlexRemaining()
	{
		if (_horizontal)
			_width = (_startX + _totalWidth) - _hCursorX;
	}

	public void EndHorizontal()
	{
		_horizontal = false;
		_width = _totalWidth;
		_y += RowHeight + Spacing;
	}

	public float TotalHeight => _y - _startY;
}
