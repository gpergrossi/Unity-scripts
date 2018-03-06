# Poly Object Importer
As long as this script and Surface shader exist within your Unity project assets, you should be able to import any Blocks model from Google Poly.

## Installation (Option 1)
1. Download the "PolyObjImporter.unitypackage" file and "import custom package" into Unity.
You may move the both the script and surface shader files to whichever asset folder you like,
they do not need to remain together.

## Installation (Option 2)
1. Create a C# script named "PolyObjImporter" in any asset folder you want and copy the code from "PolyObjImporter.cs" into it.
2. Create a Surface shader named "VertexColors" anywhere you like and copy the code from "VertexColors.shader" into it.

## Usage
1. Download a blocks model .obj from Google Poly.
2. Unzip the zip file
3. Rename the .obj file to a .polyobj extension (this is necessary for Unity to use the script)
4. Open Unity and "import new asset" in whichever asset folder you like:
   **_Make sure to import both the ".polyobj" and ".mtl" file_**

If Unity ever needs to re-import this asset, it will look for the .mtl file in the same folder.
If you never intend to re-import, you may delete the '.mtl' asset (It should look like a blank page and will probably be named "materials").
