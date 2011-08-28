using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

class TileBounds {
	Bounds b = new Bounds();
	public Vector3 center {
		get { return b.center; }
		set { b.center = value; }
	}
	public Vector3 size {
		get { return b.size; }
		set { b.size = value; }
	}
	public bool IntersectRay(Ray r, out float distance) {
		return b.IntersectRay(r, out distance);
	}
}

[CustomEditor(typeof(Map))]

public class MapEditor : Editor {
	enum EditMode {
		AddRemove=0,
		Reshape=1,
		Paint=2
	};
	bool draggingInMap=false;
	static int editZ=0;
	static bool showDimensions=true;
	static bool editModeChanged=true;
	static EditMode editMode=EditMode.AddRemove;
	Vector2 lastSize=Vector2.zero;
	float lastSideLength=0, lastH=0;
	Vector3 lastPos=Vector3.zero;
	
	static Vector2 specScrollPos = Vector2.zero;
	static Texture2D specPlaceholderTexture=null;
	static int specSelectedSpec=0;
	
	List<TileBounds> tiles=new List<TileBounds>();
	
	void RegisterUndo(string label) {
		//close, but no cigar:
		/*Map m = ((Map)target);
		Undo.RegisterUndo(EditorUtility.CollectDeepHierarchy(new Object[]{
			m.gameObject, 
			m.gameObject.GetComponent<MeshFilter>(),
			m.gameObject.GetComponent<MeshFilter>().sharedMesh}
		), label);*/
		Undo.RegisterSceneUndo(label);
	}
	
	void GUIUpdateTexture(Texture2D tex, int idx, Dictionary<string, int> names) {
		Map m = (Map)target;
		string name = "MAP_TEX_"+idx;
		names[name] = idx;
		GUI.SetNextControlName(name);
		Texture2D nextTex = (Texture2D)EditorGUILayout.ObjectField(
			tex == specPlaceholderTexture ? null : tex, 
			typeof(Texture2D), 
			false,
			GUILayout.Height(64),
			GUILayout.Width(64)
		);
		if(tex != nextTex) {
			if(tex == specPlaceholderTexture && nextTex == null) {
				//ignore, no real change
			} else {
				//are we on the bonus item?
				if(idx == m.TileSpecCount) {
					//if so, add a new spec
					RegisterUndo("Add Tile Spec");
					m.AddTileSpec(nextTex);
				} else {
					//otherwise, just update
					RegisterUndo("Change Tile Spec");
					m.UpdateTileSpecAt(specSelectedSpec, nextTex);
				}
				string path = AssetDatabase.GetAssetPath(nextTex); 
        TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter; 
				textureImporter.textureType = TextureImporterType.Advanced;
				textureImporter.anisoLevel = 1;
				textureImporter.filterMode = FilterMode.Bilinear;
				textureImporter.maxTextureSize = 1024;
				textureImporter.textureFormat = TextureImporterFormat.AutomaticCompressed;
        textureImporter.mipmapEnabled = false;    
        textureImporter.isReadable = true;
				AssetDatabase.ImportAsset(path);
			}
		}	
	}
	
	public override void OnInspectorGUI () {
		Map m = (Map)target;
		showDimensions = EditorGUILayout.Foldout(showDimensions, "Map Dimensions");
		if(showDimensions) {
			EditorGUI.indentLevel++;
			EditorGUILayout.BeginHorizontal();
			float nextXSz = EditorGUILayout.FloatField("Width", m.size.x);
			float nextYSz = EditorGUILayout.FloatField("Height", m.size.y);
			Vector2 nextSz = new Vector2(nextXSz, nextYSz);
			if(nextSz != m.size) {
				RegisterUndo("Map Size Change");
				m.size = nextSz;
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			float nextLength = EditorGUILayout.FloatField("Side Length", m.sideLength);
			if(nextLength != m.sideLength) {
				RegisterUndo("Map Side Length Change");
				m.sideLength = nextLength;
			}
			float nextHeight = EditorGUILayout.FloatField("Tile Height", m.tileHeight);
			if(nextHeight != m.tileHeight) {
				RegisterUndo("Map Side Height Change");
				m.tileHeight = nextHeight;
			}
			EditorGUILayout.EndHorizontal();
			EditorGUI.indentLevel--;
		}

		EditorGUILayout.Separator();

		int nextZ = EditorGUILayout.IntSlider("Edit Z", editZ, 0, 20);
		if(nextZ != editZ) {
//			RegisterUndo(target, "Map Height Selection");
			editZ = nextZ;
		}

		EditorGUILayout.Separator();

		EditorGUILayout.BeginHorizontal();
		GUIContent[] toolbarOptions = new GUIContent[3];
		toolbarOptions[(int)EditMode.AddRemove] = new GUIContent("Add/Remove Tiles");
		toolbarOptions[(int)EditMode.Reshape] = new GUIContent("Reshape Tiles");
		toolbarOptions[(int)EditMode.Paint] = new GUIContent("Paint Tiles");
		EditMode nextMode = (EditMode)GUILayout.Toolbar((int)editMode, toolbarOptions);
		EditorGUILayout.EndHorizontal();
		if(nextMode != editMode) {
			editMode = nextMode;
			editModeChanged = true;
		}
		if(editMode == EditMode.Paint) {
			EditorGUILayout.Separator();
			specScrollPos = EditorGUILayout.BeginScrollView(specScrollPos, true, false, GUILayout.Height(80));
			EditorGUILayout.BeginHorizontal(GUILayout.Height(64));
			//show list of texture specs controls
			//a texture spec is a rectangular Texture2D with some metadata
			//only for now, there's no metadata.
			//this metadata gets stored in the map object, which composes the
			//textures into an atlas. uvs are mapped from this atlas.
			//changes to the texture spec list force recreation of the mesh.
			//texture spec info for each face is stored as part of the MapTile class.
			Texture2D[] textures = new Texture2D[m.TileSpecCount+1];
			if(specPlaceholderTexture == null) {
				specPlaceholderTexture = EditorGUIUtility.LoadRequired("SpecPlaceholder.png") as Texture2D;
			}
			for(int i = 0; i < m.TileSpecCount; i++) {
				Texture2D specTex = m.TileSpecAt(i).texture;
				if(specTex != null) {
					textures[i] = specTex;
				} else {
					textures[i] = specPlaceholderTexture;
				}
			}
			textures[m.TileSpecCount] = specPlaceholderTexture;
			var names = new Dictionary<string, int>();
			for(int i = 0; i < textures.Length; i++) {
				Texture2D tex = textures[i];
				GUIUpdateTexture(tex, i, names);
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndScrollView();
			string n = GUI.GetNameOfFocusedControl();
			if(names.ContainsKey(n)) {
				if(names[n] < m.TileSpecCount) {
					specSelectedSpec = names[n];
				}
			}
			EditorGUI.indentLevel++;
			//now, show the parameters for this spec
			bool oldEnabled = GUI.enabled;
			GUI.enabled = specSelectedSpec < m.TileSpecCount;
			if(GUILayout.Button("Delete Tile")) {
				RegisterUndo("Delete Tile Spec");
				m.RemoveTileSpecAt(specSelectedSpec);
				while(specSelectedSpec >= m.TileSpecCount) {
					specSelectedSpec--;
					if(specSelectedSpec < 0) { specSelectedSpec = 0; break; }
				}
			}
			GUI.enabled = oldEnabled;
			EditorGUI.indentLevel--;
			EditorGUILayout.Separator();				
		}

		if(GUI.changed) {
			EditorUtility.SetDirty(target);
		}		
	}

	void SetupAddRemoveTileBounds(TileBounds b, int idx) {
		Map m = (Map)target;
		float s = m.sideLength;
		float h = m.tileHeight;
		int y = idx/(int)m.size.x;
		int x = idx-(y*(int)m.size.x);
		int z = editZ;
		int zMaxHeight = 1;
		MapTile t = m.TileAt(x,y,z);
		if(t != null) {
			z = t.z;
			zMaxHeight = t.maxHeight;
		}
		Vector3 hereTop = m.transform.position + new Vector3(x*s, (z+zMaxHeight-1)*h, y*s);
		b.center = hereTop + new Vector3(0, -(zMaxHeight*h)/2, 0);
		b.size = new Vector3(s, h*zMaxHeight, s);
		if(idx < tiles.Count) {
			tiles[idx] = b;
		} else {
			tiles.Add(b);
		}
	}

	void SetupPaintTileBounds(TileBounds b, int idx) {
		Map m = (Map)target;
		float s = m.sideLength;
		float h = m.tileHeight;
		int y = idx/(int)m.size.x;
		int x = idx-(y*(int)m.size.x);
		int z = editZ;
		if(!m.HasTileAt(x,y,z)) {
			//just go ahead and give up
			b.center = Vector3.zero;
			b.size = Vector3.zero;
		} else {
			int zMaxHeight = 1;
			MapTile t = m.TileAt(x,y,z);
			if(t != null) {
				z = t.z;
				zMaxHeight = t.maxHeight;
			}
			Vector3 hereTop = m.transform.position + new Vector3(x*s, (z+zMaxHeight-1)*h, y*s);
			b.center = hereTop + new Vector3(0, -(zMaxHeight*h)/2, 0);
			b.size = new Vector3(s, h*zMaxHeight, s);
		}
		if(idx < tiles.Count) {
			tiles[idx] = b;
		} else {
			tiles.Add(b);
		}
	}
	
	void AddIsoTileAt(int idx) {
		Map m = (Map)target;	
		int ny = idx/(int)m.size.x;
		int nx = idx-(ny*(int)m.size.x);
/*		int nz = tiles[idx].z;*/
		int nz = editZ;
		RegisterUndo("Add Tile");
		m.AddIsoTileAt(nx, ny, nz);
		EditorUtility.SetDirty(target);
	}
	
	void RemoveIsoTileAt(int idx) {
		Map m = (Map)target;	
		int ny = idx/(int)m.size.x;
		int nx = idx-(ny*(int)m.size.x);
/*		int nz = tiles[idx].z;*/
		int nz = editZ;
		RegisterUndo("Remove Tile");
		m.RemoveIsoTileAt(nx, ny, nz);
		EditorUtility.SetDirty(target);
	}
	
	void SetTileSpecAt(int idx, Map.Neighbors collidedFace) {
		Map m = (Map)target;
		int ny = idx/(int)m.size.x;
		int nx = idx-(ny*(int)m.size.x);
		int nz = editZ;
		RegisterUndo("Paint Tile");
		m.SetTileSpecOnTileAt(specSelectedSpec, nx, ny, nz, collidedFace);	
		EditorUtility.SetDirty(target);
	}
	
	float modf(float f) {
		return f - Mathf.Floor(f);
	}
	
	//FIXME: if a custom prop, don't permit this kind of tomfoolery
	void AdjustIsoHeightAt(int idx, Map.Neighbors collidedFace, Vector3 wpos, int dh) {
		Map m = (Map)target;
		int ny = idx/(int)m.size.x;
		int nx = idx-(ny*(int)m.size.x);
		int nz = editZ;
		MapTile t = m.TileAt(nx,ny,nz);
		if(t==null) { return; }
		Vector3 tpos = m.InverseTransformPointWorld(wpos);
		Map.Neighbors side = Map.Neighbors.None;
		//get the fractional part of each coord
/*		Debug.Log("adjust height at "+nx+","+ny+","+nz+", tpos "+tpos+", wpos "+wpos);*/
		tpos.x = modf(tpos.x);
		tpos.y = modf(tpos.y);
	  //tpos.z = modf(tpos.z);
		bool top = tpos.z > t.z + (t.maxHeight-t.z)/2;
/*		Debug.Log("tpos frac "+tpos.x+", "+tpos.y+", "+tpos.z);*/
		if((collidedFace & Map.Neighbors.FrontLeft) != 0) {
			side = Map.Neighbors.FrontLeft;
		} else if((collidedFace & Map.Neighbors.FrontRight) != 0) {
			side = Map.Neighbors.FrontRight;
		} else if((collidedFace & Map.Neighbors.BackRight) != 0) {
			side = Map.Neighbors.BackRight;
		} else if((collidedFace & Map.Neighbors.BackLeft) != 0) {
			side = Map.Neighbors.BackLeft;
		} else { //top or bottom
			top = (collidedFace & Map.Neighbors.Top) != 0;
			if(tpos.x < 0.3 && tpos.y < 0.75 && tpos.y > 0.25) {
				side = Map.Neighbors.FrontLeft;
			} else if(tpos.x > 0.3 && tpos.y < 0.75 && tpos.y > 0.25) {
				side = Map.Neighbors.BackRight;
			} else if(tpos.y < 0.3 && tpos.x < 0.75 && tpos.x > 0.25) {
				side = Map.Neighbors.FrontRight;
			} else if(tpos.y > 0.3 && tpos.x < 0.75 && tpos.x > 0.25) {
				side = Map.Neighbors.BackLeft;
			}
		}
		RegisterUndo("Adjust Tile Height");
		m.AdjustHeightOnSidesOfTile(nx, ny, nz, dh, side, top);
		EditorUtility.SetDirty(target);
	}
	
	bool Roughly(float a, float b) {
		const float roughEps = 0.05f;
		return Mathf.Abs(a-b) < roughEps;
	}
	
	Map.Neighbors CollidedFaceAt(Vector3 cp, TileBounds b) {
		Vector3 collisionPoint = cp - b.center;				
		Vector3 sz = b.size;
		Map.Neighbors collidedFace = Map.Neighbors.None;
		if(Roughly(collisionPoint.x, sz.x/2)) {
			collidedFace = Map.Neighbors.BackRight;
		} else if(Roughly(collisionPoint.x, -sz.x/2)) {
			collidedFace = Map.Neighbors.FrontLeft;
		} else if(Roughly(collisionPoint.z, sz.z/2)) {
			collidedFace = Map.Neighbors.BackLeft;					
		} else if(Roughly(collisionPoint.z, -sz.z/2)) {
			collidedFace = Map.Neighbors.FrontRight;
		} else if(collisionPoint.y < 0) {
			collidedFace = Map.Neighbors.Bottom;
		} else {
			collidedFace = Map.Neighbors.Top;
		}
		return collidedFace;
	}
	
	public void OnSceneGUI() {
		Event e = Event.current;
		//draw a grid at the lowest available levels for the map
		Map m = (Map)target;
		EditorUtility.SetSelectedWireframeHidden(m.GetComponent<MeshRenderer>(), true);
		Vector2 sz = m.size;
		float s = m.sideLength;
		Vector3 pos = m.transform.position;
		float h = m.tileHeight;
		if(tiles == null) {
			tiles = new List<TileBounds>();
		}
		Vector3 here = pos + new Vector3(-s/2, -h, -s/2);
		Vector3[] basePlane = new Vector3[]{
			here+new Vector3(-0.1f,-0.1f,-0.1f),
			here+new Vector3(-0.1f,-0.1f,s*sz.y+0.2f),
			here+new Vector3(s*sz.x+0.2f,-0.1f,s*sz.y+0.2f),
			here+new Vector3(s*sz.x+0.2f,-0.1f,-0.1f)
		};
		Handles.DrawSolidRectangleWithOutline(basePlane, 
		                                      Color.clear, 
		                                      Color.black);
		Vector2 mpos = e.mousePosition;
		Ray ray = HandleUtility.GUIPointToWorldRay(mpos);
		int closestIdx = -1;
		float closestDistance = Mathf.Infinity;
		Vector3 collisionPoint = Vector3.zero;
		Map.Neighbors collidedFace = Map.Neighbors.None;
		bool dimsChanged = 
			sz != lastSize || 
	   	s != lastSideLength || 
	   	pos != lastPos || 
	   	h != lastH;
		if(editMode == EditMode.AddRemove) {
			int neededBoxes = (int)(sz.x*sz.y);
			if(dimsChanged || 
				 editModeChanged ||
		   	 tiles.Count <= neededBoxes) {
				for(int i = 0; i < neededBoxes; i++) {
					SetupAddRemoveTileBounds(new TileBounds(), i);
				}
			}
			for(int i = 0; i < neededBoxes; i++) {
				TileBounds b = tiles[i];
				if(b.center == Vector3.zero && b.size == Vector3.zero) {
					continue;
				}
				float distance;
				if(b.IntersectRay(ray, out distance) && distance < closestDistance) {
					closestDistance = distance;
					closestIdx = i;
					//find the ray's collision point (ray.origin+ray.direction*closestDistance),
					collisionPoint = ray.origin+ray.direction*closestDistance;
					//now translate it to the box position by subtracting the box's center (may need to subtract `new Vector3(0, -h/2, 0)`) as well
					collidedFace = CollidedFaceAt(collisionPoint, b);
				}
				int y = i/(int)m.size.x;
				int x = i-(y*(int)m.size.x);
				int z = editZ;
				if(m.HasTileAt(x,y,z)) {
					//skip drawing, but you can still opt-click to delete
				} else {
					DrawWireBounds(b.center, b.size, Color.magenta);
				}
			}
			if(closestIdx != -1) {
				TileBounds closest = tiles[closestIdx];
				DrawWireBounds(closest.center, closest.size, Color.green);
			}
		} else if(editMode == EditMode.Reshape) {
			
		} else if(editMode == EditMode.Paint) {
			int neededBoxes = (int)(sz.x*sz.y);
			if(dimsChanged || 
				 editModeChanged ||
			 	 tiles.Count <= neededBoxes) {
				for(int i = 0; i < neededBoxes; i++) {
					SetupPaintTileBounds(new TileBounds(), i);
				}
			}
			for(int i = 0; i < neededBoxes; i++) {
				TileBounds b = tiles[i];
				if(b.center == Vector3.zero && b.size == Vector3.zero) {
					continue;
				}
				float distance;
				if(b.IntersectRay(ray, out distance) && distance < closestDistance) {
					closestDistance = distance;
					closestIdx = i;
					//find the ray's collision point (ray.origin+ray.direction*closestDistance),
					collisionPoint = ray.origin+ray.direction*closestDistance;
				}
				DrawWireBounds(b.center, b.size, Color.white);
			}
			if(closestIdx != -1) {
				TileBounds closest = tiles[closestIdx];
				//decide what face it's on by comparing its x,y,z against the bounding box's minima and maxima
				collidedFace = CollidedFaceAt(collisionPoint, closest);
				DrawWireBounds(closest.center, closest.size, Color.green, collidedFace);
			}
		}
		
		int controlID = GUIUtility.GetControlID(FocusType.Passive);
		if(e.type == EventType.mouseUp) {
			draggingInMap = false;
		}
		if(e.type == EventType.mouseDown || (e.type == EventType.mouseDrag && draggingInMap)) {
			if(editMode == EditMode.AddRemove) {
				if(closestIdx != -1) {
					if(e.alt && e.shift) {
						AdjustIsoHeightAt(closestIdx, collidedFace, collisionPoint, -1);
						draggingInMap = false;
					} else if(e.shift) {
						AdjustIsoHeightAt(closestIdx, collidedFace, collisionPoint, 1);
						draggingInMap = false;
					} else if(e.alt) {
						RemoveIsoTileAt(closestIdx);
						draggingInMap = true;
					} else {
						AddIsoTileAt(closestIdx);
						draggingInMap = true;
					}
					e.Use();
				}
			} else if(editMode == EditMode.Reshape) {
					
			} else if(editMode == EditMode.Paint) {
				if(collidedFace != Map.Neighbors.None) {
					SetTileSpecAt(closestIdx, collidedFace);
					draggingInMap = true;
					e.Use();
				}
			}
		} else if(e.type == EventType.layout) {
			HandleUtility.AddDefaultControl(controlID);
		} else if(e.type == EventType.mouseMove) {
			e.Use();
		} else if(e.type == EventType.KeyDown) {
			int nextZ = editZ;
			if(e.keyCode == KeyCode.UpArrow) {
				if(editZ < 20-1) { nextZ++; }
				e.Use();
			} else if(e.keyCode == KeyCode.DownArrow) {
				if(editZ > 0) { nextZ--; }
				e.Use();
			}
			if(nextZ != editZ) {
				//RegisterUndo(target, "Map Height Selection");
				editZ = nextZ;
			}
		}
		lastSize = sz;
		lastSideLength = s;
		lastPos = pos;
		lastH = h;
		editModeChanged = false;
		this.Repaint();
  }

	public static void DrawWireBounds(Vector3 center, Vector3 size, Color lineColor) {
		DrawWireBounds(center, size, lineColor, Map.Neighbors.All);
	}
	
	public static void DrawWireBounds(Vector3 center, Vector3 size, Color lineColor, Map.Neighbors sides) {
		float bh = size.y-0.2f;
		float bs = size.x-0.2f;
		Vector3 here = center + new Vector3(-bs/2+0.1f, -bh/2+0.1f, -bs/2+0.1f);
		if((sides & Map.Neighbors.FrontLeft) != 0) {
			Vector3[] frontLeft = new Vector3[]{
				here+new Vector3(0,0,bs),
				here+new Vector3(0,bh,bs),
				here+new Vector3(0,bh,0),
				here+new Vector3(0,0,0)
			};
			Handles.DrawSolidRectangleWithOutline(frontLeft, Color.clear, lineColor);
		}
		if((sides & Map.Neighbors.FrontRight) != 0) {
			Vector3[] frontRight = new Vector3[]{
				here+new Vector3(0,0,0),
				here+new Vector3(bs,0,0),
				here+new Vector3(bs,bh,0),
				here+new Vector3(0,bh,0),
			};
			Handles.DrawSolidRectangleWithOutline(frontRight, Color.clear, lineColor);
		}
		if((sides & Map.Neighbors.Top) != 0) {
			Vector3[] top = new Vector3[]{
				here+new Vector3(bs,bh,0),
				here+new Vector3(bs,bh,bs),
				here+new Vector3(0,bh,bs),
				here+new Vector3(0,bh,0),
			};
			Handles.DrawSolidRectangleWithOutline(top, Color.clear, lineColor);
		}
		if((sides & Map.Neighbors.Bottom) != 0) {
			Vector3[] bottom = new Vector3[]{
				here+new Vector3(bs,0,0),
				here+new Vector3(bs,0,bs),
				here+new Vector3(0,0,bs),
				here+new Vector3(0,0,0),
			};
			Handles.DrawSolidRectangleWithOutline(bottom, Color.clear, lineColor);
		}
		if((sides & Map.Neighbors.BackLeft) != 0) {
			Vector3[] backLeft = new Vector3[]{
				here+new Vector3(0,0,bs),
				here+new Vector3(bs,0,bs),
				here+new Vector3(bs,bh,bs),
				here+new Vector3(0,bh,bs),
			};
			Handles.DrawSolidRectangleWithOutline(backLeft, Color.clear, lineColor);
		}
		if((sides & Map.Neighbors.BackRight) != 0) {
			Vector3[] backRight = new Vector3[]{
				here+new Vector3(bs,bh,bs),
				here+new Vector3(bs,0,bs),
				here+new Vector3(bs,0,0),
				here+new Vector3(bs,bh,0)										
			};
			Handles.DrawSolidRectangleWithOutline(backRight, Color.clear, lineColor);
		}
	}
}
