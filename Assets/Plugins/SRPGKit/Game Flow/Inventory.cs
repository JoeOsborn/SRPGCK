using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;

[AddComponentMenu("SRPGCK/Item/Inventory")]
public class Inventory : MonoBehaviour {
	Formulae fdb { get {
		Arbiter arb = transform.parent.GetComponent<Arbiter>();
		if(arb != null) { return arb.fdb; }
		if(character != null) { return character.fdb; }
		return Formulae.DefaultFormulae;
	} }

	Character _character;
	Character character { get {
		if(_character == null) {
			Transform t = transform;
			while(t != null) {
				_character = t.GetComponent<Character>();
				if(_character != null)
					break;
				t = t.parent;
			}
		}
		return _character;
	} }

	public bool limitedCapacity=false;
	public Formula capacityF;
	public int capacity { get {
		return capacityF != null ? (int)capacityF.GetCharacterValue(fdb, character) : 20;
	} }

	public bool limitedStacks=false;
	public Formula stackLimitF;
	public int stackLimit { get {
		return stackLimitF != null ? (int)stackLimitF.GetCharacterValue(fdb, character) : 20;
	} }

	//per-stack capacity is a per-item thing

	public bool limitedWeight=false;
	public Formula weightLimitF;
	public int weightLimit { get {
		return weightLimitF != null ? (int)weightLimitF.GetCharacterValue(fdb, character) : 20;
	} }

	public bool stacksMustBeUnique=false;

	public List<Item> items;
	public List<int> counts;

	//method on awake to clear null and zero-count items out of the list?

	//methods to instantiate items and remove them from inventory
	//only instantiate present items with count > 0, for example

	//methods to destroy items and put them into inventory
	//only if there's total capacity, weight capacity, if-new-stack-then stack capacity and maybe-non-duplicate also

	//methods to check for existence, count, parameters of items

	//methods to move objects between stacks

}
