using UnityEngine;
using NodeEditorFramework;

[Node (false, "Effect/Warp Node", false)]
public class WarpNode : Node 
{
	public enum WarpType { ShearX, ShearY, ShearZ }
	public WarpType type = WarpType.ShearX;
	public float strength = 1.0f;

	public const string ID = "warpNode";
	public override string GetID { get { return ID; } }
	
	public override Node Create (Vector2 pos) 
	{
		WarpNode node = CreateInstance<WarpNode> ();
		
		node.rect = new Rect (pos.x, pos.y, 200, 100);
		node.name = "Warp Node";
		
		node.CreateInput ("Value", "GameObject");
		node.CreateOutput ("Output Obj", "GameObject");
		
		return node;
	}
	
	public override void NodeGUI () 
	{
		UnityEditor.EditorGUIUtility.labelWidth = 100;

		GUILayout.BeginHorizontal ();
		GUILayout.BeginVertical ();

		Inputs [0].DisplayLayout ();

		GUILayout.EndVertical ();
		GUILayout.BeginVertical ();
		
		Outputs [0].DisplayLayout ();
		
		GUILayout.EndVertical ();
		GUILayout.EndHorizontal ();

		type = (WarpType)UnityEditor.EditorGUILayout.EnumPopup (
			new GUIContent ("CSG Operation", "The type of warp performed on Input 1"), type);
		strength = UnityEditor.EditorGUILayout.FloatField ("Strength", strength);

		if (GUI.changed)
			NodeEditor.RecalculateFrom (this);
	}
	
	public override bool Calculate () 
	{
		if (!allInputsReady ())
			return false;
		Outputs[0].SetValue<GameObject> (Inputs[0].connection.GetValue<GameObject> ());
		return true;
	}
}
