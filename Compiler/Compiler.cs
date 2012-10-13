﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Compiler
{
    class Compiler
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
        public String Compile(String program)
        {
            IEnumerable<string> commands = Commands(program);
            foreach (var tokens in commands.Select(Parce))
            {
                if (IsVariable(tokens[0]) && (tokens.Count() == 1))
                {
                    if (declared.Contains(tokens[0]))
                    {
                        Errors.Add("Змінна " + tokens[0] + " вже оголошена");
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
                            Errors.Add("Змінна " + tokens[0] + " вже ініціалізована");
                        }
                    }
                    else
                    {
                        Errors.Add("Змінна " + tokens[0] + " не оголошена");
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
                    Errors.Add("Невідома команда (помилка в виразі)");
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
                    Errors.Add("Змінна " + token + " не ініціалізована");
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
                    Errors.Add("Змінна " + token + " не ініціалізована");
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
            var separator = new[] { ";" };
            program.Replace("\r\n", "");
            var commands = new List<String>(program.Split(separator, StringSplitOptions.RemoveEmptyEntries).AsEnumerable());
            for (var i = 0; i < commands.Count; i++)
                commands[i] = commands[i].Trim().Replace("\r\n", "");
            return commands;
        }

    }
}
