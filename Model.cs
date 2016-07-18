using System.Collections.Generic;
// ReSharper disable InconsistentNaming

namespace Schematic2Json
{
    public class Model
    {
        public string __comment;
        public Dictionary<string, string> textures;
        public Element[] elements;

        public class Element
        {
            public float[] from;
            public float[] to;
            public Dictionary<string, Face> faces;

            public class Face
            {
                public string texture;
                public float[] uv;
            }
        }
    }
}