using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Billboard : MonoBehaviour
{
	
	//static private float UNIT_WIDTH = 1;
	//static private float UNIT_HEIGHT = 1;
	
	public Texture2D atlas;
	public Material spriteSheet;
	public Material editorMaterial;
	public Mesh editorMesh;
	public Mesh currentMesh;
	public int columns;
	public int rows;
	public float width;
	public float height;
	public Rect areaRect;
	public bool generateGeometry;
	public bool repeatTexture;
	
	//private Rect sheetRect;
	public Rect transRect;

	void Start(){
		//set material
		
		//sheetRect = new Rect(0,0,spriteSheet.GetTexture("_MainTex").width, spriteSheet.GetTexture("_MainTex").height);
		Rect zeroRect = new Rect(0,0,0,0);
		if(areaRect == zeroRect){
			areaRect = new Rect(0,0,atlas.width, atlas.height);
		}
		
		currentMesh = (GetComponent(typeof(MeshFilter)) as MeshFilter).mesh;
		RedefineMesh();
		
		spriteSheet = renderer.material;
		//renderer.material
		
		SetupMaterial();
	}

	
	public void RedefineMesh(){
		
		
		float unitTexWidth = 1;
		float unitTexHeight = areaRect.height / areaRect.width;
		if(GetComponent<Sprite>() != null && GetComponent<Sprite>().frameWidth != 0){
			unitTexHeight = (float) GetComponent<Sprite>().frameHeight / (float) GetComponent<Sprite>().frameWidth;
		}
		
		//Debug.Log("width: "+unitTexWidth +", height: "+unitTexHeight);
		
		Vector3[] vertices;
		Vector2[] uv;
		int[] triangles;
		
		
		
		if(repeatTexture){
			
			//int numColumns = Mathf.CeilToInt(unitAWidth / unitTexWidth);
			//int numRows = Mathf.CeilToInt(unitAHeight / unitTexHeight);
			
			vertices = new Vector3[columns * rows * 4];
			uv = new Vector2[columns * rows * 4];
			triangles = new int[columns * rows * 6];
			
			
			float cx = -unitTexWidth / 2.0f;
			float cy = -unitTexHeight / 2.0f;
			// generate repeated texture
			for(int i = 0; i < rows; i++){
				for(int j = 0; j < columns; j++){
					//Debug.Log(i + " " + j);
					int ind = (i * columns + j)*4;
					int tInd = (i * columns +j)*6; 
					float nx = j * unitTexWidth;
					float ny = i * unitTexHeight;
					float fx = (j+1) * unitTexWidth;
					float fy = (i+1) * unitTexHeight;
					float uvx = 1.0f;
					float uvy = 1.0f;
					//Debug.Log(nx +" "+ny+" "+fx+" "+fy);
					vertices[ind] = new Vector3(nx + cx, ny + cy, 0);
					vertices[ind + 1] = new Vector3(fx + cx, ny+cy, 0);
					vertices[ind + 2] = new Vector3(nx + cx, fy+cy, 0);
					vertices[ind + 3] = new Vector3(fx + cx, fy+cy, 0);
					
					uv[ind] = new Vector2(0,0);
					uv[ind+1] = new Vector2(uvx,0);
					uv[ind+2] = new Vector2(0,uvy);
					uv[ind+3] = new Vector2(uvx,uvy);
					
					triangles[tInd] = ind;
					triangles[tInd + 1] = ind + 2;
					triangles[tInd + 2] = ind + 1;
					triangles[tInd + 3] = ind + 2;
					triangles[tInd + 4] = ind + 3;
					triangles[tInd + 5] = ind + 1;
					
				}
			}
		} else {
			//Debug.Log("no repeat");
			float uw = unitTexWidth / 2.0f;
			float uh = unitTexHeight / 2.0f;
			
			vertices = new Vector3[4];
			uv = new Vector2[4];
			triangles = new int[6];
			
			vertices[0] = new Vector3(-uw,-uh,0);
			vertices[1] = new Vector3(uw,-uh,0);
			vertices[2] = new Vector3(-uw, uh, 0);
			vertices[3] = new Vector3(uw,uh,0);
			//vertices = {new Vector3(-uw,-uh,0), new Vector3(uw,-uh,0), new Vector3(-uw, uh, 0), new Vector3(uw,uh,0)};
			uv[0] = new Vector2(0,0);
			uv[1] = new Vector2(1,0);
			uv[2] = new Vector2(0,1);
			uv[3] = new Vector2(1,1);
			//uv = { new Vector2(0,0), new Vector2(1,0), new Vector2(0,1), new Vector2(1,1) };
			triangles[0] = 0;
			triangles[1] = 2;
			triangles[2] = 1;
			triangles[3] = 2;
			triangles[4] = 3;
			triangles[5] = 1;
			
			//triangles = { 0, 2, 1, 2, 3, 1 };
		}
		
		if(GetComponent(typeof(MeshFilter)) == null){
			gameObject.AddComponent(typeof(MeshFilter));
		}
		if(GetComponent(typeof(MeshRenderer)) == null){
			gameObject.AddComponent(typeof(MeshRenderer));
		}
		
		//Mesh mesh = (GetComponent(typeof(MeshFilter)) as MeshFilter).mesh;
		
		
		currentMesh.vertices = vertices;
		currentMesh.uv = uv;
		
		currentMesh.triangles = triangles;
		
		currentMesh.RecalculateNormals();
		
	}
	
	public void SetupMaterial(){
		
		transRect = new Rect(areaRect.x / atlas.width, areaRect.y / atlas.height, areaRect.width / atlas.width, areaRect.height / atlas.height);
		
		spriteSheet.SetTextureOffset("_MainTex", new Vector2(transRect.x, 1 - transRect.y - transRect.height));
		spriteSheet.SetTextureScale("_MainTex", new Vector2(transRect.width, transRect.height));

	} 
	
	
}


