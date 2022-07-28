// ImgSpc Exporters v1.3
// Copyright 2015 Imaginary Spaces
// http://imgspc.com


Imaginary Spaces have built a Unity package that will help you export 3d models
from your game. That means your players can build a scene and get it 3d
printed, or upload it to a web viewer like Sketchfab.

The tool is also useful to debug a scene: you can export a live scene from the
Unity editor or player and import it into your 3d modeling program.

==========================
Exporting from the editor:
==========================

1. Select objects from scene hierarchy.

2. Go to File -> Export Selected -> Export to [STL|OBJ|SVG|Schematic]

We support four export formats:
*   STL is great for monochrome 3d printing.

*   OBJ is better for processing in modeling programs because it keeps track of the
    objects, and keeps the UVs and normals. The materials are also exported to an MTL file.

*   SVG gives you a wireframe drawing from angle of the main camera,
    good for giving an engineering or architectural drawing feel.

*   Schematic gives you a voxelized version of your model. 
    This format makes it easy to import your model into Minecraft or MCEdit (http://www.mcedit.net/).


==========================
Exporting during runtime:
==========================

0. Look at the example for how we do it, or follow the bullets below:

1. Attach the ImgSpcExporter script to a UI element

2. Drag the GameObjects you'd like to export from the hierarchy onto the
   "Objects to Export" list.

3. Hook up a UI element like an InputField to call the "SetFilename" slot
   on the ImgSpcExporter, or set the filename field by hand.
   If you use a relative path, it will be relative to the persistent data
   path.
      http://docs.unity3d.com/ScriptReference/Application-persistentDataPath.html

4. Hook up a UI element, for example a button, to run the "Export" slot
   on the ImgSpcExporter.


==========================
Export Marker
==========================

You can use the ImgSpcExportMarker component to guide how the export happens.
This is useful for example if you want to show an slender object in the game
but use a thicker object for 3d printing.
    1. Add the ImgSpcExportMarker component to the slender object.
    2. Uncheck 'Self' and 'Children'
    3. Create a thick object for 3d printing, and remove the renderer.
    4. Add an ImgSpcExportMarker component to the thick object.
    5. Uncheck 'RequireRenderer'
You can also add GameObjects to the "Other Objects" list to export GameObjects
which may be related but are not children of the object.


==========================
API access
==========================

You have the full source code of this asset, so as a programmer your power is
boundless. That said, we recommend you don't change the source code yourself or
add files in the package directories, so you don't get into trouble when we
release a new version and you want to upgrade.

Look at STLExporter.cs for an example of how to write a new exporter.

Look at the "Apply" functions in the editor script ExportSelected.cs to see how
to add a menu option.


==========================
Special cases
==========================

* If you try to save with an unsupported extension, then it will keep both
  extensions (e.g. when you select "Export to STL" and try to save a file called
  mesh.foo it will rename the file mesh.foo.stl)

* If you didn't actually select anything, you'll get a message in the console log.


==========================
Support
==========================

Please let us know how this package helped you, and how we can improve it for you.

Unity Forum:

Email: info@imgspc.com
