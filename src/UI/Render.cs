using UnityEngine;

#nullable enable

namespace TrainerKit.UI;

public static class Render
{
	private static GUIStyle StringStyle
	{
		get => field ??= new GUIStyle { fontSize = 14, richText = true, normal = { textColor = Color.white } };
	}

	public static Vector2 ScreenCenter => new(Screen.width / 2f, Screen.height / 2f);

	public static Color Color
	{
		get { return GUI.color; }
		set { GUI.color = value; }
	}

	public static Vector2 DrawString(Vector2 position, string label, Color color, bool centered = true)
	{
		Color = color;
		return DrawString(position, label, centered);
	}

	public static void GetContentAndSize(string label, out GUIContent content, out Vector2 size)
	{
		content = new GUIContent(label);
		var calcSize = StringStyle.CalcSize(content);
		if (calcSize.y < 10f)
			calcSize = new Vector2(label.Length * 8f, 20f);
		size = calcSize;
	}

	public static Vector2 DrawString(Vector2 position, string label, bool centered = true)
	{
		GetContentAndSize(label, out var content, out var size);
		var upperLeft = centered ? position - size / 2f : position;
		var rect = new Rect(upperLeft.x, upperLeft.y, Mathf.Max(size.x, Screen.width - upperLeft.x), Mathf.Max(size.y, 24f));
		GUI.Label(rect, content, StringStyle);
		return size;
	}

	public static void DrawCrosshair(Vector2 position, float size, Color color, float thickness)
	{
		Color = color;
		var texture = Texture2D.whiteTexture;
		GUI.DrawTexture(new Rect(position.x - size, position.y, size * 2 + thickness, thickness), texture);
		GUI.DrawTexture(new Rect(position.x, position.y - size, thickness, size * 2 + thickness), texture);
	}

	public static void DrawBox(float x, float y, float w, float h, float thickness, Color color)
	{
		Color = color;
		var texture = Texture2D.whiteTexture;
		GUI.DrawTexture(new Rect(x, y, w + thickness, thickness), texture);
		GUI.DrawTexture(new Rect(x, y, thickness, h + thickness), texture);
		GUI.DrawTexture(new Rect(x + w, y, thickness, h + thickness), texture);
		GUI.DrawTexture(new Rect(x, y + h, w + thickness, thickness), texture);
	}

	public static void DrawLine(Vector2 lineStart, Vector2 lineEnd, float thickness, Color color)
	{
		Color = color;

		var vector = lineEnd - lineStart;
		var pivot = /* 180/PI */ Mathf.Rad2Deg * Mathf.Atan(vector.y / vector.x);
		if (vector.x < 0f)
			pivot += 180f;

		thickness = Mathf.Max(thickness, 1f);
		var yOffset = (int)Mathf.Ceil(thickness / 2);

		GUIUtility.RotateAroundPivot(pivot, lineStart);
		GUI.DrawTexture(new Rect(lineStart.x, lineStart.y - yOffset, vector.magnitude, thickness), Texture2D.whiteTexture);
		GUIUtility.RotateAroundPivot(-pivot, lineStart);
	}

	public static void DrawCircle(Vector2 center, float radius, Color color, float width, int segmentsPerQuarter)
	{
		var totalSegments = segmentsPerQuarter * 4;
		var step = 1f / totalSegments;
		var lastV = center + new Vector2(radius, 0);

		for (var i = 1; i <= totalSegments; ++i)
		{
			var t = i * step;
			var currentV = center + new Vector2(
				radius * Mathf.Cos(2 * Mathf.PI * t),
				radius * Mathf.Sin(2 * Mathf.PI * t)
			);
			DrawLine(lastV, currentV, width, color);
			lastV = currentV;
		}
	}
}
