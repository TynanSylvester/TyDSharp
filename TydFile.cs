using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;

namespace Tyd
{

    ///<summary>
    /// Represents a file of Tyd data.
    /// To read a file: Use FromFile to create a TydFile from a file on disk, and then read the data you want from the TydFile.
    /// To write to a file: Build your TydDocument, create a TydFile from it, then write the TydFile to disk.
    ///</summary>
    public class TydFile
    {
        //Data
        protected TydDocument docNode;
        protected string filePath = null;

        //Properties
        public TydDocument DocumentNode
        {
            get
            {
                return docNode;
            }

            set
            {
                docNode = value;
            }
        }

        public string FilePath
        {
            get
            {
                return filePath;
            }
        }

        public string FileName
        {
            get
            {
                return Path.GetFileName(filePath);
            }
        }

        private TydFile() { }

        ///<summary>
        /// Create a new TydFile from a TydDocument.
        ///</summary>
        public static TydFile FromDocument(TydDocument doc, string filePath = null)
        {
            TydFile t = new TydFile();
            t.docNode = doc;
            t.filePath = filePath;
            return t;
        }

        ///<summary>
        /// Create a new TydFile by loading data from a file at the given path.
        ///</summary>
        public static TydFile FromFile(string filePath, bool treatXmlAsOneObject = false)
        {
            try
            {
                if (Path.GetExtension(filePath).ToLowerInvariant() == ".xml")
                {
                    //File is xml format
                    //Load it and convert the tyd nodes from it
                    string contents = File.ReadAllText(filePath);
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(contents);
                    List<TydNode> nodes = new List<TydNode>();
                    if (treatXmlAsOneObject)
                        nodes.Add(TydXml.TydNodeFromXmlDocument(xmlDoc));
                    else
                        nodes.AddRange(TydXml.TydNodesFromXmlDocument(xmlDoc));
                    return FromDocument(new TydDocument(nodes), filePath);
                }
                else
                {
                    //If it's any extension besides xml, we assume the file is Tyd format
                    string readContents;
                    using (StreamReader streamReader = new StreamReader(filePath))
                    {
                        readContents = streamReader.ReadToEnd();
                    }
                    var tydNodeList = TydFromText.Parse(readContents).ToList();
                    var tydDoc = new TydDocument(tydNodeList);
                    return FromDocument(tydDoc, filePath);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Exception loading " + filePath + ": " + e);
            }
        }

        ///<summary>
        /// Write to a file, overwriting any file present.
        /// If a path is provided, the file's path is changed to that new path before saving. Otherwise, the current path is used.
        ///</summary>
        public void Save(string path = null)
        {
            if (path != null)
                filePath = path;
            else if (filePath == null)
                throw new InvalidOperationException("Saved TydFile which had null path");

            //Build the text we're going to write
            StringBuilder tydText = new StringBuilder();
            foreach (var node in docNode)
            {
                tydText.AppendLine(TydToText.Write(node));
            }

            //Write to the file
            File.WriteAllText(filePath, tydText.ToString().TrimEnd());
        }

        public override string ToString()
        {
            return FileName;
        }
    }
}
