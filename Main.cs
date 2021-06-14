using System;
using System.IO;
using System.Collections.Generic;
using GardensPoint;
using System.Text;

public class Compiler
{
    public static int errors = 0;

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
            return 1; // TODO: make sure it can be left like that
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
        parser.Parse();
        source.Close();

        syntaxTree.CheckType();

        if (errors > 0)
        {
            Console.WriteLine($"\n {errors} errors detected\n");
        }
        else
        {
            writer = new StreamWriter(file + ".ll");
            GenCode();
            writer.Close();
            Console.WriteLine("Compilation successful!");
        }

        return errors == 0 ? 0 : 3;
    }

    private static void GenCode()
    {
        EmitCode("@int_print = constant [3 x i8] c\"%d\\00\"");
        EmitCode("@hex_int_print = constant [5 x i8] c\"0X%X\\00\"");
        EmitCode("@double_print = constant [4 x i8] c\"%lf\\00\"");
        EmitCode("@bool_print_true = constant [5 x i8] c\"True\\00\"");
        EmitCode("@bool_print_false = constant [6 x i8] c\"False\\00\"");
        foreach (StringInfo info in stringInfos)
        {
            EmitCode($"@{info.stringVarName} = constant [{info.stringLength + 1} x i8] c\"{info.stringValue}\\00\"");
        }
        EmitCode("declare i32 @printf(i8*, ...)");
        EmitCode();
        EmitCode("define i32 @main()");
        EmitCode("{");
        EmitCode($"{GetCurrentLabelName()}:");

        Console.WriteLine(syntaxTree);

        syntaxTree.GenCode();
        EmitCode("}");
    }

    public static void EmitCode(string instr = null)
    {
        writer.WriteLine(instr);
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
            if (charArray[i] == '\\') // TODO: handle the impossible case when backslash as last char
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
            throw new Exception($"Declaration of type: {type} not allowed"); // TODO: make sure it can be left if not needed
        }
    }

    public override string CheckType()
    {
        throw new NotImplementedException();
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

    public override string ToString()
    {
        return $"{base.ToString()} extends SyntaxTree: [typename: {typename}]";
    }
}

class Program : SyntaxTree
{
    private List<SyntaxTree> instructions;

    public Program(List<SyntaxTree> declList, List<SyntaxTree> instrList)
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
            table.Add(decl.identifier, decl);
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
    }

    public override string CheckType()
    {
        typename = Compiler.symbolTable[name].typename;
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
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.0###############}", value); // TODO: make sure this invariant culture stuff is correct
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
        expression.CheckType();
        return null;
    }

    public override string GenCode()
    {
        string value = expression.GenCode();

        if (expression.typename == "i32")
        {
            Compiler.EmitCode($"call i32 (i8*, ...) @printf(i8* bitcast ([3 x i8]* @int_print to i8*), i32 {value})");
        }
        else if (expression.typename == "double")
        {
            Compiler.EmitCode($"call i32 (i8*, ...) @printf(i8* bitcast ([4 x i8]* @double_print to i8*), double {value})");
        }
        else if (expression.typename == "i1") // TODO: fix bool printing, probably a branch condition True/False
        {
            string reg = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{reg} = select i1 {value}, i8* bitcast ([5 x i8]* @bool_print_true to i8*), i8* bitcast ([6 x i8]* @bool_print_false to i8*)");
            Compiler.EmitCode($"call i32 (i8*, ...) @printf(i8* {reg})");
        }
        else
        {
            throw new Exception($"Invalid expression typename: {expression.typename} provided");
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
}

class UnaryMinusOperation : UnaryOperation
{
    public UnaryMinusOperation(SyntaxTree exp) : base(exp) { }

    public override string CheckType()
    {
        typename = expression.CheckType();
        return typename;
    }

    public override string GenCode()
    {
        string value = expression.GenCode();
        string register = Compiler.GetNextRegisterName();
        Compiler.EmitCode($"{register} = sub {expression.typename} 0, {value}");
        return register;
    }
}

class BitwiseNegateOperation : UnaryOperation
{
    public BitwiseNegateOperation(SyntaxTree exp) : base(exp) { }

    public override string CheckType()
    {
        typename = expression.CheckType();
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
        typename = expression.CheckType();
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
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        throw new NotImplementedException();
    }
}

class ConvertToDoubleOperation : UnaryOperation
{
    public ConvertToDoubleOperation(SyntaxTree exp) : base(exp) { }

    public override string CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        throw new NotImplementedException();
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

    public override string CheckType() // TODO: make sure you can repeat this method everywhere like that
    {
        typename = firstExpression.CheckType();
        secondExpression.CheckType(); // TODO: calculate typename for AssignOp
        return firstExpression.typename;
    }

    public override string GenCode()
    {
        string secondExpValue = secondExpression.GenCode();
        string secondExpTypename = secondExpression.typename;
        if (secondExpValue.StartsWith("%var_"))
        {
            string varName = secondExpValue;
            secondExpValue = Compiler.GetNextRegisterName();
            Compiler.EmitCode($"{secondExpValue} = load {secondExpTypename}, {secondExpTypename}* {varName}");
        }
        Identifier ident = firstExpression as Identifier;
        string firstExpValue = "%var_" + ident.name;
        string firstExpTypename = firstExpression.typename;
        Compiler.EmitCode($"store {secondExpTypename} {secondExpValue}, {firstExpTypename}* {firstExpValue}");
        return firstExpValue;
    }
}

class LogicalSumOperation : BinaryOperation
{
    public LogicalSumOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        firstExpression.CheckType();
        secondExpression.CheckType();
        typename = "i1";
        return typename;
    }

    public override string GenCode() // TODO: nie jestem pewien, czy to są obliczenia skrócone (RACZEJ NIE SĄ), bo jest secondexpr.GenCode() -- upewnij się.
    {
        string value1 = firstExpression.GenCode();
        string value2 = secondExpression.GenCode();
        string curLabel = Compiler.GetCurrentLabelName();
        string label1 = Compiler.GetNextLabelName();
        string label2 = Compiler.GetNextLabelName();
        string reg1 = Compiler.GetNextRegisterName();
        Compiler.EmitCode($"{reg1} = icmp ne i1 {value1}, 0");
        Compiler.EmitCode($"br i1 {reg1}, label %{label2}, label %{label1}");
        Compiler.EmitCode($"{label1}:");
        string reg2 = Compiler.GetNextRegisterName();
        Compiler.EmitCode($"{reg2} = icmp ne i1 {value2}, 0");
        Compiler.EmitCode($"br label %{label2}");
        Compiler.EmitCode($"{label2}:");
        string reg3 = Compiler.GetNextRegisterName();
        Compiler.EmitCode($"{reg3} = phi i1 [ true, %{curLabel} ], [ {reg2}, %{label1} ]");
        return reg3;
    }
}

class LogicalProductOperation : BinaryOperation
{
    public LogicalProductOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        firstExpression.CheckType();
        secondExpression.CheckType();
        typename = "i1";
        return typename;
    }

    public override string GenCode()
    {
        string value1 = firstExpression.GenCode();
        string value2 = secondExpression.GenCode();
        string curLabel = Compiler.GetCurrentLabelName();
        string label1 = Compiler.GetNextLabelName();
        string label2 = Compiler.GetNextLabelName();
        string reg1 = Compiler.GetNextRegisterName();
        Compiler.EmitCode($"{reg1} = icmp ne i1 {value1}, 0");
        Compiler.EmitCode($"br i1 {reg1}, label %{label1}, label %{label2}");
        Compiler.EmitCode($"{label1}:");
        string reg2 = Compiler.GetNextRegisterName();
        Compiler.EmitCode($"{reg2} = icmp ne i1 {value2}, 0");
        Compiler.EmitCode($"br label %{label2}");
        Compiler.EmitCode($"{label2}:");
        string reg3 = Compiler.GetNextRegisterName();
        Compiler.EmitCode($"{reg3} = phi i1 [ false, %{curLabel} ], [ {reg2}, %{label1} ]");
        return reg3;
    }
}

class EqualsOperation : BinaryOperation
{
    public EqualsOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType() // TODO: implement full proper semantic type checking in the entire project
    {
        firstExpression.CheckType();
        secondExpression.CheckType();
        typename = "i1";
        return typename;
    }

    public override string GenCode()
    {
        string val1 = firstExpression.GenCode();
        string val2 = secondExpression.GenCode();
        string reg = Compiler.GetNextRegisterName();
        Compiler.EmitCode($"{reg} = icmp eq {firstExpression.typename} {val1}, {val2}");
        return reg;
    }
}

class NotEqualsOperation : BinaryOperation
{
    public NotEqualsOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        firstExpression.CheckType();
        secondExpression.CheckType();
        typename = "i1";
        return typename;
    }

    public override string GenCode()
    {
        string val1 = firstExpression.GenCode();
        string val2 = secondExpression.GenCode();
        string reg = Compiler.GetNextRegisterName();
        Compiler.EmitCode($"{reg} = icmp ne {firstExpression.typename} {val1}, {val2}");
        return reg;
    }
}

class GreaterThanOperation : BinaryOperation
{
    public GreaterThanOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        firstExpression.CheckType();
        secondExpression.CheckType();
        typename = "i1";
        return typename;
    }

    public override string GenCode()
    {
        string val1 = firstExpression.GenCode();
        string val2 = secondExpression.GenCode();
        string reg = Compiler.GetNextRegisterName();
        Compiler.EmitCode($"{reg} = icmp sgt {firstExpression.typename} {val1}, {val2}");
        return reg;
    }
}

class GreaterOrEqualOperation : BinaryOperation
{
    public GreaterOrEqualOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        firstExpression.CheckType();
        secondExpression.CheckType();
        typename = "i1";
        return typename;
    }

    public override string GenCode()
    {
        string val1 = firstExpression.GenCode();
        string val2 = secondExpression.GenCode();
        string reg = Compiler.GetNextRegisterName();
        Compiler.EmitCode($"{reg} = icmp sge {firstExpression.typename} {val1}, {val2}");
        return reg;
    }
}

class LessThanOperation : BinaryOperation
{
    public LessThanOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        firstExpression.CheckType();
        secondExpression.CheckType();
        typename = "i1";
        return typename;
    }

    public override string GenCode()
    {
        string val1 = firstExpression.GenCode();
        string val2 = secondExpression.GenCode();
        string reg = Compiler.GetNextRegisterName();
        Compiler.EmitCode($"{reg} = icmp slt {firstExpression.typename} {val1}, {val2}");
        return reg;
    }
}

class LessOrEqualOperation : BinaryOperation
{
    public LessOrEqualOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        firstExpression.CheckType();
        secondExpression.CheckType();
        typename = "i1";
        return typename;
    }

    public override string GenCode()
    {
        string val1 = firstExpression.GenCode();
        string val2 = secondExpression.GenCode();
        string reg = Compiler.GetNextRegisterName();
        Compiler.EmitCode($"{reg} = icmp sle {firstExpression.typename} {val1}, {val2}");
        return reg;
    }
}

class AdditionOperation : BinaryOperation
{
    public AdditionOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        typename = firstExpression.CheckType();
        secondExpression.CheckType();
        return typename;
    }

    public override string GenCode()
    {
        string val1 = firstExpression.GenCode();
        string val2 = secondExpression.GenCode();
        string reg = Compiler.GetNextRegisterName();
        Compiler.EmitCode($"{reg} = add {firstExpression.typename} {val1}, {val2}");
        return reg;
    }
}

class SubstractionOperation : BinaryOperation
{
    public SubstractionOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        typename = firstExpression.CheckType();
        secondExpression.CheckType();
        return typename;
    }

    public override string GenCode()
    {
        string val1 = firstExpression.GenCode();
        string val2 = secondExpression.GenCode();
        string reg = Compiler.GetNextRegisterName();
        Compiler.EmitCode($"{reg} = sub {firstExpression.typename} {val1}, {val2}");
        return reg;
    }
}

class MultiplicationOperation : BinaryOperation
{
    public MultiplicationOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        typename = firstExpression.CheckType();
        secondExpression.CheckType();
        return typename;
    }

    public override string GenCode()
    {
        string val1 = firstExpression.GenCode();
        string val2 = secondExpression.GenCode();
        string reg = Compiler.GetNextRegisterName();
        Compiler.EmitCode($"{reg} = mul {firstExpression.typename} {val1}, {val2}");
        return reg;
    }
}

class DivisionOperation : BinaryOperation
{
    public DivisionOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        typename = firstExpression.CheckType();
        secondExpression.CheckType();
        return typename;
    }

    public override string GenCode() // TODO: Fill in with fdiv usage for doubles while type checking implementation
    {
        string val1 = firstExpression.GenCode();
        string val2 = secondExpression.GenCode();
        string reg = Compiler.GetNextRegisterName();
        Compiler.EmitCode($"{reg} = sdiv {firstExpression.typename} {val1}, {val2}");
        return reg;
    }
}

class BitwiseSumOperation : BinaryOperation
{
    public BitwiseSumOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType() // TODO: only int
    {
        typename = "i32";
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

    public override string CheckType() // TODO: only int
    {
        typename = "i32";
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
        condition.CheckType();
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
        condition.CheckType();
        ifInstruction.CheckType();
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
    }

    public override string CheckType()
    {
        typename = expression.CheckType();
        return typename;
    }

    public override string GenCode()
    {
        string value = expression.GenCode();
        Compiler.EmitCode($"call i32 (i8*, ...) @printf(i8* bitcast ([5 x i8]* @hex_int_print to i8*), i32 {value})");
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
