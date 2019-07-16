using System;
using System.Collections.Generic;
using System.Text;

namespace Tyd
{
    public static class TydFromText
    {
        private enum SymbolType
        {
            RecordName,
            AttributeName,
            AttributeValue
        };

        private enum StringFormat
        {
            Naked,
            Quoted,
            Vertical
        }


        public static IEnumerable<TydNode> Parse(string text)
        {
            return Parse(text, 0, null, true);
        }

        ///<summary>
        /// Recursively parses the string 'text' starting at char index 'startIndex' and ending when there is an unmatched closing bracket or EOF.
        /// text should have any opening bracket already stripped off.
        /// This recursive method is used both for parsing files, as well as for parsing specific entries inside files.
        ///</summary>
        private static IEnumerable<TydNode> Parse(string text, int startIndex, TydNode parent, bool expectNames = true)
        {
            int p = startIndex;

            //Main loop
            while (true)
            {
                string recordName = null;
                string recordAttHandle = null;
                string recordAttSource = null;
                bool recordAttAbstract = false;
                bool recordAttNoInherit = false;

                try
                {
                    //Skip insubstantial chars
                    p = NextSubstanceIndex(text, p);

                    //We reached EOF, so we're finished
                    if (p == text.Length)
                    {
                        if( parent != null )
                            throw new FormatException("Missing closing brackets");

                        yield break;
                    }

                    //We reached a closing bracket, so we're finished with this record
                    if (text[p] == Constants.TableEndChar || text[p] == Constants.ListEndChar)
                        yield break;

                    //Read the record name if we're not reading anonymous records
                    if (expectNames)
                        recordName = ReadSymbol(text, SymbolType.RecordName, ref p);

                    //Skip whitespace
                    p = NextSubstanceIndex(text, p);

                    //Read attributes
                    while (text[p] == Constants.AttributeStartChar)
                    {
                        //Skip past the '*' character
                        p++;

                        //Read the att name
                        string attName = ReadSymbol(text, SymbolType.AttributeName, ref p);
                        if (attName == Constants.AbstractAttributeName)
                        {
                            //Just reading the abstract name indicates it's abstract, no value is needed
                            recordAttAbstract = true;
                        }
                        else if( attName == Constants.NoInheritAttributeName )
                        {
                            //Just reading the noinherit name indicates it's noinherit, no value is needed
                            recordAttNoInherit = true;
                        }
                        else
                        {
                            p = NextSubstanceIndex(text, p);

                            //Read the att value
                            string attVal = ReadSymbol(text, SymbolType.AttributeValue, ref p);
                            switch (attName)
                            {
                                case Constants.HandleAttributeName: recordAttHandle = attVal; break;
                                case Constants.SourceAttributeName: recordAttSource = attVal; break;
                                default: throw new Exception("Unknown attribute name '" + attName + "' at " + LineColumnString(text, p)
                                                            + "\n" + ErrorSectionString(text,p));
                            }
                        }

                        p = NextSubstanceIndex(text, p);
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("Exception parsing Tyd headers at " + LineColumnString(text, p) + ": " + e.ToString()
                                        + "\n" + ErrorSectionString(text,p), e);
                }

                //Read the record value.
                //After this is complete, p should be pointing at the char after the last char of the record.
                if (text[p] == Constants.TableStartChar)
                {
                    //It's a table
                    TydTable newTable = new TydTable(recordName, parent, IndexToLine(text, p));

                    //Skip past the opening bracket
                    p++;

                    p = NextSubstanceIndex(text, p);

                    //Recursively parse all of new child's children
                    foreach( var subNode in Parse(text, p, newTable, expectNames: true) )
                    {
                        newTable.AddChild(subNode);
                        p = subNode.docIndexEnd + 1;
                    }

                    p = NextSubstanceIndex(text, p);

                    //Confirm that we are indeed on the closing bracket
                    if (text[p] != Constants.TableEndChar)
                        throw new FormatException("Expected '" + Constants.TableEndChar + "' at " + LineColumnString(text, p)
                                                + "\n" + ErrorSectionString(text,p) );

                    newTable.docIndexEnd = p;
                    newTable.SetupAttributes(recordAttHandle, recordAttSource, recordAttAbstract, recordAttNoInherit);
                    yield return newTable;

                    //Move pointer one past the closing bracket
                    p++;
                }
                else if (text[p] == Constants.ListStartChar)
                {
                    //It's a list
                    TydList newList = new TydList(recordName, parent, IndexToLine(text, p));

                    //Skip past the opening bracket
                    p++;

                    p = NextSubstanceIndex(text, p);

                    //Recursively parse all of new child's children and add them to it
                    foreach (var subNode in Parse(text, p, newList, expectNames: false))
                    {
                        newList.AddChild(subNode);
                        p = subNode.docIndexEnd + 1;
                    }
                    p = NextSubstanceIndex(text, p);
                    if (text[p] != Constants.ListEndChar)
                    {
                        throw new FormatException("Expected " + Constants.ListEndChar + " at " + LineColumnString(text, p) 
                             + "\n" + ErrorSectionString(text, p));
                    }

                    newList.docIndexEnd = p;
                    newList.SetupAttributes(recordAttHandle, recordAttSource, recordAttAbstract, recordAttNoInherit);
                    yield return newList;

                    //Move pointer one past the closing bracket
                    p++;
                }
                else
                {
                    //It's a string
                    int pStart = p;
                    string val;
                    ParseStringValue(text, ref p, out val);

                    var strNode = new TydString(recordName, val, parent, IndexToLine(text, pStart));
                    strNode.docIndexEnd = p - 1;
                    yield return strNode;
                }
            }
        }

        //We are at the first char of a string value of unknown type.
        //This returns the string value, and places p at the first char after it.
        private static void ParseStringValue(string text, ref int p, out string val)
        {
            StringFormat format;
            switch( text[p] )
            {
                case '"': format = StringFormat.Quoted;   break;
                case '|': format = StringFormat.Vertical; break;
                default:  format = StringFormat.Naked;    break;
            }

            //Parse as a quoted string
            if (format == StringFormat.Quoted)
            {
                p++; //Move past the opening quote
                int pStart = p;

                //Walk forward until we find the end quote
                //We need to ignore any that are escaped
                while (p < text.Length
                    && !(text[p] == '"' && text[p - 1] != '\\'))
                    p++;

                //Set the return value to the contents of the string
                val = text.Substring(pStart, p - pStart);

                val = ResolveEscapeChars(val);

                //Move past the end quote so we're pointing just after it
                p++;
            }
            else if( format == StringFormat.Vertical )
            {
                StringBuilder sb = new StringBuilder();
                while(true) //Line-processing loop
                {
                    p++; //Move past vbar at line start
                    int lineContentStart = p;

                    //Find next end of document or EOL
                    while( p < text.Length && !IsNewline(text, p) )
                    {
                        p++;
                    }
                    //p is now pointing at the first EOL char, or just after document end

                    //Add the content of this line to the string
                    sb.Append( text.Substring( lineContentStart, p-lineContentStart ) );

                    //Skip past EOL and whitespace
                    p = NextSubstanceIndex(text, p);

                    //If the first substance we hit is a vbar, our string continues.
                    //Otherwise, it ends
                    if( p < text.Length && text[p] == '|' )
                    {
                        sb.AppendLine();
                        continue;
                    }
                    else
                    {
                        val = sb.ToString();
                        return;
                    }
                }
            }
            else if( format == StringFormat.Naked )
            {
                int pStart = p;

                //Walk forward until we're on the first string content-terminating char or char group
                //We need to ignore any that are escaped
                while (p < text.Length
                    && !IsNewline(text, p)
                    && !((text[p] == Constants.RecordEndChar
                        || text[p] == Constants.CommentChar
                        || text[p] == Constants.TableEndChar
                        || text[p] == Constants.ListEndChar)
                            && text[p - 1] != '\\'))
                    p++;

                //We are now pointing at the first char after the string value.
                //However, we now need to remove whitespace after the value.
                //So we make pointer q, and walk it backwards until it's on non-whitespace.
                //This lets us find the last non-whitespace char of the string value.
                int q = p - 1;
                while (char.IsWhiteSpace(text[q]))
                    q--;
                val = text.Substring(pStart, q - pStart + 1);

                if (val == "null") //Special case for 'null' naked string.
                    val = null;
                else
                    val = ResolveEscapeChars(val);
            }
            else
                throw new Exception();
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
        private static string ReadSymbol(string text, SymbolType symType, ref int p)
        {
            int pStart = p;
            while (true)
            {
                var c = text[p];

                if (char.IsWhiteSpace(c))
                    break;

                if (!IsSymbolChar(c))
                    break;

                p++;
            }

            if (p == pStart)
            {
                throw new FormatException("Expected " + SymbolTypeName(symType) + " at " + LineColumnString(text, p)
                                         +"\n" + ErrorSectionString(text,p));
            }

            return text.Substring(pStart, p - pStart);
        }

        private static string SymbolTypeName( SymbolType st )
        {
            switch( st )
            {
                case SymbolType.RecordName: return "record name";
                case SymbolType.AttributeName: return "attribute name";
                case SymbolType.AttributeValue: return "attribute value";
                default: throw new Exception();
            }
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

        private static bool IsNewline(string text, int p)
        {
            return IsNewlineLF(text, p) || IsNewlineCRLF(text, p);
        }

        private static bool IsNewlineLF(string text, int p)
        {
            return text[p] == '\n';
        }

        private static bool IsNewlineCRLF(string text, int p)
        {
            return text[p] == '\r' && p < text.Length - 1 && text[p + 1] == '\n';
        }

        private static string LineColumnString(string text, int index)
        {
            int line, col;
            IndexToLineColumn(text, index, out line, out col);
            return "line " + line + ", col " + col;
        }

        private static string ErrorSectionString(string text, int index)
        {
            const int CharRangeWidth = 500;

            string modText = text;
            modText = modText.Insert(Math.Min(index, text.Length-1), "[ERROR]");
            if( index > CharRangeWidth || text.Length > index + CharRangeWidth)
            {
                int start  = Math.Max(index - CharRangeWidth,0);
                int length = Math.Min(CharRangeWidth*2, text.Length - index);
                text = text.Substring(start, length);
            }
            return modText;
        }

        private static int IndexToLine(string text, int index)
        {
            int line;
            int unused;
            IndexToLineColumn(text, index, out line, out unused);
            return line;
        }

        private static void IndexToLineColumn(string text, int index, out int line, out int column)
        {
            //Note that these start at 1, not 0, because normal people are supposed to be able to use these
            line = 1;
            column = 1;
            for (int p = 0; p < index; p++)

            {
                if (IsNewlineLF(text, p))
                {
                    line++;
                    column = 0;
                }
                else if( IsNewlineCRLF(text,p) )
                {
                    line++;
                    column = 0;
                    p++;    //Skip forward an extra
                }
                column++;
            }
        }

        ///<summary>
        /// Returns the index of the next char after p that is not whitespace or part of a comment.
        /// If there is no more substance in the text, this returns an index just after the end of the text.
        ///<summary>
        private static int NextSubstanceIndex(string text, int p)
        {
            //Skip forward as long as p keeps hitting insubstantial chars or char groups
            while (true)
            {
                //Reached end of text - return an index just after text end
                if (p >= text.Length)
                    return text.Length;

                //It's whitespace - skip over it
                if (char.IsWhiteSpace(text[p]))
                {
                    p++;
                    continue;
                }

                //It's the end of an empty record - skip over it
                if (text[p] == Constants.RecordEndChar)
                {
                    p++;
                    continue;
                }

                //It's the start of a comment - skip to the next line
                if (text[p] == Constants.CommentChar)
                {
                    while (p < text.Length && !IsNewline(text, p))
                        p++;

                    //Skip past newline char(s)
                    if ( IsNewlineLF(text, p) )
                        p += 1;
                    else if( IsNewlineCRLF(text, p) )
                        p += 2;
                    else
                        throw new Exception();

                    continue;
                }

                //It's not any of the above cases, so it's a substance char
                return p;
            }
        }
    }

}