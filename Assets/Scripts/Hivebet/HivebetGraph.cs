using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FeuBleu.Hivebet
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class HivebetGraph : MaskableGraphic
    {
        public TMP_Text topLabel, middleLabel;
        private HistoryPoint[] points;
        private double maximum = 10;
        public void SetPoints(HistoryPoint[] values)
        {
            points = values;
            maximum = HivebetData.ChartMaximum(values);
            topLabel.text = maximum.ToString("0.##", CultureInfo.InvariantCulture) + " $";
            middleLabel.text = (maximum / 2).ToString("0.##", CultureInfo.InvariantCulture) + " $";
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = rectTransform.rect;
            Vector2 origin = new Vector2(rect.xMin, rect.yMin);
            Color axis = new Color(0.5f, 0.55f, 0.56f);
            Line(vh, origin, origin + new Vector2(rect.width, 0), axis, 2);
            Line(vh, origin, origin + new Vector2(0, rect.height), axis, 2);
            Draw(vh, "yes", new Color(0.39f, 0.76f, 0.29f), rect);
            Draw(vh, "no", new Color(0.85f, 0.17f, 0.32f), rect);
        }
        private void Draw(VertexHelper vh, string id, Color color, Rect rect)
        {
            if (points == null || points.Length == 0) return;
            Vector2 previous = Vector2.zero;
            for (int i = 0; i < points.Length; i++)
            {
                Candidate value = HivebetData.Find(points[i].candidates, id);
                float x = points.Length == 1 ? 0 : (float)i / (points.Length - 1);
                float y = value == null ? 0 : Mathf.Clamp01((float)(value.amountMinor / 100d / maximum));
                Vector2 current = new Vector2(rect.xMin + x * rect.width, rect.yMin + y * rect.height);
                if (i > 0) Line(vh, previous, current, color, 5);
                if (i == points.Length - 1) Line(vh, current - Vector2.right * 5, current + Vector2.right * 5, color, 10);
                previous = current;
            }
        }
        private static void Line(VertexHelper vh, Vector2 a, Vector2 b, Color color, float width)
        {
            Vector2 normal = new Vector2(-(b - a).y, (b - a).x).normalized * width / 2;
            int index = vh.currentVertCount;
            vh.AddVert(a - normal, color, Vector2.zero); vh.AddVert(a + normal, color, Vector2.zero);
            vh.AddVert(b + normal, color, Vector2.zero); vh.AddVert(b - normal, color, Vector2.zero);
            vh.AddTriangle(index, index + 1, index + 2); vh.AddTriangle(index, index + 2, index + 3);
        }
    }
}
