
namespace Obj
{
    using System.Collections.Generic;
    using UnityEngine;

    public class ModelData {

        public List<MeshData> meshes = new List<MeshData>();
        public Dictionary<string, Material> materials = new Dictionary<string, Material>();
        //add list of lists of colors for Color data
        public List<List<Color>> datColors = new List<List<Color>>();
        public string materialsLibraryName;
    }
}