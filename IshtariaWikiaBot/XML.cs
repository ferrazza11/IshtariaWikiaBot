using System.Xml.Serialization;
using System.Xml;
using System.IO;

namespace IshtariaWikiaBot
{
    public static class XML<T>
    {
        private static FileStream fs;
        /// <summary>
        /// Lee el archivo XML
        /// </summary>
        /// <returns>null si el archivo no existe</returns>
        public static T Read(string path)
        {
            T o = default(T);
            if (File.Exists(path))
            {
                XmlSerializer sz = new XmlSerializer(typeof(T));
                fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                o = (T)sz.Deserialize(fs);
                fs.Close();
            }
            return o;
        }
        /// <summary>
        /// Escribe el archivo XML
        /// </summary>
        /// <param name="o">Objeto Serializado a escribir</param>
        public static void Write(T o, string path)
        {
            if (File.Exists(path))
                File.Delete(path);
                XmlSerializer sz = new XmlSerializer(typeof(T));

                TextWriter fs = new StreamWriter(path);
                sz.Serialize(fs, o);
                fs.Close();
        }

        public static string toString(object o)
        {
            XmlSerializer xsSubmit = new XmlSerializer(typeof(T));
            StringWriter sww = new StringWriter();
            XmlWriter writer = XmlWriter.Create(sww);
            xsSubmit.Serialize(writer, o);
            return sww.ToString();
        }

        public static T fromString(string str)
        {
            XmlSerializer sz = new XmlSerializer(typeof(T));
            MemoryStream memStream = new MemoryStream(System.Text.Encoding.Unicode.GetBytes(str));
            T o = default(T);
            o = (T)sz.Deserialize(memStream);
            memStream.Close();
            return o;
        }


    }
}
