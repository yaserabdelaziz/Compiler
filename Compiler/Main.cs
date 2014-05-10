﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.CodeDom.Compiler;
using System.Diagnostics;

namespace Compiler
{
    public partial class Main : Form
    {
        List<String> declared = new List<String>();
        List<String> initialized = new List<String>();
        List<String> startinit = new List<String>();
        private String initialization = "";
        private String code = @".code
start:
";

        public List<String> Errors = new List<String>();
        String compiled = @".586
.model flat,stdcall
option casemap:none            
include \masm32\include\windows.inc
include \masm32\include\masm32.inc
include \masm32\include\kernel32.inc
include \masm32\include\gdi32.inc
include \masm32\include\user32.inc
include \masm32\include\debug.inc
includelib \masm32\lib\masm32.lib
includelib \masm32\lib\kernel32.lib
includelib \masm32\lib\gdi32.lib
includelib \masm32\lib\user32.lib
includelib \masm32\lib\debug.lib      
DBGWIN_EXT_INFO = 0
.data
";
        string path;
        int error_no = 1;

        public Main()
        {
            InitializeComponent();
            runToolStripMenuItem.Enabled = false; //Run Is Disabled
        }

        private void compileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            literalsView.Items.Clear();
            constantsView.Items.Clear();
            symbolsView.Items.Clear();
            reservedWordsView.Items.Clear();
            tbCompiled.Text = Compile(tbProgram.Text);
            tbCompiled.Text = Errors.Aggregate("", (current, error) => current + (error + Environment.NewLine));
        }

        private void testToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // create a new instance of save file dialoge
            SaveFileDialog SFD = new SaveFileDialog();
            //set the title of the save file dialog
            SFD.Title = "Save EXE to ....";
            //adding a filter to make sure it saved as exe
            SFD.Filter = "EXE|*.exe";


            // if the user pressed ok on the form build it
            if (SFD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                //clear the list
                List_Error.Items.Clear();
                //path = file name
                path = SFD.FileName;

                //Create a code Provider for the language path
                CodeDomProvider Code_Provider = CodeDomProvider.CreateProvider("CSharp");//create the pramter for code provider            }
                CompilerParameters prameters = new CompilerParameters(); //create prameters for code cmpiler
                prameters.GenerateExecutable = true; // make it that it generats an executable
                prameters.OutputAssembly = SFD.FileName; // output location == save file dialoge

                //compile the code in the rich box, return the error
                CompilerResults Results = Code_Provider.CompileAssemblyFromSource(prameters, tbProgram.Text);

                //error cheaking
                if (Results.Errors.Count > 0)
                {
                    //loop through each error
                    foreach (CompilerError cmperror in Results.Errors)
                    {
                        ListViewItem item = new ListViewItem();
                        item.Text = cmperror.ErrorNumber + error_no;
                        item.SubItems.Add(cmperror.Line.ToString());
                        item.SubItems.Add(cmperror.ErrorText);
                        List_Error.Items.Add(item);

                    }
                }
                else
                {
                    runToolStripMenuItem.Enabled = true;
                }
            }
        }

        private void runToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(path);
        }

        private void clearToolStripMenuItem_Click(object sender, System.EventArgs e)
        {
            literalsView.Items.Clear();
            constantsView.Items.Clear();
            symbolsView.Items.Clear();
            reservedWordsView.Items.Clear();
            tbCompiled.Clear();
            List_Error.Items.Clear();
            error_no = 1;
        }

        public String Compile(String program)
        {
            IEnumerable<string> commands = Commands(program);
            int line = 0;
            foreach (var tokens in commands.Select(Parce))
            {
                line++;
                //#region show tokens
                //string show = "";
                //for (int i = 0; i < tokens.Count(); i++)
                //    show += tokens[i] + " ";
                //MessageBox.Show(show);
                //#endregion

                #region scanning
                var separator = new[] { " " };
                for (int i = 0; i < tokens.Count(); i++)
                {
                    IEnumerable<string> words = tokens[i].Split(separator, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var wordNow in words)
                    {
                        string word = wordNow;
                        string now = "";
                        int ii = 0;
                        if (word[ii] == '#')
                        {
                            symbolsView.Items.Add("#");
                            string predecessor = "";
                            int iii;
                            for (iii = 1; iii < word.Length && word[iii] != '<'; iii++)
                            {
                                predecessor += word[iii];
                            }
                            string include = "";
                            for (; iii < word.Length; iii++)
                            {
                                if (word[iii] == '>')
                                {
                                    iii++;
                                    symbolsView.Items.Add(">");
                                    break;
                                }
                                if (IsSymbol(word[iii].ToString()))
                                    symbolsView.Items.Add(word[iii].ToString());
                                else
                                    include += word[iii];
                            }
                            if (include != "")
                                includesView.Items.Add(include);
                            predecessorsView.Items.Add(predecessor);
                            word = word.Substring(iii);
                        }

                        for (ii = 0; ii < word.Count(); ii++)
                        {

                            if (IsSymbol(word[ii].ToString()))
                            {
                                symbolsView.Items.Add(word[ii].ToString());
                            }
                            else
                                now += word[ii];
                        }
                        if (now == "")
                            break;
                        if (IsReservedWord(now))
                            reservedWordsView.Items.Add(now);
                        else if (IsNumber(now))
                            constantsView.Items.Add(now);
                        else if (IsVariable(now))
                            literalsView.Items.Add(now);
                    }
                }
                #endregion

                if ((tokens.Count() == 1))
                {
                    string[] words = tokens[0].Split(separator, StringSplitOptions.RemoveEmptyEntries);
                    if (words.Count() == 2 && IsType(words[0]) && IsVariable(words[1]))
                    {
                        if (declared.Contains(words[1]))
                        {
                            ListViewItem item = new ListViewItem();
                            item.Text = error_no.ToString();
                            error_no++;
                            item.SubItems.Add(line.ToString());
                            item.SubItems.Add("Variable " + words[1] + " already declared");
                            List_Error.Items.Add(item);
                        }
                        else
                        {
                            declared.Add((words[1]));
                        }
                    }
                    else if (words.Count() == 1 && IsVariable(words[0]))
                    {
                        if (declared.Contains(words[0]))
                        {
                            ListViewItem item = new ListViewItem();
                            item.Text = error_no.ToString();
                            error_no++;
                            item.SubItems.Add(line.ToString());
                            item.SubItems.Add("Variable " + words[0] + " already declared");
                            List_Error.Items.Add(item);
                        }
                        else
                        {
                            declared.Add((words[0]));
                        }
                    }
                    else
                    {
                        ListViewItem item = new ListViewItem();
                        item.Text = error_no.ToString();
                        error_no++;
                        item.SubItems.Add(line.ToString());
                        item.SubItems.Add("Unknown command (Error in expression)");
                        List_Error.Items.Add(item);
                    }
                }
                else if (IsVariable(tokens[0]) && (tokens.Count() == 1))
                {
                    if (declared.Contains(tokens[0]))
                    {
                        ListViewItem item = new ListViewItem();
                        item.Text = error_no.ToString();
                        error_no++;
                        item.SubItems.Add(line.ToString());
                        item.SubItems.Add("Variable " + tokens[0] + " already declared");
                        List_Error.Items.Add(item);
                    }
                    else
                    {
                        declared.Add((tokens[0]));
                    }
                }
                else if ((IsVariable(tokens[0])) && (tokens[1] == "=") && (IsNumber(tokens[2])))
                {
                    if (declared.Contains(tokens[0]))
                    {
                        if (!initialized.Contains(tokens[0]))
                        {
                            initialization += tokens[0] + " DW " + tokens[2] + Environment.NewLine;
                            initialized.Add(tokens[0]);
                            startinit.Add(tokens[0]);
                        }
                        else
                        {
                            ListViewItem item = new ListViewItem();
                            item.Text = error_no.ToString();
                            error_no++;
                            item.SubItems.Add(line.ToString());
                            item.SubItems.Add("Variable " + tokens[0] + " already initialized");
                            List_Error.Items.Add(item);
                        }
                    }
                    else
                    {
                        ListViewItem item = new ListViewItem();
                        item.Text = error_no.ToString();
                        error_no++;
                        item.SubItems.Add(line.ToString());
                        item.SubItems.Add("Variable " + tokens[0] + " is not declared");
                        List_Error.Items.Add(item);
                    }
                }
                else if ((IsVariable(tokens[0])) && (tokens[1] == "=") && (IsExpresion(tokens[2])))
                {
                    code += ParceExpresion(tokens[2]);
                    code += "POP AX" + Environment.NewLine;
                    code += "MOV " + tokens[0] + ",AX" + Environment.NewLine;
                    initialized.Add(tokens[0]);
                }
                else
                {
                    ListViewItem item = new ListViewItem();
                    item.Text = error_no.ToString();
                    error_no++;
                    item.SubItems.Add(line.ToString());
                    item.SubItems.Add("Unknown command (Error in expression)");
                    List_Error.Items.Add(item);
                }
            }

            foreach (var variable in declared.Where(variable => !startinit.Contains(variable)))
            {
                initialization += variable + " DW 0" + Environment.NewLine;
            }

            compiled += initialization;
            compiled += code;

            compiled += declared.Aggregate("", (current, variable) => current + ("DumpMem offset " + variable + ", 1" + Environment.NewLine));
            compiled += @"invoke ExitProcess, NULL
end start
end";
            compiled = Optimize(compiled);
            return compiled;
        }

        private string Optimize(string program)
        {
            return program.Replace("PUSH AX\r\nPOP AX\r\n", "");
        }

        private string ParceExpresion(string token)
        {
            if (IsVariable(token))
                if (initialized.Contains(token))
                {
                    var temp = "";
                    temp += "MOV AX," + token + Environment.NewLine;
                    temp += "PUSH AX" + Environment.NewLine;
                    return temp;
                }
                else
                {
                    Errors.Add("Variable " + token + " not initialized");
                    return "";
                }
            if (IsNumber(token))
            {
                var temp = "";
                temp += "MOV AX," + token + Environment.NewLine;
                temp += "PUSH AX" + Environment.NewLine;
                return temp;
            }
            var posp = token.IndexOf('+');
            var posb = token.IndexOf('(');
            if ((posb == -1) && (posp == -1)) return "";
            if (posb < 0) posb = int.MaxValue;
            if (posp < 0) posp = int.MaxValue;
            if (posp < posb)
            {
                var temp = "";
                temp += ParceExpresion(token.Substring(0, posp));
                temp += ParceExpresion(token.Substring(posp + 1, token.Length - posp - 1));
                temp += "POP AX" + Environment.NewLine;
                temp += "POP BX" + Environment.NewLine;
                temp += "ADD AX,BX" + Environment.NewLine;
                temp += "PUSH AX" + Environment.NewLine;
                return temp;
            }
            if (posb != 0) return "";
            var p = 1;
            if (posb < posp)
            {
                int i;
                for (i = 1; i < token.Length; i++)
                {
                    if (token[i] == '(') p++;
                    if (token[i] == ')') p--;
                    if (p == 0) break;
                }
                if (i + 1 == token.Length)
                {
                    var temp = "";
                    temp += ParceExpresion(token.Substring(posb + 1, i - 1));
                    return temp;
                }
                else if (token[i + 1] == '+')
                {
                    var temp = "";
                    temp += ParceExpresion(token.Substring(posb + 1, i - 1));
                    temp += ParceExpresion(token.Substring(i + 2, token.Length - i - 2));
                    temp += "POP AX" + Environment.NewLine;
                    temp += "POP BX" + Environment.NewLine;
                    temp += "ADD AX,BX" + Environment.NewLine;
                    temp += "PUSH AX" + Environment.NewLine;
                    return temp;
                }
            }
            return "";
        }

        private bool IsExpresion(string token)
        {
            if (IsVariable(token))
                if (initialized.Contains(token))
                    return true;
                else
                {
                    Errors.Add("Variable " + token + " not initialized");
                    return false;
                }
            if (IsNumber(token)) return true;
            var posp = token.IndexOf('+');
            var posb = token.IndexOf('(');
            if ((posb == -1) && (posp == -1)) return false;
            if (posb < 0) posb = int.MaxValue;
            if (posp < 0) posp = int.MaxValue;
            if (posp < posb)
            {
                return IsExpresion(token.Substring(0, posp)) &&
                       IsExpresion(token.Substring(posp + 1, token.Length - posp - 1));
            }
            if (posb != 0) return false;
            var p = 1;
            if (posb < posp)
            {
                int i;
                for (i = 1; i < token.Length; i++)
                {
                    if (token[i] == '(') p++;
                    if (token[i] == ')') p--;
                    if (p == 0) break;
                }
                if (p != 0) return false;

                if (i + 1 == token.Length)
                {
                    return IsExpresion(token.Substring(posb + 1, i - 1));
                }
                else if (token[i + 1] == '+')
                {
                    return IsExpresion(token.Substring(posb + 1, i - 1)) &&
                           IsExpresion(token.Substring(i + 2, token.Length - i - 2));
                }
                else
                {
                    return false;
                }
            }
            return false;
        }

        private bool IsNumber(String token)
        {
            return token.All(c => (c >= '0') && (c <= '9'));
        }

        private bool IsVariable(String token)
        {
            return token.All(c => ((c >= 'a') && (c <= 'z')) || ((c >= 'A') && (c <= 'Z')));
        }

        private bool IsSymbol(String token)
        {
            switch (token)
            {
                case ",":
                case "=":
                case "(":
                case ")":
                case "{":
                case "}":
                case "<":
                case ">":
                    return true;
            }
            return false;
        }

        private bool IsReservedWord(String token)
        {
            switch (token)
            {
                case "using":
                case "main":
                case "return":

                case "string":
                case "double":
                case "float":
                case "long":
                case "short":
                case "int":

                case "for":
                case "while":
                case "if":
                case "foreach":

                    return true;
            }
            return false;
        }

        private bool IsType(String token)
        {
            switch (token)
            {

                case "string":
                case "double":
                case "float":
                case "long":
                case "short":
                case "int":
                    return true;
            }
            return false;
        }

        private String[] Parce(String command)
        {
            if (command.IndexOf('=') == -1)
            {
                return new[] { command };
            }
            for (int i = 0; i < command.Count() - 2; i++)
            {
                if (((!char.IsLetter(command[i]) || !char.IsLetter(command[i + 2]))) && (command[i + 1] == ' '))
                {
                    command = command.Remove(i + 1, 1);
                }
            }
            return new[] { command.Substring(0, command.IndexOf('=')), "=", command.Substring(command.IndexOf('=') + 1, command.Length - command.IndexOf('=') - 1) };
        }

        private List<string> Commands(String program)
        {
            int semi = program.Count(c => c == ';');
            for (int i = 0; i < semi; i++)
                symbolsView.Items.Add(";");

            var separator = new[] { ";", "\r\n" };
            program.Replace("\r\n", "");
            var commands = new List<String>(program.Split(separator, StringSplitOptions.RemoveEmptyEntries).AsEnumerable());
            for (var i = 0; i < commands.Count; i++)
            {
                commands[i] = commands[i].Trim().Replace("\r\n", "");
            }
            return commands;
        }

        private static void ChangeColor(RichTextBox RTB, int StartPos, string Regex1, Color color)
        {
            Regex R = new Regex(Regex1);
            //   RTB.SelectAll();
            //  RTB.SelectionColor = Color.White;
            RTB.Select(RTB.Text.Length, 1);

            foreach (Match Match in R.Matches(RTB.Text))
            {
                RTB.Select(Match.Index, Match.Length);
                RTB.SelectionColor = color;
                RTB.SelectionStart = StartPos;
            }
            // rtb.SelectionColor = Color.Black;
        }

        private void tbProgram_TextChanged(object sender, EventArgs e)
        {

            error_no = 1;
            List_Error.Items.Clear();
            tbProgram.SelectionColor = Color.Black;
            //No semi_colon
            ChangeColor(tbProgram, 0, "[^,;]+", Color.Red);
            ChangeColor(tbProgram, 0, "#.*", Color.Black);
            ChangeColor(tbProgram, 0, ".*?\\(\\)", Color.Black);
            ChangeColor(tbProgram, 0, ".*?;", Color.Black);
            ChangeColor(tbProgram, 0, "{?", Color.Black);
            ChangeColor(tbProgram, 0, "}?", Color.Black);
            //find comments            
            ChangeColor(tbProgram, 0, "(/\\*([^*]|[\r\n]|(\\*+([^*/]|[\r\n])))*\\*+/)|(//.*)", Color.Green);
            //find types
            ChangeColor(tbProgram, 0, "int", Color.Blue);
            ChangeColor(tbProgram, 0, "string", Color.Blue);
            ChangeColor(tbProgram, 0, "double", Color.Blue);
            ChangeColor(tbProgram, 0, "float", Color.Blue);
            ChangeColor(tbProgram, 0, "long", Color.Blue);
            ChangeColor(tbProgram, 0, "short", Color.Blue);
            //find condictions
            ChangeColor(tbProgram, 0, "if", Color.Blue);
            ChangeColor(tbProgram, 0, "while", Color.Blue);
            ChangeColor(tbProgram, 0, "for", Color.Blue);
            ChangeColor(tbProgram, 0, "foreach", Color.Blue);
            //include
            ChangeColor(tbProgram, 0, "#include", Color.Purple);
            //using
            ChangeColor(tbProgram, 0, "using", Color.Blue);
            //return
            ChangeColor(tbProgram, 0, "return", Color.Blue);

       /*     if (FindRegexCount(tbProgram, 1, @"\(.*?\)") != FindRegexCount(tbProgram, 1, "\\("))
            {

                ListViewItem item = new ListViewItem();
                item.Text = error_no.ToString();
                error_no++;
                item.SubItems.Add("");
                item.SubItems.Add("Error with parenthese");
                List_Error.Items.Add(item);
            }


            if (FindRegexCount(tbProgram, 1, @"\(.*?\)") != FindRegexCount(tbProgram, 1, "\\)"))
            {

                ListViewItem item = new ListViewItem();
                item.Text = error_no.ToString();
                error_no++;
                item.SubItems.Add("");
                item.SubItems.Add("Error with parenthese");
                List_Error.Items.Add(item);
            }

            if (FindRegexCount(tbProgram, 1, @"\{.*?\}") != FindRegexCount(tbProgram, 1, "\\{"))
            {

                ListViewItem item = new ListViewItem();
                item.Text = error_no.ToString();
                error_no++;
                item.SubItems.Add("");
                item.SubItems.Add("Error with curly brackets");
                List_Error.Items.Add(item);
            }


            if (FindRegexCount(tbProgram, 1, @"\{.*?\}") != FindRegexCount(tbProgram, 1, "\\}"))
            {

                ListViewItem item = new ListViewItem();
                item.Text = error_no.ToString();
                error_no++;
                item.SubItems.Add("");
                item.SubItems.Add("Error with curly brackets");
                List_Error.Items.Add(item);
            }*/
            int leftSquatebrackt = 0;
            int rightSquarebracket = 0;
            int leftParentheses = 0;
            int rightParentheses = 0;
            int rightbraces = 0;
            int leftbraces = 0;

            for (int i = 0; i < tbProgram.Text.Length;i++ )
            {
                if (tbProgram.Text[i] == '[')
                {
                    leftSquatebrackt++;
                }
                else if(tbProgram.Text[i] == ']')
                {
                    rightSquarebracket++;
                }
                else if (tbProgram.Text[i] == '(')
                {
                    leftParentheses++;
                }
                else if (tbProgram.Text[i] == ')')
                {
                    rightParentheses++;
                }
                else if (tbProgram.Text[i] == '{')
                {
                    leftbraces++;
                }
                else if (tbProgram.Text[i] == '}')
                {
                    rightbraces++;
                }
            }
            if(leftSquatebrackt!=rightSquarebracket)
            {
                ListViewItem item = new ListViewItem();
                item.Text = error_no.ToString();
                error_no++;
                item.SubItems.Add("");
                item.SubItems.Add("Error In Arary Bracket");
                List_Error.Items.Add(item);
            }
            if (rightParentheses != leftParentheses)
            {
                ListViewItem item = new ListViewItem();
                item.Text = error_no.ToString();
                error_no++;
                item.SubItems.Add("");
                item.SubItems.Add("Error In praentheses");
                List_Error.Items.Add(item);
            }
            if(leftbraces!=rightbraces)
            {
                ListViewItem item = new ListViewItem();
                item.Text = error_no.ToString();
                error_no++;
                item.SubItems.Add("");
                item.SubItems.Add("Error In braces");
                List_Error.Items.Add(item);
            }

                tbProgram.SelectionColor = Color.Black;
        }

        private static int FindRegexCount(RichTextBox RTB, int StartPos, string Regex1)
        {
            Regex R = new Regex(Regex1);
            RTB.Select(RTB.Text.Length, 1);
            MatchCollection New_Line_Match = R.Matches(RTB.Text);
            return New_Line_Match.Count;
        }
    }
}