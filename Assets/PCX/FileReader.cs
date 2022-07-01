using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;

public class FileReader
{
    public List<string> filepaths = new List<string>();
    public List<string[]> asciiParams = new List<string[]>();
    public float[] origins = new float[3];
    
    public void processXML(string XMLpath)
    {
        List<string[]> origins = new List<string[]>();
        XmlTextReader reader = new XmlTextReader(XMLpath);
        reader.WhitespaceHandling = WhitespaceHandling.None;
        while (reader.Read())
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    if (reader.Name == "filename")
                    {
                        reader.Read();
                        filepaths.Add(AddFullPath(XMLpath, reader.Value.Trim()));
                    }
                    if (reader.Name == "ASCII_format")
                    {
                        reader.Read();
                        asciiParams.Add(reader.Value.Trim().Split(" "));
                    }
                    if (reader.Name == "origin")
                    {
                        reader.Read();
                        origins.Add(reader.Value.Trim().Split(" "));
                    }
                    break;
            }
        }

        float x = 0;
        float y = 0;
        float z = 0;
        
        for (int i = 0; i < origins.Count; i++)
        {
            x += (float.Parse(origins[i][0], CultureInfo.InvariantCulture.NumberFormat));
            y += (float.Parse(origins[i][1], CultureInfo.InvariantCulture.NumberFormat));
            z += (float.Parse(origins[i][2], CultureInfo.InvariantCulture.NumberFormat));
        }
        x /= origins.Count;
        y /= origins.Count;
        z /= origins.Count;
        this.origins[0] = x;
        this.origins[1] = y;
        this.origins[2] = z;
    }

    private string AddFullPath(string pathA, string pathB)
    {
        var basePath = pathA.IndexOf("Helios");
        var absolutePath = pathA.Substring(0, basePath);
        absolutePath += "Helios\\";

        return Path.Combine(absolutePath, pathB);
    }
}