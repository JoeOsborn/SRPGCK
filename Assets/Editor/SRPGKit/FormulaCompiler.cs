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
//Lookup is wrong, wrong, wrong -- think about it more
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
/*		Debug.Log("compile "+f.text);*/
		FormulaCompiler fc = new FormulaCompiler();
		try {
			Formula newF = fc.Parse(f.text) as Formula;
			if(newF != null) {
				newF.text = f.text;
			}
			f.CopyFrom(newF);
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
		Infix("%", 20, Rem);

		InfixR("^", 30, Pow);
		Prefix("-", 200, Neg);
		Infix("==", 5, Eq); Infix("!=", 5, Neq);
		Infix("<", 5, LT); Infix("<=", 5, LTE);
		Infix(">", 5, GT); Infix(">=", 5, GTE);
		TernaryPrefix("if", ":", ";", 1, If);

		Group("(", ")", 1);

		Builtin("abs", 1, 1, Abs);
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

		string[] sides = new string[]{"front", "left", "right", "back", "away", "sides", "towards", "default"};
		Branch("targeted-side", sides, TargetedSide);
		Branch("targeter-side", sides, TargeterSide);
		BranchFormulae("random", RandomBranch);
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
			if(parser.Token.Id != "(") {
				Formula f = new Formula();
				f.formulaType = FormulaType.ReactedEffectValue;
				return f;
			} else {
				parser.Advance("(");
				//a, b, c in x, y, z as w
				List<StatChangeType> changes = new List<StatChangeType>{StatChangeType.Change};
				List<string> stats = new List<string>();
				List<string> categories = new List<string>();
				FormulaMergeMode mergeMode = FormulaMergeMode.First;
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
						mergeMode = FormulaMergeMode.First;
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
					}
					parser.Advance(null);
				}
				parser.Advance(")");
				Formula f = new Formula();
				f.formulaType = FormulaType.Lookup;
				f.lookupType = LookupType.ReactedEffectType;
				f.searchReactedStatNames = stats.ToArray();
				f.searchReactedStatChanges = changes.ToArray();
				f.searchReactedEffectCategories = categories.ToArray();
				f.mergeMode = mergeMode;
				return f;
			}
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
			Formula f = CheckFormulaArg(left);
			do {
/*				Debug.Log("dot after "+f+" with right "+parser.Token);*/
				//definitely want just the next token
				Identifier ident = parser.Parse(int.MaxValue) as Identifier;
				if(ident == null) {
					throw new SemanticException("Expected identifier after dot");
				}
/*				Debug.Log("right is "+ident.Name);*/
				if(ident.Name == "effect") {
					throw new SemanticException("Scoped skill effect lookups unsupported");
				} else if(ident.Name == "reacted-skill") {
					throw new SemanticException("Scoped reacted-skill param lookups unsupported");
				} else if(ident.Name == "equip") {
					//prev = c, t
					if(f.formulaType == FormulaType.Lookup) {
						if(f.lookupType == LookupType.ActorStat) {
							f.lookupType = LookupType.ActorEquipmentParam;
						} else if(f.lookupType == LookupType.TargetStat) {
							f.lookupType = LookupType.TargetEquipmentParam;
						}
						FillEquipFormula(parser, f);
					}
				} else if(ident.Name == "skill") {
					//prev = c, t
					if(f.formulaType == FormulaType.Lookup) {
						if(f.lookupType == LookupType.ActorStat) {
							f.lookupType = LookupType.ActorSkillParam;
						} else if(f.lookupType == LookupType.TargetStat) {
							f.lookupType = LookupType.TargetSkillParam;
						}
					}
				} else if(ident.Name == "status") {
					if(f.formulaType == FormulaType.Lookup) {
						if(f.lookupType == LookupType.ActorStat) {
							f.lookupType = LookupType.ActorStatusEffect;
						} else if(f.lookupType == LookupType.TargetStat) {
							f.lookupType = LookupType.TargetStatusEffect;
						}
					}
				} else {
					//is lookupRef null or ""?
					//set lookupref
					//else, append to lookupref
					if(f.formulaType == FormulaType.Lookup) {
						if(f.lookupReference == null || f.lookupReference == "") {
							f.lookupReference = ident.Name;
						} else {
							f.lookupReference += "."+ident.Name;
						}
					}
				}
				if(parser.Token == null || parser.Token.Id != ".") {
					break;
				}
				parser.Advance(".");
			} while(true);
			return f;
		};

		Match("(identifier)", char.IsLetter, 0, name => new Identifier(name));

		Match("(number)", c => char.IsDigit(c) || c == '.', 0, Number);
//		Match("(identifier)", char.IsLetter, 1, Identifier);
		SkipWhile(char.IsWhiteSpace);
	}

	protected Symbol<IFormulaElement> LookupOn(string name, Func<PrattParser<IFormulaElement>, IFormulaElement> nud) {
		var elt = Symbol(name, 99);
		elt.Nud = nud;
		return elt;
	}

	protected void FillEquipFormula(PrattParser<IFormulaElement> parser, Formula f) {
		if(parser.Token.Id != "(") {
			f.equipmentSlots = null;
			f.equipmentCategories = null;
		} else {
			parser.Advance("(");
			List<string> categories = new List<string>();
			List<string> slots = new List<string>();
			FormulaMergeMode mergeMode = FormulaMergeMode.First;
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
					mergeMode = FormulaMergeMode.First;
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
				}
				parser.Advance(null);
			}
			parser.Advance(")");
			f.equipmentCategories = categories.ToArray();
			f.equipmentSlots = slots.ToArray();
			f.mergeMode = mergeMode;
		}
	}

	protected Symbol<IFormulaElement> Branch(string name, string[] optKeys, Func<IEnumerable<IFormulaElement>, IFormulaElement> selector) {
		//make sure we have our special symbols
		Symbol("{");
		Symbol("}");
		Symbol(":");
		Symbol(";");
		//ok, proceed
		Symbol<IFormulaElement> branchType = Symbol(name+"{");
		int bindingPower = 4;
		branchType.Nud = (parser) => {
			List<string> cases = new List<string>();
			List<Formula> forms = new List<Formula>();
			//maybe parser.Advance();
      if(parser.Token.Id == "}") {
				throw new SemanticException("Switch "+name+" must handle at least one case");
			} else {
				while(true) {
					cases.Add((parser.Parse(bindingPower) as Identifier).Name);
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
					sortedForms.Add(null);
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
		Symbol<IFormulaElement> branchType = Symbol(name+"{");
		int bindingPower = 4;
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
					IFormulaElement ife = parser.Parse(bindingPower);
					if(ife is Formula) {
						cases.Add(ife as Formula);
					} else if(ife is Identifier && ((ife as Identifier).Name) == "default") {
						if(defaultFormula != null) {
							throw new SemanticException("Switch may not have more than one default response");
						}
						expectingDefaultFormula = true;
					}
					if(parser.Token.Id != ":") {
						throw new SemanticException("Switch "+name+" case "+parser.Token.Id+" must be handled");
					}
					parser.Advance(":");
					Formula thisFormula = parser.Parse(bindingPower) as Formula;
					if(expectingDefaultFormula) {
						expectingDefaultFormula = false;
						defaultFormula = thisFormula;
					} else {
						forms.Add(thisFormula);
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
			return selector(cases as IEnumerable<IFormulaElement>, forms as IEnumerable<IFormulaElement>);
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
			return Formula.Lookup((ife as Identifier).Name);
		}
		return ife as Formula;
	}

	IFormulaElement TargetedSide(IEnumerable<IFormulaElement> forms) {
		Formula f = new Formula();
		f.formulaType = FormulaType.BranchAppliedSide;
		f.arguments = forms.Select(cf => CheckFormulaArg(cf)).ToArray();
		return f;
	}
	IFormulaElement TargeterSide(IEnumerable<IFormulaElement> forms) {
		Formula f = new Formula();
		f.formulaType = FormulaType.BranchApplierSide;
		f.arguments = forms.Select(cf => CheckFormulaArg(cf)).ToArray();
		return f;
	}
	IFormulaElement RandomBranch(IEnumerable<IFormulaElement> cases, IEnumerable<IFormulaElement> forms) {
		Formula f = new Formula();
		f.formulaType = FormulaType.BranchPDF;
		if(cases.Count() != forms.Count()) {
			throw new SemanticException("mismatched number of cases and formulae");
		}
		f.arguments = cases.Concat(forms).Select(form => CheckFormulaArg(form)).ToArray();
		return f;
	}

	//TODO: refactor! higher-order function could generate this with the formula type in a closure.
	IFormulaElement Abs(IEnumerable<IFormulaElement> forms) {
		Formula f = new Formula();
		f.formulaType = FormulaType.AbsoluteValue;
		f.arguments = forms.Select(cf => CheckFormulaArg(cf)).ToArray();
		return f;
	}

	IFormulaElement Root(IEnumerable<IFormulaElement> forms) {
		Formula f = new Formula();
		f.formulaType = FormulaType.Root;
		f.arguments = forms.Select(cf => CheckFormulaArg(cf)).ToArray();
		return f;
	}

	IFormulaElement Mean(IEnumerable<IFormulaElement> forms) {
		Formula f = new Formula();
		f.formulaType = FormulaType.Mean;
		f.arguments = forms.Select(cf => CheckFormulaArg(cf)).ToArray();
		return f;
	}

	IFormulaElement Min(IEnumerable<IFormulaElement> forms) {
		Formula f = new Formula();
		f.formulaType = FormulaType.Min;
		f.arguments = forms.Select(cf => CheckFormulaArg(cf)).ToArray();
		return f;
	}

	IFormulaElement Max(IEnumerable<IFormulaElement> forms) {
		Formula f = new Formula();
		f.formulaType = FormulaType.Max;
		f.arguments = forms.Select(cf => CheckFormulaArg(cf)).ToArray();
		return f;
	}

	IFormulaElement RandomRange(IEnumerable<IFormulaElement> forms) {
		Formula f = new Formula();
		f.formulaType = FormulaType.RandomRange;
		f.arguments = forms.Select(cf => CheckFormulaArg(cf)).ToArray();
		return f;
	}

	IFormulaElement ClampRange(IEnumerable<IFormulaElement> forms) {
		Formula f = new Formula();
		f.formulaType = FormulaType.ClampRange;
		f.arguments = forms.Select(cf => CheckFormulaArg(cf)).ToArray();
		return f;
	}

	IFormulaElement Floor(IEnumerable<IFormulaElement> forms) {
		Formula f = new Formula();
		f.formulaType = FormulaType.RoundDown;
		f.arguments = forms.Select(cf => CheckFormulaArg(cf)).ToArray();
		return f;
	}
	IFormulaElement Ceil(IEnumerable<IFormulaElement> forms) {
		Formula f = new Formula();
		f.formulaType = FormulaType.RoundUp;
		f.arguments = forms.Select(cf => CheckFormulaArg(cf)).ToArray();
		return f;
	}
	IFormulaElement Round(IEnumerable<IFormulaElement> forms) {
		Formula f = new Formula();
		f.formulaType = FormulaType.Round;
		f.arguments = forms.Select(cf => CheckFormulaArg(cf)).ToArray();
		return f;
	}
	IFormulaElement PickAny(IEnumerable<IFormulaElement> forms) {
		Formula f = new Formula();
		f.formulaType = FormulaType.Any;
		f.arguments = forms.Select(cf => CheckFormulaArg(cf)).ToArray();
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
		f.arguments = new Formula[]{CheckFormulaArg(lhs), CheckFormulaArg(rhs)};
		return f;
	}
	IFormulaElement Neq(IFormulaElement lhs, IFormulaElement rhs) {
		Formula f = new Formula();
		f.formulaType = FormulaType.NotEqual;
		f.arguments = new Formula[]{CheckFormulaArg(lhs), CheckFormulaArg(rhs)};
		return f;
	}
	IFormulaElement GT(IFormulaElement lhs, IFormulaElement rhs) {
		Formula f = new Formula();
		f.formulaType = FormulaType.GreaterThan;
		f.arguments = new Formula[]{CheckFormulaArg(lhs), CheckFormulaArg(rhs)};
		return f;
	}
	IFormulaElement GTE(IFormulaElement lhs, IFormulaElement rhs) {
		Formula f = new Formula();
		f.formulaType = FormulaType.GreaterThanOrEqual;
		f.arguments = new Formula[]{CheckFormulaArg(lhs), CheckFormulaArg(rhs)};
		return f;
	}
	IFormulaElement LT(IFormulaElement lhs, IFormulaElement rhs) {
		Formula f = new Formula();
		f.formulaType = FormulaType.LessThan;
		f.arguments = new Formula[]{CheckFormulaArg(lhs), CheckFormulaArg(rhs)};
		return f;
	}
	IFormulaElement LTE(IFormulaElement lhs, IFormulaElement rhs) {
		Formula f = new Formula();
		f.formulaType = FormulaType.LessThanOrEqual;
		f.arguments = new Formula[]{CheckFormulaArg(lhs), CheckFormulaArg(rhs)};
		return f;
	}
	IFormulaElement Add(IFormulaElement lhs, IFormulaElement rhs) {
		Formula f = new Formula();
		f.formulaType = FormulaType.Add;
		f.arguments = new Formula[]{CheckFormulaArg(lhs), CheckFormulaArg(rhs)};
		return f;
	}
	IFormulaElement Sub(IFormulaElement lhs, IFormulaElement rhs) {
		Formula f = new Formula();
		f.formulaType = FormulaType.Subtract;
		f.arguments = new Formula[]{CheckFormulaArg(lhs), CheckFormulaArg(rhs)};
		return f;
	}
	IFormulaElement Mul(IFormulaElement lhs, IFormulaElement rhs) {
		Formula f = new Formula();
		f.formulaType = FormulaType.Multiply;
		f.arguments = new Formula[]{CheckFormulaArg(lhs), CheckFormulaArg(rhs)};
		return f;
	}
	IFormulaElement Div(IFormulaElement lhs, IFormulaElement rhs) {
		Formula f = new Formula();
		f.formulaType = FormulaType.Divide;
		f.arguments = new Formula[]{CheckFormulaArg(lhs), CheckFormulaArg(rhs)};
		return f;
	}
	IFormulaElement Rem(IFormulaElement lhs, IFormulaElement rhs) {
		Formula f = new Formula();
		f.formulaType = FormulaType.Remainder;
		f.arguments = new Formula[]{CheckFormulaArg(lhs), CheckFormulaArg(rhs)};
		return f;
	}
	IFormulaElement Pow(IFormulaElement lhs, IFormulaElement rhs) {
		Formula f = new Formula();
		f.formulaType = FormulaType.Exponent;
		f.arguments = new Formula[]{CheckFormulaArg(lhs), CheckFormulaArg(rhs)};
		return f;
	}
	IFormulaElement Neg(IFormulaElement arg) {
		Formula outer = new Formula();
		outer.formulaType = FormulaType.Multiply;
		outer.arguments = new Formula[]{
			Formula.Constant(-1),
			CheckFormulaArg(arg)
		};
		return outer;
	}
	IFormulaElement If(IFormulaElement a, IFormulaElement b, IFormulaElement c) {
		Formula outer = new Formula();
		outer.formulaType = FormulaType.BranchIfNotZero;
		outer.arguments = new Formula[]{CheckFormulaArg(a), CheckFormulaArg(b), CheckFormulaArg(c)};
		return outer;
	}


	/*struct FormulaToken {
			enum Type {
				None,

				Number,
				Symbol,

				Comma,
				OpenParen,
				CloseParen,
				OpenBracket,
				CloseBracket,

				Dot,
				Minus,
				Negate,
				Plus,
				Star,
				Slash,
				Caret,

				Root,
				Mean,
				Clamp,
				Random,
				Floor,
				Ceil,
				Round,
				Abs,
				IfEqual,
				IfNotEqual,
				IfGreater,
				IfGreaterOrEqual,
				IfLesser,
				IfLesserOrEqual,
				IfLookup,
				BranchApplierSide,
				BranchAppliedSide,
				Any,

				SkillRef, //the default scope for effects in skills
				ActorRef, //default scope for stats
				TargetRef,
				EquipmentRef, //default scope for equipment params and passive effects?
				StatusRef, //default scope for status effect params and effects?
				ReactedSkillRef,
				ReactedEffectRef,
				FormulaRef
			}
			public Type type;
			public float numberValue;
			public string symbolValue;
			public FormulaToken() {
				type = Type.None;
			}
			public static IsInfixOperatorType(Type t) {
				switch(t) {
					case Dot:
					case Minus:
					case Plus:
					case Star:
					case Slash:
					case Caret:
						return true;
					default:
						return false;
				}
			}
			public FormulaToken(string s, Type previous) {
				if(float.TryParse(s, out numberValue)) {
					type = Type.Number;
				} else {
					numberValue = float.NaN;
					symbolValue = s;
					if(s == ",") {
						type = Type.Comma;
					} else if(s == "(") {
						type = Type.OpenParen;
					} else if(s == ")") {
						type = Type.CloseParen;
					} else if(s == "{") {
						type = Type.OpenBracket;
					} else if(s == "}") {
						type = Type.CloseBracket;
					} else if(s == ".") {
						type = Type.Dot;
					} else if(s == "-") {
						if(IsInfixOperatorType(previous) ||
							 previous == Type.Negate ||
						   previous == Type.OpenBracket ||
						   previous == Type.OpenParen ||
						   previous == Type.Comma ||
						   previous == Type.None) {
							type = Type.Negate;
						} else {
							type = Type.Minus;
						}
					} else if(s == "+") {
						type = Type.Plus;
					} else if(s == "*") {
						type = Type.Star;
					} else if(s == "/") {
						type = Type.Slash;
					} else if(s == "^") {
						type = Type.Caret;
					} else if(s == "root") {
						type = Type.Root;
					} else if(s == "mean" || s == "average" || s == "avg") {
						type = Type.Mean;
					} else if(s == "clamp") {
						type = Type.Clamp;
					} else if(s == "random") {
						type = Type.Random;
					} else if(s == "floor") {
						type = Type.Floor;
					} else if(s == "ceil") {
						type = Type.Ceil;
					} else if(s == "round") {
						type = Type.Round;
					} else if(s == "abs") {
						type = Type.Abs;
					} else if(s == "ifEqual") {
						type = Type.IfEqual;
					} else if(s == "ifNotEqual") {
						type = Type.IfNotEqual;
					} else if(s == "ifGreater") {
						type = Type.IfGreater;
					} else if(s == "ifGreaterOrEqual") {
						type = Type.IfGreaterOrEqual;
					} else if(s == "ifLesser") {
						type = Type.IfLesser;
					} else if(s == "ifLesserOrEqual") {
						type = Type.IfLesserOrEqual;
					} else if(s == "ifLookup") {
						type = Type.IfLookup;
					} else if(s == "branchApplierSide") {
						type = Type.BranchApplierSide;
					} else if(s == "branchAppliedSide") {
						type = Type.BranchAppliedSide;
					} else if(s == "any") {
						type = Type.Any;
					} else if(s == "skill") {
						type = Type.SkillRef;
					} else if(s == "c") {
						type = Type.ActorRef;
					} else if(s == "target") {
						type = Type.TargetRef;
					} else if(s == "eq") {
						type = Type.EquipmentRef;
					} else if(s == "status") {
						type = Type.StatusRef;
					} else if(s == "reactedSkill") {
						type = Type.ReactedSkillRef;
					} else if(s == "reactedEffect") {
						type = Type.ReactedEffectRef;
					} else if(s == "f") {
						type = Type.NamedFormulaRef;
					} else {
						type = Type.Symbol;
						symbolValue = s;
					}
				}
			}
		}
		public static bool Compile(Formula f, out string error) {
			FormulaCompiler fc = new FormulaCompiler(f);
			return fc.ParseInPlace(out error);
		}
		Formula formula;
		public FormulaCompiler(Formula f) {
			formula = f;
		}
		List<FormulaToken> Tokenize(out string error) {
			error = "";
			return Regex.Split(f.text, @"\s+").Select(s => new FormulaToken(s));
		}
		bool ParseInPlace(out string error) {
			string localError = null;
			List<FormulaToken> tokens = Tokenize(out localError);
			if(tokens == null) {
				error = "tokenization error:"+localError;
				return false;
			}
			if(tokens.Count == 0) {
				error = "empty formula";
				formula.type = FormulaType.Undefined;
				return false;
			}
			if(!ConsumePhraseInto(tokens, formula, out localError)) {
				error = localError;
				return false;
			}
			return true;
		}
		bool ConsumePhraseInto(List<FormulaToken> toks, ref int i, Formula f, out string error) {
			switch(toks[i].type) {
				case FormulaToken.Type.Symbol:
					i++;
					return true;
				case FormulaToken.Type.Negate:
					if(!ConsumeUnaryOp(toks, ref i, f, out localError)) {
						error = localError;
						return false;
					}
					return true;
				case FormulaToken.Type.Root:
				case FormulaToken.Type.Mean:
				case FormulaToken.Type.Clamp:
				case FormulaToken.Type.Random:
				case FormulaToken.Type.Floor:
				case FormulaToken.Type.Ceil:
				case FormulaToken.Type.Round:
				case FormulaToken.Type.Abs:
				case FormulaToken.Type.IfEqual:
				case FormulaToken.Type.IfNotEqual:
				case FormulaToken.Type.IfGreater:
				case FormulaToken.Type.IfGreaterOrEqual:
				case FormulaToken.Type.IfLesser:
				case FormulaToken.Type.IfLesserOrEqual:
				case FormulaToken.Type.IfLookup:
				case FormulaToken.Type.BranchApplierSide:
				case FormulaToken.Type.BranchAppliedSide:
				case FormulaToken.Type.Any:
					if(!ConsumePrefixOp(toks, ref i, f, out localError)) {
						error = localError;
						return false;
					}
					return true;
				case FormulaToken.Type.SkillRef:
				case FormulaToken.Type.ActorRef:
				case FormulaToken.Type.TargetRef:
				case FormulaToken.Type.EquipmentRef:
				case FormulaToken.Type.StatusRef:
				case FormulaToken.Type.ReactedSkillRef:
				case FormulaToken.Type.ReactedEffectRef:
				case FormulaToken.Type.FormulaRef:
				case FormulaToken.Type.Symbol:
					//lookup
					if(!ConsumeLookup(toks, ref i, f, out localError)) {
						error = localError;
						return false;
					}
					return true;
				default:
					//try for a
					error = "bad phrase start at token "+i+":"+toks[i].type;
					return false;
			}
		}*/
}