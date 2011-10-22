using UnityEngine;
using System;
using System.Collections.Generic;

public class DisplayGraph2D : DisplayGroup {
	
	public GameObject displayGroupType;
	public int farDepth = 1000;
	
	private int nextDepth;
	private Dictionary<string, DisplayGroup> _definedLayers;
	
	void Awake(){
		InitGroup();
		AddDefinedLayers();
		//TraceGraph();
	}
	
	void Update(){
		//CleanGroup(this);
		nextDepth = farDepth;
		SetDepth(gameObject);
	}
	
	void SetDepth(GameObject obj){
		DisplayGroup group = obj.GetComponent(typeof(DisplayGroup)) as DisplayGroup;
		//Debug.Log(group);
		if(group != null){
			for(int i = 0; i < group.objects.Count; i++){
				SetDepth(group.objects[i]);
			}
		} else {
			Vector3 tmp = obj.transform.position;
			tmp.z = nextDepth;
			obj.transform.position = tmp;
			nextDepth--;
		}
	}
	

	void AddDefinedLayers(){
		_definedLayers = new Dictionary<string, DisplayGroup>();
		GameObject[] layers = GameObject.FindGameObjectsWithTag("Layer");
		if(layers.Length > 0){
			Array.Sort(layers, delegate(GameObject g1, GameObject g2) {
				return (g1.GetComponent(typeof(Layer)) as Layer).depth.CompareTo((g2.GetComponent(typeof(Layer)) as Layer).depth);
			});
			
			Layer layer;
			for(int i = 0; i < layers.Length; i++){
				layer = layers[i].GetComponent(typeof(Layer)) as Layer;
				GameObject grObj = Instantiate(displayGroupType, new Vector3(0,0,0), Quaternion.identity) as GameObject;
				DisplayGroup gr = grObj.GetComponent(typeof(DisplayGroup)) as DisplayGroup;
				for(int j = 0; j < layer.objects.Length; j++){
					gr.AddObject(layer.objects[j]);
				}
				AddObject(grObj);
				_definedLayers.Add(layers[i].name,gr);
			}
		} else {
			UnityEngine.Object[] objects = Resources.FindObjectsOfTypeAll(typeof(DisplayObject));
			for(int i = 0; i < objects.Length; i++){
				DisplayObject d = objects[i] as DisplayObject;
				AddObject(d.gameObject);
			}
		}
		
	}
	
	void MoveObjectToLayer(string layer, GameObject obj){
		DisplayGroup gr = _definedLayers[layer];
		gr.AddObject(obj);
	}
	
	void RemoveObjectFromGraph(GameObject obj){
		foreach( KeyValuePair<string, DisplayGroup> kvp in _definedLayers){
			kvp.Value.RemoveObject(obj);
		}
	}
	
	void TraceGraph(){
		Debug.Log("Tracing - DisplayGraph2D");
		Debug.Log(objects.Count);
		for(int i = 0; i < objects.Count; i++){
			TraceGraphObj(objects[i],1);
		}
	}
	
	void TraceGraphObj(GameObject obj, int tabDepth){
		string dstring = "";
		for(int i = 0; i < tabDepth; i++){
			dstring += "\t";
		}
		DisplayObject dObj = GetDisplayObjectComponent(obj);
		DisplayGroup gr = dObj as DisplayGroup;
		if(gr != null){
			dstring += "DisplayGroup";
			Debug.Log(dstring);
			for(int i = 0; i < gr.objects.Count; i++){
				TraceGraphObj(gr.objects[i], tabDepth+1);
			}
		} else {
			dstring += obj.name;
			Debug.Log(dstring);
		}
	}
			
}
