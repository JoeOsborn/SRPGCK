using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;

[AddComponentMenu("SRPGCK/Item/Inventory")]
public class Inventory : MonoBehaviour {
	Formulae fdb { get {
		if(character != null) { return character.fdb; }
		if(team != null) { return team.fdb; }
		if(arbiter != null) { return arbiter.fdb; }
		return Formulae.DefaultFormulae;
	} }

	Character _character;
	Character character { get {
		return _character = this.FindComponentInThisOrParents<Character>(_character);
	} }

	Team _team;
	Team team { get {
		return _team = this.FindComponentInThisOrParents<Team>(_team);
	} }

	Arbiter _arbiter;
	Arbiter arbiter { get {
		return _arbiter = this.FindComponentInThisOrParents<Arbiter>(_arbiter);
	} }

	public bool limitedStacks=false;
	public Formula stackLimitF;
	public int stackLimit { get {
		return stackLimitF != null ? (int)stackLimitF.GetCharacterValue(fdb, character) : 20;
	} }

	public bool limitedStackSize=false;
	public Formula stackSizeF;
	public int stackSize { get {
		return stackSizeF != null ? (int)stackSizeF.GetCharacterValue(fdb, character) : 1;
	} }

	//per-stack capacity is a per-item thing

	public bool limitedWeight=false;
	public Formula weightLimitF;
	public float weightLimit { get {
		return weightLimitF != null ? weightLimitF.GetCharacterValue(fdb, character) : 20;
	} }

	public bool stacksMustBeUnique=false;

	public List<Item> items;
	public List<int> counts;
	public float totalWeight=0;
	
	protected void Awake() {
		for(int i = 0; i < items.Count; i++) {
			if(counts[i] <= 0) {
				items.RemoveAt(i);
				counts.RemoveAt(i);
				i--;
			} else {
				float weight = items[i].weight;
				totalWeight += weight*counts[i];
				items[i].fdb = fdb;
			}
		}
	}
	
	public InstantiatedItem InstantiateItem(
		Item it, 
		Vector3? where=null,
		Quaternion? rot=null
	) {
		return InstantiateItem(items.IndexOf(it), where, rot);
	}
	public InstantiatedItem InstantiateItem(
		int idx,
		Vector3? where=null,
		Quaternion? rot=null
	) {
		if(idx < 0 || counts[idx] <= 0) { return null; }
		if(items[idx].prefab == null) { return null; }
		InstantiatedItem iit = 
			items[idx].Instantiate(
				(where != null ? where.Value : Vector3.zero), 
				(rot != null ? rot.Value : Quaternion.identity)
			);
		RemoveItem(idx, 1);
		return iit;
	}
	public int RemoveItem(Item item, int ct=1) {
		return RemoveItem(IndexOfItem(item), ct);
	}
	public int RemoveItem(int idx, int ct=1) {
		if(idx < 0 || idx >= items.Count) { return -1; }
		int removed = 0;
		while(counts[idx] > 0 && ct > 0) {
			totalWeight -= items[idx].weight;
			counts[idx]--;
			removed++;
			if(counts[idx] <= 0) {
				items.RemoveAt(idx);
				counts.RemoveAt(idx);
			}
			ct--;
		}
		return removed;
	}
	public bool CanFitItem(Item it, int stack, bool considerWeight=true) {
		if(items[stack] != it) { return false; }
		//is this stack full?
		int limit = (int)it.stackSize;
		if(limit > 0 && counts[stack] >= limit) {
			return false;
		}
		if(limitedStackSize && counts[stack] >= stackSize) {
			return false;
		}
		//is our weight limit exceeded?
		if(considerWeight && limitedWeight) {
			float weight = it.weight;
			if(weight > 0 && totalWeight+weight >= weightLimit) {
				return false;
			}
		}
		return true;
	}
	
	public int FindStack(Item it) {
		int firstEmpty = -1;
		bool hasStack = false;
		int stackCount = 0;
		//find a stack of this item with space
		for(int i = 0; i < items.Count; i++) {
			if(items[i] == it && counts[i] > 0) {
				hasStack = true;
				if(CanFitItem(it, i)) {
					return i;
				}
			}
			if(counts[i] > 0) {
				stackCount++;
			}
			if(counts[i] == 0 && firstEmpty == -1) { firstEmpty = i; }
		}
		//if stacks must be unique and a full stack exists, fail (-1) instead of making a new stack
		if(stacksMustBeUnique && hasStack) {
			return -1;
		}
		//if the number of stacks is limited and this would push us over the limit, fail (-1)
		if(limitedStacks && (stackCount+1) > stackLimit) {
			return -1;
		}
		//make a new stack if necessary, either at the end or in the middle
		if(firstEmpty == -1) {
			items.Add(it);
			counts.Add(0);
			firstEmpty = items.Count-1;
		} else {
			items[firstEmpty] = it;
			counts[firstEmpty] = 0;
		}
		return firstEmpty;
	}
	
	public int InsertItem(Item it, int stack=-1, int count=1) {
		if(stack == -1) {
			stack = FindStack(it);
		}
		if(stack == -1) {
			return -1;
		}
		if(items[stack] != it) {
			Debug.LogError("Can't put item of type "+it+" onto stack of item type "+items[stack]);
		}
		it.fdb = fdb;
		int added = 0;
		while(CanFitItem(it, stack) && count > 0) {
			totalWeight += it.weight;
			counts[stack]++;
			count--;
			added++;
		}
		return added;
	}
	public bool InsertInstantiatedItem(InstantiatedItem iit, int stack=-1) {
		Item it = iit.item;
		if(InsertItem(it, stack, 1) == 1) {
			Destroy(iit.gameObject);
			return true;
		}
		return false;
	}
	
	public int IndexOfItem(Item it) {
		for(int i = items.Count; i != 0; i--) {
			if(items[i-1] == it && counts[i-1] > 0) { return i-1; }
		}
		return -1;
	}
	public bool HasItem(Item it) {
		return IndexOfItem(it) != -1;
	}
	public Item StackAt(int idx) {
		return items[idx];
	}
	public int CountOfStackAt(int idx) {
		return counts[idx];
	}
	public int TotalCountOfItem(Item it) {
		int count = 0;
		for(int i = 0; i < items.Count; i++) {
			if(items[i] == it) {
				count += counts[i];
			}
		}
		return count;
	}
	//TODO: get items of types... or categories... or with certain params... or whatever

	public void ExchangeStacks(int src, int dest) {
		Item tempI = items[dest];
		int tempC = counts[dest];
		items[dest] = items[src];
		counts[dest] = counts[src];
		items[src] = tempI;
		counts[dest] = tempC;
	}
	//returns -1 if the fill is invalid (from/to of distinct types)
	//returns 0 if `to` is full or `from` is empty
	//returns the number of items transferred
	public int FillStack(int from, int to) {
		if(items[from] != items[to]) { return -1; }
		Item it = items[from];
		int moved = 0;
		while(counts[from] > 0 && CanFitItem(it, to, false)) {
			counts[to]++;
			counts[from]--;
			moved++;
		}
		return moved;
	}
}
