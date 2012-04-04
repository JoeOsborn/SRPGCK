using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using System.Linq;

/*
F <- NULL | Phrase.
Phrase <- UnaryExpression
        | PrefixExpression
        | IfExpression
        | InfixExpression
        | Term.
UnaryExpression <- UNARY_OP Phrase.
InfixExpression <- Phrase INFIX_OP Phrase.

PrefixExpression <-
									| 'root' '(' Phrase [',' Phrase] ')'
								  | 'mean' '(' Phrase [',' Phrase]* ')'
									| 'random' '(' [Phrase [',' Phrase]*] ')'
									| 'clamp' '(' Phrase [',' Phrase ',' Phrase] ')'
									| 'floor' '(' Phrase ')'
									| 'ceil' '(' Phrase ')'
									| 'round' '(' Phrase ')'
									| 'abs' '(' Phrase ')'
									| 'any' '{' Phrase [',' Phrase]* '}'
									| SIDE_SELECTOR '{' SideResponse [';' SideResponse]* '}'.

IfExpression <- 'if' Condition ':' Phrase [';' Phrase]?.
SideResponse <- SIDE_CHOICE ':' Phrase.
Term <- CONSTANT | Lookup.
Lookup <- CharacterLookup | SkillLookup | EffectLookup | FormulaLookup.
FormulaLookup <- 'f' '.' LookupTerminal.
SkillLookup <- [SKILL_SCOPE '.'] LookupTerminal.
CharacterLookup <- [CHARACTER_SCOPE '.'] LookupTerminal | [CHARACTER_SCOPE '.'] EquipmentLookup | [CHARACTER_SCOPE '.'] StatusLookup.
EquipmentLookup <- 'equip' ['(' EquipSearch ')'] '.' LookupTerminal.
EquipSearch <- [SYMBOL [',' SYMBOL]*] ['in' SYMBOL [',' SYMBOL]*] SearchMerge.
EffectLookup <- 'effect'             ; get value of immediate reacted effect
						  | 'effect' '('STAT_CHANGE [',' STAT_CHANGE]* ['in' SYMBOL [',' SYMBOL]*] ['by' SYMBOL [',' SYMBOL]*] SearchMerge ')'. ; get value of aggregated search for effects in the reacted skill. note no lookup afterwards!

LookupTerminal <- SYMBOL | LookupTerminal '.' SYMBOL.


ConditionalLookup <- Lookup | StatusLookup.
StatusLookup <- CHARACTER_SCOPE '.' 'status' '.' SYMBOL.

SearchMerge <- NULL | 'get' MERGE_MODE.

INFIX_OP = '+' | '-' | '*' | '/' | '^'.

SKILL_SCOPE = 'skill'.
CHARACTER_SCOPE = 't' | 'c'.

STAT_CHANGE = 'increase' | 'decrease' | 'change' | 'no_change' | 'any'.

MERGE_MODE = 'first' | 'last' | 'min' | 'max' | 'mean' | 'sum'.

SIDE_SELECTOR = 'targetedSide' | 'targeterSide'.
SIDE_CHOICE = 'front' | 'back' | 'left' | 'right' | 'sides' | 'away' | 'towards' | 'default'.

*/

public class Identifier : IFormulaElement {
	public Identifier(string n) { Name = n; }
	public string Name { get; set; }
	public override string ToString() { return Name; }
}

public class FormulaCompiler : Grammar<IFormulaElement> {

	public static bool CompileInPlace(Formula f) {
		FormulaCompiler fc = new FormulaCompiler();
		try {
			IFormulaElement ife = fc.Parse(f.text);
			Formula newF = ife as Formula;
			if(ife is Identifier) {
				// Debug.Log("just identifier "+(ife as Identifier).Name+", auto lookup");
				newF = Formula.Lookup((ife as Identifier).Name);
			}
			if(Formula.NotNullFormula(newF)) {
				newF.text = f.text;
			}
			string n = f.name;
			f.CopyFrom(newF);
			f.name = n;
			f.compilationError = "";
			return true;
		} catch(ParseException pe) {
			f.compilationError = pe.ToString();
		}
		return false;
	}

	public FormulaCompiler() {
		Infix("+", 10, Add); Infix("-", 10, Sub);
		Infix("*", 20, Mul); Infix("/", 20, Div);
		Infix("div", 20, IntDiv);
		Infix("%", 20, Rem);

		Infix("or", 3, Or);
		Infix("and", 3, And);

		InfixR("^", 30, Pow);
		Prefix("-", 200, Neg);
		Prefix("not", 4, Not);
		Infix("==", 5, Eq); Infix("!=", 5, Neq);
		Infix("<", 5, LT); Infix("<=", 5, LTE);
		Infix(">", 5, GT); Infix(">=", 5, GTE);
		TernaryPrefix("if", ":", ";", 1, If);

		Group("(", ")", 1);

		Builtin("abs", 1, 1, Abs);
		Builtin("trunc", 1, 1, Trunc);
		Builtin("root", 1, 2, Root);
		Builtin("mean", 1, int.MaxValue, Mean);
		Builtin("min", 1, int.MaxValue, Min);
		Builtin("max", 1, int.MaxValue, Max);
		Builtin("random", 0, 2, RandomRange);
		Builtin("clamp", 1, 3, ClampRange);
		Builtin("floor", 1, 1, Floor);
		Builtin("ceil", 1, 1, Ceil);
		Builtin("round", 1, 1, Round);
		Builtin("any", 1, int.MaxValue, PickAny);

		Builtin("exists", 1, 1, LookupSuccessful);

		LookupOn("true", (parser) => {
			Formula f = Formula.True();
			return f;
		});

		LookupOn("false", (parser) => {
			Formula f = Formula.False();
			return f;
		});

		string[] sides = new string[]{
			"front",
			"left",
			"right",
			"back",
			"away",
			"sides",
			"towards",
			"default"
		};
		Branch("targeted-side", sides, TargetedSide);
		Branch("targeter-side", sides, TargeterSide);
		BranchFormulae("random", RandomBranch);
		BranchFormulae("cond", CondBranch);
		//more branches could be added later! woo!

		Symbol("no-change");
		Symbol("increase");
		Symbol("decrease");
		Symbol("change");
		Symbol("any");
		Symbol("in");
		Symbol("by");
		Symbol("get");

		Symbol("first");
		Symbol("last");
		Symbol("min");
		Symbol("max");
		Symbol("mean");
		Symbol("sum");

		//lookup reacted effect
		var effectLookup = Symbol("effect", 10);
		effectLookup.Nud = (parser) => {
			Formula f = new Formula();
			f.formulaType = FormulaType.Lookup;
			f.lookupType = LookupType.SkillEffectType;
			FillSkillEffectTypeFormula(parser, f);
			return f;
		};
		var equipLookup = Symbol("equip", 10);
		equipLookup.Nud = (parser) => {
			Formula f = new Formula();
			f.formulaType = FormulaType.Lookup;
			f.lookupType = LookupType.ActorEquipmentParam;
			FillEquipFormula(parser, f);
			return f;
		};

		LookupOn("f", (parser) => {
			Formula f = new Formula();
			f.formulaType = FormulaType.Lookup;
			f.lookupType = LookupType.NamedFormula;
			return f;
		});
		LookupOn("c", (parser) => {
			Formula f = new Formula();
			f.formulaType = FormulaType.Lookup;
			f.lookupType = LookupType.ActorStat;
			return f;
		});
		LookupOn("t", (parser) => {
			Formula f = new Formula();
			f.formulaType = FormulaType.Lookup;
			f.lookupType = LookupType.TargetStat;
			return f;
		});
		LookupOn("mount", (parser) => {
			Formula f = new Formula();
			f.formulaType = FormulaType.Lookup;
			f.lookupType = LookupType.ActorMountStat;
			return f;
		});
		LookupOn("mounter", (parser) => {
			Formula f = new Formula();
			f.formulaType = FormulaType.Lookup;
			f.lookupType = LookupType.ActorMounterStat;
			return f;
		});
		LookupOn("skill", (parser) => { //TODO: let this search on the name/path to the skill
			Formula f = new Formula();
			f.formulaType = FormulaType.Lookup;
			f.lookupType = LookupType.SkillParam;
			return f;
		});
		LookupOn("reacted-skill", (parser) => {
			Formula f = new Formula();
			f.formulaType = FormulaType.Lookup;
			f.lookupType = LookupType.ReactedSkillParam;
			return f;
		});
		LookupOn("status", (parser) => {
			Formula f = new Formula();
			f.formulaType = FormulaType.Lookup;
			f.lookupType = LookupType.ActorStatusEffect;
			return f;
		});
//LookupOn() and equip() generate the leftmost lookup formulae, and DOT composes the lookup formulae
//with additional constraints and path components.
		Symbol(".");
		int dotLBP = 101;
		var dot = Symbol(".", dotLBP);
		dot.Led = (parser, left) => {
			//we have left, now start chewing up the things to the right
			// Debug.Log("dot left of "+parser.Token);
			Formula f = CheckFormulaArg(left);
			if(f.formulaType != FormulaType.Lookup) {
				throw new SemanticException("`.` only valid in lookup");
			}
			do {
				//definitely want just the next token
				// var oldRight = parser.Token;
				IFormulaElement next = parser.Parse(int.MaxValue);
				// Debug.Log("dot after "+f+" with right "+oldRight+" parse as "+next);
				if(next == null) {
					throw new SemanticException("Expected something after dot");
				}
				Identifier ident = next as Identifier;
				if(next is Formula) {
					Formula nextF = next as Formula;
					// Debug.Log("next is formula "+nextF.formulaType+"->"+nextF.lookupType+":"+nextF.lookupReference);
					if(nextF.lookupType == LookupType.SkillEffectType ||
						 nextF.lookupType == LookupType.ReactedEffectType) {
					  if(f.formulaType == FormulaType.Lookup) {
 							if(f.lookupType == LookupType.SkillParam) {
 								nextF.lookupType = LookupType.SkillEffectType;
 							} else if(f.lookupType == LookupType.ReactedSkillParam) {
 								nextF.lookupType = LookupType.ReactedEffectType;
 							}
 							f = nextF;
						}
					} else if(nextF.lookupType == LookupType.ActorEquipmentParam ||
						        nextF.lookupType == LookupType.ReactedSkillParam) {
						if(f.formulaType == FormulaType.Lookup) {
							if(f.lookupType == LookupType.ActorStat) {
								nextF.lookupType = LookupType.ActorEquipmentParam;
							} else if(f.lookupType == LookupType.TargetStat) {
								nextF.lookupType = LookupType.TargetEquipmentParam;
							} else if(f.lookupType == LookupType.ActorMountStat) {
								nextF.lookupType = LookupType.ActorMountEquipmentParam;
							} else if(f.lookupType == LookupType.ActorMounterStat) {
								nextF.lookupType = LookupType.ActorMounterEquipmentParam;
							} else if(f.lookupType == LookupType.TargetMountStat) {
								nextF.lookupType = LookupType.TargetMountEquipmentParam;
							} else if(f.lookupType == LookupType.TargetMounterStat) {
								nextF.lookupType = LookupType.TargetMounterEquipmentParam;
							}
							f = nextF;
						}
					} else {
						if(f.formulaType == FormulaType.Lookup) {
							if(f.lookupType == LookupType.ActorStat) {
								if(f.lookupType == LookupType.TargetStat) {
									nextF.lookupType = LookupType.ActorStat;
								} else if(f.lookupType == LookupType.TargetMountStat) {
									nextF.lookupType = LookupType.ActorMountStat;
								} else if(f.lookupType == LookupType.TargetMounterStat) {
									nextF.lookupType = LookupType.ActorMounterStat;
								} else if(f.lookupType == LookupType.TargetMountSkillParam) {
									nextF.lookupType = LookupType.ActorMountSkillParam;
								} else if(f.lookupType == LookupType.TargetMounterSkillParam) {
									nextF.lookupType = LookupType.ActorMounterSkillParam;
								} else if(f.lookupType == LookupType.TargetMountEquipmentParam) {
									nextF.lookupType = LookupType.ActorMountEquipmentParam;
								} else if(f.lookupType == LookupType.TargetMounterEquipmentParam) {
									nextF.lookupType = LookupType.ActorMounterEquipmentParam;
								}
							 	f = nextF;
							} else if(f.lookupType == LookupType.TargetStat) {
								if(f.lookupType == LookupType.ActorStat) {
									nextF.lookupType = LookupType.TargetStat;
								} else if(f.lookupType == LookupType.ActorMountStat) {
									nextF.lookupType = LookupType.TargetMountStat;
								} else if(f.lookupType == LookupType.ActorMounterStat) {
									nextF.lookupType = LookupType.TargetMounterStat;
								} else if(f.lookupType == LookupType.ActorMountSkillParam) {
									nextF.lookupType = LookupType.TargetMountSkillParam;
								} else if(f.lookupType == LookupType.ActorMounterSkillParam) {
									nextF.lookupType = LookupType.TargetMounterSkillParam;
								} else if(f.lookupType == LookupType.ActorMountEquipmentParam) {
									nextF.lookupType = LookupType.TargetMountEquipmentParam;
								} else if(f.lookupType == LookupType.ActorMounterEquipmentParam) {
									nextF.lookupType = LookupType.TargetMounterEquipmentParam;
								}
								f = nextF;
							}
						}
					}
				} else if(ident != null && ident.Name == "reacted-skill") {
					throw new SemanticException("Scoped reacted-skill param lookups unsupported");
				} else if(ident != null && ident.Name == "mount") {
					//prev = c, t
					if(f.lookupType == LookupType.ActorStat) {
						// Debug.Log("c mount stat");
						f.lookupType = LookupType.ActorMountStat;
					} else if(f.lookupType == LookupType.TargetStat) {
						// Debug.Log("t mount stat");
						f.lookupType = LookupType.TargetMountStat;
					} else {
						throw new SemanticException("bad parent scope for mount check");
					}
				} else if(ident != null && ident.Name == "mounter") {
					//prev = c, t
					if(f.lookupType == LookupType.ActorStat) {
						f.lookupType = LookupType.ActorMounterStat;
					} else if(f.lookupType == LookupType.TargetStat) {
						f.lookupType = LookupType.TargetMounterStat;
					} else {
						throw new SemanticException("bad parent scope for mounter check");
					}
				} else if(ident != null && ident.Name == "skill") {
					//prev = c, t
					if(f.lookupType == LookupType.ActorStat) {
						f.lookupType = LookupType.ActorSkillParam;
					} else if(f.lookupType == LookupType.TargetStat) {
						f.lookupType = LookupType.TargetSkillParam;
					} else if(f.lookupType == LookupType.ActorMountStat) {
						f.lookupType = LookupType.ActorMountSkillParam;
					} else if(f.lookupType == LookupType.ActorMounterStat) {
						f.lookupType = LookupType.ActorMounterSkillParam;
					} else if(f.lookupType == LookupType.TargetMountStat) {
						f.lookupType = LookupType.TargetMountSkillParam;
					} else if(f.lookupType == LookupType.TargetMounterStat) {
						f.lookupType = LookupType.TargetMounterSkillParam;
					}
				} else if(ident != null && ident.Name == "status") {
					if(f.lookupType == LookupType.ActorStat) {
						f.lookupType = LookupType.ActorStatusEffect;
					} else if(f.lookupType == LookupType.TargetStat) {
						f.lookupType = LookupType.TargetStatusEffect;
					} else if(f.lookupType == LookupType.ActorMountStat) {
						f.lookupType = LookupType.ActorMountStatusEffect;
					} else if(f.lookupType == LookupType.ActorMounterStat) {
						f.lookupType = LookupType.ActorMounterStatusEffect;
					} else if(f.lookupType == LookupType.TargetMountStat) {
						f.lookupType = LookupType.TargetMountStatusEffect;
					} else if(f.lookupType == LookupType.TargetMounterStat) {
						f.lookupType = LookupType.TargetMounterStatusEffect;
					}
				} else if(ident != null) {
					// Debug.Log("non-null ident "+ident.Name+" for "+f.formulaType+"->"+f.lookupType);
					//else, append to lookupref
					if(ident.Name == "isNull" || ident.Name == "isNotNull") {
						if((f.lookupReference == null || f.lookupReference == "") &&
						    f.lookupType == LookupType.TargetStat) {
							f.formulaType = ident.Name == "isNull" ? FormulaType.TargetIsNull : FormulaType.TargetIsNotNull;
						} else {
							throw new SemanticException("nullity checks only supported for `t.`");
						}
					} else {
						//is lookupRef null or ""?
						if(f.lookupReference == null || f.lookupReference == "") {
							//set lookupref
							f.lookupReference = ident.Name;
							// Debug.Log("set lookupref to "+f.lookupReference);
						} else {
							// Debug.Log("append "+ident.Name+" to "+f.lookupReference);
							f.lookupReference += "."+ident.Name;
						}
					}
				} else {
					throw new SemanticException("no identifier or other usable thing after dot");
				}
				if(parser.Token == null || parser.Token.Id != ".") {
					break;
				}
				parser.Advance(".");
			} while(true);
			return f;
		};

		Match(
			"(identifier)",
			Regex("[a-zA-Z_][a-zA-Z0-9_]*"),
			0,
			name => new Identifier(name)
		);

		Match("(number)", c => char.IsDigit(c) || c == '.', 0, Number);
//		Match("(identifier)", char.IsLetter, 1, Identifier);
		SkipWhile(char.IsWhiteSpace);
	}

	protected Symbol<IFormulaElement> LookupOn(string name, Func<PrattParser<IFormulaElement>, IFormulaElement> nud) {
		var elt = Symbol(name, 99);
		elt.Nud = nud;
		return elt;
	}

	protected void FillSkillEffectTypeFormula(PrattParser<IFormulaElement> parser, Formula f) {
		if(parser.Token.Id != "(") {
			if(f.formulaType == FormulaType.Lookup) {
				if(f.lookupType == LookupType.ReactedEffectType) {
					f.formulaType = FormulaType.ReactedEffectValue;
				} else if(f.lookupType == LookupType.SkillEffectType) {
					f.formulaType = FormulaType.SkillEffectValue;
				}
			} else {
				f.formulaType = FormulaType.ReactedEffectValue;
			}
		} else {
			parser.Advance("(");
			//a, b, c in x, y, z as w
			List<StatChangeType> changes = new List<StatChangeType>{StatChangeType.Change};
			List<string> stats = new List<string>();
			List<string> categories = new List<string>();
			FormulaMergeMode mergeMode = FormulaMergeMode.Nth;
			int mergeNth = 0;
			while(true) {
				if(parser.Token.Id == "any") {
        	changes.Add(StatChangeType.Any);
				} else if(parser.Token.Id == "change") {
        	changes.Add(StatChangeType.Change);
				} else if(parser.Token.Id == "increase") {
        	changes.Add(StatChangeType.Increase);
				} else if(parser.Token.Id == "decrease") {
        	changes.Add(StatChangeType.Decrease);
				} else if(parser.Token.Id == "no-change") {
        	changes.Add(StatChangeType.NoChange);
				} else {
					break;
				}
				parser.Advance(null);
				if(parser.Token.Id != ",") {
					break;
				}
				parser.Advance(",");
			}
			if(parser.Token.Id == "in") {
				parser.Advance("in");
				while(true) {
					Identifier ident = parser.Parse(0) as Identifier;
					stats.Add(ident.Name);
					if(parser.Token.Id != ",") {
						break;
					}
					parser.Advance(",");
				}
			}
			if(parser.Token.Id == "by") {
				parser.Advance("by");
				while(true) {
					Identifier ident = parser.Parse(0) as Identifier;
					categories.Add(ident.Name);
					if(parser.Token.Id != ",") {
						break;
					}
					parser.Advance(",");
				}
			}
			if(parser.Token.Id == "get") {
				parser.Advance("get");
				if(parser.Token.Id == "first") {
					mergeMode = FormulaMergeMode.Nth;
					mergeNth = 0;
				} else if(parser.Token.Id == "last") {
					mergeMode = FormulaMergeMode.Last;
				} else if(parser.Token.Id == "min") {
					mergeMode = FormulaMergeMode.Min;
				} else if(parser.Token.Id == "max") {
					mergeMode = FormulaMergeMode.Max;
				} else if(parser.Token.Id == "mean") {
					mergeMode = FormulaMergeMode.Mean;
				} else if(parser.Token.Id == "sum") {
					mergeMode = FormulaMergeMode.Sum;
				} else if(int.TryParse(parser.Token.Id, out mergeNth)) {
					mergeMode = FormulaMergeMode.Nth;
					mergeNth = mergeNth - 1;
				}
				parser.Advance(null);
			}
			parser.Advance(")");
			f.searchReactedStatNames = stats.ToArray();
			f.searchReactedStatChanges = changes.ToArray();
			f.searchReactedEffectCategories = categories.ToArray();
			f.mergeMode = mergeMode;
			f.mergeNth = mergeNth;
		}
	}

	protected void FillEquipFormula(PrattParser<IFormulaElement> parser, Formula f) {
		if(parser.Token.Id != "(") {
			f.equipmentSlots = null;
			f.equipmentCategories = null;
		} else {
			parser.Advance("(");
			List<string> categories = new List<string>();
			List<string> slots = new List<string>();
			FormulaMergeMode mergeMode = FormulaMergeMode.Nth;
			int mergeNth = 0;
			while(true) {
				Identifier ident = parser.Parse(0) as Identifier;
				categories.Add(ident.Name);
				if(parser.Token.Id != ",") {
					break;
				}
				parser.Advance(",");
			}
			if(parser.Token.Id == "in") {
				parser.Advance("in");
				while(true) {
					Identifier ident = parser.Parse(0) as Identifier;
					slots.Add(ident.Name);
					if(parser.Token.Id != ",") {
						break;
					}
					parser.Advance(",");
				}
			}
			if(parser.Token.Id == "get") {
				parser.Advance("get");
				if(parser.Token.Id == "first") {
					mergeMode = FormulaMergeMode.Nth;
					mergeNth = 0;
				} else if(parser.Token.Id == "last") {
					mergeMode = FormulaMergeMode.Last;
				} else if(parser.Token.Id == "min") {
					mergeMode = FormulaMergeMode.Min;
				} else if(parser.Token.Id == "max") {
					mergeMode = FormulaMergeMode.Max;
				} else if(parser.Token.Id == "mean") {
					mergeMode = FormulaMergeMode.Mean;
				} else if(parser.Token.Id == "sum") {
					mergeMode = FormulaMergeMode.Sum;
				} else if(int.TryParse(parser.Token.Id, out mergeNth)) {
					mergeMode = FormulaMergeMode.Nth;
					mergeNth = mergeNth - 1;
				}
				parser.Advance(null);
			}
			parser.Advance(")");
			f.equipmentCategories = categories.ToArray();
			f.equipmentSlots = slots.ToArray();
			f.mergeMode = mergeMode;
			f.mergeNth = mergeNth;
		}
	}

	protected Symbol<IFormulaElement> Branch(string name, string[] optKeys, Func<IEnumerable<IFormulaElement>, IFormulaElement> selector) {
		//make sure we have our special symbols
		Symbol("{");
		Symbol("}");
		Symbol(":");
		Symbol(";");
		//ok, proceed
		Symbol<IFormulaElement> branchType = Match("("+name+")", Regex(name+"\\s*\\{"), 0, null);
		int bindingPower = 0;
		branchType.Nud = (parser) => {
			List<string> cases = new List<string>();
			List<Formula> forms = new List<Formula>();
			//maybe parser.Advance();
      if(parser.Token.Id == "}") {
				throw new SemanticException("Switch "+name+" must handle at least one case");
			} else {
				while(true) {
					if(optKeys.Contains(parser.Token.Id)) {
						cases.Add(parser.Token.Id);
						parser.Advance(parser.Token.Id);
					} else if(parser.Token.Id == "(identifier)") {
						Identifier ident = (parser.Parse(bindingPower) as Identifier);
						if(optKeys.Contains(ident.Name)) {
							cases.Add(ident.Name);
						} else {
							throw new SemanticException("Invalid case "+ident.Name);
						}
					} else {
						throw new SemanticException("Invalid case "+parser.Token.Id);
					}
					if(parser.Token.Id != ":") {
						throw new SemanticException("Switch "+name+" case "+parser.Token.Id+" must be handled");
					}
					parser.Advance(":");
					forms.Add(parser.Parse(bindingPower) as Formula);
					if(parser.Token.Id != ";") {
						break;
					}
					parser.Advance(";");
				}
			}
			parser.Advance("}");
			List<IFormulaElement> sortedForms = new List<IFormulaElement>();
			foreach(string k in optKeys) {
				int idx = cases.IndexOf(k);
				if(idx != -1) {
					sortedForms.Add(forms[idx]);
				} else {
					sortedForms.Add(Formula.Null());
				}
			}
			return selector(sortedForms);
		};
		return branchType;
	}

	protected Symbol<IFormulaElement> BranchFormulae(string name, Func<IEnumerable<IFormulaElement>, IEnumerable<IFormulaElement>, IFormulaElement> selector) {
		//make sure we have our special symbols
		Symbol("{");
		Symbol("}");
		Symbol(":");
		Symbol(";");
		Symbol("default");
		//ok, proceed
		Symbol<IFormulaElement> branchType = Match("("+name+")", Regex(name+"\\s*\\{"), 0, null);
		int bindingPower = 0;
		branchType.Nud = (parser) => {
			List<Formula> cases = new List<Formula>();
			List<Formula> forms = new List<Formula>();
			bool expectingDefaultFormula=false;
			Formula defaultFormula = null;
			//maybe parser.Advance();
      if(parser.Token.Id == "}") {
				throw new SemanticException("Switch "+name+" must handle at least one case");
			} else {
				while(true) {
					if(parser.Token.Id == "default") {
						if(defaultFormula != null) {
							throw new SemanticException("Switch may not have more than one default response");
						}
						expectingDefaultFormula = true;
						parser.Advance("default");
					} else {
						IFormulaElement ife = parser.Parse(bindingPower);
						Formula condF = CheckFormulaArg(ife);
						if(condF != null) {
							cases.Add(condF);
						} else {
							Debug.Log("skip ife "+(ife as Identifier).Name);
					 	}
					}
					if(parser.Token.Id != ":") {
						throw new SemanticException("Switch "+name+" case "+parser.Token.Id+" must be handled");
					}
					parser.Advance(":");
					IFormulaElement fife = parser.Parse(bindingPower);
					Formula thisFormula = fife as Formula;
					if(expectingDefaultFormula) {
						expectingDefaultFormula = false;
						defaultFormula = thisFormula;
					} else {
						forms.Add(thisFormula);
					}
					if(parser.Token.Id == ":") {
						throw new SemanticException(": cannot follow branch clause, use ;");
					}
					if(parser.Token.Id != ";") {
						break;
					}
					parser.Advance(";");
				}
			}
			parser.Advance("}");
			if(defaultFormula != null) {
				cases.Add(Formula.Constant(1));
				forms.Add(defaultFormula);
			}
			return selector(
				cases.ConvertAll(f => f as IFormulaElement),
				forms.ConvertAll(f => f as IFormulaElement)
			);
		};
		return branchType;
	}

	protected Symbol<IFormulaElement> Builtin(string name, int argsMin, int argsMax, Func<IEnumerable<IFormulaElement>, IFormulaElement> selector) {
		//make sure we have our special symbols
		Symbol("(");
		Symbol(")");
		Symbol(",");
		//ok, proceed
		Symbol<IFormulaElement> builtin = Symbol(name);
		int bindingPower = 5;
		builtin.Nud = (parser) => {
			List<IFormulaElement> forms = new List<IFormulaElement>();
			//maybe parser.Advance();
			parser.Advance("(");
      if(parser.Token.Id == ")") {
				//nop
			} else {
				while(true) {
					forms.Add(parser.Parse(bindingPower));
					if(parser.Token.Id != ",") {
						break;
					}
					parser.Advance(",");
				}
			}
			parser.Advance(")");
			if(forms.Count < argsMin) {
				throw new SemanticException(""+forms.Count+" is too few arguments to "+name);
			}
			if(forms.Count > argsMax) {
				throw new SemanticException(""+forms.Count+" is too many arguments to "+name);
			}
			return selector(forms);
		};
		return builtin;
	}

	Formula CheckFormulaArg(IFormulaElement ife) {
		if(ife != null && !(ife is Formula)) {
			// Debug.Log("check just identifier "+(ife as Identifier).Name+", auto lookup, could we do better?");
			return Formula.Lookup((ife as Identifier).Name);
		}
		return ife as Formula;
	}

	IFormulaElement TargetedSide(IEnumerable<IFormulaElement> forms) {
		Formula f = new Formula();
		f.formulaType = FormulaType.BranchAppliedSide;
		f.arguments = forms.Select(cf => CheckFormulaArg(cf)).ToList();
		return f;
	}
	IFormulaElement TargeterSide(IEnumerable<IFormulaElement> forms) {
		Formula f = new Formula();
		f.formulaType = FormulaType.BranchApplierSide;
		f.arguments = forms.Select(cf => CheckFormulaArg(cf)).ToList();
		return f;
	}
	IFormulaElement RandomBranch(IEnumerable<IFormulaElement> cases, IEnumerable<IFormulaElement> forms) {
		Formula f = new Formula();
		f.formulaType = FormulaType.BranchPDF;
		if(cases.Count() != forms.Count()) {
			throw new SemanticException("mismatched number of cases and formulae");
		}
		f.arguments = cases.Concat(forms).Select(form => CheckFormulaArg(form)).ToList();
		return f;
	}

	IFormulaElement CondBranch(IEnumerable<IFormulaElement> cases, IEnumerable<IFormulaElement> forms) {
		Formula f = new Formula();
		f.formulaType = FormulaType.BranchCond;
		if(cases.Count() != forms.Count()) {
			throw new SemanticException("mismatched number of cases and formulae");
		}
		f.arguments = cases.Concat(forms).Select(form => CheckFormulaArg(form)).ToList();
		return f;
	}

	//TODO: refactor! higher-order function could generate this with the formula type in a closure.
	IFormulaElement Abs(IEnumerable<IFormulaElement> forms) {
		Formula f = new Formula();
		f.formulaType = FormulaType.AbsoluteValue;
		f.arguments = forms.Select(cf => CheckFormulaArg(cf)).ToList();
		return f;
	}

	IFormulaElement Trunc(IEnumerable<IFormulaElement> forms) {
		Formula f = new Formula();
		f.formulaType = FormulaType.Trunc;
		f.arguments = forms.Select(cf => CheckFormulaArg(cf)).ToList();
		return f;
	}

	IFormulaElement Root(IEnumerable<IFormulaElement> forms) {
		Formula f = new Formula();
		f.formulaType = FormulaType.Root;
		f.arguments = forms.Select(cf => CheckFormulaArg(cf)).ToList();
		return f;
	}

	IFormulaElement Mean(IEnumerable<IFormulaElement> forms) {
		Formula f = new Formula();
		f.formulaType = FormulaType.Mean;
		f.arguments = forms.Select(cf => CheckFormulaArg(cf)).ToList();
		return f;
	}

	IFormulaElement Min(IEnumerable<IFormulaElement> forms) {
		Formula f = new Formula();
		f.formulaType = FormulaType.Min;
		f.arguments = forms.Select(cf => CheckFormulaArg(cf)).ToList();
		return f;
	}

	IFormulaElement Max(IEnumerable<IFormulaElement> forms) {
		Formula f = new Formula();
		f.formulaType = FormulaType.Max;
		f.arguments = forms.Select(cf => CheckFormulaArg(cf)).ToList();
		return f;
	}

	IFormulaElement RandomRange(IEnumerable<IFormulaElement> forms) {
		Formula f = new Formula();
		f.formulaType = FormulaType.RandomRange;
		f.arguments = forms.Select(cf => CheckFormulaArg(cf)).ToList();
		return f;
	}

	IFormulaElement ClampRange(IEnumerable<IFormulaElement> forms) {
		Formula f = new Formula();
		f.formulaType = FormulaType.ClampRange;
		f.arguments = forms.Select(cf => CheckFormulaArg(cf)).ToList();
		return f;
	}

	IFormulaElement Floor(IEnumerable<IFormulaElement> forms) {
		Formula f = new Formula();
		f.formulaType = FormulaType.RoundDown;
		f.arguments = forms.Select(cf => CheckFormulaArg(cf)).ToList();
		return f;
	}
	IFormulaElement Ceil(IEnumerable<IFormulaElement> forms) {
		Formula f = new Formula();
		f.formulaType = FormulaType.RoundUp;
		f.arguments = forms.Select(cf => CheckFormulaArg(cf)).ToList();
		return f;
	}
	IFormulaElement Round(IEnumerable<IFormulaElement> forms) {
		Formula f = new Formula();
		f.formulaType = FormulaType.Round;
		f.arguments = forms.Select(cf => CheckFormulaArg(cf)).ToList();
		return f;
	}
	IFormulaElement PickAny(IEnumerable<IFormulaElement> forms) {
		Formula f = new Formula();
		f.formulaType = FormulaType.Any;
		f.arguments = forms.Select(cf => CheckFormulaArg(cf)).ToList();
		return f;
	}

	IFormulaElement LookupSuccessful(IEnumerable<IFormulaElement> forms) {
		Formula f = new Formula();
		f.CopyFrom(CheckFormulaArg(forms.ElementAt(0)));
		f.formulaType = FormulaType.LookupSuccessful;
		return f;
	}


	IFormulaElement Number(string lit) { return Formula.Constant(float.Parse(lit)); }
	IFormulaElement Eq(IFormulaElement lhs, IFormulaElement rhs) {
		Formula f = new Formula();
		f.formulaType = FormulaType.Equal;
		f.arguments = new List<Formula>(){CheckFormulaArg(lhs), CheckFormulaArg(rhs)};
		return f;
	}
	IFormulaElement Neq(IFormulaElement lhs, IFormulaElement rhs) {
		Formula f = new Formula();
		f.formulaType = FormulaType.NotEqual;
		f.arguments = new List<Formula>(){CheckFormulaArg(lhs), CheckFormulaArg(rhs)};
		return f;
	}
	IFormulaElement GT(IFormulaElement lhs, IFormulaElement rhs) {
		Formula f = new Formula();
		f.formulaType = FormulaType.GreaterThan;
		f.arguments = new List<Formula>(){CheckFormulaArg(lhs), CheckFormulaArg(rhs)};
		return f;
	}
	IFormulaElement GTE(IFormulaElement lhs, IFormulaElement rhs) {
		Formula f = new Formula();
		f.formulaType = FormulaType.GreaterThanOrEqual;
		f.arguments = new List<Formula>(){CheckFormulaArg(lhs), CheckFormulaArg(rhs)};
		return f;
	}
	IFormulaElement LT(IFormulaElement lhs, IFormulaElement rhs) {
		Formula f = new Formula();
		f.formulaType = FormulaType.LessThan;
		f.arguments = new List<Formula>(){CheckFormulaArg(lhs), CheckFormulaArg(rhs)};
		return f;
	}
	IFormulaElement LTE(IFormulaElement lhs, IFormulaElement rhs) {
		Formula f = new Formula();
		f.formulaType = FormulaType.LessThanOrEqual;
		f.arguments = new List<Formula>(){CheckFormulaArg(lhs), CheckFormulaArg(rhs)};
		return f;
	}
	IFormulaElement LeftAssocFormula(FormulaType ft, IFormulaElement lhs, IFormulaElement rhs) {
		Formula f = new Formula();
		f.formulaType = ft;
		Formula lhsf = CheckFormulaArg(lhs);
		Formula rhsf = CheckFormulaArg(rhs);
		var args = new List<Formula>();
		if(lhsf.formulaType == f.formulaType) {
			args.AddRange(lhsf.arguments);
		} else {
			args.Add(lhsf);
		}
		if(rhsf.formulaType == f.formulaType) {
			args.AddRange(rhsf.arguments);
		} else {
			args.Add(rhsf);
		}
		f.arguments = args;
		return f;
	}
	IFormulaElement Add(IFormulaElement lhs, IFormulaElement rhs) {
		return LeftAssocFormula(FormulaType.Add, lhs, rhs);
	}
	IFormulaElement Sub(IFormulaElement lhs, IFormulaElement rhs) {
		return LeftAssocFormula(FormulaType.Subtract, lhs, rhs);
	}
	IFormulaElement Mul(IFormulaElement lhs, IFormulaElement rhs) {
		return LeftAssocFormula(FormulaType.Multiply, lhs, rhs);
	}
	IFormulaElement Div(IFormulaElement lhs, IFormulaElement rhs) {
		return LeftAssocFormula(FormulaType.Divide, lhs, rhs);
	}
	IFormulaElement IntDiv(IFormulaElement lhs, IFormulaElement rhs) {
		return LeftAssocFormula(FormulaType.IntDivide, lhs, rhs);
	}
	IFormulaElement Or(IFormulaElement lhs, IFormulaElement rhs) {
		return LeftAssocFormula(FormulaType.Or, lhs, rhs);
	}
	IFormulaElement And(IFormulaElement lhs, IFormulaElement rhs) {
		return LeftAssocFormula(FormulaType.And, lhs, rhs);
	}

	IFormulaElement Rem(IFormulaElement lhs, IFormulaElement rhs) {
		Formula f = new Formula();
		f.formulaType = FormulaType.Remainder;
		f.arguments = new List<Formula>(){CheckFormulaArg(lhs), CheckFormulaArg(rhs)};
		return f;
	}
	IFormulaElement Pow(IFormulaElement lhs, IFormulaElement rhs) {
		Formula f = new Formula();
		f.formulaType = FormulaType.Exponent;
		f.arguments = new List<Formula>(){CheckFormulaArg(lhs), CheckFormulaArg(rhs)};
		return f;
	}
	IFormulaElement Neg(IFormulaElement arg) {
		Formula outer = new Formula();
		outer.formulaType = FormulaType.Negate;
		outer.arguments = new List<Formula>(){
			CheckFormulaArg(arg)
		};
		return outer;
	}
	IFormulaElement Not(IFormulaElement arg) {
		Formula outer = new Formula();
		outer.formulaType = FormulaType.Not;
		outer.arguments = new List<Formula>(){
			CheckFormulaArg(arg)
		};
		return outer;
	}
	IFormulaElement If(IFormulaElement a, IFormulaElement b, IFormulaElement c) {
		Formula outer = new Formula();
		outer.formulaType = FormulaType.BranchIfNotZero;
		outer.arguments = new List<Formula>(){CheckFormulaArg(a), CheckFormulaArg(b), CheckFormulaArg(c)};
		return outer;
	}
}