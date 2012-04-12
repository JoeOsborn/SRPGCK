using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum TeamLocation {
	Local,
	AI,
	Network
}

public class Team : MonoBehaviour {
	//editor only
	public bool editorShowParameters=true;

	//team id, team controller, allies/enemies
	public int id=1;
	public TeamLocation type=TeamLocation.Local;
	public List<int> _allies, _enemies;
	public List<int> allies { get {
		if(_allies == null) { _allies = new List<int>(); }
		return _allies;
	} }
	public List<int> enemies { get {
		if(_enemies == null) { _enemies = new List<int>(); }
		return _enemies;
	} }

	public List<Parameter> _parameters;
	public List<Parameter> parameters {
		get {
			if(_parameters == null) { _parameters = new List<Parameter>(); }
			return _parameters;
		}
		set {
			_parameters = value;
		}
	}

	public Arbiter _arbiter;
	public Arbiter arbiter { get {
		if(_arbiter == null) {
			_arbiter = transform.parent != null ?
				transform.parent.GetComponent<Arbiter>() :
				null;
		}
		return _arbiter;
	} }

	public Formulae fdb { get {
		if(arbiter != null) { return arbiter.fdb; }
		return Formulae.DefaultFormulae;
	} }

	public void Start() {
		if(arbiter != null) {
			arbiter.AddTeam(this);
		}
	}

	public virtual bool HasParam(string pname) {
		return parameters.Any(p => p.Name == pname);
	}

	public virtual float GetParam(
		string pname,
		float fallback=float.NaN,
		SkillDef parentCtx=null
	) {
		Parameter p = parameters.FirstOrDefault(pn => pn.Name == pname);
		if(p != null) {
			return p.Formula.GetValue(fdb, parentCtx);
		} else {
			if(float.IsNaN(fallback)) {
				Debug.LogError("No fallback for missing param "+pname);
			}
			return fallback;
		}
	}

	public virtual void SetParam(string pname, float value) {
		Parameter p = parameters.FirstOrDefault(pn => pn.Name == pname);
		if(p == null) {
			parameters.Add(new Parameter(pname, Formula.Constant(value)));
		} else {
			if(p.Formula.formulaType == FormulaType.Constant) {
				p.Formula.constantValue = value;
			} else {
				Debug.LogError("Can't set value of non-constant param "+pname);
			}
		}
	}
	public virtual void SetParam(string pname, Formula f) {
		Parameter p = parameters.FirstOrDefault(pn => pn.Name == pname);
		if(p == null) {
			parameters.Add(new Parameter(pname, f));
		} else {
			p.Formula = f;
		}
	}

	public override string ToString() {
		return "Team "+id;
	}
}
