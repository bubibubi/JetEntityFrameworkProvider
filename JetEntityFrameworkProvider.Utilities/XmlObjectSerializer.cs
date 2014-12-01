using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml.Serialization;

namespace JetEntityFrameworkProvider.Utilities
{

    /// <summary>
    /// Object serializer
    /// </summary>
    public static class XmlObjectSerializer
    {

        /// <summary>
        /// Gets the xml rapresenting the object
        /// </summary>
        /// <param name="o">The object</param>
        /// <returns>The xml rapresenting the object</returns>
        public static string GetXml(object o)
        {
            Type objectType = o.GetType();
            XmlSerializer xmlSerializer = GetSerializer(objectType);

            MemoryStream stream = new MemoryStream();

            xmlSerializer.Serialize(stream, o);

            string retString = UTF8Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);

            stream.Close();

            return retString;
        }

        /// <summary>
        /// Creates a new object of the specified type and sets the property readed from xml
        /// </summary>
        /// <param name="xml">The xml</param>
        /// <param name="objectType">The type of the object that will be created</param>
        /// <returns>The created object</returns>
        public static object GetObject(string xml, Type objectType)
        {
            XmlSerializer xmlSerializer = GetSerializer(objectType);

            MemoryStream stream = new MemoryStream(UTF8Encoding.UTF8.GetBytes(xml));

            object o = xmlSerializer.Deserialize(stream);

            stream.Close();

            return o;
        }


        /// <summary>
        /// Writes a file with the xml that rapresent the object
        /// </summary>
        /// <param name="path">File path</param>
        /// <param name="o">Object to write</param>
        public static void WriteFile(string path, object o)
        {
            string fileContent = GetXml(o);
            File.WriteAllText(path, fileContent);
        }

        /// <summary>
        /// Create an object of the specified type and sets the properties reading from the xml file
        /// </summary>
        /// <param name="path">File path</param>
        /// <param name="objectType">The type of the object that will be created</param>
        /// <returns>The created object</returns>
        public static object ReadFile(string path, Type objectType)
        {
            string fileContent = File.ReadAllText(path);
            return GetObject(fileContent, objectType);
        }


        static Dictionary<string, XmlSerializer> xmlSerializers = new Dictionary<string, XmlSerializer>();

        private static XmlSerializer GetSerializer(Type objectType)
        {
            lock (xmlSerializers)
            {
                XmlSerializer xmlSerializer;

                try
                {
                    xmlSerializer = xmlSerializers[objectType.FullName];
                }
                catch
                {
                    xmlSerializer = new XmlSerializer(objectType);
                    xmlSerializers.Add(objectType.FullName, xmlSerializer);
                }

                return xmlSerializer;
            }

        }

    }

}
