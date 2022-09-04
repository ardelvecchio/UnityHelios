# UnityHelios

This repository is designed as an application for Helios, the software created by Brian Bailey and the Plant Simulation Lab at UC Davis which can be foud here: https://baileylab.ucdavis.edu/software/helios/ 

XML and OBJ files are created in Helios, and then imported into the UnityHelios program.

Once the OBJ file or XML file has been created, they can be read by the program and the models can be loaded. The plant models contain tens of thousands of vertices which can customized in Helios before being parsed into a procedural mesh in the Unity application.

OBJ models can be loaded with radation applied to get following model: The scale is draw to apply the radiation to the tree, and the tree is rotated upon the axis shown in the red circle, in this case the 'Y' axis. Clicking on the circle will change the desired axis of rotation.

![SpinningTree](https://user-images.githubusercontent.com/81535423/183919368-8dfa26e0-ba79-4c4f-90db-bdd2a439fdf9.gif)

Similarly, Point Clouds can be loaded. The corresponding LiDAR data is read from XYZ files to get the following model:

![LiDARgif2](https://user-images.githubusercontent.com/81535423/183904447-25accb71-ebc0-4494-9e9f-c17c3eb020fa.gif)

Here three Walnut Trees over a dirt patch are loaded into the scene.

![WalnutTree](https://user-images.githubusercontent.com/81535423/186017997-a0e6e775-2dc4-4d16-87e3-bcff8634e314.png)
