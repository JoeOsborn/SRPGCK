using UnityEditor;
using UnityEngine;
using System.Text;
using System.IO;

public class MapExporter : ScriptableWizard {
	public Map map;
  [MenuItem ("SRPGCK/Export Map")]
  static void CreateWizard () {
    ScriptableWizard.DisplayWizard<MapExporter>("Export Map", "Export");
  }
  void OnWizardCreate () {
		if(!map) { return; }
		StringBuilder exportStr = new StringBuilder(1024);
		Vector2 size = map.size;
		exportStr.Append("[\n");
		for(int y = 0; y < size.y; y++) {
			exportStr.Append("\t[");
			for(int x = 0; x < size.x; x++) {
				MapColumn tc = map.TileColumnAt(x,y);
				if(tc.Count == 0) { exportStr.Append("[]"); }
				else {
					exportStr.Append("[");
					//emit Z range and tile, Z range and tile, Z range and tile...
					for(int i = 0; i < tc.Count; i++) {
						MapTile t = tc.At(i);
						int zMin = t.z;
						int h = t.maxHeight;
						exportStr.AppendFormat("{{{0},{1},cube}}",zMin,h);
						if(i < tc.Count-1) {
							exportStr.Append(",");
						}
					}
					exportStr.Append("]");
				}
				if(x < size.x-1) {
					exportStr.Append(", ");
				}
			}
			exportStr.Append("]");
			if(y < size.y-1) { exportStr.Append(","); }
			exportStr.Append("\n");
		}
		exportStr.Append("].");
		File.WriteAllText(Application.dataPath + "/"+map.name+"_export.txt", exportStr.ToString());
    AssetDatabase.Refresh();
  }
}
