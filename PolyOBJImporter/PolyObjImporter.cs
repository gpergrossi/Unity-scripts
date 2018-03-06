using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor.Experimental.AssetImporters;
using UnityEditor;
using UnityEngine;

/**
 * Scripted importers implement an OnImportAsset method which handles
 * importing of any file with the given file extenson. In this case ".polyobj"
 */
[ScriptedImporter(1, "polyobj")]

/**
 * Poly Object Importer for Unity
 *
 *   Copyright 2018 Gregary Pergrossi
 *
 *   Licensed under the Apache License, Version 2.0 (the "License");
 *   you may not use this file except in compliance with the License.
 *   You may obtain a copy of the License at
 *
 *       http://www.apache.org/licenses/LICENSE-2.0
 *
 *   Unless required by applicable law or agreed to in writing, software
 *   distributed under the License is distributed on an "AS IS" BASIS,
 *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *   See the License for the specific language governing permissions and
 *   limitations under the License.
 *
 * This script will allow you to import an OBJ file downloaded from
 * a Google Poly Blocks creation. It differes from the standard Unity
 * OBJ importer by importing the entire model as a single mesh with
 * vertex colors. You must rename the ".obj" file to have an extension
 * of ".polyobj" for Unity to make use of this script. When importing
 * the asset, you must also import the ".mtl" file it references (if present).
 */
public class PolyObjImporter : ScriptedImporter {

	[SerializeField]
	public Boolean m_GenerateTexCoords = false;

	private static Shader shader = Shader.Find("Custom/VertexColors");

	/**
	 * This method is called by Unity when a file with the 
	 * ".polyobj" file extension is imported or re-imported.
	 */
	public override void OnImportAsset(AssetImportContext ctx) {
		var materials = new Dictionary<string, ObjMaterial>();
		var vertices = new List<Vertex>();
		var faces = new List<Face>();

		// Parse the OBJ file and referenced MTL file
		ReadGeometry(ctx.assetPath, ref materials, ref faces, ref vertices);

		// Link face materials to vertex colors + triangulate faces
		foreach (var face in faces) {
			ObjMaterial om = materials[face.material];
			foreach (var vertex in face.vertices) vertex.color = om.diffuse;
			face.Triangulate(); // Converts all N-gons to triangles
		}

		// Create a GameObject asset for the mesh
		GameObject main = new GameObject();
		main.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
		ctx.AddObjectToAsset(main.name, main);

		// Create a mesh asset and add it to the GameObject
		var meshFilter = main.AddComponent<MeshFilter>();
		var mesh = BuildMesh(ref vertices, ref faces);
		ctx.AddObjectToAsset(mesh.name, mesh);
		meshFilter.sharedMesh = mesh;

		// Create a material asset and add it to the GameObject
		var meshRenderer = main.AddComponent<MeshRenderer>();
		var material = new Material(shader);
		ctx.AddObjectToAsset(material.name, material);
		meshRenderer.material = material;

		// Finish import
		ctx.SetMainObject(main);
		Debug.Log("Import successful: Poly OBJ with " + vertices.Count + " vertices and " + faces.Count + " faces.");
	}

	/**
	 * Reads all geometry lines from the OBJ file at the given path.
	 * All output is written to the referenced materials Dictionary, list of faces, and list of vertices.
	 */
	private static void ReadGeometry(string path, ref Dictionary<string, ObjMaterial> materials, ref List<Face> faces, ref List<Vertex> vertices) {

		// These lists are only needed locally, as this data
		// will be stored in the vertices list instead.
		var positions = new List<Vector3>();
		var normals = new List<Vector3>();
		var uvs = new List<Vector2>();

		// Keep track of most recently named Object, Group, and Material
		string currentObject = null;
		string currentGroup = null;
		string currentMaterial = null;

		string[] lines = File.ReadAllLines(path, Encoding.ASCII);
		foreach (var linein in lines) {
			var line = linein.Trim();

			// Vertex line: 3 floats represent x,y,z
			if (line.StartsWith("v ")) {
				var pos = ParseVec3(line.Substring(2));
				positions.Add(pos);
			}

			// Vertex normal line: 3 floats represent x,y,z
			else if (line.StartsWith("vn ")) {
				var norm = ParseVec3(line.Substring(3));
				normals.Add(norm);
			}

			// Vertex tex coord line: 2 floats represent u,v
			else if (line.StartsWith("vt ")) {
				var uv = ParseVec2(line.Substring(3));
				uvs.Add(uv);
			}

			// Face line: at least 3 vertex entries represent the vertices in a polygon.
			// Each vertex entry will be up to 3 integers separated by /'s. The integers
			// represent the vertex index, uv index, and normal index, respectively.
			else if (line.StartsWith("f ")) {
				Face face = new Face();
				face.vertices = new List<Vertex>();
				face.groupName = currentGroup;
				face.objectName = currentObject;
				face.material = currentMaterial;

				var verts = ParseFace(line.Substring(2), ref positions, ref uvs, ref normals);
				foreach (var vert in verts) {
					vertices.Add(vert);
					face.vertices.Add(vert);
				}

				faces.Add(face);
			} 

			// Material Library line: the file named after "mtllib " is the materials file.
			// It will be opened and parsed for diffuse colors which will be added to the
			// appropriate vertices' information.
			else if (line.StartsWith("mtllib ")) {
				var mtlFileName = line.Substring(7);
				var mtlPath = Path.GetDirectoryName(path);
				mtlPath = Path.Combine(mtlPath, mtlFileName);
				ReadMaterials(mtlPath, ref materials);
			}
			
			// Object name (in OBJ format, the Object is the highest level of grouping)
			else if (line.StartsWith("o ")) currentObject = line.Substring(2);

			// Group name (in OBJ format, Groups are sub-categories within objects)
			else if (line.StartsWith("g ")) currentGroup = line.Substring(2);

			// Material declaration: all following faces will use the material named.
			else if (line.StartsWith("usemtl ")) currentMaterial = line.Substring(7);
		}
	}

	/**
	 * Reads the MTL file at the given path and produces entries in the given materials Dictionary.
	 */
	private static void ReadMaterials(string path, ref Dictionary<string, ObjMaterial> materials) {
		if (!File.Exists(path)) {
			Debug.LogWarning("Could not find the MTL file referenced by this PolyOBJ file. Make sure to import both files! Material information will not be loaded.");
			return;
		}

		string[] lines = File.ReadAllLines(path, Encoding.ASCII);

		string currentName = null;
		ObjMaterial currentMaterial = null;

		foreach (var linein in lines) {
			var line = linein.Trim();

			if (line.StartsWith("newmtl")) {
				if (currentName != null) materials.Add(currentName, currentMaterial);

				currentName = line.Substring(7);
				currentMaterial = new ObjMaterial();
			} else if (line.StartsWith("Kd ")) {
				var color = ParseVec3(line.Substring(3));
				currentMaterial.diffuse = new Color(color.x, color.y, color.z, 1);
			}
		}

		if (currentName != null) materials.Add(currentName, currentMaterial);
	}

	/**
	 * Accepts a string of (at least) 3 space separated floats.
	 * Used for reading vertex positions and normals from the OBJ file.
	 */
	private static Vector3 ParseVec3(string line) {
		string[] parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length < 3) throw new FormatException("Expected 3+ floats separated by ' '!");
		Vector3 result = new Vector3();
		result.x = float.Parse(parts[0]);
		result.y = float.Parse(parts[1]);
		result.z = float.Parse(parts[2]);
		return result;
	}

	/**
	 * Accepts a string of (at least) 2 space separated floats.
	 * Generally used for reading texture coordinates from the OBJ file.
	 * Some OBJ files will store three floats, where the last value is 0.000.
	 */
	private static Vector2 ParseVec2(string line) {
		string[] parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length < 2) throw new FormatException("Expected 2+ floats separated by ' '!");
		Vector2 result = new Vector2();
		result.x = float.Parse(parts[0]);
		result.y = float.Parse(parts[1]);
		return result;
	}

	/**
	 * Accepts a string of at least 3 vertices. 
	 * Each vertex should have a format accepted by ParseVert.
	 */
	private static Vertex[] ParseFace(string line, ref List<Vector3> positions, ref List<Vector2> uvs, ref List<Vector3> normals) {
		string[] verts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		if (verts.Length < 3) throw new FormatException("Expected >3 vertices separated by ' '!");
		var result = new Vertex[verts.Length];
		var i = 0;

		foreach (var vert in verts) {
			result[i++] = ParseVert(vert, ref positions, ref uvs, ref normals);
		}
		return result;
	}

	/**
	 * Accepts a vertex string of the format [Vertex Index]/[UV Index]/[Normal Index].
	 * These / separated integers refer to the values stored in the positions, uvs, and normals
	 * arrays, respectively. The values in the string are indexed from 1 (heathens)
	 */
	private static Vertex ParseVert(string vert, ref List<Vector3> positions, ref List<Vector2> uvs, ref List<Vector3> normals) {
		string[] parts = vert.Split(new char[] { '/' });
		Vertex vertex = new Vertex();

		// Read Vertex index
		if (parts.Length >= 1 && parts[0].Length > 0) {
			var index = int.Parse(parts[0]) - 1;
			if (index >= positions.Count) throw new IndexOutOfRangeException("Vertex Index "+index+" is out of range ("+positions.Count+" available)");
			vertex.position = positions[index];
		} else throw new FormatException("Expected at least one integer referring to position index");

		// Read UV index
		if (parts.Length >= 2 && parts[1].Length > 0) {
			var index = int.Parse(parts[1]) - 1;
			if (index >= uvs.Count) throw new IndexOutOfRangeException("UV Index " + index + " is out of range (" + uvs.Count + " available)");
			vertex.uv = uvs[index];
		}

		// Read Normal index
		if (parts.Length >= 3 && parts[2].Length > 0) {
			var index = int.Parse(parts[2]) - 1;
			if (index >= normals.Count) throw new IndexOutOfRangeException("Normal Index " + index + " is out of range (" + normals.Count + " available)");
			vertex.normal = normals[index];
		}

		return vertex;
	}

	private Mesh BuildMesh(ref List<Vertex> vertices, ref List<Face> faces) {
		// Build all necessary lists from the face and vertex information
		var verts = new List<Vector3>();
		var norms = new List<Vector3>();
		var uvs = new List<Vector2>();
		var colors = new List<Color>();
		var triangles = new List<int>();
		var i = 0;
		foreach (var vertex in vertices) {
			vertex.index = i++;
			verts.Add(vertex.position);
			norms.Add(vertex.normal);
			colors.Add(vertex.color);
			uvs.Add(vertex.uv);
		}
		foreach (var face in faces) {
			triangles.AddRange(face.vertices.ConvertAll((v) => v.index));
		}

		// Build the Mesh object
		Mesh mesh = new Mesh();
		mesh.SetVertices(verts);
		mesh.SetNormals(norms);
		mesh.SetUVs(0, uvs);
		mesh.SetColors(colors);
		mesh.SetTriangles(triangles, 0);

		mesh.name = "Imported Poly OBJ Mesh";

		if (m_GenerateTexCoords) {
			// If Generate Tex Coords is on, then the whole mesh gets built a second time
			// using texture coordinates from Unwrapping.GeneratePerTriangle().
			Vector2[] unwrappedUVs = Unwrapping.GeneratePerTriangleUV(mesh); // Produces 3 tex coords per triangle in the mesh

			// We're going to clear out and rebuild the vertex array
			// according to the new texture coordinates
			foreach (var vertex in vertices) vertex.index = -1;
			vertices.Clear();
			
			// Each vertex will be added back into the vertex array one by one.
			// If two triangles in the same polygon refer to the same vertex but
			// different texture coordinates, then two or more vertices are produced
			// (One for each texture coordinate).
			var triIndex = 0;
			foreach (var face in faces) {
				var numTris = face.vertices.Count / 3;
				for (var tri = 0; tri < numTris; tri++) {
					for (var v = 0; v < 3; v++) {
						var vertex = face.vertices[tri * 3 + v];
						var uv = unwrappedUVs[triIndex * 3 + v];
						
						if (vertex.index == -1) {
							// Vertex is new, add it
							vertex.index = vertices.Count;
							vertex.uv = uv;
							vertices.Add(vertex);
						} else if (vertex.uv != uv) {
							// Vertex is a duplicate with a different texture coordinate
							// Create a duplicate and give it a new texture coordinate.
							Vertex split = new Vertex(vertex);
							split.index = vertices.Count;
							split.uv = uv;
							vertices.Add(split);

							// Write the new vertex into this face's vertices array
							face.vertices[tri * 3 + v] = split;
						}
					}
					triIndex++;
				}
			}

			// Rebuild the necessary lists
			verts.Clear();
			norms.Clear();
			uvs.Clear();
			colors.Clear();
			foreach (var vertex in vertices) {
				verts.Add(vertex.position);
				norms.Add(vertex.normal);
				colors.Add(vertex.color);
				uvs.Add(vertex.uv);
			}
			triangles.Clear();
			foreach (var face in faces) {
				triangles.AddRange(face.vertices.ConvertAll((v) => v.index));
			}

			// Rebuild the Mesh object
			mesh.Clear();
			mesh.SetVertices(verts);
			mesh.SetNormals(norms);
			mesh.SetUVs(0, uvs);
			mesh.SetColors(colors);
			mesh.SetTriangles(triangles, 0);
		}

		return mesh;
	}

	/**
	 * Represents all data for a single vertex.
	 */
	private class Vertex {
		public int index;
		public Vector3 position;
		public Vector3 normal;
		public Vector2 uv;
		public Color color;

		public Vertex() {}

		public Vertex(Vertex copyFrom) {
			this.index = copyFrom.index;
			this.position = copyFrom.position;
			this.normal = copyFrom.normal;
			this.uv = copyFrom.uv;
			this.color = copyFrom.color;
		}
	}

	/**
	 * Represents a poly N-gon from the OBJ file.
	 */
	private class Face {
		public string objectName;
		public string groupName;
		public string material;
		public List<Vertex> vertices;
		private Boolean triangulated = false;

		public void Triangulate() {
			if (triangulated) return;
			triangulated = true;

			if (vertices.Count == 3) return;

			var newList = new List<Vertex>();
			for (var i = 2; i < vertices.Count; i++) {
				newList.Add(vertices[0]);
				newList.Add(vertices[i-1]);
				newList.Add(vertices[i]);
			}
			this.vertices = newList;
		}
	}

	/**
	 * Stores information for each material in the MTL file.
	 * Currently supports only diffuse color.
	 */
	private class ObjMaterial {
		public Color diffuse;
	}

}
