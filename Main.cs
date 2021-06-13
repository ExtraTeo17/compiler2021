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

    public static int Main(string[] args)
    {
        string file;
        FileStream source;
        Console.WriteLine("\nA Compiler for MINI language");

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
            Console.WriteLine(" compilation successful\n");
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

    public static string NextRegisterName()
    {
        return "register" + registerNameId++;
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
        Compiler.EmitCode($"%{identifier} = alloca {typename}");
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
        string registerName = Compiler.NextRegisterName();
        Compiler.EmitCode($"%{registerName} = load {typename}, {typename}* %{name}");
        return "%" + registerName;
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
        throw new NotImplementedException();
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
            if (value == "true")
                Compiler.EmitCode("call i32 (i8*, ...) @printf(i8* bitcast ([5 x i8]* @bool_print_true to i8*))");
            else if (value == "false")
                Compiler.EmitCode("call i32 (i8*, ...) @printf(i8* bitcast ([6 x i8]* @bool_print_false to i8*))");
            else
                throw new Exception($"Invalid boolean value: {value} provided");
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
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        throw new NotImplementedException();
    }
}

class BitwiseNegateOperation : UnaryOperation
{
    public BitwiseNegateOperation(SyntaxTree exp) : base(exp) { }

    public override string CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        throw new NotImplementedException();
    }
}

class LogicalNegateOperation : UnaryOperation
{
    public LogicalNegateOperation(SyntaxTree exp) : base(exp) { }

    public override string CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        throw new NotImplementedException();
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
        firstExpression.CheckType();
        secondExpression.CheckType(); // TODO: calculate typename for AssignOp
        return null;
    }

    public override string GenCode()
    {
        string secondExpValue = secondExpression.GenCode();
        string secondExpTypename = secondExpression.typename;
        Identifier ident = firstExpression as Identifier;
        string firstExpValue = ident.name;
        string firstExpTypename = firstExpression.typename;
        Compiler.EmitCode($"store {secondExpTypename} {secondExpValue}, {firstExpTypename}* %{firstExpValue}");
        return firstExpValue;
    }
}

class LogicalSumOperation : BinaryOperation
{
    public LogicalSumOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        throw new NotImplementedException();
    }
}

class LogicalProductOperation : BinaryOperation
{
    public LogicalProductOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        throw new NotImplementedException();
    }
}

class EqualsOperation : BinaryOperation
{
    public EqualsOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        throw new NotImplementedException();
    }
}

class NotEqualsOperation : BinaryOperation
{
    public NotEqualsOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        throw new NotImplementedException();
    }
}

class GreaterThanOperation : BinaryOperation
{
    public GreaterThanOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        throw new NotImplementedException();
    }
}

class GreaterOrEqualOperation : BinaryOperation
{
    public GreaterOrEqualOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        throw new NotImplementedException();
    }
}

class LessThanOperation : BinaryOperation
{
    public LessThanOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        throw new NotImplementedException();
    }
}

class LessOrEqualOperation : BinaryOperation
{
    public LessOrEqualOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        throw new NotImplementedException();
    }
}

class AdditionOperation : BinaryOperation
{
    public AdditionOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        throw new NotImplementedException();
    }
}

class SubstractionOperation : BinaryOperation
{
    public SubstractionOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        throw new NotImplementedException();
    }
}

class MultiplicationOperation : BinaryOperation
{
    public MultiplicationOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        throw new NotImplementedException();
    }
}

class DivisionOperation : BinaryOperation
{
    public DivisionOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        throw new NotImplementedException();
    }
}

class BitwiseSumOperation : BinaryOperation
{
    public BitwiseSumOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        throw new NotImplementedException();
    }
}

class BitwiseProductOperation : BinaryOperation
{
    public BitwiseProductOperation(SyntaxTree exp1, SyntaxTree exp2) : base(exp1, exp2) { }

    public override string CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        string value = expression.GenCode();
        Compiler.EmitCode($"call i32 (i8*, ...) @printf(i8* bitcast ([5 x i8]* @hex_int_print to i8*), i32 {value.ToString()})");
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
