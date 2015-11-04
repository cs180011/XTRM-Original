using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.Data.Odbc;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace XTRMlib
{
    public class XDictionaryLoader
    {
        private string className;
        private string dictionaryFile = "";
        private XmlTextReader reader = null;
        public XDictionaryLoader(string name = "XDictionaryLoader")
        {
            className = name;
        }
        ~XDictionaryLoader()
        {
        }

        public bool Initialize(string filename = "")
        {
            bool bExists = false;

            try
            {
                string defaultFile = filename;
                if (defaultFile.Length == 0)
                {
                    // Try to get from the environment!
                    defaultFile = Environment.GetEnvironmentVariable("XDB_DICTIONARY");
                }
                if (File.Exists(defaultFile))
                {
                    // If exists as specified, then use it!
                    bExists = true;
                    dictionaryFile = defaultFile;
                }
                else
                {
                    // Otherwise, must go look for it!
                    dictionaryFile = XTRMObject.FindMyDictionaryFile(defaultFile);
                    // Does the file exist?
                    bExists = File.Exists(dictionaryFile);
                    if (!bExists)
                    {
                        dictionaryFile = defaultFile;
                        bExists = File.Exists(dictionaryFile);
                        if (!bExists)
                        {
                            // Cannot Find.
                            dictionaryFile = "";
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            finally
            {
            }
            return bExists;
        }
        public bool InitializeX(string filename = "")
        {
            bool bRC = true;
            bool bExists = false;
            bool bReady = true;

            try
            {
                dictionaryFile = filename;
                if (dictionaryFile.Length == 0)
                {
                    // Try to get from the environment!
                    dictionaryFile = Environment.GetEnvironmentVariable("XDB_DICTIONARY");
                    if (dictionaryFile == null)
                    {
                        dictionaryFile = @"\\jmptool\source\XLator\Config\XDictionary.xml";
                        bReady = XTRMBase.IsDriveReady("jmptool");
                    }
                }
                // Does the file exist?
                if (!bReady || !(bExists = File.Exists(dictionaryFile)))
                {
                    dictionaryFile = @"c:\XLator\Config\XDictionary.xml";
                    bExists = File.Exists(dictionaryFile);
                }
                //bRC = bExists;
            }
            catch (Exception)
            {
                dictionaryFile = @"c:\XLator\Config\XDictionary.xml";
                bExists = File.Exists(dictionaryFile);
            }
            finally
            {
                bRC = bExists;
            }

            return bRC;
        }
        public Dictionary<String, String> ParseXML(string fileName = "XDictionary.xml", bool bUpperKey = false, bool bUpperValue = false)
        {
            int count = 0;
            Dictionary<String, String> newDictionary = new Dictionary<String, String>();
            Dictionary<String, String> elementAttributes = new Dictionary<String, String>();
            int lElementType = 0;
            bool bRC = Initialize(fileName);
            if (bRC)
            {
                try
                {
                    // Load the reader with the data file and ignore all white space nodes. 
                    // Check the file first!
                    reader = new XmlTextReader(dictionaryFile);
                    newDictionary.Add("LoadDisposition", "1");
                    // Load the reader with the data file and ignore all white space nodes.         
                    reader.WhitespaceHandling = WhitespaceHandling.None;
                    // Parse the file and display each of the nodes.
                    bool bResult = reader.Read();
                    while (bResult)
                    {
                        switch (reader.NodeType)
                        {
                            case XmlNodeType.Element:
                                string elementName = reader.Name;
                                // May wish to get all the  attributes here for new elements!
                                elementAttributes = new Dictionary<String, String>();
                                if (reader.HasAttributes)
                                {
                                    reader.MoveToFirstAttribute();
                                    for (int i = 0; i < reader.AttributeCount; i++)
                                    {
                                        elementAttributes.Add(reader.Name, reader.Value);
                                        reader.MoveToNextAttribute();
                                    }
                                    reader.MoveToElement();
                                }
                                // Need to see if we are interested in this element!
                                lElementType = 0;
                                switch (elementName)
                                {
                                    case "Variable": // Variable
                                        lElementType = 1;
                                        bResult = reader.Read();
                                        break;
                                    default:
                                        bResult = reader.Read();
                                        break;
                                }
                                break;
                            case XmlNodeType.XmlDeclaration:
                            case XmlNodeType.ProcessingInstruction:
                                bResult = reader.Read();
                                break;
                            case XmlNodeType.Comment:
                                bResult = reader.Read();
                                break;
                            case XmlNodeType.EndElement:
                                bResult = reader.Read();
                                break;
                            case XmlNodeType.Text:
                                switch (lElementType)
                                {
                                    case 1:     // Variable
                                        //    thisTask.parms.Add(reader.Value);
                                        string name = elementAttributes["Name"];
                                        if (name != null)
                                        {
                                            string value = reader.Value;
                                            if (value != null)
                                            {
                                                if (bUpperKey)
                                                {
                                                    name = name.ToUpper();
                                                }
                                                if (bUpperValue)
                                                {
                                                    value = value.ToUpper();
                                                }
                                                newDictionary[name] = value;
                                            }
                                        }
                                        break;
                                    default:
                                        break;
                                }
                                bResult = reader.Read();
                                break;
                            default:
                                bResult = reader.Read();
                                break;
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    Console.Write("Use Local File");
                }
                catch (Exception)
                {
                    newDictionary["LoadDisposition"] = "-1";
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Close();
                    }
                    newDictionary["LoadCount"] = Convert.ToString(count);
                    newDictionary["RootFolder"] = XTRMObject.GetRootFolder();
                }
            }
            else
            {
                // Cannot load dictionary.
                newDictionary["LoadedCount"] = "0";
                newDictionary["LoadDisposition"] = "0";
                newDictionary["RootFolder"] = XTRMObject.GetRootFolder();
            }
            return newDictionary;
        }
        public Dictionary<String, String> Augment(Dictionary<String, String> existDictionary, string XmlFragment, bool bUpper = false)
        {
            XmlParserContext context = null;
            int count = 0;
            bool bRC = true;
            if (bRC)
            {
                try
                {
                    // Load the reader with the data file and ignore all white space nodes. 
                    // Check the file first!
                    //Create the XmlParserContext.
                    context = new XmlParserContext(null, null, null, XmlSpace.None);
                    reader = new XmlTextReader(XmlFragment, XmlNodeType.Element, context);
                    int lElementType = 0;
                    Dictionary<String, String> elementAttributes = new Dictionary<String, String>();

                    existDictionary["LoadDisposition"] = "1";
                    // Load the reader with the data file and ignore all white space nodes.         
                    reader.WhitespaceHandling = WhitespaceHandling.None;
                    // Parse the file and display each of the nodes.
                    bool bResult = reader.Read();
                    while (bResult)
                    {
                        switch (reader.NodeType)
                        {
                            case XmlNodeType.Element:
                                string elementName = reader.Name;
                                // May wish to get all the  attributes here for new elements!
                                elementAttributes = new Dictionary<String, String>();
                                if (reader.HasAttributes)
                                {
                                    reader.MoveToFirstAttribute();
                                    for (int i = 0; i < reader.AttributeCount; i++)
                                    {
                                        elementAttributes.Add(reader.Name, reader.Value);
                                        reader.MoveToNextAttribute();
                                    }
                                    reader.MoveToElement();
                                }
                                // Need to see if we are interested in this element!
                                lElementType = 0;
                                switch (elementName)
                                {
                                    case "Variable": // Variable
                                        lElementType = 1;
                                        bResult = reader.Read();
                                        break;
                                    default:
                                        bResult = reader.Read();
                                        break;
                                }
                                break;
                            case XmlNodeType.XmlDeclaration:
                            case XmlNodeType.ProcessingInstruction:
                                bResult = reader.Read();
                                break;
                            case XmlNodeType.Comment:
                                bResult = reader.Read();
                                break;
                            case XmlNodeType.EndElement:
                                bResult = reader.Read();
                                break;
                            case XmlNodeType.Text:
                                switch (lElementType)
                                {
                                    case 1:     // Variable
                                    //    thisTask.parms.Add(reader.Value);
                                        string name = elementAttributes["Name"];
                                        if (name != null)
                                        {
                                            string value = reader.Value;
                                            if (value != null)
                                            {
                                                if (bUpper)
                                                {
                                                    name = name.ToUpper();
                                                    value = value.ToUpper();
                                                }
                                                existDictionary[name] = value;
                                            }
                                        }
                                        break;
                                    default:
                                        break;
                                }
                                bResult = reader.Read();
                                break;
                            default:
                                bResult = reader.Read();
                                break;
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    Console.Write("Use Local File");
                }
                catch (Exception)
                {
                    existDictionary["LoadDisposition"] = "-1";
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Close();
                    }
                    existDictionary["LoadCount"] = Convert.ToString(count);
                    existDictionary["RootFolder"] = XTRMObject.GetRootFolder();
                }
            }
            else
            {
                // Cannot load dictionary.
                existDictionary["LoadedCount"] = "0";
                existDictionary["LoadDisposition"] = "0";
                existDictionary["RootFolder"] = XTRMObject.GetRootFolder();
            }
            return existDictionary;
        }
    }
}