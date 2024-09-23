# World-Altering Editor User Manual

We try to make the editor's user interface intuitive, but in case it is not enough, this guide contains some helpful tips and tricks.

**Note:** Most keybinds mentioned here can be changed in the *Keyboard Options* menu. This guide assumes you use the defaults.

## Painting terrain

To paint terrain, select a tile from either the top bar or from the TileSet selector in the bottom of the screen. Then you can simply click to place it on the map.

**To fill an enclosed area**, select a 1x1 sized tile and click on the area while holding Ctrl. This logic is similar to the "bucket tool" in MS Paint and many other image and map editors.

### Painting water

To paint water, select the Water TileSet from the TileSet selector in the bottom of the screen. Then select the 1x1 sized tile and paint. You can increase the brush size to cover larger areas, or use the fill functionality mentioned above.

### But my water looks all samey if I only use the 1x1 tile?

Once you are done detailing the map, you can run *Tools -> Run Script... -> Smoothen Water.cs*. The script will randomize all the water tiles on the map.

## Copy and paste

Like in most programs, Ctrl+C and Ctrl+V keys enable regular rectangular copy and paste features. They can also be accessed from the Edit menu.

Alt+C activates a tool for copying a custom-shaped area.

### Copying more than just terrain

Sometimes you might want to copy more than just terrain: buildings, units, trees, overlay etc. You can select what map elements are copied from *Edit -> Configure Copied Objects*.