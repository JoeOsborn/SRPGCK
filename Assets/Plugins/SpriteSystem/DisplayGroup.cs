using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DisplayGroup : DisplayObject {

	private List<GameObject> _objects;
	
	// Use this for initialization
	void Awake () {
		InitGroup();
	}
	
	public void InitGroup(){
		_objects = new List<GameObject>();
	}
	
	public void AddObject( GameObject obj ){
		DisplayObject dObj = GetDisplayObjectComponent(obj);
		if(dObj != null){
			if(dObj.parent != null){
				dObj.parent.RemoveObject(obj);
			}
			dObj.parent = this;
			_objects.Add(obj);
		} else {
			foreach(Transform child in obj.transform){
				AddObject(child.gameObject);
			}
		}
	}
	
	protected DisplayObject GetDisplayObjectComponent(GameObject obj){
		DisplayObject ret  = obj.GetComponent(typeof(Billboard)) as DisplayObject;
		if(ret == null){
			ret = obj.GetComponent(typeof(Sprite)) as DisplayObject;
		}
		if(ret == null){
			ret = obj.GetComponent(typeof(DisplayGroup)) as DisplayObject;
		}
		
		return ret;
	}
	
	public void RemoveObject( GameObject obj ){
		if(_objects.Remove(obj)){
			DisplayObject dObj = GetDisplayObjectComponent(obj);
			dObj.parent = null;
		} else {
			foreach(Transform child in obj.transform){
				RemoveObject(child.gameObject);
			}
		}
	}
	
	public void CleanGroup(DisplayGroup gr){
		List<GameObject> toRemove = new List<GameObject>();
		for(int i = 0; i < _objects.Count; i++){
			if(_objects[i] == null){
				toRemove.Add(_objects[i]);
			} else {
				DisplayGroup objGroup = _objects[i].GetComponent(typeof(DisplayGroup)) as DisplayGroup;
				if(objGroup != null){
					CleanGroup(objGroup);
				}
			}
		}
		for(int i = 0; i < toRemove.Count; i++){
			_objects.Remove(toRemove[i]);
		}
	}
	
	public List<GameObject> objects{
		get {
			return _objects;
		}
	}
	
	public void BringForward( GameObject obj ){
		int ind = _objects.IndexOf(obj);
		if(ind != -1 && ind + 1 != _objects.Count){
			GameObject tmp = _objects[ind + 1];
			_objects[ind + 1] = _objects[ind];
			_objects[ind] = tmp;
		}
	}
	
	public void PushBack( GameObject obj) {
		int ind = _objects.IndexOf(obj);
		if(ind != -1 && ind != 0){
			GameObject tmp = _objects[ind - 1];
			_objects[ind - 1] = _objects[ind];
			_objects[ind] = tmp;
		}
	}
	
	public void PushToBack( GameObject obj ){
		if(_objects.Remove(obj)){
			_objects.Insert(_objects.Count, obj);
		}
	}
	
	public void BringToFront( GameObject obj ){
		if(_objects.Remove(obj)){
			_objects.Insert(0, obj);
		}
	}
			
	
}