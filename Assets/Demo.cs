using System;
using UnityEngine;
using Net3dBool;

public class Demo : MonoBehaviour
{
	public Material ObjMaterial;
    public Net3dBool.Solid mesh;

	public Net3dBool.Color3f[] getColorArray(int length, Color c)
	{
		var ar = new Net3dBool.Color3f[length];
		for (var i = 0; i < length; i++)
			ar[i] = new Net3dBool.Color3f(c.r, c.g, c.b);
		return ar;
	}

    public void Start()
    {

        var box = new Net3dBool.Solid(Net3dBool.DefaultCoordinates.DEFAULT_BOX_VERTICES, 
		                              	 Net3dBool.DefaultCoordinates.DEFAULT_BOX_COORDINATES, 
		                              	 getColorArray(Net3dBool.DefaultCoordinates.DEFAULT_BOX_VERTICES.Length, Color.red));
        
		var sphere = new Net3dBool.Solid(Net3dBool.DefaultCoordinates.DEFAULT_SPHERE_VERTICES, 
		                                 Net3dBool.DefaultCoordinates.DEFAULT_SPHERE_COORDINATES, 
		                                 getColorArray(Net3dBool.DefaultCoordinates.DEFAULT_SPHERE_VERTICES.Length, Color.red));
        sphere.scale(0.68, 0.68, 0.68);

        var cylinder1 = new Net3dBool.Solid(Net3dBool.DefaultCoordinates.DEFAULT_CYLINDER_VERTICES, 
		                                 Net3dBool.DefaultCoordinates.DEFAULT_CYLINDER_COORDINATES, 
		                                 getColorArray(Net3dBool.DefaultCoordinates.DEFAULT_CYLINDER_VERTICES.Length, Color.green));
        cylinder1.scale(0.38, 1, 0.38);

        var cylinder2 = new Net3dBool.Solid(Net3dBool.DefaultCoordinates.DEFAULT_CYLINDER_VERTICES, 
		                                 Net3dBool.DefaultCoordinates.DEFAULT_CYLINDER_COORDINATES, 
		                                 getColorArray(Net3dBool.DefaultCoordinates.DEFAULT_CYLINDER_VERTICES.Length, Color.green));
        cylinder2.scale(0.38, 1, 0.38);
        cylinder2.rotate(Math.PI / 2, 0);

        var cylinder3 = new Net3dBool.Solid(Net3dBool.DefaultCoordinates.DEFAULT_CYLINDER_VERTICES, 
		                                 Net3dBool.DefaultCoordinates.DEFAULT_CYLINDER_COORDINATES, 
		                                 getColorArray(Net3dBool.DefaultCoordinates.DEFAULT_CYLINDER_VERTICES.Length, Color.green));
        cylinder3.scale(0.38, 1, 0.38);
        cylinder3.rotate(Math.PI / 2, 0);
        cylinder3.rotate(0, Math.PI / 2);

        //--

        //mesh = s;

        //--

//            var modeller = new Net3dBool.BooleanModeller(b, c1);
//            mesh = modeller.getDifference();

        //--

        var modeller = new Net3dBool.BooleanModeller(box, sphere);
        var tmp = modeller.getIntersection();

        modeller = new Net3dBool.BooleanModeller(tmp, cylinder1);
        tmp = modeller.getDifference();

        modeller = new Net3dBool.BooleanModeller(tmp, cylinder2);
        tmp = modeller.getDifference();

        modeller = new Net3dBool.BooleanModeller(tmp, cylinder3);
        tmp = modeller.getDifference();

        mesh = tmp;

		CSGGameObject.GenerateMesh (gameObject, ObjMaterial, mesh);
	}

}

