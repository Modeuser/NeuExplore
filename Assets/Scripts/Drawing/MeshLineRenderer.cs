using UnityEngine;
using System.Collections.Generic;

/*
class Point {
	public Vector3 p;
	public Point next;
} 
*/

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class MeshLineRenderer : MonoBehaviour
{
	//define a line material
	public Material lmat;
	//instance for mesh
	private Mesh ml;
	//vector "s" to keep track of where the start point was
	private Vector3 s;
	//private variable for the line size, set to public if/when a line size modifier is added
	private float lineSize = .1f;
	//check if this was the first quad we've drawn
	private bool firstQuad = true;

	GameObject rightCon;

	void Start()
	{
		//ml meshfilter gets the mesh
		ml = GetComponent<MeshFilter>().mesh;
		//set the material of the meshRenderer to "lmat", as defined by us
		GetComponent<MeshRenderer>().material = lmat;

		//find the right con
		rightCon = GameObject.FindWithTag("GameController");
	}

	public void setWidth(float width)
	{
		lineSize = width;
	}

	public void AddPoint(Vector3 point)
	{
		if (s != Vector3.zero)
		{
			AddLine(ml, MakeQuad(s, point, lineSize, firstQuad));
			firstQuad = false;
		}
		s = point;
	}

	//"MakeQuad" makes all the quads by defining all the vectors to reference when drawing
	Vector3[] MakeQuad(Vector3 s, Vector3 e, float w, bool all)
	{
		//because the width will be counted for both top and bottom, w = w/2
		w = w / 2;
		//creates new vector3 array
		Vector3[] q;
		if (all)
		{
			//if this is the first quad, then we need 4 points in the array
			q = new Vector3[4];
		}
		else
		{
			//if this isn't a new quad, then we only need 2 points in the array
			q = new Vector3[2];
		}

		//the "n"ormal is the cross product of "start" and "end"
		Vector3 n = Vector3.Cross(s, e);
		//the "l"ine would then be the cross product of the "n"ormal and e - s
		//Vector3 l = Vector3.Cross(n, e - s);
		//Just track the forward transform of the controller:
		//Vector3 l = Vector3.Cross(rightCon.transform.forward, e - s);
		//the alternative below will cause the stroke's to always face the direction of the headset's forward:
		Vector3 l = Vector3.Cross(Camera.main.transform.forward, e - s);
		//should be able to create a variable that tracks the transform.forward of the controller instead of the headset
		l.Normalize();

		if (all)
		{
			//if this is the firstquad, define these 4 points to reference when drawing
			q[0] = transform.InverseTransformPoint(s + l * w);
			q[1] = transform.InverseTransformPoint(s + l * -w);
			q[2] = transform.InverseTransformPoint(e + l * w);
			q[3] = transform.InverseTransformPoint(e + l * -w);
		}
		else
		{
			//if this is not the first quad, define these 2 points to reference
			q[0] = transform.InverseTransformPoint(s + l * w);
			q[1] = transform.InverseTransformPoint(s + l * -w);
		}
		return q;
	}

	//"AddLine": adds the mesh onto the quads that we've constructed in "makequads"
	void AddLine(Mesh m, Vector3[] quad)
	{
		//the integer array tells the mesh the order by which the triangles
		//should be constructed
		int vl = m.vertices.Length;

		Vector3[] vs = m.vertices;
		vs = resizeVertices(vs, 2 * quad.Length);

		for (int i = 0; i < 2 * quad.Length; i += 2)
		{
			vs[vl + i] = quad[i / 2];
			vs[vl + i + 1] = quad[i / 2];
		}

		Vector2[] uvs = m.uv;
		uvs = resizeUVs(uvs, 2 * quad.Length);

		if (quad.Length == 4)
		{
			uvs[vl] = Vector2.zero;
			uvs[vl + 1] = Vector2.zero;
			uvs[vl + 2] = Vector2.right;
			uvs[vl + 3] = Vector2.right;
			uvs[vl + 4] = Vector2.up;
			uvs[vl + 5] = Vector2.up;
			uvs[vl + 6] = Vector2.one;
			uvs[vl + 7] = Vector2.one;
		}
		else
		{
			if (vl % 8 == 0)
			{
				uvs[vl] = Vector2.zero;
				uvs[vl + 1] = Vector2.zero;
				uvs[vl + 2] = Vector2.right;
				uvs[vl + 3] = Vector2.right;

			}
			else
			{
				uvs[vl] = Vector2.up;
				uvs[vl + 1] = Vector2.up;
				uvs[vl + 2] = Vector2.one;
				uvs[vl + 3] = Vector2.one;
			}
		}

		int tl = m.triangles.Length;

		int[] ts = m.triangles;
		ts = resizeTriangles(ts, 12);

		if (quad.Length == 2)
		{
			vl -= 4;
		}

		// front-facing quad
		// if you draw your triangles counter clockwise, it'll face towards you (normal)
		ts[tl] = vl;
		ts[tl + 1] = vl + 2;
		ts[tl + 2] = vl + 4;

		ts[tl + 3] = vl + 2;
		ts[tl + 4] = vl + 6;
		ts[tl + 5] = vl + 4;

		// back-facing quad
		// if you draw your triangles clockwise, it'll create the mesh away from you (normal)
		ts[tl + 6] = vl + 5;
		ts[tl + 7] = vl + 3;
		ts[tl + 8] = vl + 1;

		ts[tl + 9] = vl + 5;
		ts[tl + 10] = vl + 7;
		ts[tl + 11] = vl + 3;

		m.vertices = vs;
		m.uv = uvs;
		m.triangles = ts;
		m.RecalculateBounds();
		m.RecalculateNormals();
	}

	Vector3[] resizeVertices(Vector3[] ovs, int ns)
	{
		Vector3[] nvs = new Vector3[ovs.Length + ns];
		for (int i = 0; i < ovs.Length; i++)
		{
			nvs[i] = ovs[i];
		}

		return nvs;
	}

	Vector2[] resizeUVs(Vector2[] uvs, int ns)
	{
		Vector2[] nvs = new Vector2[uvs.Length + ns];
		for (int i = 0; i < uvs.Length; i++)
		{
			nvs[i] = uvs[i];
		}

		return nvs;
	}

	int[] resizeTriangles(int[] ovs, int ns)
	{
		int[] nvs = new int[ovs.Length + ns];
		for (int i = 0; i < ovs.Length; i++)
		{
			nvs[i] = ovs[i];
		}

		return nvs;
	}
}