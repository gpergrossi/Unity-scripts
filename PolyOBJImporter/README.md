[Poly Object Importer]
As long as this script and Surface shader exist within your Unity project assets, you should be able to import any Blocks model from Google Poly.

[Installation]
1. Create a C# script named "PolyObjImporter" in any asset folder you want and copy the code from "PolyObjImporter.cs" into it.
2. Create a Surface shader named "VertexColors" anywhere you like and copy the code from "VertexColors.shader" into it.
3. Copy the LICENSE file from this repository's root directoy into your project and rename it "PolyOBJImporter-LICENSE"

[Usage]
1. Download a blocks model .obj from Google Poly.
2. Unzip the zip file
3. Rename the .obj file to a .polyobj extension (this is necessary for Unity to use the script)
4. Open Unity and "import new asset" in whichever asset folder you like:
   *Make sure to import both the ".polyobj" and ".mtl" file*

If Unity ever needs to re-import this asset, it will look for the .mtl file in the same folder.
If you never intend to re-import, you may delete the '.mtl' asset (It should look like a blank page and will probably be named "materials").
