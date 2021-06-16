using System;
using System.IO;
using System.Collections.Generic;
using GardensPoint;
using System.Text;
using System.Text.RegularExpressions;

public class Compiler
{
    public static int errors = 0;
    private static int lineno = 1;

    public static SyntaxTree syntaxTree;
    public static Dictionary<string, SyntaxTree> symbolTable;

    public static List<string> source;

    private static StreamWriter writer;

    private static List<StringInfo> stringInfos = new List<StringInfo>();

    private static int stringVarNameId = 1;
    private static int registerNameId = 1;
    private static int labelNameId = 1;

    public static int Main(string[] args)
    {
        string file;
        FileStream source;
        Console.WriteLine("\nCompiler for MINI programming language");

        if (args.Length >= 1)
        {
            file = args[0];
        }
        else
        {
            Console.WriteLine("Please provide filename for compilation!");
            return 1;
        }

        try
        {
            var reader = new StreamReader(file);
            string fileContent = reader.ReadToEnd();
            reader.Close();
            Compiler.source = new List<string>(fileContent.Split(new string[] { "\r\n" }, System.StringSplitOptions.None));
            source = new FileStream(file, FileMode.Open);
        }
        catch (Exception e)
        {
            Console.WriteLine("\n" + e.Message);
            return 2;
        }

        Scanner scanner = new Scanner(source);
        Parser parser = new Parser(scanner);
        Console.WriteLine();

        try
        {
            parser.Parse();
            source.Close();
            syntaxTree?.CheckType();
        }
        catch (Exception)
        {
            PrintError("Compilation error");
        }

        if (errors > 0)
        {
            Console.WriteLine("==========================================");
            Console.WriteLine($"Compilation failed -- {errors} errors detected :(\n");
        }
        else
        {
            writer = new StreamWriter(file + ".ll");
            try
            {
                GenCode();
            }
            catch (Exception)
            {
                PrintError("Compilation error");
                writer.Close();
                return 3;
            }
            writer.Close();
            Console.WriteLine("Compilation successful! :)\n");
        }

        return errors == 0 ? 0 : 3;
    }

    private static void GenCode()
    {
        EmitCode("@int_print = constant [3 x i8] c\"%d\\00\"");
        EmitCode("@hex_int_scan = constant [3 x i8] c\"%X\\00\"");
        EmitCode("@hex_int_print = constant [5 x i8] c\"0X%X\\00\"");
        EmitCode("@double_print = constant [4 x i8] c\"%lf\\00\"");
        EmitCode("@bool_print_true = constant [5 x i8] c\"True\\00\"");
        EmitCode("@bool_print_false = constant [6 x i8] c\"False\\00\"");
        foreach (StringInfo info in stringInfos)
        {
            EmitCode($"@{info.stringVarName} = constant [{info.stringLength + 1} x i8] c\"{info.stringValue}\\00\"");
        }
        EmitCode("declare i32 @printf(i8*, ...)");
        EmitCode("declare i32 @scanf_s(i8*, ...)");
        EmitCode();
        EmitCode("define i32 @main()");
        EmitCode("{");
        EmitCode($"{GetCurrentLabelName()}:");

        //Console.WriteLine("\n" + syntaxTree);

        syntaxTree.GenCode();
        EmitCode("}");
    }

    public static void EmitCode(string instr = null)
    {
        writer.WriteLine(instr);
    }

    public static void HandleLexicalError(string symbol)
    {
        PrintError("Lexical error", lineno.ToString(), "unexpected symbol " + Regex.Unescape(symbol));
    }

    public static void HandleSyntaxError()
    {
        PrintError("Syntax error", lineno.ToString());
    }

    public static void HandleSyntaxError(string content)
    {
        PrintError("Syntax error", lineno.ToString(), content);
    }

    public static void HandleSemanticError(int line, string content)
    {
        PrintError("Semantic error", line.ToString(), content);
    }

    private static void PrintError(string errorType, string lineNum = null, string errorContent = null)
    {
        ++errors;
        if (errorContent != null)
            Console.WriteLine(errorType + ": line " + lineNum + " -- " + errorContent);
        else if (lineNum != null)
            Console.WriteLine(errorType + ": line " + lineNum);
        else
            Console.WriteLine(errorType);
    }

    public static string DisplayType(string type)
    {
        if (type == "i32")
            return "int";
        else if (type == "i1")
            return "bool";
        else if (type == "double" || type == "undeclared variable")
            return type;
        return "incalculable expression";
    }

    public static int CurrentLine()
    {
        return lineno;
    }

    public static void NextLine()
    {
        lineno++;
    }

    public static StringInfo AddString(string stringValue)
    {
        stringValue = stringValue.Substring(1, stringValue.Length - 2);
        stringValue = UnescapeNewlineQuotasAndBackslash(stringValue);
        int stringLength = stringValue.Length;
        StringBuilder stringBuilder = new StringBuilder();
        foreach (char charValue in stringValue)
        {
            stringBuilder.Append("\\" + ((int)charValue).ToString("X2"));
        }
        stringValue = stringBuilder.ToString();
        StringInfo info = new StringInfo(GetNextStringVarName(), stringLength, stringValue);
        stringInfos.Add(info);
        return info;
    }

    private static string UnescapeNewlineQuotasAndBackslash(string value)
    {
        List<char> unescapedCharArray = new List<char>();
        char[] charArray = value.ToCharArray();
        for (int i = 0; i < charArray.Length; ++i)
        {
            if (charArray[i] == '\\')
            {
                if (charArray[i + 1] == 'n')
                {
                    unescapedCharArray.Add('\n');
                }
                else
                {
                    unescapedCharArray.Add(charArray[i + 1]);
                }
                i++;
                continue;
            }
            unescapedCharArray.Add(charArray[i]);
        }
        return new string(unescapedCharArray.ToArray());
    }

    internal static string GetNextStringVarName()
    {
        return "string" + stringVarNameId++;
    }

    public static string GetNextRegisterName()
    {
        return "%register" + registerNameId++;
    }

    public static string GetNextLabelName()
    {
        return "label" + ++labelNameId;
    }

    public static string GetCurrentLabelName()
    {
        return "label" + labelNameId;
    }
}

public class StringInfo
{
    public string stringVarName;
    public int stringLength;
    public string stringValue;

    public StringInfo(string varName, int strLen, string strVal)
    {
        stringVarName = varName;
        stringLength = strLen;
        stringValue = strVal;
    }
}

public class Declaration : SyntaxTree
{
    public string identifier;

    public Declaration(string ident, string type)
    {
        identifier = ident;
        line = Compiler.CurrentLine();

        if (type == "int")
        {
            typename = "i32";
        }
        else if (type == "double")
        {
            typename = "double";
        }
        else if (type == "bool")
        {
            typename = "i1";
        }
        else
        {
            throw new Exception($"Declaration of type: {type} not allowed");
        }
    }

    public override string CheckType()
    {
        return "";
    }

    public override string GenCode()
    {
        Compiler.EmitCode($"%var_{identifier} = alloca {typename}");
        return null;
    }

    public override string ToString()
    {
        return base.ToString() + $", [identifier: {identifier}]";
    }
}

public abstract class SyntaxTree
{
    public string typename;
    public int line = -1;
    public abstract string CheckType();
    public abstract string GenCode();

    public SyntaxTree() { line = Compiler.CurrentLine(); }

    public override string ToString()
    {
        return $"{base.ToString()} extends SyntaxTree: [typename: {typename}]";
    }
}

class ProgramBlock : SyntaxTree
{
    private List<SyntaxTree> instructions;

    public ProgramBlock(List<SyntaxTree> declList, List<SyntaxTree> instrList)
    {
        instructions = instrList;
        Compiler.symbolTable = InitializeSymbolTable(declList);
    }

    private Dictionary<string, SyntaxTree> InitializeSymbolTable(List<SyntaxTree> declarations)
    {
        Dictionary<string, SyntaxTree> table = new Dictionary<string, SyntaxTree>();
        foreach (var elem in declarations)
        {
            Declaration decl = elem as Declaration;
            if (table.ContainsKey(decl.identifier))
            {
                Compiler.HandleSemanticError(decl.line, "variable already declared: " + decl.identifier);
            }
            else
            {
                table.Add(decl.identifier, decl);
            }
        }
        return table;
    }

    public override string CheckType()
    {
        foreach (SyntaxTree instr in instructions)
        {
            instr.CheckType();
        }
        return null;
    }

    public override string GenCode()
    {
        foreach (var decl in Compiler.symbolTable.Values)
        {
            decl.GenCode();
        }
        foreach (var instr in instructions)
        {
            instr.GenCode();
        }
        Compiler.EmitCode("ret i32 0");
        return null;
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("DECLARATIONS:\n");
        foreach (KeyValuePair<string, SyntaxTree> entry in Compiler.symbolTable)
        {
            sb.Append("[");
            sb.Append("Key: " + entry.Key);
            sb.Append(", Value: " + entry.Value);
            sb.Append("]\n");
        }
        sb.Append("\n");
        sb.Append("INSTRUCTIONS:\n");
        foreach (SyntaxTree tree in instructions)
        {
            sb.Append(tree + "\n");
        }
        return sb.ToString();
    }
}

class Identifier : SyntaxTree
{
    public string name;

    public Identifier(string id)
    {
        name = id;
        line = Compiler.CurrentLine();
    }

    public override string CheckType()
    {
        if (Compiler.symbolTable.ContainsKey(name))
        {
            typename = Compiler.symbolTable[name].typename;
        }
        else
        {
            typename = "undeclared variable";
            Compiler.HandleSemanticError(line, "undeclared variable: " + name);
        }
        return typename;
    }

    public override string GenCode()
    {
        string register = Compiler.GetNextRegisterName();
        Compiler.EmitCode($"{register} = load {typename}, {typename}* %var_{name}");
        return register;
    }

    public override string ToString()
    {
        return base.ToString() + $", [name: {name}]";
    }
}

class IntNumber : SyntaxTree
{
    private int value;

    public IntNumber(int val)
    {
        value = val;
    }

    public override string CheckType()
    {
        typename = "i32";
        return typename;
    }

    public override string GenCode()
    {
        return value.ToString();
    }
}

class RealNumber : SyntaxTree
{
    private double value;

    public RealNumber(double val)
    {
        value = val;
    }

    public override string CheckType()
    {
        typename = "double";
        return typename;
    }

    public override string GenCode()
    {
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.0###############}", value);
    }
}

class BoolValue : SyntaxTree
{
    private bool value;

    public BoolValue(bool val)
    {
        value = val;
    }

    public override string CheckType()
    {
        typename = "i1";
        return typename;
    }

    public override string GenCode()
    {
        return value ? "true" : "false";
    }
}

class WriteInstruction : SyntaxTree
{
    private SyntaxTree expression;

    public WriteInstruction(SyntaxTree exp)
    {
        expression = exp;
    }

    public override string CheckType()
    {
        typename = expression.CheckType();
        if (typename != "i1" && typename != "i32" && typename != "double")
        {
            Compiler.HandleSemanticError(line, "cannot perform write instruction on: " + Compiler.DisplayType(typename));
        }
        return typename;
    }

    public override string GenCode()
    {
        string value = expression.GenCode();

        if (typename == "i32")
        {
            Compiler.EmitCode($"call i32 (i8*, ...) @printf(i8* bitcast ([3 x i8]* @int_print to i8*), i32 {value})");
        }
        else if (typename == "double")
        {
            Compiler.EmitCode($"call i32 (i8*, ...) @printf(i8* bitcast ([4 x i8]* @double_print to i8*), double {value})");
        }
        else if (typename == "i1")
        {
            string reg = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg} = select i1 {value}, i8* bitcast ([5 x i8]* @bool_print_true to i8*), i8* bitcast ([6 x i8]* @bool_print_false to i8*)");
            Compiler.EmitCode($"call i32 (i8*, ...) @printf(i8* {reg})");
        }
        else
        {
            throw new Exception($"Invalid typename: {typename} provided");
        }

        return null;
    }
}

abstract class UnaryOperation : SyntaxTree
{
    protected SyntaxTree expression;

    public UnaryOperation(SyntaxTree exp)
    {
        expression = exp;
    }

    public UnaryOperation(SyntaxTree exp, int lineNum)
    {
        expression = exp;
        line = lineNum;
    }
}

class UnaryMinusOperation : UnaryOperation
{
    public UnaryMinusOperation(SyntaxTree exp) : base(exp) { }

    public override string CheckType()
    {
        expression.CheckType();
        if (expression.typename == "i32" || expression.typename == "double")
        {
            typename = expression.typename;
        }
        else
        {
            Compiler.HandleSemanticError(line, "cannot perform unary minus on: " + Compiler.DisplayType(expression.typename));
        }
        return typename;
    }

    public override string GenCode()
    {
        string value = expression.GenCode();
        string register = Compiler.GetNextRegisterName();
        if (typename == "i32")
            Compiler.EmitCode($"{register} = sub {expression.typename} 0, {value}");
        else
            Compiler.EmitCode($"{register} = fneg {expression.typename} {value}");
        return register;
    }
}

class BitwiseNegateOperation : UnaryOperation
{
    public BitwiseNegateOperation(SyntaxTree exp) : base(exp) { }

    public override string CheckType()
    {
        expression.CheckType();
        if (expression.typename == "i32")
        {
            typename = "i32";
        }
        else
        {
            Compiler.HandleSemanticError(line, "cannot perform bitwise negate on: " + Compiler.DisplayType(expression.typename));
        }
        return typename;
    }

    public override string GenCode()
    {
        string value = expression.GenCode();
        string register = Compiler.GetNextRegisterName();
        Compiler.EmitCode($"{register} = xor {expression.typename} {value}, -1");
        return register;
    }
}

class LogicalNegateOperation : UnaryOperation
{
    public LogicalNegateOperation(SyntaxTree exp) : base(exp) { }

    public override string CheckType()
    {
        expression.CheckType();
        if (expression.typename == "i1")
        {
            typename = "i1";
        }
        else
        {
            Compiler.HandleSemanticError(line, "cannot perform logical negate on: " + Compiler.DisplayType(expression.typename));
        }
        return typename;
    }

    public override string GenCode()
    {
        string value = expression.GenCode();
        string reg = Compiler.GetNextRegisterName();
        Compiler.EmitCode($"{reg} = icmp ne {expression.typename} {value}, 0");
        string reg2 = Compiler.GetNextRegisterName();
        Compiler.EmitCode($"{reg2} = xor i1 {reg}, true");
        return reg2;
    }
}

class ConvertToIntOperation : UnaryOperation
{
    public ConvertToIntOperation(SyntaxTree exp) : base(exp) { }

    public override string CheckType()
    {
        expression.CheckType();
        if (expression.typename != "i32" && expression.typename != "i1" && expression.typename != "double")
        {
            Compiler.HandleSemanticError(line, "cannot cast undeclared variable to int");
        }
        typename = "i32";
        return typename;
    }

    public override string GenCode()
    {
        string value = expression.GenCode();
        if (expression.typename == "i32")
        {
            return value;
        }
        else if (expression.typename == "double")
        {
            string reg1 = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg1} = fptosi double {value} to i32");
            return reg1;
        }
        else if (expression.typename == "i1")
        {
            string reg1 = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg1} = zext i1 {value} to i32");
            return reg1;
        }
        else
        {
            throw new Exception("Unknown type: " + expression.typename);
        }
    }
}

class ConvertToDoubleOperation : UnaryOperation
{
    public ConvertToDoubleOperation(SyntaxTree exp) : base(exp, Compiler.CurrentLine()) { }

    public override string CheckType()
    {
        expression.CheckType();
        if (expression.typename == "i32" || expression.typename == "double")
        {
            typename = "double";
        }
        else if (expression.typename == "i1")
        {
            Compiler.HandleSemanticError(line, "Cannot convert from bool to double");
        }
        else
        {
            Compiler.HandleSemanticError(line, "cannot cast undeclared variable to double");
        }
        return typename;
    }

    public override string GenCode()
    {
        string value = expression.GenCode();
        if (expression.typename == "double")
        {
            return value;
        }
        else if (expression.typename == "i32")
        {
            string reg1 = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg1} = sitofp i32 {value} to double");
            return reg1;
        }
        else
        {
            throw new Exception("Unknown type: " + expression.typename);
        }
    }
}

abstract class BinaryOperation : SyntaxTree
{
    protected SyntaxTree firstExpression;
    protected SyntaxTree secondExpression;

    public BinaryOperation(SyntaxTree exp1, SyntaxTree exp2)
    {
        firstExpression = exp1;
        secondExpression = exp2;
    }

    public override string ToString()
    {
        return $"BinaryOperation: [firstExpression: [{firstExpression}], secondExpression: [{secondExpression}]]";
    }
}

class AssignOperation : BinaryOperation
{
    public AssignOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        firstExpression.CheckType();
        secondExpression.CheckType();
        if (firstExpression.typename == "double")
        {
            if (secondExpression.typename != "double" && secondExpression.typename != "i32")
            {
                Compiler.HandleSemanticError(line, "cannot assign " + Compiler.DisplayType(secondExpression.typename)
                    + " to a double");
                return null;
            }
            typename = "double";
        }
        else if (firstExpression.typename == "i32")
        {
            if (secondExpression.typename != "i32")
            {
                Compiler.HandleSemanticError(line, "cannot assign " + Compiler.DisplayType(secondExpression.typename)
                    + " to an int");
                return null;
            }
            typename = "i32";
        }
        else if (firstExpression.typename == "i1")
        {
            if (secondExpression.typename != "i1")
            {
                Compiler.HandleSemanticError(line, "cannot assign " + Compiler.DisplayType(secondExpression.typename)
                    + " to a bool");
                return null;
            }
            typename = "i1";
        }
        else
        {
            Compiler.HandleSemanticError(line, "cannot assign to undeclared variable");
        }
        return typename;
    }

    public override string GenCode()
    {
        string secondExpValue = secondExpression.GenCode();
        string secondExpTypename = secondExpression.typename;
        Identifier ident = firstExpression as Identifier;
        string firstExpValue = "%var_" + ident.name;
        string firstExpTypename = firstExpression.typename;
        if (secondExpression.typename == "i32" && firstExpression.typename == "double")
        {
            string reg1 = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg1} = sitofp i32 {secondExpValue} to double");
            secondExpValue = reg1;
            secondExpTypename = "double";
        }
        Compiler.EmitCode($"store {secondExpTypename} {secondExpValue}, {firstExpTypename}* {firstExpValue}");
        return secondExpValue;
    }
}

class LogicalSumOperation : BinaryOperation
{
    public LogicalSumOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        firstExpression.CheckType();
        secondExpression.CheckType();
        if (firstExpression.typename == "i1" && secondExpression.typename == "i1")
        {
            typename = "i1";
        }
        else
        {
            Compiler.HandleSemanticError(line, "cannot perform logical sum on: " + Compiler.DisplayType(firstExpression.typename)
                + ", " + Compiler.DisplayType(secondExpression.typename));
        }
        return typename;
    }

    public override string GenCode()
    {
        string labelStart = Compiler.GetNextLabelName();
        string labelUnfortunatelyNeedToCalculate = Compiler.GetNextLabelName();
        string labelHadToDoFullCalc = Compiler.GetNextLabelName();
        string labelEnd = Compiler.GetNextLabelName();
        string value1 = firstExpression.GenCode();
        Compiler.EmitCode($"br label %{labelStart}");
        Compiler.EmitCode($"{labelStart}:");
        Compiler.EmitCode($"br i1 {value1}, label %{labelEnd}, label %{labelUnfortunatelyNeedToCalculate}");
        Compiler.EmitCode($"{labelUnfortunatelyNeedToCalculate}:");
        string value2 = secondExpression.GenCode();
        Compiler.EmitCode($"br label %{labelHadToDoFullCalc}"); // those "redundant" labels are to make sure phi node predecessors/control paths are covered properly
        Compiler.EmitCode($"{labelHadToDoFullCalc}:");
        Compiler.EmitCode($"br label %{labelEnd}");
        Compiler.EmitCode($"{labelEnd}:");
        string finalReg = Compiler.GetNextRegisterName();
        Compiler.EmitCode($"{finalReg} = phi i1 [ true, %{labelStart} ], [ {value2}, %{labelHadToDoFullCalc} ]");
        return finalReg;
    }
}

class LogicalProductOperation : BinaryOperation
{
    public LogicalProductOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        firstExpression.CheckType();
        secondExpression.CheckType();
        if (firstExpression.typename == "i1" && secondExpression.typename == "i1")
        {
            typename = "i1";
        }
        else
        {
            Compiler.HandleSemanticError(line, "cannot perform logical sum on: " + Compiler.DisplayType(firstExpression.typename)
                + ", " + Compiler.DisplayType(secondExpression.typename));
        }
        return typename;
    }

    public override string GenCode()
    {
        string labelStart = Compiler.GetNextLabelName();
        string labelUnfortunatelyNeedToCalculate = Compiler.GetNextLabelName();
        string labelHadToDoFullCalc = Compiler.GetNextLabelName();
        string labelEnd = Compiler.GetNextLabelName();
        string value1 = firstExpression.GenCode();
        Compiler.EmitCode($"br label %{labelStart}");
        Compiler.EmitCode($"{labelStart}:");
        Compiler.EmitCode($"br i1 {value1}, label %{labelUnfortunatelyNeedToCalculate}, label %{labelEnd}");
        Compiler.EmitCode($"{labelUnfortunatelyNeedToCalculate}:");
        string value2 = secondExpression.GenCode();
        Compiler.EmitCode($"br label %{labelHadToDoFullCalc}"); // those "redundant" labels are to make sure phi node predecessors/control paths are covered properly
        Compiler.EmitCode($"{labelHadToDoFullCalc}:");
        Compiler.EmitCode($"br label %{labelEnd}");
        Compiler.EmitCode($"{labelEnd}:");
        string finalReg = Compiler.GetNextRegisterName();
        Compiler.EmitCode($"{finalReg} = phi i1 [ false, %{labelStart} ], [ {value2}, %{labelHadToDoFullCalc} ]");
        return finalReg;
    }
}

class EqualsOperation : BinaryOperation
{
    public EqualsOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        firstExpression.CheckType();
        secondExpression.CheckType();
        if (!((firstExpression.typename == "i32" && secondExpression.typename == "i32")
            || (firstExpression.typename == "double" && secondExpression.typename == "i32")
            || (firstExpression.typename == "i32" && secondExpression.typename == "double")
            || (firstExpression.typename == "double" && secondExpression.typename == "double")
            || (firstExpression.typename == "i1" && secondExpression.typename == "i1")))
        {
            Compiler.HandleSemanticError(line, "cannot perform '==' on: "
                + Compiler.DisplayType(firstExpression.typename) + ", " + Compiler.DisplayType(secondExpression.typename));
        }
        typename = "i1";
        return typename;
    }

    public override string GenCode()
    {
        string val1 = firstExpression.GenCode();
        string val2 = secondExpression.GenCode();
        if ((firstExpression.typename == "i32" && secondExpression.typename == "i32")
            || (firstExpression.typename == "i1" && secondExpression.typename == "i1"))
        {
            string reg = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg} = icmp eq {firstExpression.typename} {val1}, {val2}");
            return reg;
        }
        else if (firstExpression.typename == "double" && secondExpression.typename == "i32")
        {
            string reg1 = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg1} = sitofp i32 {val2} to double");
            string reg2 = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg2} = fcmp oeq double {val1}, {reg1}");
            return reg2;
        }
        else if (firstExpression.typename == "i32" && secondExpression.typename == "double")
        {
            string reg1 = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg1} = sitofp i32 {val1} to double");
            string reg2 = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg2} = fcmp oeq double {reg1}, {val2}");
            return reg2;
        }
        else
        {
            string reg = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg} = fcmp oeq double {val1}, {val2}");
            return reg;
        }
    }
}

class NotEqualsOperation : BinaryOperation
{
    public NotEqualsOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        firstExpression.CheckType();
        secondExpression.CheckType();
        if (!((firstExpression.typename == "i32" && secondExpression.typename == "i32")
            || (firstExpression.typename == "double" && secondExpression.typename == "i32")
            || (firstExpression.typename == "i32" && secondExpression.typename == "double")
            || (firstExpression.typename == "double" && secondExpression.typename == "double")
            || (firstExpression.typename == "i1" && secondExpression.typename == "i1")))
        {
            Compiler.HandleSemanticError(line, "cannot perform '!=' on: "
                + Compiler.DisplayType(firstExpression.typename) + ", " + Compiler.DisplayType(secondExpression.typename));
        }
        typename = "i1";
        return typename;
    }

    public override string GenCode()
    {
        string val1 = firstExpression.GenCode();
        string val2 = secondExpression.GenCode();
        if ((firstExpression.typename == "i32" && secondExpression.typename == "i32")
            || (firstExpression.typename == "i1" && secondExpression.typename == "i1"))
        {
            string reg = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg} = icmp ne {firstExpression.typename} {val1}, {val2}");
            return reg;
        }
        else if (firstExpression.typename == "double" && secondExpression.typename == "i32")
        {
            string reg1 = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg1} = sitofp i32 {val2} to double");
            string reg2 = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg2} = fcmp one double {val1}, {reg1}");
            return reg2;
        }
        else if (firstExpression.typename == "i32" && secondExpression.typename == "double")
        {
            string reg1 = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg1} = sitofp i32 {val1} to double");
            string reg2 = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg2} = fcmp one double {reg1}, {val2}");
            return reg2;
        }
        else
        {
            string reg = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg} = fcmp one double {val1}, {val2}");
            return reg;
        }
    }
}

class GreaterThanOperation : BinaryOperation
{
    public GreaterThanOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        firstExpression.CheckType();
        secondExpression.CheckType();
        if (!((firstExpression.typename == "i32" && secondExpression.typename == "i32")
            || (firstExpression.typename == "double" && secondExpression.typename == "i32")
            || (firstExpression.typename == "i32" && secondExpression.typename == "double")
            || (firstExpression.typename == "double" && secondExpression.typename == "double")))
        {
            Compiler.HandleSemanticError(line, "cannot perform '>' on: "
                + Compiler.DisplayType(firstExpression.typename) + ", " + Compiler.DisplayType(secondExpression.typename));
        }
        typename = "i1";
        return typename;
    }

    public override string GenCode()
    {
        string val1 = firstExpression.GenCode();
        string val2 = secondExpression.GenCode();
        if (firstExpression.typename == "i32" && secondExpression.typename == "i32")
        {
            string reg = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg} = icmp sgt {firstExpression.typename} {val1}, {val2}");
            return reg;
        }
        else if (firstExpression.typename == "double" && secondExpression.typename == "i32")
        {
            string reg1 = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg1} = sitofp i32 {val2} to double");
            string reg2 = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg2} = fcmp ogt double {val1}, {reg1}");
            return reg2;
        }
        else if (firstExpression.typename == "i32" && secondExpression.typename == "double")
        {
            string reg1 = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg1} = sitofp i32 {val1} to double");
            string reg2 = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg2} = fcmp ogt double {reg1}, {val2}");
            return reg2;
        }
        else
        {
            string reg = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg} = fcmp ogt double {val1}, {val2}");
            return reg;
        }
    }
}

class GreaterOrEqualOperation : BinaryOperation
{
    public GreaterOrEqualOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        firstExpression.CheckType();
        secondExpression.CheckType();
        if (!((firstExpression.typename == "i32" && secondExpression.typename == "i32")
            || (firstExpression.typename == "double" && secondExpression.typename == "i32")
            || (firstExpression.typename == "i32" && secondExpression.typename == "double")
            || (firstExpression.typename == "double" && secondExpression.typename == "double")))
        {
            Compiler.HandleSemanticError(line, "cannot perform '>=' on: "
                + Compiler.DisplayType(firstExpression.typename) + ", " + Compiler.DisplayType(secondExpression.typename));
        }
        typename = "i1";
        return typename;
    }

    public override string GenCode()
    {
        string val1 = firstExpression.GenCode();
        string val2 = secondExpression.GenCode();
        if (firstExpression.typename == "i32" && secondExpression.typename == "i32")
        {
            string reg = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg} = icmp sge {firstExpression.typename} {val1}, {val2}");
            return reg;
        }
        else if (firstExpression.typename == "double" && secondExpression.typename == "i32")
        {
            string reg1 = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg1} = sitofp i32 {val2} to double");
            string reg2 = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg2} = fcmp oge double {val1}, {reg1}");
            return reg2;
        }
        else if (firstExpression.typename == "i32" && secondExpression.typename == "double")
        {
            string reg1 = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg1} = sitofp i32 {val1} to double");
            string reg2 = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg2} = fcmp oge double {reg1}, {val2}");
            return reg2;
        }
        else
        {
            string reg = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg} = fcmp oge double {val1}, {val2}");
            return reg;
        }
    }
}

class LessThanOperation : BinaryOperation
{
    public LessThanOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        firstExpression.CheckType();
        secondExpression.CheckType();
        if (!((firstExpression.typename == "i32" && secondExpression.typename == "i32")
            || (firstExpression.typename == "double" && secondExpression.typename == "i32")
            || (firstExpression.typename == "i32" && secondExpression.typename == "double")
            || (firstExpression.typename == "double" && secondExpression.typename == "double")))
        {
            Compiler.HandleSemanticError(line, "cannot perform '<' on: "
                + Compiler.DisplayType(firstExpression.typename) + ", " + Compiler.DisplayType(secondExpression.typename));
        }
        typename = "i1";
        return typename;
    }

    public override string GenCode()
    {
        string val1 = firstExpression.GenCode();
        string val2 = secondExpression.GenCode();
        if (firstExpression.typename == "i32" && secondExpression.typename == "i32")
        {
            string reg = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg} = icmp slt {firstExpression.typename} {val1}, {val2}");
            return reg;
        }
        else if (firstExpression.typename == "double" && secondExpression.typename == "i32")
        {
            string reg1 = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg1} = sitofp i32 {val2} to double");
            string reg2 = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg2} = fcmp olt double {val1}, {reg1}");
            return reg2;
        }
        else if (firstExpression.typename == "i32" && secondExpression.typename == "double")
        {
            string reg1 = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg1} = sitofp i32 {val1} to double");
            string reg2 = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg2} = fcmp olt double {reg1}, {val2}");
            return reg2;
        }
        else
        {
            string reg = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg} = fcmp olt double {val1}, {val2}");
            return reg;
        }
    }
}

class LessOrEqualOperation : BinaryOperation
{
    public LessOrEqualOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        firstExpression.CheckType();
        secondExpression.CheckType();
        if (!((firstExpression.typename == "i32" && secondExpression.typename == "i32")
            || (firstExpression.typename == "double" && secondExpression.typename == "i32")
            || (firstExpression.typename == "i32" && secondExpression.typename == "double")
            || (firstExpression.typename == "double" && secondExpression.typename == "double")))
        {
            Compiler.HandleSemanticError(line, "cannot perform '<=' on: "
                + Compiler.DisplayType(firstExpression.typename) + ", " + Compiler.DisplayType(secondExpression.typename));
        }
        typename = "i1";
        return typename;
    }

    public override string GenCode()
    {
        string val1 = firstExpression.GenCode();
        string val2 = secondExpression.GenCode();
        if (firstExpression.typename == "i32" && secondExpression.typename == "i32")
        {
            string reg = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg} = icmp sle {firstExpression.typename} {val1}, {val2}");
            return reg;
        }
        else if (firstExpression.typename == "double" && secondExpression.typename == "i32")
        {
            string reg1 = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg1} = sitofp i32 {val2} to double");
            string reg2 = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg2} = fcmp ole double {val1}, {reg1}");
            return reg2;
        }
        else if (firstExpression.typename == "i32" && secondExpression.typename == "double")
        {
            string reg1 = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg1} = sitofp i32 {val1} to double");
            string reg2 = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg2} = fcmp ole double {reg1}, {val2}");
            return reg2;
        }
        else
        {
            string reg = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg} = fcmp ole double {val1}, {val2}");
            return reg;
        }
    }
}

class AdditionOperation : BinaryOperation
{
    public AdditionOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        firstExpression.CheckType();
        secondExpression.CheckType();
        if (firstExpression.typename == "i32" && secondExpression.typename == "i32")
        {
            typename = "i32";
        }
        else if ((firstExpression.typename != "i32" && firstExpression.typename != "double") ||
            (secondExpression.typename != "i32" && secondExpression.typename != "double"))
        {
            Compiler.HandleSemanticError(line, "cannot perform addition for: " + Compiler.DisplayType(firstExpression.typename)
                + ", " + Compiler.DisplayType(secondExpression.typename));
        }
        else
        {
            typename = "double";
        }
        return typename;
    }

    public override string GenCode()
    {
        string val1 = firstExpression.GenCode();
        string val2 = secondExpression.GenCode();
        string reg = Compiler.GetNextRegisterName();
        if (typename == "i32")
        {
            Compiler.EmitCode($"{reg} = add i32 {val1}, {val2}");
        }
        else
        {
            if (firstExpression.typename == "i32")
            {
                string reg1 = Compiler.GetNextRegisterName();
                Compiler.EmitCode($"{reg1} = sitofp i32 {val1} to double");
                val1 = reg1;
            }
            else if (secondExpression.typename == "i32")
            {
                string reg2 = Compiler.GetNextRegisterName();
                Compiler.EmitCode($"{reg2} = sitofp i32 {val2} to double");
                val2 = reg2;
            }
            Compiler.EmitCode($"{reg} = fadd double {val1}, {val2}");
        }
        return reg;
    }
}

class SubstractionOperation : BinaryOperation
{
    public SubstractionOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        firstExpression.CheckType();
        secondExpression.CheckType();
        if (firstExpression.typename == "i32" && secondExpression.typename == "i32")
        {
            typename = "i32";
        }
        else if ((firstExpression.typename != "i32" && firstExpression.typename != "double") ||
            (secondExpression.typename != "i32" && secondExpression.typename != "double"))
        {
            Compiler.HandleSemanticError(line, "cannot perform substraction for: " + Compiler.DisplayType(firstExpression.typename)
                + ", " + Compiler.DisplayType(secondExpression.typename));
        }
        else
        {
            typename = "double";
        }
        return typename;
    }

    public override string GenCode()
    {
        string val1 = firstExpression.GenCode();
        string val2 = secondExpression.GenCode();
        string reg = Compiler.GetNextRegisterName();
        if (typename == "i32")
        {
            Compiler.EmitCode($"{reg} = sub i32 {val1}, {val2}");
        }
        else
        {
            if (firstExpression.typename == "i32")
            {
                string reg1 = Compiler.GetNextRegisterName();
                Compiler.EmitCode($"{reg1} = sitofp i32 {val1} to double");
                val1 = reg1;
            }
            else if (secondExpression.typename == "i32")
            {
                string reg2 = Compiler.GetNextRegisterName();
                Compiler.EmitCode($"{reg2} = sitofp i32 {val2} to double");
                val2 = reg2;
            }
            Compiler.EmitCode($"{reg} = fsub double {val1}, {val2}");
        }
        return reg;
    }
}

class MultiplicationOperation : BinaryOperation
{
    public MultiplicationOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        firstExpression.CheckType();
        secondExpression.CheckType();
        if (firstExpression.typename == "i32" && secondExpression.typename == "i32")
        {
            typename = "i32";
        }
        else if ((firstExpression.typename != "i32" && firstExpression.typename != "double") ||
            (secondExpression.typename != "i32" && secondExpression.typename != "double"))
        {
            Compiler.HandleSemanticError(line, "cannot perform multiplication for: " + Compiler.DisplayType(firstExpression.typename)
                + ", " + Compiler.DisplayType(secondExpression.typename));
        }
        else
        {
            typename = "double";
        }
        return typename;
    }

    public override string GenCode()
    {
        string val1 = firstExpression.GenCode();
        string val2 = secondExpression.GenCode();
        string reg = Compiler.GetNextRegisterName();
        if (typename == "i32")
        {
            Compiler.EmitCode($"{reg} = mul i32 {val1}, {val2}");
        }
        else
        {
            if (firstExpression.typename == "i32")
            {
                string reg1 = Compiler.GetNextRegisterName();
                Compiler.EmitCode($"{reg1} = sitofp i32 {val1} to double");
                val1 = reg1;
            }
            else if (secondExpression.typename == "i32")
            {
                string reg2 = Compiler.GetNextRegisterName();
                Compiler.EmitCode($"{reg2} = sitofp i32 {val2} to double");
                val2 = reg2;
            }
            Compiler.EmitCode($"{reg} = fmul double {val1}, {val2}");
        }
        return reg;
    }
}

class DivisionOperation : BinaryOperation
{
    public DivisionOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        firstExpression.CheckType();
        secondExpression.CheckType();
        if (firstExpression.typename == "i32" && secondExpression.typename == "i32")
        {
            typename = "i32";
        }
        else if ((firstExpression.typename != "i32" && firstExpression.typename != "double") ||
            (secondExpression.typename != "i32" && secondExpression.typename != "double"))
        {
            Compiler.HandleSemanticError(line, "cannot perform division for: " + Compiler.DisplayType(firstExpression.typename)
                + ", " + Compiler.DisplayType(secondExpression.typename));
        }
        else
        {
            typename = "double";
        }
        return typename;
    }

    public override string GenCode()
    {
        string val1 = firstExpression.GenCode();
        string val2 = secondExpression.GenCode();
        string reg = Compiler.GetNextRegisterName();
        if (typename == "i32")
        {
            Compiler.EmitCode($"{reg} = sdiv i32 {val1}, {val2}");
        }
        else
        {
            if (firstExpression.typename == "i32")
            {
                string reg1 = Compiler.GetNextRegisterName();
                Compiler.EmitCode($"{reg1} = sitofp i32 {val1} to double");
                val1 = reg1;
            }
            else if (secondExpression.typename == "i32")
            {
                string reg2 = Compiler.GetNextRegisterName();
                Compiler.EmitCode($"{reg2} = sitofp i32 {val2} to double");
                val2 = reg2;
            }
            Compiler.EmitCode($"{reg} = fdiv double {val1}, {val2}");
        }
        return reg;
    }
}

class BitwiseSumOperation : BinaryOperation
{
    public BitwiseSumOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        firstExpression.CheckType();
        secondExpression.CheckType();
        if (firstExpression.typename == "i32" && secondExpression.typename == "i32")
        {
            typename = "i32";
        }
        else
        {
            Compiler.HandleSemanticError(line, "cannot perform bitwise sum on: " + Compiler.DisplayType(firstExpression.typename)
                + ", " + Compiler.DisplayType(secondExpression.typename));
        }
        return typename;
    }

    public override string GenCode()
    {
        string val1 = firstExpression.GenCode();
        string val2 = secondExpression.GenCode();
        string reg = Compiler.GetNextRegisterName();
        Compiler.EmitCode($"{reg} = or i32 {val1}, {val2}");
        return reg;
    }
}

class BitwiseProductOperation : BinaryOperation
{
    public BitwiseProductOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        firstExpression.CheckType();
        secondExpression.CheckType();
        if (firstExpression.typename == "i32" && secondExpression.typename == "i32")
        {
            typename = "i32";
        }
        else
        {
            Compiler.HandleSemanticError(line, "cannot perform bitwise product on: " + Compiler.DisplayType(firstExpression.typename)
                + ", " + Compiler.DisplayType(secondExpression.typename));
        }
        return typename;
    }

    public override string GenCode()
    {
        string val1 = firstExpression.GenCode();
        string val2 = secondExpression.GenCode();
        string reg = Compiler.GetNextRegisterName();
        Compiler.EmitCode($"{reg} = and i32 {val1}, {val2}");
        return reg;
    }
}

class BlockInstruction : SyntaxTree
{
    private List<SyntaxTree> instructions;

    public BlockInstruction(List<SyntaxTree> instrList)
    {
        instructions = instrList;
    }

    public override string CheckType()
    {
        foreach (var instr in instructions)
        {
            instr.CheckType();
        }
        return null;
    }

    public override string GenCode()
    {
        foreach (var instr in instructions)
        {
            instr.GenCode();
        }
        return null;
    }
}

class ReturnInstruction : SyntaxTree
{
    public override string CheckType()
    {
        return null;
    }

    public override string GenCode()
    {
        Compiler.EmitCode("ret i32 0");
        return null;
    }
}

class LoopInstruction : SyntaxTree
{
    private SyntaxTree condition;
    private SyntaxTree instruction;

    public LoopInstruction(SyntaxTree cond, SyntaxTree instr)
    {
        condition = cond;
        instruction = instr;
    }

    public override string CheckType()
    {
        if (condition.CheckType() != "i1")
        {
            Compiler.HandleSemanticError(condition.line, "condition for 'while' instruction has to be a bool instead of: " + Compiler.DisplayType(condition.typename));
        }
        instruction.CheckType();
        return null;
    }

    public override string GenCode()
    {
        string labelStart = Compiler.GetNextLabelName();
        string labelInstruction = Compiler.GetNextLabelName();
        string labelEnd = Compiler.GetNextLabelName();
        Compiler.EmitCode($"br label %{labelStart}");
        Compiler.EmitCode($"{labelStart}:");
        string conditionResult = condition.GenCode();
        Compiler.EmitCode($"br i1 {conditionResult}, label %{labelInstruction}, label %{labelEnd}");
        Compiler.EmitCode($"{labelInstruction}:");
        instruction.GenCode();
        Compiler.EmitCode($"br label %{labelStart}");
        Compiler.EmitCode($"{labelEnd}:");
        return null;
    }
}

class ConditionalInstruction : SyntaxTree
{
    private SyntaxTree condition;
    private SyntaxTree ifInstruction;
    private SyntaxTree elseInstruction;

    public ConditionalInstruction(SyntaxTree cond, SyntaxTree ifInstr, SyntaxTree elseInstr = null)
    {
        condition = cond;
        ifInstruction = ifInstr;
        elseInstruction = elseInstr;
    }

    public override string CheckType()
    {
        if (condition.CheckType() != "i1")
        {
            Compiler.HandleSemanticError(condition.line, "condition for 'if' instruction has to be a bool instead of: " + Compiler.DisplayType(condition.typename));
        }
        ifInstruction?.CheckType();
        if (elseInstruction != null)
            elseInstruction.CheckType();
        return null;
    }

    public override string GenCode()
    {
        string conditionResult = condition.GenCode();
        string labelTrue = Compiler.GetNextLabelName();
        string labelFalse = Compiler.GetNextLabelName();
        string labelEnd = Compiler.GetNextLabelName();
        Compiler.EmitCode($"br i1 {conditionResult}, label %{labelTrue}, label %{labelFalse}");
        Compiler.EmitCode($"{labelTrue}:");
        ifInstruction.GenCode();
        Compiler.EmitCode($"br label %{labelEnd}");
        Compiler.EmitCode($"{labelFalse}:");
        if (elseInstruction != null)
        {
            elseInstruction.GenCode();
        }
        Compiler.EmitCode($"br label %{labelEnd}");
        Compiler.EmitCode($"{labelEnd}:");
        return null;
    }
}

class HexWriteInstruction : SyntaxTree
{
    private SyntaxTree expression;

    public HexWriteInstruction(SyntaxTree exp)
    {
        expression = exp;
        line = Compiler.CurrentLine();
    }

    public override string CheckType()
    {
        expression.CheckType();
        if (expression.typename == "i32")
        {
            typename = expression.typename;
        }
        else
        {
            Compiler.HandleSemanticError(line, "cannot perform hex write instruction on: " + Compiler.DisplayType(expression.typename));
        }
        return typename;
    }

    public override string GenCode()
    {
        string value = expression.GenCode();
        Compiler.EmitCode($"call i32 (i8*, ...) @printf(i8* bitcast ([5 x i8]* @hex_int_print to i8*), i32 {value})");
        return null;
    }
}

class ReadInstruction : SyntaxTree
{
    private SyntaxTree identifier;

    public ReadInstruction(SyntaxTree ident)
    {
        identifier = ident;
        line = Compiler.CurrentLine();
    }

    public override string CheckType()
    {
        identifier.CheckType();
        if (identifier.typename == "i32" || identifier.typename == "double")
        {
            typename = identifier.typename;
        }
        else
        {
            Compiler.HandleSemanticError(line, "cannot perform read instruction on: " + Compiler.DisplayType(identifier.typename));
        }
        return typename;
    }

    public override string GenCode()
    {
        Identifier ident = identifier as Identifier;

        if (typename == "i32")
        {
            Compiler.EmitCode($"call i32 (i8*, ...) @scanf_s(i8* bitcast ([3 x i8]* @int_print to i8*), {typename}* %var_{ident.name})");
        }
        else if (typename == "double")
        {
            Compiler.EmitCode($"call i32 (i8*, ...) @scanf_s(i8* bitcast ([4 x i8]* @double_print to i8*), {typename}* %var_{ident.name})");
        }
        else
        {
            throw new Exception($"Invalid typename: {typename} provided");
        }

        return null;
    }
}

class HexReadInstruction : SyntaxTree
{
    private SyntaxTree identifier;

    public HexReadInstruction(SyntaxTree ident)
    {
        identifier = ident;
        line = Compiler.CurrentLine();
    }

    public override string CheckType()
    {
        identifier.CheckType();
        if (identifier.typename == "i32")
        {
            typename = identifier.typename;
        }
        else
        {
            Compiler.HandleSemanticError(line, "cannot perform hex read instruction on: " + Compiler.DisplayType(identifier.typename));
        }
        return typename;
    }

    public override string GenCode()
    {
        Identifier ident = identifier as Identifier;

        if (typename == "i32")
        {
            Compiler.EmitCode($"call i32 (i8*, ...) @scanf_s(i8* bitcast ([3 x i8]* @hex_int_scan to i8*), {typename}* %var_{ident.name})");
        }

        return null;
    }
}

class StringWriteInstruction : SyntaxTree
{
    private StringInfo stringInfo;

    public StringWriteInstruction(StringInfo info)
    {
        stringInfo = info;
    }

    public override string CheckType()
    {
        return null;
    }

    public override string GenCode()
    {
        Compiler.EmitCode($"call i32 (i8*, ...) @printf(i8* bitcast ([{stringInfo.stringLength + 1} x i8]* @{stringInfo.stringVarName} to i8*))");
        return null;
    }
}

namespace GardensPoint
{
    public sealed partial class Scanner
    {
        public override void yyerror(string message, params object[] args)
        {
            Compiler.HandleSyntaxError();
        }
    }
}
