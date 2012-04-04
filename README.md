#The SRPG Construction Kit
Please see `Documentation.pdf` for now!
#LICENSE
The SRPGCK is comprised of two components: the SRPGCK Editor Scripts and the SRPGCK Library. The Editor Scripts are licensed under the LGPL, due to a dependency on the LGPL’d [Sasa.Parsing library]( http://sourceforge.net/projects/sasa/ "Sasa Library"). The Library is licensed under the 3-clause BSD license.
Since the source code of both the Editor Scripts and the Library are provided, Sasa.Parsing can be replaced with an interface-equivalent module, so I believe this conforms to the terms of the LGPL. Furthermore, since it is my understanding that editor scripts are linked into a separate assembly from game scripts, the LGPL’s linking requirement is not violated.

The Editor Scripts also make use of Daniele Giardini's HOEditorUndoManager.

Sasa.Parsing: Copyright (c) Sandro Magi. LGPL.
HOEditorUndoManager.cs: Created by Daniele Giardini - 2011 - Holoville.
