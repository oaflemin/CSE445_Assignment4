using System;
using System.Xml;
using System.Xml.Schema;
using System.Text;
using Newtonsoft.Json;

namespace ConsoleApp1
{
    public class Submission
    {
        // URLs point to my GitHub Pages where the files are hosted
        public static string xmlURL = "https://oaflemin.github.io/CSE445_Assignment4/NationalParks.xml"; //Q1.2
        public static string xmlErrorURL = "https://oaflemin.github.io/CSE445_Assignment4/NationalParksErrors.xml"; //Q1.3
        public static string xsdURL = "https://oaflemin.github.io/CSE445_Assignment4/NationalParks.xsd"; //Q1

        public static void Main(string[] args)
        {

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

        // Takes a URL to an XML file and a URL to an XSD file, and checks if the
        // XML follows the rules defined in the XSD. Returns a message describing
        // any errors found, or a success message if there are none.
        public static string Verification(string xmlUrl, string xsdUrl)
        {
            StringBuilder errors = new StringBuilder();

            try
            {
                // Settings object tells the XmlReader to validate against a schema
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.ValidationType = ValidationType.Schema;

                settings.Schemas.Add(null, xsdUrl);

                // Every time a validation error is found, this event fires
                settings.ValidationEventHandler += (sender, e) =>
                {
                    errors.AppendLine(e.Message);
                };

                // Create a reader using our settings and read through the whole file
                using (XmlReader reader = XmlReader.Create(xmlUrl, settings))
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

            if (errors.Length == 0)
            {
                return "No errors are found";
            }
            else
            {
                return errors.ToString();
            }
        }

        // Takes a URL to an XML file and converts it into a Json string
        public static string Xml2Json(string xmlUrl)
        {
            // Load the XML file into an XmlDocument 
            XmlDocument doc = new XmlDocument();
            doc.Load(xmlUrl);

            Newtonsoft.Json.Linq.JArray parkArray = new Newtonsoft.Json.Linq.JArray();

            // Go through every NationalPark node under the root
            foreach (XmlNode parkNode in doc.DocumentElement.ChildNodes)
            {
                if (parkNode.Name != "NationalPark")
                {
                    continue;
                }

                Newtonsoft.Json.Linq.JObject parkObject = new Newtonsoft.Json.Linq.JObject();

                // Collect all phone numbers into a list since there can be more than one
                Newtonsoft.Json.Linq.JArray phoneArray = new Newtonsoft.Json.Linq.JArray();

                foreach (XmlNode child in parkNode.ChildNodes)
                {
                    if (child.Name == "Name")
                    {
                        parkObject["Name"] = child.InnerText;
                    }
                    else if (child.Name == "Phone")
                    {
                        phoneArray.Add(child.InnerText);
                    }
                    else if (child.Name == "Address")
                    {
                        Newtonsoft.Json.Linq.JObject addressObject = new Newtonsoft.Json.Linq.JObject();

                        foreach (XmlNode addressChild in child.ChildNodes)
                        {
                            addressObject[addressChild.Name] = addressChild.InnerText;
                        }

                        // NearestAirport is an attribute on Address, not a child element,
                        // so we grab it separately and prefix it with @ like the sample shows
                        if (child.Attributes["NearestAirport"] != null)
                        {
                            addressObject["@NearestAirport"] = child.Attributes["NearestAirport"].Value;
                        }

                        parkObject["Phone"] = phoneArray;
                        parkObject["Address"] = addressObject;
                    }
                }

                // Rating is an attribute on NationalPark itself
                if (parkNode.Attributes["Rating"] != null)
                {
                    parkObject["@Rating"] = parkNode.Attributes["Rating"].Value;
                }

                parkArray.Add(parkObject);
            }

            // Wrap everything in the outer NationalParks -> NationalPark structure
            Newtonsoft.Json.Linq.JObject root = new Newtonsoft.Json.Linq.JObject();
            Newtonsoft.Json.Linq.JObject nationalParksWrapper = new Newtonsoft.Json.Linq.JObject();

            nationalParksWrapper["NationalPark"] = parkArray;
            root["NationalParks"] = nationalParksWrapper;

            // Indented makes it readable when printed to console
            string jsonText = root.ToString(Newtonsoft.Json.Formatting.Indented);

            return jsonText;
        }
    }
}
