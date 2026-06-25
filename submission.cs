using System;
using System.Xml.Schema;
using System.Xml;
using Newtonsoft.Json;
using System.IO;
using System.Text;

namespace ConsoleApp1
{
    public class Submission
    {
        // These URLs point to my GitHub Pages site where the files are hosted
        public static string xmlURL = "https://oaflemin.github.io/CSE445_Assignment4/NationalParks.xml"; //Q1.2
        public static string xmlErrorURL = "https://oaflemin.github.io/CSE445_Assignment4/NationalParksErrors.xml"; //Q1.3
        public static string xsdURL = "https://oaflemin.github.io/CSE445_Assignment4/NationalParks.xsd"; //Q1.1

        public static void Main(string[] args)
        {
            // Q3: Run all three required operations

            // 1) Validate the good XML file against the schema 
            string result = Verification(xmlURL, xsdURL);
            Console.WriteLine(result);

            // 2) Validate the broken XML file against the same schema 
            result = Verification(xmlErrorURL, xsdURL);
            Console.WriteLine(result);

            // 3) Convert the good XML file into Json text
            result = Xml2Json(xmlURL);
            Console.WriteLine(result);

            Console.ReadLine();
        }

        // Q2.1
        // Takes a URL to an XML file and a URL to an XSD file, and checks if the
        // XML follows the rules defined in the XSD. 
        public static string Verification(string xmlUrl, string xsdUrl)
        {
            StringBuilder errors = new StringBuilder();

            try
            {
                // Download both files as plain text first instead of letting
                // XmlReader hit the URL directly
                string xmlContent = DownloadContent(xmlUrl);
                string xsdContent = DownloadContent(xsdUrl);

                // Settings object tells the XmlReader to validate against a schema
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.ValidationType = ValidationType.Schema;

                // Load the schema from the downloaded text instead of a URL
                using (StringReader xsdStringReader = new StringReader(xsdContent))
                using (XmlReader xsdReader = XmlReader.Create(xsdStringReader))
                {
                    XmlSchema schema = XmlSchema.Read(xsdReader, null);
                    settings.Schemas.Add(schema);
                }

                // Every time a validation error is found, this event fires
                settings.ValidationEventHandler += (sender, e) =>
                {
                    errors.AppendLine(e.Message);
                };

                // Create a reader over the downloaded XML text and read through the whole file
                using (StringReader xmlStringReader = new StringReader(xmlContent))
                using (XmlReader reader = XmlReader.Create(xmlStringReader, settings))
                {
                    while (reader.Read()) { }
                }
            }
            catch (XmlException ex)
            {
                // This catches malformed XML
                errors.AppendLine("XML parsing error: " + ex.Message);
            }
            catch (Exception ex)
            {
                // Catch anything else unexpected
                errors.AppendLine("Error: " + ex.Message);
            }

            // If nothing was added to errors
            if (errors.Length == 0)
            {
                return "No errors are found";
            }
            else
            {
                return errors.ToString();
            }
        }

        // Q2.2
        // Takes a URL to an XML file and converts it into a Json string
        public static string Xml2Json(string xmlUrl)
        {
            // Download the XML content as text, then load it into an XmlDocument
            string xmlContent = DownloadContent(xmlUrl);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlContent);

            // Convert starting from the root element, then wrap it in one
            // more object using the root's own name as the key
            Newtonsoft.Json.Linq.JObject root = new Newtonsoft.Json.Linq.JObject();
            root[doc.DocumentElement.Name] = ElementToJson(doc.DocumentElement);

            string jsonText = root.ToString(Newtonsoft.Json.Formatting.Indented);

            return jsonText;
        }

        // Recursively converts one XML element into a Json value
        private static Newtonsoft.Json.Linq.JToken ElementToJson(XmlElement element)
        {
            // Simple case: no attributes and no child elements at all
            if (GetRealAttributes(element).Count == 0 && !HasChildElements(element))
            {
                return element.InnerText;
            }

            Newtonsoft.Json.Linq.JObject obj = new Newtonsoft.Json.Linq.JObject();

            // Add attributes first, each prefixed with @
            foreach (XmlAttribute attr in GetRealAttributes(element))
            {
                obj["@" + attr.LocalName] = attr.Value;
            }

            // Collect just the real child elements
            var childElements = new System.Collections.Generic.List<XmlElement>();
            foreach (XmlNode child in element.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Element)
                {
                    childElements.Add((XmlElement)child);
                }
            }

            // If there were no child elements, but there were attributes,
            // still keep the element's own text under a #text key
            if (childElements.Count == 0)
            {
                if (!string.IsNullOrEmpty(element.InnerText))
                {
                    obj["#text"] = element.InnerText;
                }
                return obj;
            }

            // Count how many times each child element name appears
            var nameCounts = new System.Collections.Generic.Dictionary<string, int>();
            foreach (XmlElement child in childElements)
            {
                if (nameCounts.ContainsKey(child.Name))
                {
                    nameCounts[child.Name]++;
                }
                else
                {
                    nameCounts[child.Name] = 1;
                }
            }

            foreach (XmlElement child in childElements)
            {
                Newtonsoft.Json.Linq.JToken childValue = ElementToJson(child);

                // Phone always becomes an array, even if there's only one
                bool forceArray = (child.Name == "Phone") || (nameCounts[child.Name] > 1);

                if (forceArray)
                {
                    // This name repeats, so collect all of them into one array
                    if (obj[child.Name] == null)
                    {
                        obj[child.Name] = new Newtonsoft.Json.Linq.JArray();
                    }

                    ((Newtonsoft.Json.Linq.JArray)obj[child.Name]).Add(childValue);
                }
                else
                {
                    obj[child.Name] = childValue;
                }
            }

            return obj;
        }

        // Returns only the real attributes on an element, skipping namespace
        // declarations like xmlns, xmlns:xsi, and xsi:noNamespaceSchemaLocation
        private static System.Collections.Generic.List<XmlAttribute> GetRealAttributes(XmlElement element)
        {
            var result = new System.Collections.Generic.List<XmlAttribute>();

            foreach (XmlAttribute attr in element.Attributes)
            {
                bool isNamespaceDeclaration =
                    attr.Name == "xmlns" ||
                    attr.Name.StartsWith("xmlns:") ||
                    attr.NamespaceURI == "http://www.w3.org/2001/XMLSchema-instance";

                if (!isNamespaceDeclaration)
                {
                    result.Add(attr);
                }
            }

            return result;
        }

        // Checks if an element has at least one child that is itself an element
        private static bool HasChildElements(XmlElement element)
        {
            foreach (XmlNode child in element.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Element)
                {
                    return true;
                }
            }
            return false;
        }

        // Helper method to download content from URL
        private static string DownloadContent(string url)
        {
            using (System.Net.WebClient client = new System.Net.WebClient())
            {
                return client.DownloadString(url);
            }
        }
    }
}
