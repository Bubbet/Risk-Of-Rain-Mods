using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BubbetsItems.Components
{
	public class ManagerGraphic : MaskableGraphic
	{

		public static Vector2 PerpCcw(Vector2 v) => new Vector2(-v.y, v.x);
		public static Vector2 PerpCw(Vector2 v) => new Vector2(v.y, -v.x);

		/// <summary>
		/// The width of the edge to create.
		/// </summary>
		float width = 2.0f;

		/// <summary>
		/// The points we're tracking (that we made in Start()) to make into a line strip.
		/// </summary>
		//private List<RectTransform> _points = new List<RectTransform>();

		public List<Vector2> lineStrip = new List<Vector2>();

		
		/// <summary>
		/// Virtual function to create the UI mesh.
		/// </summary>
		public override void OnPopulateMesh(VertexHelper vh)
		{
			if (lineStrip.Count == 0) return;
			//if(!built) parent.BuildGraph();
			vh.Clear();
			MakeGrid(vh);
			List<Vector2> infs = GetInflationVectors(lineStrip);

			// Create the inflated mesh at the width the user specified from the control.
			CreateInflatedMesh(vh, lineStrip, infs, this.width, new Color(0.9f, 0.9f, 0.9f));

			// And then create an inflated line strip mesh on top that's just 1 radius thickness
			// that represents the unprocessed line.
			//CreateInflatedMesh(vh, lineStrip, infs, 1.0f, Color.black);
		}

		private float _cellWidth;
		private float _cellHeight;
		public Vector2Int gridSize = new Vector2Int(49, 5);
		private float thickness = 2f;
		public Vector2 size;
		public LogBookPageScalingGraph? parent;

		private void MakeGrid(VertexHelper vh)
		{
			//vh.Clear();
			//var width = rectTransform.rect.width * 5f;
			//var height = rectTransform.rect.height * 5f;

			_cellWidth = size.x / gridSize.x;
			_cellHeight = size.y / gridSize.y;

			int count = 0;
			for (int y = 0; y < gridSize.y; y++)
			{
				for (int x = 0; x < gridSize.x; x++)
				{
					DrawCell(x, y, count, vh);
					count++;
				}
			}
		}

		private void DrawCell(int x, int y, int index, VertexHelper vh)
		{
			float xPos = _cellWidth * x - 0.5f * size.x;
			float yPos = _cellHeight * y - 0.5f * size.y;
            
			UIVertex vertex = UIVertex.simpleVert;
			vertex.color = new Color(0.6f, 0.6f, 0.6f);
			//vertex.color = color;
            
			vertex.position = new Vector3(xPos, yPos);
			vh.AddVert(vertex);

			vertex.position = new Vector3(xPos, yPos + _cellHeight);
			vh.AddVert(vertex);
            
			vertex.position = new Vector3(xPos + _cellWidth, yPos + _cellHeight);
			vh.AddVert(vertex);
            
			vertex.position = new Vector3(xPos + _cellWidth, yPos);
			vh.AddVert(vertex);
            
			//vh.AddTriangle(0, 1, 2);
			//vh.AddTriangle(2, 3, 0);

			var widthSqr = thickness * thickness;
			var distanceSqr = widthSqr / 2f;
			var distance = Mathf.Sqrt(distanceSqr);

			vertex.position = new Vector3(xPos + distance, yPos + distance);
			vh.AddVert(vertex);
            
			vertex.position = new Vector3(xPos + distance, yPos + _cellHeight - distance);
			vh.AddVert(vertex);
            
			vertex.position = new Vector3(xPos + _cellWidth - distance, yPos + _cellHeight - distance);
			vh.AddVert(vertex);
            
			vertex.position = new Vector3(xPos + _cellWidth - distance, yPos + distance);
			vh.AddVert(vertex);

			int offset = index * 8;
            
			//left
			vh.AddTriangle(offset + 0, offset + 1, offset + 5);
			vh.AddTriangle(offset + 5, offset + 4, offset + 0);
            
			//top
			vh.AddTriangle(offset + 1, offset + 2, offset + 6);
			vh.AddTriangle(offset + 6, offset + 5, offset + 1);
            
			//right
			vh.AddTriangle(offset + 2, offset + 3, offset + 7);
			vh.AddTriangle(offset + 7, offset + 6, offset + 2);
            
			//bottom
			vh.AddTriangle(offset + 3, offset + 0, offset + 4);
			vh.AddTriangle(offset + 4, offset + 7, offset + 3);
		}

		/// <summary>
		/// Create an inflated mesh at a specified color and width.
		/// </summary>
		/// <param name="vh">The VertexHelpter from OnPopulateMesh().</param>
		/// <param name="lst">The line strip.</param>
		/// <param name="inf">The unit-inflation amount form line strip.</param>
		/// <param name="amt">The radius of how much to inflate the line strip.</param>
		/// <param name="c">The color of the inflated line mesh.</param>
		public static void CreateInflatedMesh(VertexHelper vh, List<Vector2> lst, List<Vector2> inf,
			float amt, Color c)
		{
			int ct = vh.currentVertCount;

			// Add the positive and the negative side - this inflates the line on both
			// side and makes the inflation amount a radius that's half the actual width.
			for (int i = 0; i < lst.Count; ++i)
			{
				UIVertex vt = new UIVertex();
				vt.position = lst[i] + inf[i] * amt;
				vt.color = c;

				UIVertex vb = new UIVertex();
				vb.position = lst[i] - inf[i] * amt;
				vb.color = c;

				vh.AddVert(vt);
				vh.AddVert(vb);
			}

			// Triangulate the vertices as quads.
			for (int i = 0; i < lst.Count - 1; ++i)
			{
				int t0 = ct + i * 2 + 0;
				int t1 = ct + i * 2 + 1;
				int t2 = ct + i * 2 + 2;
				int t3 = ct + i * 2 + 3;

				vh.AddTriangle(t0, t1, t2);
				vh.AddTriangle(t1, t3, t2);
			}
		}

		/// <summary>
		/// Get the amount we need to extend per unit width.
		/// </summary>
		/// <param name="vecs">The line strip to process.</param>
		/// <returns>
		/// A 1-1 mapping of vectors for how much to move to inflate the vector
		/// 1 unit.</returns>
		public static List<Vector2> GetInflationVectors(List<Vector2> vecs)
		{
			List<Vector2> ret = new List<Vector2>();

			ret.Add(PerpCcw((vecs[1] - vecs[0]).normalized));

			for (int i = 1; i < vecs.Count - 1; ++i)
			{
				Vector2 toPt = PerpCcw((vecs[i] - vecs[i - 1]).normalized);
				Vector2 frPt = PerpCw((vecs[i] - vecs[i + 1]).normalized);
				Vector2 half = (toPt + frPt).normalized;
				float dot = Vector2.Dot(toPt, half);
				ret.Add((1.0f / dot) * half);
			}

			ret.Add(PerpCcw((vecs[vecs.Count - 1] - vecs[vecs.Count - 2]).normalized));
			return ret;
		}
	}
}