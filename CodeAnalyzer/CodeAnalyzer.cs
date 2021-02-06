﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// TODO: find interfaces!
namespace CodeAnalyzer
{
    class FileProcessor
    {
        /* Saved copies of input input arguments */
        private CodeAnalysisData codeAnalysisData;
        private ProgramFile programFile;

        /* Stacks to keep track of the current scope */
        private readonly Stack<string> scopeStack = new Stack<string>();
        private readonly Stack<ProgramType> typeStack = new Stack<ProgramType>();

        private StringBuilder stringBuilder = new StringBuilder("");
        int stringBuilderSize = 0;

        /* Scope syntax rules to check for */
        int ifScope = 0;        // [0-1]: "if" ONLY if elseIfScope == 0, [1-2]: "(", [2-3]: word(s), [3-4]: ")", [4-0]: "{" or bracketless
        int elseIfScope = 0;    // [0-1]: "else", [1-2]: "if", [2-3]: "(", [3-4]: word(s), [4-5]: ")", [5-0]: "{" or bracketless
        int elseScope = 0;      // [0-1]: "else", [1-0]: "{" or bracketless NOT "if"
        int forScope = 0;       // [0-1]: "for", [1-2]: "(", [2-3]: word(s), [3-4]: ";", [4-5]: word(s), [5-6]: ";", [6-7]: word(s), [7-8]: ")", [8-0]: "{" or bracketless
        int forEachScope = 0;   // [0-1]: "foreach", [1-2]: "(", [2-3]: word(s), [3-4]: ")", [4-0]: "{" or bracketless
        int whileScope = 0;     // [0-1]: "while", [1-2]: "(", [2-3]: word(s), [3-4]: ")", [4-0]: "{" or bracketless NOT ";"
        int doWhileScope = 0;   // [0-1]: "do", [1-0]: "{" or bracketless
        int switchScope = 0;    // [0-1]: "switch", [1-2]: "(", [2-3]: word(s), [3-4]: ")", [4-0]: "{"

        int savedScopeStackCount = 0;

        public void ProcessFile(CodeAnalysisData codeAnalysisData, ProgramFile programFile)
        {
            this.codeAnalysisData = codeAnalysisData;
            this.programFile = programFile;

            codeAnalysisData.ProcessedFiles.Add(programFile);   // add file to processed files list

            this.SetFileTextData();                  // preprocess the text into a list

            // send it off to have its structure and functional data fully processed
            this.ProcessFileData();
        }

        /* Puts the file next into a string array, dividing elements logically */
        private void SetFileTextData()
        {
            this.ClearStringBuilder();
            string[] programLines = programFile.FileText.Split(new String[] { Environment.NewLine }, StringSplitOptions.None);

            for (int i = 0; i < programLines.Length; i++)
            {
                IEnumerator enumerator = programLines[i].GetEnumerator();

                while (enumerator.MoveNext())
                {
                    if (Char.IsWhiteSpace((char)enumerator.Current))
                    {
                        if (stringBuilder.Length > 0)
                        {
                            programFile.FileTextData.Add(stringBuilder.ToString());
                            this.ClearStringBuilder();
                        }
                        continue;
                    }

                    if ((Char.IsPunctuation((char)enumerator.Current) || Char.IsSymbol((char)enumerator.Current)) && !((char)enumerator.Current).Equals('_'))
                    {
                        if (stringBuilder.Length > 0)
                        {
                            if (stringBuilder.Length == 1)
                            {
                                if (Char.IsPunctuation((char)stringBuilder.ToString()[0]) || Char.IsSymbol((char)stringBuilder.ToString()[0]))
                                {
                                    // double-character symbols
                                    if ((stringBuilder.ToString().Equals("/") && 
                                            (((char)enumerator.Current).Equals('/') || ((char)enumerator.Current).Equals('*') || ((char)enumerator.Current).Equals('=')))
                                        || (stringBuilder.ToString().Equals("*") && ((char)enumerator.Current).Equals('/'))
                                        || (stringBuilder.ToString().Equals("+") && (((char)enumerator.Current).Equals('+') || ((char)enumerator.Current).Equals('=')))
                                        || (stringBuilder.ToString().Equals("-") && (((char)enumerator.Current).Equals('-') || ((char)enumerator.Current).Equals('=')))
                                        || (stringBuilder.ToString().Equals(">") && (((char)enumerator.Current).Equals('>') || ((char)enumerator.Current).Equals('=')))
                                        || (stringBuilder.ToString().Equals("<") && (((char)enumerator.Current).Equals('<') || ((char)enumerator.Current).Equals('=')))
                                        || (stringBuilder.ToString().Equals("=") || stringBuilder.ToString().Equals("*") 
                                            || stringBuilder.ToString().Equals("!") || stringBuilder.ToString().Equals("%"))
                                            && ((char)enumerator.Current).Equals('=')
                                        || (stringBuilder.ToString().Equals("=") && ((char)enumerator.Current).Equals('>'))
                                        || (stringBuilder.ToString().Equals("&") && ((char)enumerator.Current).Equals('&'))
                                        || (stringBuilder.ToString().Equals("|") && ((char)enumerator.Current).Equals('|'))
                                        || (stringBuilder.ToString().Equals("\\") && (((char)enumerator.Current).Equals('\\') 
                                            || ((char)enumerator.Current).Equals('"') || ((char)enumerator.Current).Equals('\''))))
                                    {
                                        this.AppendToStringBuilder(enumerator.Current);
                                        programFile.FileTextData.Add(stringBuilder.ToString());
                                        this.ClearStringBuilder();
                                        continue;
                                    }
                                }
                            }
                            programFile.FileTextData.Add(stringBuilder.ToString());
                            this.ClearStringBuilder();
                        }
                    }
                    else
                    {
                        if (stringBuilder.Length == 1)
                        {
                            if ((Char.IsPunctuation((char)stringBuilder.ToString()[0]) || Char.IsSymbol((char)stringBuilder.ToString()[0])) 
                                && !((char)stringBuilder.ToString()[0]).Equals('_'))
                            {
                                programFile.FileTextData.Add(stringBuilder.ToString());
                                this.ClearStringBuilder();
                            }
                        }
                    }
                    this.AppendToStringBuilder(enumerator.Current);
                }
                if (stringBuilder.Length > 0)
                {
                    programFile.FileTextData.Add(stringBuilder.ToString());
                    this.ClearStringBuilder();
                }

                programFile.FileTextData.Add(" ");
            }
            
        }

        /* Fully processes the file data (except relationship data), fills all internal Child ProgramType lists */
        private void ProcessFileData()
        {
            this.ClearStringBuilder();
            string entry;

            for (int index = 0; index < programFile.FileTextData.Count; index++)
            {
                entry = programFile.FileTextData[index];

                /* ---------- Determine whether to ignore the entry ---------- */
                if (this.IgnoreEntry(entry))
                    continue;

                /* ---------- If starting an area to ignore, push the entry ---------- */
                if (this.StartPlainTextArea(entry))
                    continue;

                /* ---------- Check for the end of an existing scope ---------- */
                if (entry.Equals("}"))
                {
                    if (scopeStack.Count > 0)
                        if (scopeStack.Peek().Equals("{")) scopeStack.Pop();
                    if (scopeStack.Count > 0)
                    {
                        if (scopeStack.Peek().Equals("namespace") || scopeStack.Peek().Equals("class")
                        || scopeStack.Peek().Equals("interface") || scopeStack.Peek().Equals("struct")
                        || scopeStack.Peek().Equals("enum") || scopeStack.Peek().Equals("function"))
                        {
                            scopeStack.Pop();
                            if (typeStack.Count > 0) typeStack.Pop();
                        }
                    }
                    this.ClearStringBuilder();
                    continue;
                }

                /* ---------- Check for a new namespace ---------- */
                if (entry.Equals("namespace"))
                {
                    index = this.NewNamespace(index);
                    continue;
                }

                /* ---------- Check for a new class ---------- */
                if (entry.Equals("class"))
                {
                    index = this.NewClass(index);
                    continue;
                }

                /* ---------- Check if other type of open brace ---------- */
                if (entry.Equals("{"))
                    scopeStack.Push(entry);

                /* ---------- Update stringBuilder ---------- */
                this.UpdateStringBuilder(entry);
            }
        }

        /* Processes data within a ProgramClassType (class or interface) scope */
        private int ProcessProgramClassTypeData(int i)
        {
            string entry;
            int index;

            for (index = i; index < programFile.FileTextData.Count; index++)
            {
                entry = programFile.FileTextData[index];
                
                /* ---------- Determine whether to ignore the entry ---------- */
                if (this.IgnoreEntry(entry))
                    continue;

                /* ---------- If starting an area to ignore, push the entry ---------- */
                if (this.StartPlainTextArea(entry))
                    continue;

                /* ---------- Add entry to current ProgramDataType's text list ---------- */
                if (typeStack.Count > 0 && (typeStack.Peek().GetType() == typeof(ProgramClass)
                    || typeStack.Peek().GetType() == typeof(ProgramInterface) || typeStack.Peek().GetType() == typeof(ProgramFunction)))
                    ((ProgramDataType)typeStack.Peek()).TextData.Add(entry);

                /* ---------- Check for the end of an existing scope ---------- */
                if (entry.Equals("}"))
                {
                    if (scopeStack.Count > 0)
                        if (scopeStack.Peek().Equals("{")) scopeStack.Pop();
                    if (scopeStack.Count > 0)
                    {
                        if (scopeStack.Peek().Equals("class")) // return from the class
                        {
                            scopeStack.Pop();
                            if (typeStack.Count > 0) typeStack.Pop();
                            this.ClearStringBuilder();
                            return index;
                        }
                        if (scopeStack.Peek().Equals("namespace") || scopeStack.Peek().Equals("interface")              // ending a different named scope
                        || scopeStack.Peek().Equals("struct") || scopeStack.Peek().Equals("enum") || scopeStack.Peek().Equals("function"))
                        {
                            scopeStack.Pop();
                            if (typeStack.Count > 0) typeStack.Pop();
                        }
                    }
                    continue;
                }

                /* ---------- Check for a new class ---------- */
                if (entry.Equals("class"))
                {
                    index = this.NewClass(index);
                    continue;
                }

                /* ---------- Check if a new function is being started ---------- */
                if (entry.Equals("{"))
                {
                    if (this.CheckIfFunction()) // valid function or method syntax
                    {
                        index = this.ProcessFunctionData(++index);
                        continue;
                    }
                    else /* ---------- Other type of open brace ---------- */
                        scopeStack.Push(entry);
                }

                /* ---------- Update stringBuilder ---------- */
                this.UpdateStringBuilder(entry);
            }

            return index;
        }

        /* Processes data within a Function scope */
        private int ProcessFunctionData(int i)
        {
            string entry;
            int index;
            bool scopeOpener;

            for (index = i; index < programFile.FileTextData.Count; index++)
            {
                entry = programFile.FileTextData[index];
                scopeOpener = false;

                /* ---------- Determine whether to ignore the entry ---------- */
                if (this.IgnoreEntry(entry))
                    continue;

                /* ---------- If starting an area to ignore, push the entry ---------- */
                if (this.StartPlainTextArea(entry))
                    continue;

                /* ---------- Add entry to current ProgramDataType's text list ---------- */
                if (typeStack.Count > 0 && typeStack.Peek().GetType() == typeof(ProgramDataType))
                    ((ProgramDataType)typeStack.Peek()).TextData.Add(entry);

                /* ---------- Check for a new line ---------- */
                if (entry.Equals(" ") && typeStack.Peek().GetType() == typeof(ProgramFunction))
                    ((ProgramFunction)typeStack.Peek()).Size++;

                /* ---------- Check for closing parenthesis ---------- */
                if (entry.Equals(")"))
                    if (scopeStack.Count > 0)
                        if (scopeStack.Peek().Equals("("))
                            scopeStack.Pop();

                /* ---------- Check for the end of an existing bracketed scope ---------- */
                if (entry.Equals("}"))
                {
                    if (scopeStack.Count > 0)
                        if (scopeStack.Peek().Equals("{")) scopeStack.Pop();
                    if (scopeStack.Count > 0)
                    {
                        if (scopeStack.Peek().Equals("function")) // return from the function
                        {
                            scopeStack.Pop();
                            if (typeStack.Count > 0) typeStack.Pop();
                            this.ClearStringBuilder();
                            return index;
                        }
                        // ending a different ProgramType scope
                        if (scopeStack.Peek().Equals("namespace") || scopeStack.Peek().Equals("class")
                        || scopeStack.Peek().Equals("interface") || scopeStack.Peek().Equals("struct") || scopeStack.Peek().Equals("enum"))
                        {
                            scopeStack.Pop();
                            if (typeStack.Count > 0) typeStack.Pop();
                        }
                        else // ending another named scope
                            while (scopeStack.Count > 0 && (scopeStack.Peek().Equals("if") || scopeStack.Peek().Equals("else if") || scopeStack.Peek().Equals("else")
                                || scopeStack.Peek().Equals("for") || scopeStack.Peek().Equals("foreach") || scopeStack.Peek().Equals("while") 
                                || scopeStack.Peek().Equals("do while") || scopeStack.Peek().Equals("switch")))
                            scopeStack.Pop();
                    }
                    this.ClearStringBuilder();
                    continue;
                }

                /* ---------- Check for opening scope statements ---------- */
                if (!entry.Equals(" ") && typeStack.Count > 0 && typeStack.Peek().GetType() == typeof(ProgramFunction))
                {
                    if (this.CheckScopes(entry))
                        scopeOpener = true;
                }

                /* ---------- Check for open parenthesis ---------- */
                if (entry.Equals("("))
                    scopeStack.Push("(");

                /* Push an opening bracket */
                if (entry.Equals("{"))
                {
                    /* ---------- Check if a new function is being started ---------- */
                    if (!scopeOpener && this.CheckIfFunction())
                    {
                        index = this.ProcessFunctionData(++index);
                        continue;
                    }
                    else /* ---------- Other type of open bracket ---------- */
                        scopeStack.Push(entry);
                }

                /* ---------- Check for the end of an existing bracketless scope ---------- */
                if (entry.Equals(";") && forScope == 0 && scopeStack.Count > 0 && typeStack.Count > 0 && typeStack.Peek().GetType() == typeof(ProgramFunction))
                {
                    while (scopeStack.Peek().Equals("if") || scopeStack.Peek().Equals("else if") || scopeStack.Peek().Equals("else")
                        || scopeStack.Peek().Equals("for") || scopeStack.Peek().Equals("foreach") || scopeStack.Peek().Equals("while")
                        || scopeStack.Peek().Equals("do while") || scopeStack.Peek().Equals("switch"))
                    {
                        scopeStack.Pop();
                    }
                }

                /* ---------- Update stringBuilder ---------- */
                this.UpdateStringBuilder(entry);
            }

            return index;
        }


        /* -------------------- Helper Methods -------------------- */
        private int NewNamespace(int index)
        {
            string entry;

            scopeStack.Push("namespace"); // push the type of the next scope opener onto scopeStack

            this.ClearStringBuilder();
            while (++index < programFile.FileTextData.Count) // get the name of the namespace
            {
                entry = programFile.FileTextData[index];
                if (entry.Equals("{"))
                {
                    scopeStack.Push("{"); // push the new scope opener onto scopeStack
                    break;
                }
                if (!entry.Equals(" ")) this.AppendToStringBuilder(entry);
            }

            ProgramNamespace programNamespace = new ProgramNamespace(stringBuilder.ToString());
            this.ClearStringBuilder();

            // add new namespace to its parent's ChildList
            if (typeStack.Count > 0) typeStack.Peek().ChildList.Add(programNamespace);
            else programFile.ChildList.Add(programNamespace);

            typeStack.Push(programNamespace); // push the namespace onto typeStack
            return index;
        }

        private int NewClass(int index)
        {
            string entry;
            string classModifiers = stringBuilder.ToString();   // get the class modifiers
            List<string> classText = new List<string>();
            int brackets = 0;

            scopeStack.Push("class"); // push the type of the next scope opener onto scopeStack

            this.ClearStringBuilder();
            while (++index < programFile.FileTextData.Count) // get the name of the class
            {
                entry = programFile.FileTextData[index];

                /* ---------- Determine whether to ignore the entry ---------- */
                if (this.IgnoreEntry(entry))
                    continue;

                /* ---------- If starting an area to ignore, push the entry ---------- */
                if (this.StartPlainTextArea(entry))
                    continue;

                /* ---------- Add entry to class's TextData list ---------- */
                classText.Add(entry);

                if (entry.Equals("{"))
                {
                    scopeStack.Push("{"); // push the new scope opener onto scopeStack
                    break;
                }
                if (classText.Count == 0)
                {
                    if (entry.Equals("<") || entry.Equals("["))
                    {
                        brackets++;
                        this.AppendToStringBuilder(entry);
                        continue;
                    }
                    else if (brackets != 0)
                    {
                        if (entry.Equals(">") || entry.Equals("]"))
                        {
                            brackets--;
                        }
                        this.AppendToStringBuilder(entry);
                        continue;
                    }
                }
                if (stringBuilder.Length == 0)
                {
                    if (!entry.Equals(" ")) this.AppendToStringBuilder(entry); // the next entry after "class" will be the name
                    continue;
                }
            }

            ProgramClass programClass = new ProgramClass(stringBuilder.ToString(), classModifiers);
            this.ClearStringBuilder();

            // add text/inheritance data, and add class to general ProgramClassType list
            programClass.TextData = classText;
            codeAnalysisData.AddClass(programClass);

            // add new class to its parent's ChildList
            if (typeStack.Count > 0)  typeStack.Peek().ChildList.Add(programClass);
            else programFile.ChildList.Add(programClass);

            typeStack.Push(programClass); // push the class onto typeStack

            return this.ProcessProgramClassTypeData(++index); // reads the rest of the class
        }

        /* Used to detect the syntax for a function signature */
        private bool CheckIfFunction()
        {
            string[] functionIdentifier = stringBuilder.ToString().Split(' ');

            /* ---------- Determine whether new scope is a function ---------- */
            int functionRequirement = 0;    // the requirement to check next
                                            // (Optional: modifiers), [0-1]: return type, [1-2]: name, [2-3]: open parenthesis, (Optional: parameters), 
                                            // [3-4]: close parenthesis, [4]: all requirements fulfilled for *normal* functions
            string modifiers = "";
            string returnType = "";
            string name = "";
            List<string> parameters = new List<string>();
            string baseParameters = "";

            int parentheses = 0;            // ensure the same number of opening and closing parentheses
            int brackets = 0;
            int periods = 0;

            for (int i = 0; i < functionIdentifier.Length; i++)
            {
                string text = functionIdentifier[i];
                if (text.Length < 1) continue;

                if (text.Equals("(")) parentheses++;
                else if (text.Equals(")")) parentheses--;
                else if (text.Equals("[") || text.Equals("<")) brackets++;
                else if (text.Equals(".")) periods++;

                switch (functionRequirement)
                {
                    case 0:
                        if (text.Equals(" ")) continue;
                        if ((!Char.IsSymbol((char)text[0]) && !Char.IsPunctuation((char)text[0])) || ((char)text[0]).Equals('_'))
                        {
                            returnType = text;
                            functionRequirement = 1;
                            break;
                        }
                        functionRequirement = -1; // failed function syntax
                        break;
                    case 1:
                        if (text.Equals(" ")) continue;
                        if (brackets == 0 && periods == 0 && (!Char.IsSymbol((char)text[0]) && !Char.IsPunctuation((char)text[0])) || ((char)text[0]).Equals('_'))
                        {
                            name = text;
                            functionRequirement = 2;
                            break;
                        }
                        if (brackets != 0 || periods != 0 || text.Equals(".") || text.Equals("<") || text.Equals("[") || text.Equals(">") || text.Equals("]"))
                        {
                            if (name.Length == 0)
                                returnType += text;
                            else
                                name += text;
                            break;
                        }
                        functionRequirement = -1; // failed function syntax
                        break;
                    case 2:
                        if (text.Equals(" ")) continue;
                        if (text.Equals("("))
                        {
                            functionRequirement = 3;
                            break;
                        }
                        if (brackets == 0 && periods == 0 && (!Char.IsSymbol((char)text[0]) && !Char.IsPunctuation((char)text[0])) || ((char)text[0]).Equals('_'))
                        {
                            if (modifiers.Length > 0) modifiers += " ";
                            modifiers += returnType;
                            returnType = name;
                            name = text;
                            break;
                        }
                        if (brackets != 0 || periods != 0 || text.Equals(".") || text.Equals("<") || text.Equals("[") || text.Equals(">") || text.Equals("]"))
                        {
                            if (name.Length == 0)
                                returnType += text;
                            else
                                name += text;
                            break;
                        }
                        functionRequirement = -1; // failed function syntax
                        break;
                    case 3:
                        if (text.Equals(" ")) continue;
                        if (text.Equals(")") && parentheses == 0)
                        {
                            functionRequirement = 4;
                            break;
                        }
                        parameters.Add(text);
                        break;
                    case 4:
                        if (text.Equals(" ")) continue;
                        functionRequirement = -1; // failed function syntax
                        break;
                }
                if (periods > 0 && !text.Equals(".")) periods--;
                else if (text.Equals("]") || text.Equals(">")) brackets--;
            }

            if (functionRequirement == 4) // function signature detected
            {
                this.RemoveFunctionSignatureFromTextData(functionIdentifier.Length);
                this.ClearStringBuilder();

                ProgramFunction programFunction = new ProgramFunction(name, modifiers, returnType, parameters, baseParameters);

                // add new function to its parent's ChildList
                if (typeStack.Count > 0) typeStack.Peek().ChildList.Add(programFunction);
                else programFile.ChildList.Add(programFunction);

                // add the function and scope to scopeStack
                scopeStack.Push("function");
                scopeStack.Push("{");

                typeStack.Push(programFunction); // push the class onto typeStack
                return true;
            }
            else if (this.CheckIfConstructor(functionIdentifier))
                return true;
            else if (this.CheckIfDeconstructor(functionIdentifier))
                return true;

            return false;
        }

        /* Used to detect the syntax for a Deconstructor function */
        private bool CheckIfDeconstructor(string[] functionIdentifier)
        {
            int functionRequirement = 0;
            // [0-1]: ~, [1-2]: name == class.Name [2-3]: open parenthesis, [3-4] close paranethesis
            // [4]: all requirements fulfilled for *deconstructor* functions

            string modifiers = "";
            string returnType = "";
            string name = "";
            List<string> parameters = new List<string>();
            string baseParameters = "";

            for (int i = 0; i < functionIdentifier.Length; i++)
            {
                string text = functionIdentifier[i];
                if (text.Length < 1) continue;

                switch (functionRequirement)
                {
                    case 0:
                        if (text.Equals(" ")) continue;
                        if (text.Equals("~"))
                        {
                            functionRequirement = 1;
                            break;
                        }
                        functionRequirement = -1; // failed deconstructor syntax
                        break;
                    case 1:
                        if (text.Equals(" ")) continue;
                        if (typeStack.Count > 0 && typeStack.Peek().GetType() == typeof(ProgramClass))
                        {
                            string className = typeStack.Peek().Name;
                            if (className.Contains("<"))
                                className = className.Substring(0, className.IndexOf("<") + 1);
                            else if (className.Contains("["))
                                className = className.Substring(0, className.IndexOf("[") + 1);
                            if (className.Equals(text))
                            {
                                functionRequirement = 2;
                                break;
                            }
                        }
                        functionRequirement = -1; // failed deconstructor syntax
                        break;
                    case 2:
                        if (text.Equals(" ")) continue;
                        if (text.Equals("("))
                        {
                            functionRequirement = 3;
                            break;
                        }
                        functionRequirement = -1; // failed deconstructor syntax
                        break;
                    case 3:
                        if (text.Equals(" ")) continue;
                        if (text.Equals(")"))
                        {
                            functionRequirement = 4;
                            break;
                        }
                        functionRequirement = -1; // failed deconstructor syntax
                        break;
                    case 4:
                        if (text.Equals(" ")) continue;
                        functionRequirement = -1;
                        break;
                }
            }

            if (functionRequirement == 4) // deconstructor signature detected
            {
                this.RemoveFunctionSignatureFromTextData(functionIdentifier.Length);
                this.ClearStringBuilder();

                ProgramFunction programFunction = new ProgramFunction(name, modifiers, returnType, parameters, baseParameters);

                // add new function to its parent's ChildList
                if (typeStack.Count > 0) typeStack.Peek().ChildList.Add(programFunction);
                else programFile.ChildList.Add(programFunction);

                // add the function and scope to scopeStack
                scopeStack.Push("function");
                scopeStack.Push("{");

                typeStack.Push(programFunction); // push the class onto typeStack
                return true;
            }

            return false;
        }

        /* Used to detect the syntax for a Constructor function */
        // TODO: redo to check for a single-word Constructor function
        private bool CheckIfConstructor(string[] functionIdentifier)
        {
            int functionRequirement = 0;
            // (Optional: modifiers), [0-1]: name, [1-2]: open parenthesis & name == ClassName, (Optional: parameters), 
            // [2-3]: close parenthesis, OPTIONAL: [3-4]: colon, [4-5]: "base" keyword, [5-6]: open paranthesis,
            // (Optional: parameters), [6-7]: close parenthesis, [7]: all requirements fulfilled for *constructor* functions

            string modifiers = "";
            string returnType = "";
            string name = "";
            List<string> parameters = new List<string>();
            string baseParameters = "";

            int parentheses = 0;            // ensure the same number of opening and closing parentheses
            int brackets = 0;
            int periods = 0;

            for (int i = 0; i < functionIdentifier.Length; i++)
            {
                string text = functionIdentifier[i];
                if (text.Length < 1) continue;

                if (text.Equals("(")) parentheses++;
                else if (text.Equals(")")) parentheses--;
                else if (text.Equals("[") || text.Equals("<")) brackets++;
                else if (text.Equals(".")) periods++;

                switch (functionRequirement)
                {
                    case 0:
                        if (text.Equals(" ")) continue;
                        if ((!Char.IsSymbol((char)text[0]) && !Char.IsPunctuation((char)text[0])) || ((char)text[0]).Equals('_'))
                        {
                            name = text;
                            functionRequirement = 1;
                            break;
                        }
                        functionRequirement = -1; // failed constructor syntax
                        break;
                    case 1:
                        if (text.Equals(" ")) continue;
                        if (text.Equals("("))
                        {
                            if (typeStack.Count > 0 && typeStack.Peek().GetType() == typeof(ProgramClass))
                            {
                                string className = typeStack.Peek().Name;
                                if (className.Contains("<"))
                                    className = className.Substring(0, className.IndexOf("<") + 1);
                                else if (className.Contains("["))
                                    className = className.Substring(0, className.IndexOf("[") + 1);
                                if (className.Equals(name))
                                {
                                    functionRequirement = 2;
                                    break;
                                }
                            }
                            functionRequirement = -1;
                            break;
                        }
                        if (brackets == 0 && periods == 0 && (!Char.IsSymbol((char)text[0]) && !Char.IsPunctuation((char)text[0])) || ((char)text[0]).Equals('_'))
                        {
                            if (modifiers.Length > 0) modifiers += " ";
                            modifiers += name;
                            name = text;
                            break;
                        }
                        if (brackets != 0 || periods != 0 || text.Equals(".") || text.Equals("<") || text.Equals("[") || text.Equals(">") || text.Equals("]"))
                        {
                            name += text;
                            break;
                        }
                        functionRequirement = -1; // failed constructor syntax
                        break;
                    case 2:
                        if (text.Equals(" ")) continue;
                        if (text.Equals(")") && parentheses == 0)
                        {
                            functionRequirement = 3;
                            break;
                        }
                        parameters.Add(text);
                        break;
                    case 3:
                        if (text.Equals(" ")) continue;
                        if (text.Equals(":"))
                        {
                            baseParameters = text;
                            functionRequirement = 4;
                            break;
                        }
                        functionRequirement = -1; // failed constructor syntax
                        break;
                    case 4:
                        if (text.Equals(" ")) continue;
                        if (text.Equals("base"))
                        {
                            baseParameters = " " + text;
                            functionRequirement = 5;
                            break;
                        }
                        functionRequirement = -1; // failed constructor syntax
                        break;
                    case 5:
                        if (text.Equals(" ")) continue;
                        if (text.Equals("("))
                        {
                            baseParameters += text;
                            functionRequirement = 6;
                            break;
                        }
                        functionRequirement = -1; // failed constructor syntax
                        break;
                    case 6:
                        if (text.Equals(" ")) continue;
                        if (text.Equals(")"))
                        {
                            baseParameters += text;
                            functionRequirement = 7;
                            break;
                        }
                        if (!baseParameters[baseParameters.Length - 1].Equals('(') && !text.Equals(",")) baseParameters += " ";
                        baseParameters += text;
                        break;
                    case 7:
                        if (text.Equals(" ")) continue;
                        functionRequirement = -1; // failed constructor syntax
                        break;
                }
                if (periods > 0 && !text.Equals(".")) periods--;
                else if (text.Equals("]") || text.Equals(">")) brackets--;
            }

            if (functionRequirement == 3 || functionRequirement == 7) // constructor signature detected
            {
                this.RemoveFunctionSignatureFromTextData(functionIdentifier.Length);
                this.ClearStringBuilder();

                ProgramFunction programFunction = new ProgramFunction(name, modifiers, returnType, parameters, baseParameters);

                // add new function to its parent's ChildList
                if (typeStack.Count > 0) typeStack.Peek().ChildList.Add(programFunction);
                else programFile.ChildList.Add(programFunction);

                // add the function and scope to scopeStack
                scopeStack.Push("function");
                scopeStack.Push("{");

                typeStack.Push(programFunction); // push the class onto typeStack

                return true;
            }

            return false;
        }


        /* ---------- Scope Detectors (within functions) ---------- */

        private bool CheckScopes(string entry)
        {
            bool scopeOpener = false;

            bool ifScopeChecked = false;
            bool elseIfScopeChecked = false;
            bool elseScopeChecked = false;
            bool forScopeChecked = false;
            bool forEachScopeChecked = false;
            bool whileScopeChecked = false;
            bool doWhileScopeChecked = false;
            bool switchScopeChecked = false;

            /* ---------- Check "if" scope ---------- */
            if (ifScope > 0)
            {
                if (this.CheckIfScope(entry))
                    scopeOpener = true;
                ifScopeChecked = true;
            }

            /* ---------- Check "else if" scope ---------- */
            if (elseIfScope > 0)
            {
                if (this.CheckElseIfScope(entry))
                    scopeOpener = true;
                elseIfScopeChecked = true;
            }

            /* ---------- Check "else" scope ---------- */
            if (elseScope > 0)
            {
                if (this.CheckElseScope(entry))
                    scopeOpener = true;
                elseScopeChecked = true;
            }

            /* ---------- Check "for" scope ---------- */
            if (forScope > 0)
            {
                if (this.CheckForScope(entry))
                    scopeOpener = true;
                forScopeChecked = true;
            }

            /* ---------- Check "foreach" scope ---------- */
            if (forEachScope > 0)
            {
                if (this.CheckForEachScope(entry))
                    scopeOpener = true;
                forEachScopeChecked = true;
            }

            /* ---------- Check "while" scope ---------- */
            if (whileScope > 0)
            {
                if (this.CheckWhileScope(entry))
                    scopeOpener = true;
                whileScopeChecked = true;
            }

            /* ---------- Check "do while" scope ---------- */
            if (doWhileScope > 0)
            {
                if (this.CheckDoWhileScope(entry))
                    scopeOpener = true;
                doWhileScopeChecked = true;
            }

            /* ---------- Check "switch" scope ---------- */
            if (switchScope > 0)
            {
                if (this.CheckSwitchScope(entry))
                    scopeOpener = true;
                switchScopeChecked = true;
            }

            /* ---------- Check "if" scope ---------- */
            if (!ifScopeChecked)
                if (this.CheckIfScope(entry))
                    scopeOpener = true;

            /* ---------- Check "else if" scope ---------- */
            if (!elseIfScopeChecked)
                if (this.CheckElseIfScope(entry))
                    scopeOpener = true;

            /* ---------- Check "else" scope ---------- */
            if (!elseScopeChecked)
                if (this.CheckElseScope(entry))
                    scopeOpener = true;

            /* ---------- Check "for" scope ---------- */
            if (!forScopeChecked)
                if (this.CheckForScope(entry))
                    scopeOpener = true;

            /* ---------- Check "foreach" scope ---------- */
            if (!forEachScopeChecked)
                if (this.CheckForEachScope(entry))
                    scopeOpener = true;

            /* ---------- Check "while" scope ---------- */
            if (!whileScopeChecked)
                if (this.CheckWhileScope(entry))
                    scopeOpener = true;

            /* ---------- Check "do while" scope ---------- */
            if (!doWhileScopeChecked)
                if (this.CheckDoWhileScope(entry))
                    scopeOpener = true;

            /* ---------- Check "switch" scope ---------- */
            if (!switchScopeChecked)
                if (this.CheckSwitchScope(entry))
                    scopeOpener = true;

            return scopeOpener;
        }

        private bool CheckIfScope(string entry)
        {
            switch (ifScope)
            {
                case 0:
                    if (entry.Equals("if") && elseIfScope == 0)
                    {
                        ifScope = 1;
                        savedScopeStackCount = scopeStack.Count;
                    }
                    break;
                case 1:
                    if (entry.Equals("(")) ifScope = 2;
                    else ifScope = 0;
                    break;
                case 2:
                    ifScope = 3;
                    break;
                case 3:
                    if (entry.Equals(")") && savedScopeStackCount == scopeStack.Count)
                    {

                        ifScope = 4;
                    }
                    break;
                case 4:
                    ((ProgramFunction)typeStack.Peek()).Complexity++;
                    scopeStack.Push("if");
                    this.ClearStringBuilder();
                    if (entry.Equals("if") && elseIfScope == 0) // check if the next statement starts an "if"
                    {
                        ifScope = 1;
                        savedScopeStackCount = scopeStack.Count;
                    }
                    else ifScope = 0; // reset the ifScope rule
                    return true;
            }
            return false;
        }

        private bool CheckElseIfScope(string entry)
        {
            switch (elseIfScope)
            {
                case 0:
                    if (entry.Equals("else"))
                    {
                        elseIfScope = 1;
                        savedScopeStackCount = scopeStack.Count;
                    }
                    break;
                case 1:
                    if (entry.Equals("if")) elseIfScope = 2;
                    else elseIfScope = 0;
                    break;
                case 2:
                    if (entry.Equals("(")) elseIfScope = 3;
                    else elseIfScope = 0;
                    break;
                case 3:
                    elseIfScope = 4;
                    break;
                case 4:
                    if (entry.Equals(")") && savedScopeStackCount == scopeStack.Count) elseIfScope = 5;
                    break;
                case 5:
                    ((ProgramFunction)typeStack.Peek()).Complexity++;
                    scopeStack.Push("else if");
                    this.ClearStringBuilder();
                    if (entry.Equals("else")) // check if the next statement starts an "else if"
                    {
                        elseIfScope = 1;
                        savedScopeStackCount = scopeStack.Count;
                    }
                    else elseIfScope = 0; // reset the elseIfScope rule
                    return true;
            }
            return false;
        }

        private bool CheckElseScope(string entry)
        {
            switch (elseScope)
            {
                case 0:
                    if (entry.Equals("else")) elseScope = 1;
                    break;
                case 1:
                    if (entry.Equals("if"))
                    {
                        elseScope = 0;
                        break;
                    }
                    ((ProgramFunction)typeStack.Peek()).Complexity++;
                    scopeStack.Push("else");
                    this.ClearStringBuilder();
                    if (entry.Equals("else")) // check if the next statement starts an "else"
                    {
                        elseScope = 1;
                        savedScopeStackCount = scopeStack.Count;
                    }
                    else elseScope = 0; // reset the elseScope rule
                    return true;
            }
            return false;
        }

        private bool CheckForScope(string entry)
        {
            switch (forScope)
            {
                case 0:
                    if (entry.Equals("for"))
                    {
                        forScope = 1;
                        savedScopeStackCount = scopeStack.Count;
                    }
                    break;
                case 1:
                    if (entry.Equals("(")) forScope = 2;
                    else forScope = 0;
                    break;
                case 2:
                    forScope = 3;
                    break;
                case 3:
                    if (entry.Equals(";")) forScope = 4;
                    break;
                case 4:
                    forScope = 5;
                    break;
                case 5:
                    if (entry.Equals(";")) forScope = 6;
                    break;
                case 6:
                    forScope = 7;
                    break;
                case 7:
                    if (entry.Equals(")") && savedScopeStackCount == scopeStack.Count) forScope = 8;
                    break;
                case 8:
                    ((ProgramFunction)typeStack.Peek()).Complexity++;
                    scopeStack.Push("for");
                    this.ClearStringBuilder();
                    if (entry.Equals("for")) // check if the next statement starts a "for"
                    {
                        forScope = 1;
                        savedScopeStackCount = scopeStack.Count;
                    }
                    else forScope = 0; // reset the forScope rule
                    return true;
            }
            return false;
        }

        private bool CheckForEachScope(string entry)
        {
            switch (forEachScope)
            {
                case 0:
                    if (entry.Equals("foreach"))
                    {
                        forEachScope = 1;
                        savedScopeStackCount = scopeStack.Count;
                    }
                    break;
                case 1:
                    if (entry.Equals("(")) forEachScope = 2;
                    else forEachScope = 0;
                    break;
                case 2:
                    forEachScope = 3;
                    break;
                case 3:
                    if (entry.Equals(")") && savedScopeStackCount == scopeStack.Count) forEachScope = 4;
                    break;
                case 4:
                    ((ProgramFunction)typeStack.Peek()).Complexity++;
                    scopeStack.Push("foreach");
                    this.ClearStringBuilder();
                    if (entry.Equals("foreach")) // check if the next statement starts a "foreach"
                    {
                        forEachScope = 1;
                        savedScopeStackCount = scopeStack.Count;
                    }
                    else forEachScope = 0; // reset the forEachScope rule
                    return true;
            }
            return false;
        }

        private bool CheckWhileScope(string entry)
        {
            switch (whileScope)
            {
                case 0:
                    if (entry.Equals("while"))
                    {
                        whileScope = 1;
                        savedScopeStackCount = scopeStack.Count;
                    }
                    break;
                case 1:
                    if (entry.Equals("(")) whileScope = 2;
                    else whileScope = 0;
                    break;
                case 2:
                    whileScope = 3;
                    break;
                case 3:
                    if (entry.Equals(")") && savedScopeStackCount == scopeStack.Count) whileScope = 4;
                    break;
                case 4:
                    if (entry.Equals(";"))
                    {
                        whileScope = 0;
                        break;
                    }
                    ((ProgramFunction)typeStack.Peek()).Complexity++;
                    scopeStack.Push("while");
                    this.ClearStringBuilder();
                    if (entry.Equals("while")) // check if the next statement starts a "while"
                    {
                        whileScope = 1;
                        savedScopeStackCount = scopeStack.Count;
                    }
                    else whileScope = 0; // reset the whileScope rule
                    return true;
            }
            return false;
        }

        private bool CheckDoWhileScope(string entry)
        {
            switch (doWhileScope)
            {
                case 0:
                    if (entry.Equals("do")) doWhileScope = 1;
                    break;
                case 1:
                    ((ProgramFunction)typeStack.Peek()).Complexity++;
                    scopeStack.Push("do while");
                    this.ClearStringBuilder();
                    if (entry.Equals("do")) doWhileScope = 1; // check if the next statement starts a "do while"
                    else doWhileScope = 0; // reset the doWhileScope rule
                    return true;
            }
            return false;
        }

        private bool CheckSwitchScope(string entry)
        {
            switch (switchScope)
            {
                case 0:
                    if (entry.Equals("switch"))
                    {
                        switchScope = 1;
                        savedScopeStackCount = scopeStack.Count;
                    }
                    break;
                case 1:
                    if (entry.Equals("(")) switchScope = 2;
                    else switchScope = 0;
                    break;
                case 2:
                    switchScope = 3;
                    break;
                case 3:
                    if (entry.Equals(")") && savedScopeStackCount == scopeStack.Count) switchScope = 4;
                    break;
                case 4:
                    if (!entry.Equals("{"))
                    {
                        switchScope = 0;
                        break;
                    }
                    ((ProgramFunction)typeStack.Peek()).Complexity++;
                    scopeStack.Push("switch");
                    this.ClearStringBuilder();
                    switchScope = 0; // reset the switchScope rule
                    return true;
            }
            return false;
        }


        /* ---------- General Upkeep ---------- */

        private bool IgnoreEntry(string entry)
        {
            if (scopeStack.Count() > 0)
            {
                if (scopeStack.Peek().Equals("\""))
                {
                    if (entry.Equals("\"")) scopeStack.Pop();
                    return true;
                }
                if (scopeStack.Peek().Equals("'"))
                {
                    if (entry.Equals("'")) scopeStack.Pop();
                    return true;
                }
                if (scopeStack.Peek().Equals("//"))
                {
                    if (entry.Equals(" "))
                    {
                        scopeStack.Pop();
                        return false;
                    }
                    return true;
                }
                if (scopeStack.Peek().Equals("/*"))
                {
                    if (entry.Equals("*/")) scopeStack.Pop();
                    return true;
                }
            }
            return false;
        }

        private bool StartPlainTextArea(string entry)
        {
            if (entry.Equals("\"") || entry.Equals("'") || entry.Equals("//") || entry.Equals("/*"))
            {
                scopeStack.Push(entry);
                return true;
            }
            return false;
        }

        private void AppendToStringBuilder(object text)
        {
            stringBuilder.Append(text);
            stringBuilderSize++;
        }

        private void ClearStringBuilder()
        {
            stringBuilder.Clear();
            stringBuilderSize = 0;
        }

        private void UpdateStringBuilder(string entry)
        {
            if (!entry.Equals(" "))
            {
                if (entry.Equals(";") || entry.Equals("}") || entry.Equals("{"))
                {
                    this.ClearStringBuilder();
                    return;
                }
                if (stringBuilder.Length > 0) this.AppendToStringBuilder(" ");
                this.AppendToStringBuilder(entry);
            }
        }

        private void RemoveFunctionSignatureFromTextData(int size)
        {
            if (typeStack.Count > 0 && (typeStack.Peek().GetType() == typeof(ProgramClass) 
                || typeStack.Peek().GetType() == typeof(ProgramInterface) || typeStack.Peek().GetType() == typeof(ProgramFunction)))
            {
                int textDataIndex = ((ProgramDataType)typeStack.Peek()).TextData.Count - 1; // last index of TextData
                while (textDataIndex >= 0 && !((ProgramDataType)typeStack.Peek()).TextData[textDataIndex].Equals(")")) textDataIndex--; // get the index of the last closing parentheses
                size += ((ProgramDataType)typeStack.Peek()).TextData.Count - textDataIndex - 1;
                ((ProgramDataType)typeStack.Peek()).TextData
                    = ((ProgramDataType)typeStack.Peek()).TextData.GetRange(0, ((ProgramDataType)typeStack.Peek()).TextData.Count - size); // remove function signature
            }
        }
    }

    class RelationshipProcessor
    {
        CodeAnalysisData codeAnalysisData;
        ProgramClass programClass;
        
        public void ProcessRelationships(ProgramClassType programClassType, CodeAnalysisData codeAnalysisData)
        {
            if (programClassType.GetType() != typeof(ProgramClass)) return; // interfaces will only collect subclasses

            this.codeAnalysisData = codeAnalysisData;
            this.programClass = (ProgramClass)programClassType;

            this.SetInheritanceRelationships(); // get the superclass/subclass data from the beginning of the class text

            // (1) get the aggregation data from the class text and text of all children
            // (2) get the using data from the parameters fields of all child functions
            this.SetAggregationAndUsingRelationships(this.programClass);

        }

        private void SetInheritanceRelationships()
        {
            string entry;
            int index;
            bool hasSuperclasses = false;
            int brackets = 0;

            for (index = 0;  index < programClass.TextData.Count; index++)
            {
                entry = programClass.TextData[index];

                if (!hasSuperclasses)
                {
                    if (entry.Equals(":"))
                    {
                        hasSuperclasses = true;
                        continue;
                    }
                }

                if (entry.Equals("{"))
                {
                    programClass.TextData = programClass.TextData.GetRange(++index, programClass.TextData.Count - index);
                    return;
                }

                if (entry.Equals("[") || entry.Equals("<"))
                {
                    brackets++;
                    continue;
                }

                if (entry.Equals("]") || entry.Equals(">"))
                {
                    brackets--;
                    continue;
                }

                if (brackets > 0) continue;

                if (hasSuperclasses)
                {
                    if (programClass.Name != entry && codeAnalysisData.ProgramClassTypes.Contains(entry))
                    {
                        ProgramClassType super = codeAnalysisData.ProgramClassTypes[entry];
                        super.SubClasses.Add(programClass);
                        programClass.SuperClasses.Add(super);
                        programClass.TextData.RemoveAt(index);
                    }
                }
            }
        }

        private void SetAggregationAndUsingRelationships(ProgramDataType programDataType)
        {
            // get the aggregation data
            foreach (string entry in programDataType.TextData)
            {
                if (programClass.Name != entry && codeAnalysisData.ProgramClassTypes.Contains(entry))
                {
                    ProgramClassType owned = codeAnalysisData.ProgramClassTypes[entry];
                    if (!programClass.OwnedClasses.Contains(owned) && owned.GetType() == typeof(ProgramClass))
                    {
                        ((ProgramClass)owned).OwnedByClasses.Add(programClass);
                        programClass.OwnedClasses.Add(owned);
                    }
                }
            }

            // get the using data
            if (programDataType.GetType() == typeof(ProgramFunction))
            {
                if (((ProgramFunction)programDataType).Parameters.Count > 0)
                {
                    foreach (string parameter in ((ProgramFunction)programDataType).Parameters)
                    {
                        if (programClass.Name != parameter && codeAnalysisData.ProgramClassTypes.Contains(parameter))
                        {
                            ProgramClassType used = codeAnalysisData.ProgramClassTypes[parameter];
                            if (!programClass.UsedClasses.Contains(used))
                            {
                                used.UsedByClasses.Add(programClass);
                                programClass.UsedClasses.Add(used);
                            }
                        }
                    }
                }
            }

            // repeat for each child, grandchild, etc.
            foreach (ProgramType programType in programDataType.ChildList)
            {
                if (programType.GetType() == typeof(ProgramClass) || programType.GetType() == typeof(ProgramFunction))
                {
                    this.SetAggregationAndUsingRelationships((ProgramDataType)programType);
                }
            }
        }
    }
}
