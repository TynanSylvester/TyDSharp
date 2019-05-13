using System;
using System.Collections.Generic;

namespace Tyd
{

    public static class TydFromText
    {
        //Working temp
        private static List<string> usedNames = new List<string>();

        public static IEnumerable<TydNode> Parse(string doc)
        {
            return Parse(doc, 0, null, true);
        }

        ///<summary>
        /// Recursively parses the string 'doc' starting at char index 'startIndex' and ending when there is an unmatched closing bracket or EOF.
        /// doc should have any opening bracket already stripped off.
        /// This recursive method is used both for parsing files, as well as for parsing specific entries inside files.
        ///</summary>
        private static IEnumerable<TydNode> Parse(string doc, int startIndex, TydNode parent, bool expectNames = true)
        {
            int p = startIndex;

            //Main loop
            while (true)
            {
                string recordName = null;
                string recordAttHandle = null;
                string recordAttSource = null;
                bool recordAttAbstract = false;

                try
                {
                    //Skip insubstantial chars
                    p = NextSubstanceIndex(doc, p);

                    //We reached EOF, so we're finished
                    if (p == doc.Length)
                        yield break;

                    //We reached a closing bracket, so we're finished with this record
                    if (doc[p] == Constants.TableEndChar || doc[p] == Constants.ListEndChar)
                        yield break;

                    //Read the record name if we're not reading anonymous records
                    if (expectNames)
                        recordName = ReadSymbol(doc, ref p);

                    //Skip whitespace
                    p = NextSubstanceIndex(doc, p);

                    //Read attributes
                    while (doc[p] == Constants.AttributeStartChar)
                    {
                        //Skip past the '*' character
                        p++;

                        //Read the att name
                        string attName = ReadSymbol(doc, ref p);
                        if (attName == Constants.AbstractAttributeName)
                        {
                            //Just reading the abstract name indicates it's abstract, no value is needed
                            recordAttAbstract = true;
                        }
                        else
                        {
                            p = NextSubstanceIndex(doc, p);

                            //Read the att value
                            string attVal = ReadSymbol(doc, ref p);
                            switch (attName)
                            {
                                case Constants.HandleAttributeName: recordAttHandle = attVal; break;
                                case Constants.SourceAttributeName: recordAttSource = attVal; break;
                                default: throw new Exception("Unknown attribute name '" + attName + "' at " + IndexToLocationString(doc, p));
                            }
                        }

                        p = NextSubstanceIndex(doc, p);
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("Exception parsing Tyd headers at " + IndexToLocationString(doc, p) + ": " + e.ToString(), e);
                }

                //Read the record value.
                //After this is complete, p should be pointing at the char after the last char of the record.
                if (doc[p] == Constants.TableStartChar)
                {
                    //It's a table
                    TydTable newTable = new TydTable(recordName, parent, IndexToLine(doc, p));

                    //Skip past the opening bracket
                    p++;

                    p = NextSubstanceIndex(doc, p);

                    //Recursively parse all of new child's children and add them to it
                    try
                    {
                        foreach (var subNode in Parse(doc, p, newTable, expectNames: true))
                        {
                            if (usedNames.Contains(subNode.Name))
                                throw new FormatException("Duplicate record name " + subNode.Name + " at " + IndexToLocationString(doc, p));
                            usedNames.Add(subNode.Name);

                            newTable.AddChild(subNode);
                            p = subNode.docIndexEnd + 1;
                        }
                    }
                    finally
                    {
                        usedNames.Clear();
                    }

                    p = NextSubstanceIndex(doc, p);

                    if (doc[p] != Constants.TableEndChar)
                        throw new FormatException("Expected ']' at " + IndexToLocationString(doc, p));

                    newTable.docIndexEnd = p;
                    newTable.SetupAttributes(recordAttHandle, recordAttSource, recordAttAbstract);
                    yield return newTable;

                    //Move pointer one past the closing bracket
                    p++;
                }
                else if (doc[p] == Constants.ListStartChar)
                {
                    //It's a list
                    TydList newList = new TydList(recordName, parent, IndexToLine(doc, p));

                    //Skip past the opening bracket
                    p++;

                    p = NextSubstanceIndex(doc, p);

                    //Recursively parse all of new child's children and add them to it
                    foreach (var subNode in Parse(doc, p, newList, expectNames: false))
                    {
                        newList.AddChild(subNode);
                        p = subNode.docIndexEnd + 1;
                    }
                    p = NextSubstanceIndex(doc, p);

                    if (doc[p] != Constants.ListEndChar)
                        throw new FormatException("Expected " + Constants.ListEndChar + " at " + IndexToLocationString(doc, p));

                    newList.docIndexEnd = p;
                    newList.SetupAttributes(recordAttHandle, recordAttSource, recordAttAbstract);
                    yield return newList;

                    //Move pointer one past the closing bracket
                    p++;
                }
                else
                {
                    //It's a string
                    int pStart = p;
                    string val;
                    ParseStringValue(doc, ref p, out val);

                    var strNode = new TydString(recordName, val, parent, IndexToLine(doc, pStart));
                    strNode.docIndexEnd = p - 1;
                    yield return strNode;
                }
            }
        }

        //We are at the first char of a string value.
        //This returns the string value, and places p at the first char after it.
        private static void ParseStringValue(string doc, ref int p, out string val)
        {
            bool quoted = doc[p] == '"';

            //Parse as a quoted string
            if (quoted)
            {
                p++; //Move past the opening quote
                int pStart = p;

                //Walk forward until we find the end quote
                //We need to ignore any that are escaped
                while (p < doc.Length
                    && !(doc[p] == '"' && doc[p - 1] != '\\'))
                    p++;

                //Set the return value to the contents of the string
                val = doc.Substring(pStart, p - pStart);

                val = ResolveEscapeChars(val);

                //Move past the end quote so we're pointing just after it
                p++;
            }
            else //Parse as a naked string
            {
                int pStart = p;

                //Walk forward until we're on the first string content-terminating char or char group
                //We need to ignore any that are escaped
                while (p < doc.Length
                    && !IsNewline(doc, p)
                    && !((doc[p] == Constants.RecordEndChar
                        || doc[p] == Constants.CommentChar
                        || doc[p] == Constants.TableEndChar
                        || doc[p] == Constants.ListEndChar)
                            && doc[p - 1] != '\\'))
                    p++;

                //We are now pointing at the first char after the string value.
                //However, we now need to remove whitespace after the value.
                //So we make pointer q, and walk it backwards until it's on non-whitespace.
                //This lets us find the last non-whitespace char of the string value.
                int q = p - 1;
                while (char.IsWhiteSpace(doc[q]))
                    q--;
                val = doc.Substring(pStart, q - pStart + 1);

                if (val == "null") //Special case for 'null' naked string.
                    val = null;
                else
                    val = ResolveEscapeChars(val);

                //Special case for ';': We want to be pointing after it, not on it.
                if (p < doc.Length && doc[p] == ';')
                    p++;
            }

            
        }

        /// <summary>
        /// Take the input string and replace any escape sequences with the final chars they correspond to.
        /// This can be opimized
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static string ResolveEscapeChars(string input)
        {
            for (int k = 0; k < input.Length; k++)
            {
                if (input[k] == '\\')
                {
                    if (input.Length <= k + 1)
                        throw new Exception("Tyd string value ends with single backslash: " + input);

                    char resolvedChar = EscapedCharOf(input[k + 1]);
                    input = input.Substring(0, k) + resolvedChar + input.Substring(k + 2);
                }
            }
            return input;
        }

        //Returns the character that an escape sequence should resolve to, based on the second char of the escape sequence (after the backslash).
        private static char EscapedCharOf(char inputChar)
        {
            switch (inputChar)
            {
                case '\\': return '\\';
                case '"': return '"';
                case '#': return '#';
                case ';': return ';';
                case ']': return ']';
                case '}': return '}';
                case '\r': return '\u000D';
                case 'n': return '\u000A';
                case 't': return '\u0009';
                default: throw new Exception("Cannot escape char: \\" + inputChar);
            }
        }

        //Reads a symbol and return it. Places p at the first char after the symbol.
        //Symbols include:
        //  -Record names
        //  -Attribute names
        //  -Attribute values
        private static string ReadSymbol(string doc, ref int p)
        {
            int pStart = p;
            while (true)
            {
                var c = doc[p];
                if (char.IsWhiteSpace(doc[p]))
                    break;

                if (!IsSymbolChar(c))
                    break;

                p++;
            }

            if (p == pStart)
                throw new FormatException("Missing symbol at " + IndexToLocationString(doc, p));

            return doc.Substring(pStart, p - pStart);
        }

        private static bool IsSymbolChar(char c)
        {
            //This can be optimized to a range check
            for (int i = 0; i < Constants.SymbolChars.Length; i++)
            {
                if (Constants.SymbolChars[i] == c)
                    return true;
            }
            return false;
        }

        //Todo fully support \n or \r\n
        private static bool IsNewline(string doc, int p)
        {
            return doc[p] == '\n'
            || (doc[p] == '\r' && p < doc.Length - 1 && doc[p + 1] == '\n');
        }

        private static string IndexToLocationString(string doc, int index)
        {
            int line, col;

            IndexToLineColumn(doc, index, out line, out col);

            return "line " + line + " col " + col;
        }

        private static int IndexToLine(string doc, int index)
        {
            int line, _;

            IndexToLineColumn(doc, index, out line, out _);

            return line;
        }

        private static void IndexToLineColumn(string doc, int index, out int line, out int column)
        {
            line = 1;
            column = 1;

            for (int p = 0; p < index; p++)
            {
                if (IsNewline(doc, p))
                {
                    line++;
                    column = 0;
                }
                column++;
            }
        }

        ///<summary>
        /// Returns the index of the next char after p that is not whitespace or part of a comment.
        /// If there is no more substance in the doc, this returns an index just after the end of the doc.
        ///<summary>
        private static int NextSubstanceIndex(string doc, int p)
        {
            //As long as p keeps hitting comment starts or whitespace, we skip forward
            while (true)
            {
                //Reached end of doc - return an index just after doc end
                if (p >= doc.Length)
                    return doc.Length;

                //It's whitespace - skip over it
                if (char.IsWhiteSpace(doc[p]))
                {
                    p++;
                    continue;
                }

                //It's the comment char - skip to the next line
                if (doc[p] == Constants.CommentChar)
                {
                    while (p < doc.Length && !IsNewline(doc, p))
                        p++;

                    //Skip past newline char(s). Since there may be just \n or \r\n, we have to handle both cases.
                    if (doc[p] == '\n')
                        p++;
                    else
                        p += 2;   //If it's not \n, we assume it's \r\n and skip two

                    continue;
                }

                //It's not whitespace or the comment char - it's substance
                return p;
            }
        }
    }

}