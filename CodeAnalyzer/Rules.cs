﻿using System.Collections.Generic;
using System.Linq;

namespace CodeAnalyzer
{
    public interface IRule
    {
        bool IsPassed(string entry, int scopeCount);
    }

    public interface ICFScopeRule : IRule
    {
        string GetScopeType();
    }

    public abstract class CFScopeRule : ICFScopeRule
    {
        public int Step { get; internal set; }
        public bool Complete { get; internal set; }
        public CFScopeRule() { Step = 0; Complete = false; }
        public abstract bool IsPassed(string entry, int scopeCount);
        public abstract string GetScopeType();
    }

    /* Tests rules for "if" statement syntax */
    public class IfRule : CFScopeRule
    {
        private int savedScopeCount;

        public IfRule(int scopeCount) : base() { savedScopeCount = scopeCount; }

        public override bool IsPassed(string entry, int scopeCount)
        {
            switch (Step)
            {
                case 0: // To pass: Find opening parenthesis.
                    if (entry.Equals("(")) Step = 1; else Complete = true;
                    break;
                case 1: // To pass: Find at least one entry inside parentheses.
                    Step = 2;
                    break;
                case 2: // To pass: Find closing parenthesis.
                    if (entry.Equals(")") && savedScopeCount == scopeCount) Step = 3;
                    break;
                case 3: // Rule passed.
                    return Complete = true;
            }
            return false;
        }

        public override string GetScopeType() => "if";
    }

    /* Tests rules for "else if" statement syntax */
    public class ElseIfRule : CFScopeRule
    {
        private int savedScopeCount;

        public ElseIfRule(int scopeCount) : base() { savedScopeCount = scopeCount; }

        public override bool IsPassed(string entry, int scopeCount)
        {
            switch (Step)
            {
                case 0: // To pass: Find opening parenthesis.
                    if (entry.Equals("(")) Step = 1; else Complete = true;
                    break;
                case 1: // To pass: Find at least one entry inside parentheses.
                    Step = 2;
                    break;
                case 2: // To pass: Find closing parenthesis.
                    if (entry.Equals(")") && savedScopeCount == scopeCount) Step = 3;
                    break;
                case 3: // Rule passed.
                    return Complete = true;
            }
            return false;
        }

        public override string GetScopeType() => "else if";
    }

    /* Tests rules for "else" statement syntax */
    public class ElseRule : CFScopeRule
    {
        private int savedScopeCount;

        public ElseRule(int scopeCount) : base() { savedScopeCount = scopeCount; }

        public override bool IsPassed(string entry, int scopeCount)
        {
            // To pass: Anything except "if".
            if (entry.Equals("if"))
            {
                Complete = true;
                return false;
            }
            return Complete = true; // Rule passed.
        }

        public override string GetScopeType() => "else";
    }

    /* Tests rules for "for" loop syntax for C# */
    public class ForRule_CS : CFScopeRule
    {
        private int savedScopeCount;

        public ForRule_CS(int scopeCount) : base() { savedScopeCount = scopeCount; }

        public override bool IsPassed(string entry, int scopeCount)
        {
            switch (Step)
            {
                case 0: // To pass: Find opening parenthesis.
                    if (entry.Equals("(")) Step = 1; else Complete = true;
                    break;
                case 1: // To pass: Find at least one entry.
                    Step = 2;
                    break;
                case 2: // To pass: Find semicolon.
                    if (entry.Equals(";")) Step = 3;
                    break;
                case 3: // To pass: Find at least one entry.
                    Step = 4;
                    break;
                case 4: // To pass: Find semicolon.
                    if (entry.Equals(";")) Step = 5;
                    break;
                case 5: // To pass: Find at least one entry.
                    Step = 6;
                    break;
                case 6: // To pass: Find closing parenthesis.
                    if (entry.Equals(")") && savedScopeCount == scopeCount) Step = 7;
                    break;
                case 7: // Rule passed.
                    return Complete = true;
            }
            return false;
        }

        public override string GetScopeType() => "for";
    }

    /* Tests rules for "for" loop syntax for Java */
    public class ForRule_JAVA : CFScopeRule
    {
        private int savedScopeCount;

        public ForRule_JAVA(int scopeCount) : base() { savedScopeCount = scopeCount; }

        public override bool IsPassed(string entry, int scopeCount)
        {
            switch (Step)
            {
                case 0: // To pass: Find openeing parenthesis.
                    if (entry.Equals("(")) Step = 1; else Complete = true;
                    break;
                case 1: //To pass:Find at least one entry inside parentheses.
                    Step = 2;
                    break;
                case 2: // To pass: Find closing parenthesis.
                    if (entry.Equals(")") && savedScopeCount == scopeCount) Step = 3;
                    break;
                case 3: // Rule passed.
                    return Complete = true;
            }
            return false;
        }

        public override string GetScopeType() => "for";
    }

    /* Tests rules for "foreach" loop syntax */
    public class ForEachRule : CFScopeRule
    {
        private int savedScopeCount;

        public ForEachRule(int scopeCount) : base() { savedScopeCount = scopeCount; }

        public override bool IsPassed(string entry, int scopeCount)
        {
            switch (Step)
            {
                case 0: // To pass: Find opening parenthesis.
                    if (entry.Equals("(")) Step = 1; else Complete = true;
                    break;
                case 1: // To pass: Find at least one entry inside parentheses.
                    Step = 2;
                    break;
                case 2: // To pass: Find closing parenthesis.
                    if (entry.Equals(")") && savedScopeCount == scopeCount) Step = 3;
                    break;
                case 3: // Rule passed.
                    return Complete = true;
            }
            return false;
        }

        public override string GetScopeType() => "for each";
    }

    /* Tests rules for "while" loop syntax */
    public class WhileRule : CFScopeRule
    {
        private int savedScopeCount;

        public WhileRule(int scopeCount) : base() { savedScopeCount = scopeCount; }

        public override bool IsPassed(string entry, int scopeCount)
        {
            switch (Step)
            {
                case 0: // To pass: Find opening parenthesis.
                    if (entry.Equals("(")) Step = 1; else Complete = true;
                    break;
                case 1: // To pass: Find at least one entry inside parentheses.
                    Step = 2;
                    break;
                case 2: // To pass: Find closing parenthesis.
                    if (entry.Equals(")") && savedScopeCount == scopeCount) Step = 3;
                    break;
                case 3: // To pass: Anything except semicolon.
                    if (entry.Equals(";"))
                    {
                        Complete = true;
                        break;
                    }
                    return Complete = true; // Rule passed.
            }
            return false;
        }

        public override string GetScopeType() => "while";
    }

    /* Tests rules for "do while" loop syntax */
    public class DoWhileRule : CFScopeRule
    {
        private int savedScopeCount;

        public DoWhileRule(int scopeCount) : base() { savedScopeCount = scopeCount; }

        public override bool IsPassed(string entry, int scopeCount)
        {
            return Complete = true;
        }

        public override string GetScopeType() => "do while";
    }

    /* Tests rules for "switch" statement syntax */
    public class SwitchRule : CFScopeRule
    {
        private int savedScopeCount;

        public SwitchRule(int scopeCount) : base() { savedScopeCount = scopeCount; }

        public override bool IsPassed(string entry, int scopeCount)
        {
            switch (Step)
            {
                case 0: // To pass: Find opening parenthesis.
                    if (entry.Equals("(")) Step = 1; else Complete = true;
                    break;
                case 1: // To pass: Find at least one entry inside parentheses.
                    Step = 2;
                    break;
                case 2: // To pass: Find closing parenthesis.
                    if (entry.Equals(")") && savedScopeCount == scopeCount) Step = 3;
                    break;
                case 3: // To pass: Find opening bracket.
                    if (!entry.Equals("{"))
                    {
                        Complete = true;
                        break;
                    }
                    return Complete = true; // Rule passed.
            }
            return false;
        }

        public override string GetScopeType() => "switch";
    }

    public static class CFScopeRuleFactory
    {
        public static CFScopeRule GetRule(List<CFScopeRule> activeRules, string rule, int scopeCount, string fileType)
        {
            if (rule == "if")
            {
                if (!activeRules.OfType<ElseRule>().Where(item => item.Step == 0).Any())
                    return new IfRule(scopeCount);
                else
                {
                    activeRules.RemoveAll(item => item.GetType() == typeof(ElseRule) && ((ElseRule)item).Step == 0);
                    return new ElseIfRule(scopeCount);
                }
            }

            if (rule == "else") return new ElseRule(scopeCount);

            if (rule == "for")
            {
                if (fileType.Equals("*.cs")) return new ForRule_CS(scopeCount);
                if (fileType.Equals("*.java")) return new ForRule_JAVA(scopeCount);
            }

            if (rule == "foreach" && !fileType.Equals("*.java")) return new ForEachRule(scopeCount);

            if (rule == "while") return new WhileRule(scopeCount);

            if (rule == "do") return new DoWhileRule(scopeCount);

            if (rule == "switch") return new SwitchRule(scopeCount);

            return null;
        }
    }
}
