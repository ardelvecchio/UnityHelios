using UnityEngine;
using UnityEngine.Rendering;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Pcx
{
    
    public class PlyImporter
    {
        #region ScriptedImporter implementation

        public enum ContainerType
        {
            Mesh,
            ComputeBuffer,
            Texture
        }

        
        #endregion

        #region Internal data structure

        enum DataFormat
        {
            Undefined = -1,
            Ascii,
            BinaryLittleEndian
        };

        enum DataProperty
        {
            Invalid,
            R8,
            G8,
            B8,
            A8,
            R16,
            G16,
            B16,
            A16,
            R32,
            G32,
            B32,
            A32,
            SingleX,
            SingleY,
            SingleZ,
            DoubleX,
            DoubleY,
            DoubleZ,
            Data8,
            Data16,
            Data32,
            Data64
        }

        static int GetPropertySize(DataProperty p)
        {
            switch (p)
            {
                case DataProperty.R8: return 1;
                case DataProperty.G8: return 1;
                case DataProperty.B8: return 1;
                case DataProperty.A8: return 1;
                case DataProperty.R16: return 2;
                case DataProperty.G16: return 2;
                case DataProperty.B16: return 2;
                case DataProperty.A16: return 2;
                case DataProperty.R32: return 4;
                case DataProperty.G32: return 4;
                case DataProperty.B32: return 4;
                case DataProperty.A32: return 4;
                case DataProperty.SingleX: return 4;
                case DataProperty.SingleY: return 4;
                case DataProperty.SingleZ: return 4;
                case DataProperty.DoubleX: return 8;
                case DataProperty.DoubleY: return 8;
                case DataProperty.DoubleZ: return 8;
                case DataProperty.Data8: return 1;
                case DataProperty.Data16: return 2;
                case DataProperty.Data32: return 4;
                case DataProperty.Data64: return 8;
            }

            return 0;
        }

        class DataHeader
        {
            public DataFormat dataFormat = DataFormat.Undefined;
            public List<DataProperty> properties = new List<DataProperty>();
            public int vertexCount = -1;
        }

        class DataBody
        {
            public List<Vector3> vertices;
            public List<Color32> colors;

            public DataBody(int vertexCount = 0)
            {
                vertices = new List<Vector3>(vertexCount);
                colors = new List<Color32>(vertexCount);
            }

            public void AddPoint(
                float x, float y, float z,
                byte r, byte g, byte b, byte a
            )
            {
                vertices.Add(new Vector3(x, y, z));
                colors.Add(new Color32(r, g, b, a));
            }
        }

        #endregion

        #region Reader implementation

        void ReadHeaderAndData(string path, out DataHeader header, out DataBody body)
        {
            var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);

            header = ReadDataHeader(new StreamReader(stream));

            body = null;
            switch (header.dataFormat)
            {
                case DataFormat.BinaryLittleEndian:
                    body = ReadDataBodyFormatBinaryLittleEndian(header, new BinaryReader(stream));
                    break;

                case DataFormat.Ascii:
                    body = ReadDataBodyFormatAscii(header, new StreamReader(stream));
                    break;
            }
        }

        public Mesh ImportAsMesh(string path)
        {
            try
            {
                ReadHeaderAndData(path, out var header, out var body);

                var mesh = new Mesh();
                mesh.name = Path.GetFileNameWithoutExtension(path);

                mesh.indexFormat = header.vertexCount > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;

                mesh.SetVertices(body.vertices);
                mesh.SetColors(body.colors);

                mesh.SetIndices(
                    Enumerable.Range(0, header.vertexCount).ToArray(),
                    MeshTopology.Points, 0
                );

                mesh.UploadMeshData(true);
                return mesh;
            }
            catch (Exception e)
            {
                Debug.LogError("Failed importing " + path + ". " + e.Message);
                return null;
            }
        }
        
        public Mesh ImportXYZAsMesh(string[] path, string[][] dataOrder)
        {
            try
            {
                var body = ReadDataBodyFormatXYZ(path, dataOrder);

                var mesh = new Mesh();
                mesh.name = Path.GetFileNameWithoutExtension(path[0]);

                mesh.indexFormat = body.vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;

                mesh.SetVertices(body.vertices);
                mesh.SetColors(body.colors);

                mesh.SetIndices(
                    Enumerable.Range(0, body.vertices.Count).ToArray(),
                    MeshTopology.Points, 0
                );

                mesh.UploadMeshData(true);
                return mesh;
            }
            catch (Exception e)
            {
                Debug.LogError("Failed importing " + path + ". " + e.Message);
                return null;
            }
        }
        
        public PointCloudData ImportAsPointCloudData(string path)
        {
            try
            {
                ReadHeaderAndData(path, out var header, out var body);

                var data = ScriptableObject.CreateInstance<PointCloudData>();
                data.Initialize(body.vertices, body.colors);
                data.name = Path.GetFileNameWithoutExtension(path);
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError("Failed importing " + path + ". " + e.Message);
                return null;
            }
        }
        
        public PointCloudData ImportAsPointCloudDataXYZ(string[] paths, string[][] dataOrder)
        {
            try
            {
                var body = ReadDataBodyFormatXYZ(paths, dataOrder);

                var data = ScriptableObject.CreateInstance<PointCloudData>();
                data.Initialize(body.vertices, body.colors);
                data.name = Path.GetFileNameWithoutExtension(paths[0]);
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError("Failed importing " + paths[0] + ". " + e.Message);
                return null;
            }
        }
    /*
        BakedPointCloud ImportAsBakedPointCloud(string path)
        {
            try
            {
                ReadHeaderAndData(path, out var header, out var body);

                var data = ScriptableObject.CreateInstance<BakedPointCloud>();
                data.Initialize(body.vertices, body.colors);
                data.name = Path.GetFileNameWithoutExtension(path);
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError("Failed importing " + path + ". " + e.Message);
                return null;
            }
        }
    */
        DataHeader ReadDataHeader(StreamReader reader)
        {
            var data = new DataHeader();
            var readCount = 0;

            // Magic number line ("ply")
            var line = reader.ReadLine();
            readCount += line.Length + 1;
            if (line != "ply")
                throw new ArgumentException("Magic number ('ply') mismatch.");

            // Data format: check if it's binary/little endian.
            line = reader.ReadLine();
            readCount += line.Length + 1;

            switch (line)
            {
                case "format binary_little_endian 1.0":
                    data.dataFormat = DataFormat.BinaryLittleEndian;
                    break;

                case "format ascii 1.0":
                    data.dataFormat = DataFormat.Ascii;
                    break;

                default:
                    throw new ArgumentException(
                        $"Invalid data format ('{line}'). Should be binary(little endian) or ASCII");
            }

            // Read header contents.
            for (var skip = false;;)
            {
                // Read a line and split it with white space.
                line = reader.ReadLine();
                readCount += line.Length + 1;
                if (line == "end_header") break;
                var col = line.ToLower().Split();

                if (col[0] == "comment")
                {
                    skip = true;
                }
                else if (col[0] == "element") // Element declaration (unskippable)
                {
                    if (col[1] == "vertex")
                    {
                        data.vertexCount = Convert.ToInt32(col[2]);
                        skip = false;
                    }
                    else
                    {
                        // Don't read elements other than vertices.
                        skip = true;
                    }
                }

                if (skip) continue;

                // Property declaration line
                if (col[0] == "property")
                {
                    var prop = DataProperty.Invalid;

                    // Parse the property name entry.
                    switch (col[2])
                    {
                        case "red":
                            prop = DataProperty.R8;
                            break;
                        case "green":
                            prop = DataProperty.G8;
                            break;
                        case "blue":
                            prop = DataProperty.B8;
                            break;
                        case "alpha":
                            prop = DataProperty.A8;
                            break;
                        case "x":
                            prop = DataProperty.SingleX;
                            break;
                        case "y":
                            prop = DataProperty.SingleY;
                            break;
                        case "z":
                            prop = DataProperty.SingleZ;
                            break;
                    }

                    // Check the property type.
                    if (col[1] == "char" || col[1] == "uchar" ||
                        col[1] == "int8" || col[1] == "uint8")
                    {
                        if (prop == DataProperty.Invalid)
                            prop = DataProperty.Data8;
                        else if (GetPropertySize(prop) != 1)
                            throw new ArgumentException("Invalid property type ('" + line + "').");
                    }
                    else if (col[1] == "short" || col[1] == "ushort" ||
                             col[1] == "int16" || col[1] == "uint16")
                    {
                        switch (prop)
                        {
                            case DataProperty.Invalid:
                                prop = DataProperty.Data16;
                                break;
                            case DataProperty.R8:
                                prop = DataProperty.R16;
                                break;
                            case DataProperty.G8:
                                prop = DataProperty.G16;
                                break;
                            case DataProperty.B8:
                                prop = DataProperty.B16;
                                break;
                            case DataProperty.A8:
                                prop = DataProperty.A16;
                                break;
                        }

                        if (GetPropertySize(prop) != 2)
                            throw new ArgumentException("Invalid property type ('" + line + "').");
                    }
                    else if (col[1] == "int" || col[1] == "uint" || col[1] == "float" ||
                             col[1] == "int32" || col[1] == "uint32" || col[1] == "float32")
                    {
                        switch (prop)
                        {
                            case DataProperty.Invalid:
                                prop = DataProperty.Data32;
                                break;
                            case DataProperty.R8:
                                prop = DataProperty.R32;
                                break;
                            case DataProperty.G8:
                                prop = DataProperty.G32;
                                break;
                            case DataProperty.B8:
                                prop = DataProperty.B32;
                                break;
                            case DataProperty.A8:
                                prop = DataProperty.A32;
                                break;
                        }

                        if (GetPropertySize(prop) != 4)
                            throw new ArgumentException("Invalid property type ('" + line + "').");
                    }
                    else if (col[1] == "int64" || col[1] == "uint64" ||
                             col[1] == "double" || col[1] == "float64")
                    {
                        switch (prop)
                        {
                            case DataProperty.Invalid:
                                prop = DataProperty.Data64;
                                break;
                            case DataProperty.SingleX:
                                prop = DataProperty.DoubleX;
                                break;
                            case DataProperty.SingleY:
                                prop = DataProperty.DoubleY;
                                break;
                            case DataProperty.SingleZ:
                                prop = DataProperty.DoubleZ;
                                break;
                        }

                        if (GetPropertySize(prop) != 8)
                            throw new ArgumentException("Invalid property type ('" + line + "').");
                    }
                    else
                    {
                        throw new ArgumentException("Unsupported property type ('" + line + "').");
                    }

                    data.properties.Add(prop);
                }
            }


            if (data.dataFormat == DataFormat.BinaryLittleEndian)
            {
                // Rewind the stream back to the exact position of the reader (LF line endings)
                reader.BaseStream.Position = readCount;
            }
            else
            {
                reader.BaseStream.Position = 0;
            }

            return data;
        }

        DataBody ReadDataBodyFormatBinaryLittleEndian(DataHeader header, BinaryReader reader)
        {
            var data = new DataBody(header.vertexCount);

            float x = 0, y = 0, z = 0;
            Byte r = 255, g = 255, b = 255, a = 255;

            for (var i = 0; i < header.vertexCount; i++)
            {
                foreach (var prop in header.properties)
                {
                    switch (prop)
                    {
                        case DataProperty.R8:
                            r = reader.ReadByte();
                            break;
                        case DataProperty.G8:
                            g = reader.ReadByte();
                            break;
                        case DataProperty.B8:
                            b = reader.ReadByte();
                            break;
                        case DataProperty.A8:
                            a = reader.ReadByte();
                            break;

                        case DataProperty.R16:
                            r = (byte)(reader.ReadUInt16() >> 8);
                            break;
                        case DataProperty.G16:
                            g = (byte)(reader.ReadUInt16() >> 8);
                            break;
                        case DataProperty.B16:
                            b = (byte)(reader.ReadUInt16() >> 8);
                            break;
                        case DataProperty.A16:
                            a = (byte)(reader.ReadUInt16() >> 8);
                            break;

                        case DataProperty.R32:
                            r = (byte)(reader.ReadSingle() * 255.0f);
                            break;
                        case DataProperty.G32:
                            g = (byte)(reader.ReadSingle() * 255.0f);
                            break;
                        case DataProperty.B32:
                            b = (byte)(reader.ReadSingle() * 255.0f);
                            break;
                        case DataProperty.A32:
                            a = (byte)(reader.ReadSingle() * 255.0f);
                            break;

                        case DataProperty.SingleX:
                            x = reader.ReadSingle();
                            break;
                        case DataProperty.SingleY:
                            y = reader.ReadSingle();
                            break;
                        case DataProperty.SingleZ:
                            z = reader.ReadSingle();
                            break;

                        case DataProperty.DoubleX:
                            x = (float)reader.ReadDouble();
                            break;
                        case DataProperty.DoubleY:
                            y = (float)reader.ReadDouble();
                            break;
                        case DataProperty.DoubleZ:
                            z = (float)reader.ReadDouble();
                            break;

                        case DataProperty.Data8:
                            reader.ReadByte();
                            break;
                        case DataProperty.Data16:
                            reader.BaseStream.Position += 2;
                            break;
                        case DataProperty.Data32:
                            reader.BaseStream.Position += 4;
                            break;
                        case DataProperty.Data64:
                            reader.BaseStream.Position += 8;
                            break;
                    }
                }

                data.AddPoint(x, y, z, r, g, b, a);
            }

            return data;
        }

        DataBody ReadDataBodyFormatAscii(DataHeader header, StreamReader reader)
        {
            var data = new DataBody(header.vertexCount);

            float x = 0, y = 0, z = 0;
            Byte r = 255, g = 255, b = 255, a = 255;

            var line = reader.ReadLine();

            // Skip header
            while (line != "end_header")
            {
                line = reader.ReadLine();
            }

            // Parse data according to properties list
            int propertiesCount = header.properties.Count;

            var ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            ci.NumberFormat.NumberDecimalSeparator = ".";

            for (var i = 0; i < header.vertexCount; ++i)
            {
                line = reader.ReadLine();
                var values = line.Split();

                for (int j = 0; j < propertiesCount; ++j)
                {
                    var prop = header.properties[j];
                    var value = values[j];

                    switch (prop)
                    {
                        case DataProperty.R8:
                            r = Byte.Parse(value);
                            break;
                        case DataProperty.G8:
                            g = Byte.Parse(value);
                            break;
                        case DataProperty.B8:
                            b = Byte.Parse(value);
                            break;
                        case DataProperty.A8:
                            a = Byte.Parse(value);
                            break;

                        case DataProperty.R16:
                            r = (byte)(UInt16.Parse(value) >> 8);
                            break;
                        case DataProperty.G16:
                            g = (byte)(UInt16.Parse(value) >> 8);
                            break;
                        case DataProperty.B16:
                            b = (byte)(UInt16.Parse(value) >> 8);
                            break;
                        case DataProperty.A16:
                            a = (byte)(UInt16.Parse(value) >> 8);
                            break;

                        case DataProperty.R32:
                            r = (byte)(float.Parse(value, NumberStyles.Float, ci) * 255.0f);
                            break;
                        case DataProperty.G32:
                            g = (byte)(float.Parse(value, NumberStyles.Float, ci) * 255.0f);
                            break;
                        case DataProperty.B32:
                            b = (byte)(float.Parse(value, NumberStyles.Float, ci) * 255.0f);
                            break;
                        case DataProperty.A32:
                            a = (byte)(float.Parse(value, NumberStyles.Float, ci) * 255.0f);
                            break;

                        case DataProperty.SingleX:
                            x = float.Parse(value, NumberStyles.Float, ci);
                            break;
                        case DataProperty.SingleY:
                            y = float.Parse(value, NumberStyles.Float, ci);
                            break;
                        case DataProperty.SingleZ:
                            z = float.Parse(value, NumberStyles.Float, ci);
                            break;

                        case DataProperty.DoubleX:
                            x = (float)double.Parse(value, NumberStyles.Float, ci);
                            break;
                        case DataProperty.DoubleY:
                            y = (float)double.Parse(value, NumberStyles.Float, ci);
                            break;
                        case DataProperty.DoubleZ:
                            z = (float)double.Parse(value, NumberStyles.Float, ci);
                            break;
                    }
                }

                data.AddPoint(x, y, z, r, g, b, a);
            }

            return data;
        }

        DataBody ReadDataBodyFormatXYZ(string[] paths, string[][] dataOrder)
        {
            var data = new DataBody();
            for(int i=0; i < paths.Length; i++)
            {
                StreamReader streamReader = File.OpenText(paths[i]);
                float x = 0, y = 0, z = 0;
                Byte r = 255, g = 255, b = 255, a = 255;
                var ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();
                int index_x = Array.IndexOf(dataOrder[i], "x");
                int index_y = Array.IndexOf(dataOrder[i], "y");
                int index_z = Array.IndexOf(dataOrder[i], "z");

                int index_r = Array.IndexOf(dataOrder[i], "r");
                int index_g = Array.IndexOf(dataOrder[i], "g");
                int index_b = Array.IndexOf(dataOrder[i], "b");

                int index_r255 = Array.IndexOf(dataOrder[i], "r255");
                int index_g255 = Array.IndexOf(dataOrder[i], "g255");
                int index_b255 = Array.IndexOf(dataOrder[i], "b255");

                if (index_r < 0 || index_g < 0 || index_b < 0)
                {
                    index_b = index_b255;
                    index_g = index_g255;
                    index_r = index_r255;
                }
                
                while (!streamReader.EndOfStream)
                {
                    var line = streamReader.ReadLine();
                    var values = line.Trim().Split(' ');

                    x = float.Parse(values[index_x], NumberStyles.Float, ci);
                    y = float.Parse(values[index_y], NumberStyles.Float, ci);
                    z = float.Parse(values[index_z], NumberStyles.Float, ci);
                    
                    if (index_r < 0 || index_g < 0 || index_b < 0)
                    {
                        r = 255;
                        g = 255;
                        b = 255;
                    }
                    else
                    {
                        r = Byte.Parse(values[index_r], NumberStyles.Float, ci);
                        g = Byte.Parse(values[index_g], NumberStyles.Float, ci);
                        b = Byte.Parse(values[index_b], NumberStyles.Float, ci);
                    }

                    data.AddPoint(x, y, z, r, g, b, a);
                }
            }

            return data;
        }

        #endregion
    }

}